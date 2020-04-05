using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Numerics;

public class PlanGenerator
{
    /// <summary>Performs work in order to find optimum location in new constellation for given node
    /// <para>Recieves planrequest, finds best free location, or trades locations with other node in order to optimize net cost</para>
    /// </summary>
    public static void GeneratePlan(INode myNode, PlanRequest request)
    {
        //If the request isn't meant for this node, just return. Node.cs will relay the message
        if (request.DestinationID != myNode.ID)
        {
            return;
        }
        else
        {
            myNode.executingPlan = false;
            myNode.State = Node.NodeState.PLANNING;
            myNode.GeneratingPlan = request.Plan;

            //OptimalOneRevolutionPlanGeneration(myNode, request);
            SingleRevolutionPlanGeneration(myNode, request);
            //MultipleRevolutionsPlanGeneration(myNode, request);
        }
    }

    /// <summary> Used for finding and "taking" optimal spot for given satellite
    /// <para>Functions as part of GeneratePlan method</para>
    /// </summary>
    static ConstellationPlan TakeSlot(INode myNode, ConstellationPlan plan, int entryIndex, float newValue)
    {
        //Create a new plan as copy of current plan
        ConstellationPlan newPlan = new ConstellationPlan(plan.Entries);

        //Find cheapest slot to take
        ConstellationPlanEntry currentSlot = newPlan.Entries.Find(entry => entry.NodeID != null && entry.NodeID == myNode.ID);

        //TODO: MAKE "TRADING" WORK
        //if (currentSlot != null && plan.Entries[entryIndex].NodeID != null)
        //{
        //    currentSlot.NodeID = plan.Entries[entryIndex].NodeID;
        //    currentSlot.Fields["DeltaV"].Value = Position.Distance(currentSlot.Position, currentSlot.Node.Position);
        //}

        newPlan.Entries[entryIndex].NodeID = myNode.ID;
        newPlan.Entries[entryIndex].Fields["DeltaV"].Value = newValue;

        return newPlan;
    }

