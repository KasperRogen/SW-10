using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FailureDetection
{


    /// <summary>Handles failure detection requests, updating own router, and relaying or performing aliveness check
    /// <para>  </para>
    /// </summary>
    public async static void DetectFailure(INode myNode, DetectFailureRequest request)
    {
        //Update router, ensure we don't try to route through the bad connection
        myNode.Router.DeleteEdge(request.SourceID, request.NodeToCheck); //Should we do this already?

        myNode.State = request.isDead == null ? Node.NodeState.EXECUTING : Node.NodeState.PASSIVE;


        
        if (myNode.ID == request.DestinationID)
        {
            //If we don't have a live already, we assume the connection has been determined to be bad
            if(myNode.Router.NetworkMap[myNode.ID].Contains(request.NodeToCheck) == false)// TODO: Probably safer check here
            {
                Response response = new Response();
                response.DestinationID = request.SourceID;
                response.SourceID = myNode.ID;
                response.ResponseCode = Response.ResponseCodes.ERROR;

                uint? nextHop = myNode.Router.NextHop(myNode.ID, request.SourceID);

                myNode.CommsModule.Send(nextHop, response);
            } else
            {
                Request ping = new Request();
                ping.SourceID = myNode.ID;
                ping.DestinationID = request.NodeToCheck;
                ping.Command = Request.Commands.Ping;
                Response pingResponse = await myNode.CommsModule.SendAsync(ping.DestinationID, ping, 1000);

                Response requestResponse;
                if (pingResponse == null || pingResponse.ResponseCode == Response.ResponseCodes.ERROR)
                {
                    myNode.Router.DeleteEdge(myNode.ID, request.NodeToCheck);
                    requestResponse = new Response(myNode.ID, request.SourceID, Response.ResponseCodes.ERROR, request.MessageIdentifer);
                } else
                {
                    requestResponse = new Response(myNode.ID, request.SourceID, Response.ResponseCodes.ERROR, request.MessageIdentifer);
                }

                uint? nextResponseHop = myNode.Router.NextHop(myNode.ID, requestResponse.DestinationID);

                myNode.CommsModule.Send(nextResponseHop, requestResponse); //TODO: make the response contain the fact that the node IS dead, so other nodes can update
            }
        }
        

    }



    /// <summary>Should be used on the node when it detects a failure
    /// <para>Will initiate a failure detection operation, asking neighbours of failed node about aliveness</para>
    /// </summary>
    public async static void FailureDetected(INode myNode, uint? failedNode)
    {
        //Remove edge from router, ensuring it won't try to route through the failed node
        myNode.Router.DeleteEdge(myNode.ID, failedNode); //TODO: Should we do this already???

        //Get a immidiate neighbour to the failed node
        uint? neighbourID = myNode.Router.NetworkMap[failedNode][0]; //TODO: what if we are only neighbour? what if there are more? or a best?
        uint? nextHop = myNode.Router.NextHop(myNode.ID, neighbourID);

        DetectFailureRequest request = new DetectFailureRequest
        {
            Command = Request.Commands.DetectFailure,
            DestinationID = neighbourID,
            SourceID = myNode.ID,
            NodeToCheck = failedNode
        };

        Response response = await myNode.CommsModule.SendAsync(nextHop, request, 15000);

        if(response.ResponseCode == Response.ResponseCodes.ERROR)
        {
            TargetConstellationGenerator.instance.GenerateTargetConstellation();
            return;
        }

    }

}
