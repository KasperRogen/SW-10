using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Linq;

public class PlanExecuter
{


    /// <summary>Method for starting execution of the plan in planrequest
    /// <para>  </para>
    /// </summary>
    public static void ExecutePlan(INode myNode, PlanRequest request)
    {


        if (request.DestinationID != myNode.ID)
        {

            return;

        }
        else
        {

            myNode.State = Node.NodeState.EXECUTING;

            
            if (myNode.executingPlan)
            {
                myNode.State = Node.NodeState.PASSIVE;
                return; // Ignore Execute command if already executing which stops the execute communication loop
            }
            else
            {
                myNode.executingPlan = true;
            }


            PlanRequest newRequest = request.DeepCopy();

            uint? nextSeq = myNode.Router.NextSequential(myNode, request.Dir);

            if(nextSeq == null)
            {
                Router.CommDir newDir = request.Dir == Router.CommDir.CW ? Router.CommDir.CCW : Router.CommDir.CW;
                newRequest.Dir = newDir;
                nextSeq = myNode.Router.NextSequential(myNode, newDir);
            }

            if(nextSeq != null)
            {
                newRequest.SourceID = myNode.ID;
                newRequest.DestinationID = nextSeq;
                uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
                myNode.CommsModule.Send(nextHop, newRequest);
            }

            // Find the ID of the node that has to travel the furthest comparing the active plan to the new plan
            IEnumerable<Tuple<uint?, float>> travelDistanceByID = Enumerable.Zip(
                    myNode.ActivePlan.Entries.Where(x => newRequest.Plan.Entries.Select(y => y.NodeID).Contains(x.NodeID)).OrderBy(x => x.NodeID),
                    newRequest.Plan.Entries.OrderBy(x => x.NodeID),
                    (x, y) => new Tuple<uint?, Vector3, Vector3>(x.NodeID, x.Position, y.Position))
                .Select(x => new Tuple<uint?, float>(x.Item1, Vector3.Distance(x.Item2, x.Item3)));
            float maxTravelDistance = travelDistanceByID.Max(x => x.Item2);
            uint? maxTravelID = travelDistanceByID.Single(x => x.Item2 == maxTravelDistance).Item1;

            // If the found ID is this node's, then discovery should be started when the node is at its new location.
            if (maxTravelID == myNode.ID)
            {
                DiscoveryIfNewNeighboursAfterExecuting(myNode);
            }

            myNode.ActivePlan = newRequest.Plan;
            //Set my targetposition to the position i was assigned in the plan
            myNode.TargetPosition = request.Plan.Entries.Find(entry => entry.NodeID == myNode.ID).Position;
            
            myNode.Router.UpdateNetworkMap(newRequest.Plan);

            Thread.Sleep(1000);
            myNode.State = Node.NodeState.PASSIVE;

        }

    }

    private static async void DiscoveryIfNewNeighboursAfterExecuting(INode myNode)
    {
        while (Vector3.Distance(myNode.Position, myNode.TargetPosition) > 0.01f)
        {
            await Task.Delay(100);
        }

        // If ReachableNodes contains any that are not in networkmap neighbours -> Any new neighbours
        if (myNode.CommsModule.Discover().Except(myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours).Count() > 0)
        {
            Discovery.StartDiscovery(myNode);
        }
    }
}

