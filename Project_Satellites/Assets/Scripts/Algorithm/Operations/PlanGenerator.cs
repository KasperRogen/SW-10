using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class PlanGenerator
{
    /// <summary>Performs work in order to find optimum location in new constellation for given node
    /// <para>Recieves planrequest, finds best free location, or trades locations with other node in order to optimize net cost</para>
    /// </summary>
    public static void GeneratePlan(INode myNode, PlanRequest request)
    {
        new Thread(delegate ()
        {
            //If the request isn't meant for this node, just return. Node.cs will relay the message
            if (request.DestinationID != myNode.ID) { 
                return;
            } 
            else {
                myNode.executingPlan = false;
                myNode.Plan = request.Plan;
                myNode.State = Node.NodeState.PLANNING;


                Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

                //Calculate cost of each location in target constellation
                for (int i = 0; i < request.Plan.Entries.Count; i++)
                {
                    float requiredDeltaV = Position.Distance(myNode.Position, request.Plan.Entries[i].Position);
                    fieldDeltaVPairs.Add(i, requiredDeltaV);
                }

                ConstellationPlan newPlan = null;

                //If this node currently has no location in the target constellation
                if (request.Plan.Entries.Any(entry => entry.NodeID == myNode.ID) == false)
                {
                    ConstellationPlanEntry slotToTake = request.Plan.Entries.Where(entry => entry.NodeID == null) //Only allow satellite to take free locations
                    .Aggregate((CurrentBest, currentTest) => //Iterate each entry
                    Position.Distance(currentTest.Position, myNode.Position) <=  //This entry currently being tested to improve over current best
                    Position.Distance(CurrentBest.Position, myNode.Position) ?  //current best 
                    currentTest : CurrentBest); //return best candidate of currenttest and currentbest

                    newPlan = TakeSlot(myNode, request.Plan, request.Plan.Entries.IndexOf(slotToTake), Position.Distance(slotToTake.Position, myNode.Position));
                }
                //TODO: Fix problem with requirering knowledge about all nodes in order to "trade" with them
                //else if (plan.entries.Any(entry => entry.Node == null) == false)
                //{

                //    foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))
                //    {
                //        if (plan.ReduceBy("DeltaV", pair.Key, pair.Value, this))
                //        {
                //            if (plan.entries[pair.Key].Node != null && plan.entries[pair.Key].Node.ID != ID)
                //            {
                //                State = NodeState.OVERRIDE;
                //            }

                //            newPlan = TakeSlot(plan, pair.Key, pair.Value);
                //            break;
                //        }
                //    }
                //}

                //If we have made any changes to the plan
                if (newPlan != null && newPlan != request.Plan)
                {
                    request.Plan = newPlan;
                    myNode.justChangedPlan = true;
                    request.Plan.lastEditedBy = myNode.ID;

                    myNode.Plan = request.Plan;
                    Thread.Sleep(1000);
                }
                else
                {
                    Thread.Sleep(250);
                }

                //If we were the last node to edit the plan, and we didn't edit the plan in the current pass
                //We know the plan has taken an entire revolution without being changed, hence is at optimum,
                //Start executing the plan
                if (request.Plan.lastEditedBy == myNode.ID && myNode.justChangedPlan == false)
                {
                    myNode.State = Node.NodeState.EXECUTING;

                    request.Command = Request.Commands.Execute;
                    request.DestinationID = myNode.ID;
                    request.SourceID = myNode.ID;

                    //Notify self about execution
                    //TODO: SOMETHING MORE ELEGANT THAN THIS :D
                    myNode.CommsModule.Send(myNode.ID, request);
                }
                else
                {
                    myNode.justChangedPlan = false;
                    myNode.State = Node.NodeState.PASSIVE;

                    //Pass the plan to the next sequential node
                    uint? nextSeq = myNode.Router.NextSequential(myNode.ID);
                    request.DestinationID = nextSeq;

                    if (myNode.Router.NetworkMap[myNode.ID].Contains(nextSeq))
                    {
                        myNode.CommsModule.Send(nextSeq, request);
                    }
                    else
                    {
                        uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
                        request.DestinationID = nextSeq;
                        myNode.CommsModule.Send(nextHop, request);
                    }

                }


            }

        }).Start();

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
