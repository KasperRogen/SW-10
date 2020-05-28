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


        if (request.DestinationID != myNode.Id)
        {

            return;

        }
        else
        {

            myNode.State = Node.NodeState.EXECUTING;

            
            if (myNode.ExecutingPlan)
            {
                myNode.State = Node.NodeState.PASSIVE;
                return; // Ignore Execute command if already executing which stops the execute communication loop
            }
            else
            {
                myNode.ExecutingPlan = true;
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
                newRequest.SourceID = myNode.Id;
                newRequest.DestinationID = nextSeq;
                uint? nextHop = myNode.Router.NextHop(myNode.Id, nextSeq);
                myNode.CommsModule.Send(nextHop, newRequest);
            }

            //Set my targetposition to the position i was assigned in the plan
            myNode.TargetPosition = request.Plan.Entries.Find(entry => entry.NodeID == myNode.Id).Position;

            // Entries in active plan that are also in the new plan
            List<ConstellationPlanEntry> activeEntries = new List<ConstellationPlanEntry>();
            // Entries in new plan
            List<ConstellationPlanEntry> newEntries = request.Plan.Entries;
            // IDs of entries in the new plan
            IEnumerable<uint?> newEntryIDs = newRequest.Plan.Entries.Select(entry => entry.NodeID);
            // Fill out activeEntries
            foreach (ConstellationPlanEntry entry in myNode.ActivePlan.Entries) {
                if (newEntryIDs.Contains(entry.NodeID)) {
                    activeEntries.Add(entry);
                }
            }

            // Order by ID pre-zip
            activeEntries = activeEntries.OrderBy(entry => entry.NodeID).ToList();
            newEntries = newEntries.OrderBy(entry => entry.NodeID).ToList();
            // Zip active and new entries together on NodeID including Position of them both
            IEnumerable<Tuple<uint?, Vector3, Vector3>> entriesZipped = Enumerable.Zip(activeEntries, newEntries, (ae, ne) => new Tuple<uint?, Vector3, Vector3>(ae.NodeID, ae.Position, ne.Position));
            // Distance nodes have to travel based on active and new plan
            IEnumerable<Tuple<uint?, float>> travelDistanceByID = entriesZipped.Select(entry => new Tuple<uint?, float>(entry.Item1, Vector3.Distance(entry.Item2, entry.Item3)));
            // Find max travel distance and ID of node that has to travel that
            float maxTravelDistance = travelDistanceByID.Max(x => x.Item2);
            uint? maxTravelID = travelDistanceByID.Single(x => x.Item2 == maxTravelDistance).Item1;

            // If the found ID is this node's, then discovery should be started when the node is at its new location.
            if (maxTravelID == myNode.Id)
            {
                DiscoveryIfNewNeighboursAfterExecuting(myNode);
            }

            myNode.ActivePlan = newRequest.Plan;

            myNode.Router.ClearNetworkMap();
            myNode.Router.UpdateNetworkMap(newRequest.Plan);

            Thread.Sleep(1000 / Constants.TimeScale);
            myNode.State = Node.NodeState.PASSIVE;

        }

    }

    private static async void DiscoveryIfNewNeighboursAfterExecuting(INode myNode)
    {
        while (Vector3.Distance(myNode.Position, myNode.TargetPosition) > 0.01f)
        {
            await Task.Delay(100 / Constants.TimeScale);
        }

        // If ReachableNodes contains any that are not in networkmap neighbours -> Any new neighbours
        if (myNode.CommsModule.Discover().Except(myNode.Router.NetworkMap.GetEntryByID(myNode.Id).Neighbours).Count() > 0)
        {
            Discovery.StartDiscovery(myNode, true);
        }
    }
}

