using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlanExecuter : MonoBehaviour
{

    public static void ExecutePlan(INode myNode, PlanRequest request)
    {

        new Thread(delegate ()

        {

            if (request.DestinationID != myNode.ID)
                return;

            if (request.Command != Request.Commands.Execute)
            {
                throw new Exception("Wrong command"); // Only accept Execute command
            }

            if (request.DestinationID == myNode.ID)
            {

                myNode.State = Node.NodeState.EXECUTING;
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
                    myNode.Router = new Router(request.Plan);
                }

                uint? nextSeq = myNode.Router.NextSequential(myNode.ID);
                request.DestinationID = nextSeq;
                uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
                myNode.CommsModule.Send(nextHop, request);

                myNode.Router.UpdateNetworkMap(request.Plan);

            }
            //else TODO: DO WE NEED THIS BIT?
            //{
            //    uint? nextHop = myNode.Router.NextHop(myNode.ID, request.DestinationID);
            //    request.DestinationID = nextHop;
            //    myNode.CommsModule.Send(request);
            //}


        }).Start();
    }

}