    private static void MultipleRevolutionsPlanGeneration(INode myNode, PlanRequest request)
    {
        ConstellationPlan newPlan = null;

        // Phase 1: All locations are taken one by one by a node
        //If this node currently has no location in the target constellation
        if (request.Plan.Entries.Any(entry => entry.NodeID == myNode.ID) == false) {
            ConstellationPlanEntry slotToTake = request.Plan.Entries.Where(entry => entry.NodeID == null) //Only allow satellite to take free locations
            .Aggregate((CurrentBest, currentTest) => //Iterate each entry
            Vector3.Distance(currentTest.Position, myNode.Position) <=  //This entry currently being tested to improve over current best
            Vector3.Distance(CurrentBest.Position, myNode.Position) ?  //current best 
            currentTest : CurrentBest); //return best candidate of currenttest and currentbest

            newPlan = TakeSlot(myNode, request.Plan, request.Plan.Entries.IndexOf(slotToTake), Vector3.Distance(slotToTake.Position, myNode.Position));
        }
        // Phase 2: Nodes can swap locations if it optimises the cost
        //TODO: Fix problem with requirering knowledge about all nodes in order to "trade" with them
        else if (request.Plan.Entries.Any(entry => entry.NodeID == null) == false) {
            Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

            //Calculate cost of each location in target constellation
            for (int i = 0; i < request.Plan.Entries.Count; i++) {
                if (request.Plan.Entries[i].NodeID != myNode.ID) // Exclude location that current node has taken.
                {
                    float requiredDeltaV = Vector3.Distance(myNode.Position, request.Plan.Entries[i].Position);
                    fieldDeltaVPairs.Add(i, requiredDeltaV);
                }
            }

            foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value)) {
                if (request.Plan.TrySwapNodes(myNode.ID, myNode.Position, request.Plan.Entries[pair.Key].NodeID, request.Plan.Entries[pair.Key].Position, out newPlan)) {
                    newPlan.LastEditedBy = myNode.ID;
                    myNode.State = Node.NodeState.OVERRIDE;
                    break;
                } else {
                    newPlan = null;
                }
            }
        }

        PlanRequest newRequest = request.DeepCopy();
        newRequest.SenderID = myNode.ID;

        //If we have made any changes to the plan
        if (newPlan != null && newPlan != newRequest.Plan) {
            newRequest.Plan = newPlan;
            myNode.justChangedPlan = true;
            newRequest.Plan.LastEditedBy = myNode.ID;

            myNode.GeneratingPlan = newRequest.Plan;
        }

        //If we were the last node to edit the plan, and we didn't edit the plan in the current pass
        //We know the plan has taken an entire revolution without being changed, hence is at optimum,
        //Start executing the plan
        if (newRequest.Plan.LastEditedBy == myNode.ID && myNode.justChangedPlan == false) {
            newRequest.Command = Request.Commands.EXECUTE;
            newRequest.DestinationID = myNode.ID;
            newRequest.SourceID = myNode.ID;

            //Notify self about execution
            //TODO: SOMETHING MORE ELEGANT THAN THIS :D
            myNode.CommsModule.Send(myNode.ID, newRequest);
        } else {
            myNode.justChangedPlan = false;
            myNode.State = Node.NodeState.PASSIVE;

            //Pass the plan to the next sequential node
            uint? nextSeq = myNode.Router.NextSequential(myNode);
            newRequest.DestinationID = nextSeq;

            if (myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours.Contains(nextSeq)) {
                myNode.CommsModule.Send(nextSeq, newRequest);
            } else {
                uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
                newRequest.DestinationID = nextSeq;
                myNode.CommsModule.Send(nextHop, newRequest);
            }
        }
    }

    private static void SingleRevolutionPlanGeneration(INode myNode, PlanRequest request)
    {
        ConstellationPlan newPlan = request.Plan.DeepCopy();
        KeyValuePair<Vector3, float> locationToTakeWithDistance; // Is filled by loop below
        bool foundLocation = false;
        IOrderedEnumerable<KeyValuePair<Vector3, float>> freeLocationsWithDistancesOrdered = newPlan.Entries
            .Where(x => x.NodeID == null)
            .Select(x => new KeyValuePair<Vector3, float>(x.Position, Vector3.Distance(myNode.Position, x.Position)))
            .OrderBy(x => x.Value);

        foreach (KeyValuePair<Vector3, float> freeLocationWithDistance in freeLocationsWithDistancesOrdered) {
            IEnumerable<ConstellationPlanEntry> nodesCanDoCheaper = myNode.ActivePlan.Entries
                .Where(x => Vector3.Distance(x.Position, freeLocationWithDistance.Key) < freeLocationWithDistance.Value);
            
            // If no other node can take this location cheaper, then this node gets it
            if (nodesCanDoCheaper.Count() == 0)
            {
                locationToTakeWithDistance = freeLocationWithDistance;
                foundLocation = true;
                break;
            }
            else
            {
                foreach (ConstellationPlanEntry nodeCanDoCheaper in nodesCanDoCheaper) {
                    var freeLocationswithDistancesCheaperNode = newPlan.Entries
                        .Where(x => x.NodeID == null)
                        .Select(x => new KeyValuePair<Vector3, float>(x.Position, Vector3.Distance(nodeCanDoCheaper.Position, x.Position)));

                    // Otherwise check if cheaper node has other locations it can take, that are cheaper, then this node gets the location
                    if (freeLocationswithDistancesCheaperNode.Any(x => x.Value < freeLocationWithDistance.Value))
                    {
                        locationToTakeWithDistance = freeLocationWithDistance;
                        foundLocation = true;
                        break;
                    }
                }

                // Break outer loop if location is found
                if (foundLocation)
                {
                    break;
                }
            }
        }

        ConstellationPlanEntry entryToTake = newPlan.Entries.Single(x => x.Position == locationToTakeWithDistance.Key);
        entryToTake.NodeID = myNode.ID;
        entryToTake.Fields["DeltaV"].Value = locationToTakeWithDistance.Value;

        PlanRequest newRequest = request.DeepCopy();
        newRequest.Plan = newPlan;
        newRequest.SenderID = myNode.ID;

        myNode.GeneratingPlan = newRequest.Plan;

        // If last location is filled, execute the plan
        if (newPlan.Entries.All(x => x.NodeID != null))
        {
            newRequest.Command = Request.Commands.EXECUTE;
            newRequest.SourceID = myNode.ID;

            PlanExecuter.ExecutePlan(myNode, newRequest);
        }
        else
        {
            myNode.State = Node.NodeState.PASSIVE;
            uint? nextSeq = myNode.Router.NextSequential(myNode);
            newRequest.DestinationID = nextSeq;

            if (myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours.Contains(nextSeq)) {
                myNode.CommsModule.Send(nextSeq, newRequest);
            } else {
                uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
                myNode.CommsModule.Send(nextHop, newRequest);
            }
        }
    }

    private static void OptimalOneRevolutionPlanGeneration(INode myNode, PlanRequest request)
    {
        ConstellationPlan temporaryPlan = request.Plan.DeepCopy();
        temporaryPlan.Entries.ForEach(x => x.Fields["DeltaV"].Value = float.MaxValue);
        ConstellationPlan currentBestPlan = temporaryPlan.DeepCopy(); // Current best plan with worst-case DeltaV usage
        IEnumerable<IList<ConstellationPlanEntry>> permutations = Utility.Permutations(myNode.ActivePlan.Entries);

        foreach (IList<ConstellationPlanEntry> permutation in permutations)
        {
            for (int i = 0; i < permutation.Count(); i++) {
                temporaryPlan.Entries[i].NodeID = permutation[i].NodeID;
                temporaryPlan.Entries[i].Fields["DeltaV"].Value = Vector3.Distance(permutation[i].Position, temporaryPlan.Entries[i].Position);
            }

            if (temporaryPlan.Cost() < currentBestPlan.Cost()) 
            {
                currentBestPlan = temporaryPlan.DeepCopy();
            }
        }

        int indexOfEntryToTake = currentBestPlan.Entries.FindIndex(x => x.NodeID == myNode.ID);

        PlanRequest newRequest = request.DeepCopy();
        newRequest.Plan.Entries[indexOfEntryToTake] = currentBestPlan.Entries[indexOfEntryToTake].DeepCopy();
        newRequest.SenderID = myNode.ID;

        // If last location is filled, execute the plan
        if (newRequest.Plan.Entries.All(x => x.NodeID != null)) {
            newRequest.Command = Request.Commands.EXECUTE;
            newRequest.SourceID = myNode.ID;

            PlanExecuter.ExecutePlan(myNode, newRequest);
        } else {
            myNode.State = Node.NodeState.PASSIVE;
            uint? nextSeq = myNode.Router.NextSequential(myNode);
            newRequest.DestinationID = nextSeq;

            if (myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours.Contains(nextSeq)) {
                myNode.CommsModule.Send(nextSeq, newRequest);
            } else {
                uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
                myNode.CommsModule.Send(nextHop, newRequest);
            }
        }
    }
}
