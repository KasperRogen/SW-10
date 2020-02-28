using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class PlanGenerator
{

    public static void GeneratePlan(INode myNode, PlanRequest request)
    {
        new Thread(delegate ()
        {
            if (request.Command != Request.Commands.Generate)
            {
                throw new Exception("Wrong command"); // Only accept Generate command
            }

            if (request.DestinationID == myNode.ID)
            {
                myNode.executingPlan = false;

                myNode.Plan = request.Plan;

                myNode.State = Node.NodeState.PLANNING;

                Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

                for (int i = 0; i < request.Plan.Entries.Count; i++)
                {
                    float requiredDeltaV = Position.Distance(myNode.Position, request.Plan.Entries[i].Position);
                    fieldDeltaVPairs.Add(i, requiredDeltaV);
                }

                ConstellationPlan newPlan = null;

                if (request.Plan.Entries.Any(entry => entry.NodeID == myNode.ID) == false)
                {
                    ConstellationPlanEntry slotToTake = request.Plan.Entries.Where(entry => entry.NodeID == null) //Only allow satellite to take free locations
                    .Aggregate((CurrentBest, currentTest) => //Iterate each entry
                    Position.Distance(currentTest.Position, myNode.Position) <=  //This entry currently being tested to improve over current best
                    Position.Distance(CurrentBest.Position, myNode.Position) ?  //current best 
                    currentTest : CurrentBest); //return best candidate of currenttest and currentbest

                    newPlan = TakeSlot(myNode, request.Plan, request.Plan.Entries.IndexOf(slotToTake), Position.Distance(slotToTake.Position, myNode.Position));
                }
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


                if (request.Plan.lastEditedBy == myNode.ID && myNode.justChangedPlan == false)
                {
                    myNode.State = Node.NodeState.EXECUTING;

                    request.Command = Request.Commands.Execute;
                    request.DestinationID = myNode.ID;
                    request.SourceID = myNode.ID;

                    myNode.CommsModule.Send(myNode.ID, request);
                }
                else
                {
                    myNode.justChangedPlan = false;
                    myNode.State = Node.NodeState.PASSIVE;
                    uint? nextSeq = myNode.Router.NextSequential(myNode.ID);
                    if (myNode.Router.NetworkMap[myNode.ID].Contains(nextSeq))
                    {
                        request.DestinationID = nextSeq;
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



    static ConstellationPlan TakeSlot(INode myNode, ConstellationPlan plan, int entryIndex, float newValue)
    {
        ConstellationPlan newPlan = new ConstellationPlan(plan.Entries);

        ConstellationPlanEntry currentSlot = newPlan.Entries.Find(entry => entry.NodeID != null && entry.NodeID == myNode.ID);
        
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
