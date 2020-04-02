using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class FailureDetection
{


    /// <summary>Handles failure detection requests, updating own router, and relaying or performing aliveness check
    /// <para>  </para>
    /// </summary>
    public async static void DetectFailure(INode myNode, DetectFailureRequest request)
    {
        //Update router, ensure we don't try to route through the bad connection

        request.DeadEdges.ForEach(edge => myNode.Router.DeleteEdge(edge.Item1, edge.Item2));


        
        if (myNode.ID == request.DestinationID)
        {
            //If we don't have a live already, we assume the connection has been determined to be bad
            if(myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours.Contains(request.NodeToCheck) == false)// TODO: Probably safer check here
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
                ping.Command = Request.Commands.PING;
                //Response pingResponse = await myNode.CommsModule.SendAsync(ping.DestinationID, ping, 1000);
                Response pingResponse = await AttemptRequest(myNode, ping, 5);

                FailureDetectionResponse requestResponse;
                if (pingResponse == null || pingResponse.ResponseCode == Response.ResponseCodes.ERROR)
                {
                    Tuple<uint?, uint?> deadEdge = new Tuple<uint?, uint?>(myNode.ID, request.NodeToCheck);
                    request.DeadEdges.Add(deadEdge);
                    request.DeadEdges.ForEach(edge => myNode.Router.DeleteEdge(edge.Item1, edge.Item2));
                    requestResponse = new FailureDetectionResponse(myNode.ID, request.SourceID, Response.ResponseCodes.ERROR, request.MessageIdentifer, request.DeadEdges);
                } else
                {
                    requestResponse = new FailureDetectionResponse(myNode.ID, request.SourceID, Response.ResponseCodes.OK, request.MessageIdentifer, request.DeadEdges);
                }

                uint? nextResponseHop = myNode.Router.NextHop(myNode.ID, requestResponse.DestinationID);

                myNode.CommsModule.Send(nextResponseHop, requestResponse); //TODO: make the response contain the fact that the node IS dead, so other nodes can update
            }
        }
        

    }

    private async static Task<Response> AttemptRequest(INode myNode, Request request, int attempts)
    {
        Response response = null;
        int sleepTime = 1000;

        for (int i = 0; i < attempts; i++) {
            response = await myNode.CommsModule.SendAsync(request.DestinationID, request, 1000);

            if (response == null || response.ResponseCode == Response.ResponseCodes.ERROR)
            {
                Thread.Sleep(sleepTime);
                sleepTime *= sleepTime; // Increase sleep time exponentially and keep attempting
            }
            else
            {
                break; // Escape loop if we got a response
            }
        }

        return response;
    }

    /// <summary>Should be used on the node when it detects a failure
    /// <para>Will initiate a failure detection operation, asking neighbours of failed node about aliveness</para>
    /// </summary>
    public async static void FailureDetected(INode myNode, uint? failedNode)
    {
        //Remove edge from router, ensuring it won't try to route through the failed node
        myNode.Router.DeleteEdge(myNode.ID, failedNode); //TODO: Should we do this already???

        //Get a immidiate neighbour to the failed node
        uint? neighbourID = myNode.Router.NetworkMap.GetEntryByID(failedNode).Neighbours[0]; //TODO: what if we are only neighbour? what if there are more? or a best?
        uint? nextHop = myNode.Router.NextHop(myNode.ID, neighbourID);

        DetectFailureRequest request = new DetectFailureRequest
        {
            Command = Request.Commands.DETECTFAILURE,
            DestinationID = neighbourID,
            SourceID = myNode.ID,
            NodeToCheck = failedNode,
            DeadEdges = new List<System.Tuple<uint?, uint?>> {new System.Tuple<uint?, uint?>(myNode.ID, failedNode) }
        };

        Response response = await myNode.CommsModule.SendAsync(nextHop, request, 15000);

        if(response.ResponseCode == Response.ResponseCodes.ERROR)
        {
            ConstellationPlan RecoveryPlan = GenerateConstellation.GenerateTargetConstellation(SatManager._instance.satellites.Count, 7.152f);
            PlanRequest recoveryRequest = new PlanRequest
            {
                SourceID = myNode.ID,
                DestinationID = myNode.ID,
                Command = Request.Commands.GENERATE,
                Plan = RecoveryPlan,
            };

            myNode.CommsModule.Send(myNode.ID, recoveryRequest);
            return;
        }

    }

}
