using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FailureDetection
{

    public async static void DetectFailure(INode myNode, DetectFailureRequest request)
    {
        myNode.Router.DeleteEdge(request.SourceID, request.NodeToCheck);

        myNode.State = request.isDead == null ? Node.NodeState.EXECUTING : Node.NodeState.PASSIVE;


        
        if (myNode.ID == request.DestinationID)
        {
            
            if(myNode.Router.NetworkMap[myNode.ID].Contains(request.NodeToCheck) == false)
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

                myNode.CommsModule.Send(nextResponseHop, requestResponse);
            }
        }
        

    }


//    myNode.Router.DeleteEdge(myNode.ID, node);
//                DetectFailureRequest failureRequest = new DetectFailureRequest();
//    failureRequest.NodeToCheck = node;
//                failureRequest.SourceID = myNode.ID;
//                failureRequest.Command = Request.Commands.DetectFailure;

//                failureRequest.DestinationID = myNode.Router.NetworkMap[node][0];

//                //TODO: Make this handle 0 or best neighbour
//                uint? nextHop = myNode.Router.NextHop(myNode.ID, failureRequest.DestinationID);
//    response = await myNode.CommsModule.SendAsync(nextHop, failureRequest, 30000);
//                //TODO: MAKE THIS NOT JUST ERROR CODE BASED, ALSO WHAT TO DO WHEN NO ERROR?
//                if(response.ResponseCode == Response.ResponseCodes.ERROR)
//                {
//                    throw new NotImplementedException("GENERATE A PLAN HERE");
//    //TODO: IT IS DEAD, GENERATE NEW PLAN
//}

public async static void FailureDetected(INode myNode, uint? failedNode)
    {
        myNode.Router.DeleteEdge(myNode.ID, failedNode);

        uint? neighbourID = myNode.Router.NetworkMap[failedNode][0];
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
