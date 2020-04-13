using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Numerics;

public class FailureDetection
{


    /// <summary>Handles failure detection requests, updating own router, and relaying or performing aliveness check
    /// <para>  </para>
    /// </summary>
    public async static void DetectFailure(INode myNode, DetectFailureRequest request)
    {
        //Update router, ensure we don't try to route through the bad connection

        request.DeadEdges.ForEach(edge => myNode.Router.DeleteEdge(edge.Item1, edge.Item2));

        //Response r = new Response();
        //r.DestinationID = request.SenderID;
        //r.SourceID = myNode.ID;
        //r.ResponseCode = Response.ResponseCodes.OK;
        //r.MessageIdentifer = request.MessageIdentifer;
        //myNode.CommsModule.Send(r.DestinationID, r);
        
        if (myNode.ID == request.DestinationID)
        {
            //If we don't have a live already, we assume the connection has been determined to be bad
            if(myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours.Contains(request.NodeToCheck) == false)// TODO: Probably safer check here
            {
                Recovery(myNode);
            } else
            {
                Request ping = new Request();
                ping.SourceID = myNode.ID;
                ping.DestinationID = request.NodeToCheck;
                ping.Command = Request.Commands.PING;
                Response pingResponse = await myNode.CommsModule.SendAsync(ping.DestinationID, ping, 1000, 3);

                FailureDetectionResponse requestResponse;
                if (pingResponse == null || pingResponse.ResponseCode == Response.ResponseCodes.ERROR)
                {
                    Recovery(myNode);
                } else
                {
                    requestResponse = new FailureDetectionResponse(myNode.ID, request.SourceID, Response.ResponseCodes.OK, request.MessageIdentifer, request.DeadEdges);
                    uint? nextResponseHop = myNode.Router.NextHop(myNode.ID, requestResponse.DestinationID);

                    myNode.CommsModule.Send(nextResponseHop, requestResponse); 
                    //TODO: make the response contain the fact that the node IS dead, so other nodes can update
                }

               
            }
        }
        

    }

    /// <summary>Should be used on the node when it detects a failure
    /// <para>Will initiate a failure detection operation, asking neighbours of failed node about aliveness</para>
    /// </summary>
    public async static void FailureDetected(INode myNode, uint? failedNode)
    {
        //Remove edge from router, ensuring it won't try to route through the failed node
        myNode.Router.DeleteEdge(myNode.ID, failedNode);

        //Get a immidiate neighbour to the failed node
        uint? neighbourID = myNode.Router.NetworkMap.GetEntryByID(failedNode).Neighbours[0]; //TODO: what if we are only neighbour? what if there are more? or a best?
        uint? nextHop = myNode.Router.NextHop(myNode.ID, neighbourID);

        DetectFailureRequest request = new DetectFailureRequest
        {
            DestinationID = neighbourID,
            SourceID = myNode.ID,
            SenderID = myNode.ID,
            Command = Request.Commands.DETECTFAILURE,
            ResponseExpected = true,
            AckExpected = true,
            NodeToCheck = failedNode,
            DeadEdges = new List<Tuple<uint?, uint?>> {new Tuple<uint?, uint?>(myNode.ID, failedNode) }
        };

        // TODO: Response here is just response from nexthop and not response from neighbour to failed node.
        // We should probably support this by being able to both send and route both requests and responses across the network.
        // OBS: Recovery code below is never run because of the above reasons. It does not wait for the "response request" that is sent to it.
        Response response = await myNode.CommsModule.SendAsync(nextHop, request, 30000, 3);

        if(response.ResponseCode == Response.ResponseCodes.ERROR)
        {
            ConstellationPlan RecoveryPlan = GenerateConstellation.GenerateTargetConstellation(myNode.Router.ReachableSats(myNode).Count, 7.152f);


            PlanRequest recoveryRequest = new PlanRequest
            {
                SourceID = myNode.ID,
                DestinationID = myNode.ID,
                Command = Request.Commands.GENERATE,
                Plan = RecoveryPlan,
                Dir = Router.CommDir.CW
            };

            if (myNode.Router.NextSequential(myNode, Router.CommDir.CW) == null)
            {
                recoveryRequest.Dir = Router.CommDir.CCW;
            }

                myNode.CommsModule.Send(myNode.ID, recoveryRequest);
            return;
        }

    }


    public static void Recovery(INode myNode)
    {
            ConstellationPlan RecoveryPlan = GenerateConstellation.GenerateTargetConstellation(myNode.Router.ReachableSats(myNode).Count, 7.152f);


            PlanRequest recoveryRequest = new PlanRequest
            {
                SourceID = myNode.ID,
                DestinationID = myNode.ID,
                Command = Request.Commands.GENERATE,
                Plan = RecoveryPlan,
                Dir = Router.CommDir.CW
            };

            if (myNode.Router.NextSequential(myNode, Router.CommDir.CW) == null)
            {
                recoveryRequest.Dir = Router.CommDir.CCW;
            }

            myNode.CommsModule.Send(myNode.ID, recoveryRequest);
    }


    public static void Recovery(INode myNode, uint? secondFailedNode)
    {
        //Remove edge from router, ensuring it won't try to route through the failed node
        myNode.Router.DeleteEdge(myNode.ID, secondFailedNode);

        // Find positions of nodes this node can reach
        List<Vector3> positions = new List<Vector3> { myNode.Position };
        List<uint?> nodesToVisit = new List<uint?> { myNode.ID };
        
        while(nodesToVisit.Count > 0)
        {
            uint? nodeToVisit = nodesToVisit[0];
            nodesToVisit.RemoveAt(0);
            List<NetworkMapEntry> neighbourEntries = myNode.Router.NetworkMap.GetEntryByID(nodeToVisit).Neighbours.Select(x => myNode.Router.NetworkMap.GetEntryByID(x)).ToList();
            nodesToVisit.AddRange(neighbourEntries.Where(x => positions.Contains(x.Position) == false).Select(x => x.ID));
            positions.AddRange(neighbourEntries.Where(x => positions.Contains(x.Position) == false).Select(x => x.Position));
        }

        // Calculate midpoint
        Vector3 midpoint = positions.Aggregate(Vector3.Zero, (x, y) => x + y);
        Vector3 midpointOnRightAltitude = Vector3.Normalize(midpoint) * Vector3.Distance(Vector3.Zero, myNode.Position);

        // Generate recovery plan and start planning
        ConstellationPlan recoveryPlan = GenerateConstellation.GenerateRecoveryConstellation(midpointOnRightAltitude, positions.Count);

        PlanRequest recoveryRequest = new PlanRequest {
            SourceID = myNode.ID,
            DestinationID = myNode.ID,
            Command = Request.Commands.GENERATE,
            Plan = recoveryPlan,
        };

        myNode.CommsModule.Send(myNode.ID, recoveryRequest);
    }
}
