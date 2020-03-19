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

            ConstellationPlan newPlan = null;

            // Phase 1: All locations are taken one by one by a node
            //If this node currently has no location in the target constellation
            if (request.Plan.Entries.Any(entry => entry.NodeID == myNode.ID) == false)
            {
                ConstellationPlanEntry slotToTake = request.Plan.Entries.Where(entry => entry.NodeID == null) //Only allow satellite to take free locations
                .Aggregate((CurrentBest, currentTest) => //Iterate each entry
                Vector3.Distance(currentTest.Position, myNode.Position) <=  //This entry currently being tested to improve over current best
                Vector3.Distance(CurrentBest.Position, myNode.Position) ?  //current best 
                currentTest : CurrentBest); //return best candidate of currenttest and currentbest

                newPlan = TakeSlot(myNode, request.Plan, request.Plan.Entries.IndexOf(slotToTake), Vector3.Distance(slotToTake.Position, myNode.Position));
            }
            // Phase 2: Nodes can swap locations if it optimises the cost
            //TODO: Fix problem with requirering knowledge about all nodes in order to "trade" with them
            else if (request.Plan.Entries.Any(entry => entry.NodeID == null) == false)
            {
                Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

                //Calculate cost of each location in target constellation
                for (int i = 0; i < request.Plan.Entries.Count; i++)
                {
                    if (request.Plan.Entries[i].NodeID != myNode.ID) // Exclude location that current node has taken.
                    {
                        float requiredDeltaV = Vector3.Distance(myNode.Position, request.Plan.Entries[i].Position);
                        fieldDeltaVPairs.Add(i, requiredDeltaV);
                    }
                }

                foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))
                {
                    if (request.Plan.TrySwapNodes(myNode.ID, myNode.Position, request.Plan.Entries[pair.Key].NodeID, request.Plan.Entries[pair.Key].Position, out newPlan))
                    {
                        newPlan.LastEditedBy = myNode.ID;
                        myNode.State = Node.NodeState.OVERRIDE;
                        break;
                    }
                    else
                    {
                        newPlan = null;
                    }
                }
            }

            PlanRequest newRequest = request.DeepCopy();
            newRequest.SenderID = myNode.ID;

                //If we have made any changes to the plan
            if (newPlan != null && newPlan != newRequest.Plan)
            {
                newRequest.Plan = newPlan;
                myNode.justChangedPlan = true;
                newRequest.Plan.LastEditedBy = myNode.ID;

                myNode.GeneratingPlan = newRequest.Plan;
            }

            //If we were the last node to edit the plan, and we didn't edit the plan in the current pass
            //We know the plan has taken an entire revolution without being changed, hence is at optimum,
            //Start executing the plan
            if (newRequest.Plan.LastEditedBy == myNode.ID && myNode.justChangedPlan == false)
            {
                newRequest.Command = Request.Commands.Execute;
                newRequest.DestinationID = myNode.ID;
                newRequest.SourceID = myNode.ID;

                //Notify self about execution
                //TODO: SOMETHING MORE ELEGANT THAN THIS :D
                myNode.CommsModule.Send(myNode.ID, newRequest);
            }
            else
            {
                myNode.justChangedPlan = false;
                myNode.State = Node.NodeState.PASSIVE;

                //Pass the plan to the next sequential node
                uint? nextSeq = myNode.Router.NextSequential(myNode.ID, request.SenderID);
                newRequest.DestinationID = nextSeq;

                if (myNode.Router.NetworkMap[myNode.ID].Contains(nextSeq))
                {
                    myNode.CommsModule.Send(nextSeq, newRequest);
                }
                else
                {
                    uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
                    newRequest.DestinationID = nextSeq;
                    myNode.CommsModule.Send(nextHop, newRequest);
                }
            }
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
}
