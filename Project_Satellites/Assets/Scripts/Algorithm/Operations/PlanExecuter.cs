using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlanExecuter : MonoBehaviour
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

            //Set my targetposition to the position i was assigned in the plan
            myNode.TargetPosition = request.Plan.Entries.Find(entry => entry.NodeID == myNode.ID).Position;

            if (myNode.executingPlan)
            {
                return; // Ignore Execute command if already executing which stops the execute communication loop
            }
            else
            {
                myNode.executingPlan = true;
            }

            if (myNode.Router == null)
            {
                myNode.Router = new Router(myNode, request.Plan);
            }


            PlanRequest newRequest = request.DeepCopy();

            newRequest.SenderID = myNode.ID;
            uint? nextSeq = myNode.Router.NextSequential(myNode);

            newRequest.SourceID = myNode.ID;
            newRequest.DestinationID = nextSeq;
            uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
            myNode.CommsModule.Send(nextHop, newRequest);


            myNode.ActivePlan = newRequest.Plan;

            myNode.Router.UpdateNetworkMap(newRequest.Plan);



        }

    }

}

