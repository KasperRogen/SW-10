using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FailureDetection
{

    public void DetectFailure(INode myNode, DetectFailureRequest request)
    {
        //TODO: Set this up to non non node-based system
        //myNode.router.DeleteEdge(request.SourceID, request.NodeToCheck);

        myNode.State = request.isDead == null ? Node.NodeState.EXECUTING : Node.NodeState.PASSIVE;

        if (myNode.ID == request.SourceID)
        {
            if (request.isDead != null && request.isDead == true)
            {
                TargetConstellationGenerator.instance.GenerateTargetConstellation();
                return;
            }
            else
            {
                return;
            }
        }

        if (myNode.ID != request.DestinationID) // check if other dead sat, otherwise relay.
        {
            uint? nextHopTarget = request.isDead == null ? request.DestinationID: request.SourceID;
            
            //TODO: set this up with the new comms system
            //bool response = myNode.router.NextHop(myNode, nextHopTarget).Communicate(Request.Commands.DetectFailure, source, target, deadNode, isDead, isChecked);

            // Handle additional dead satellites
        }
        else if (myNode.ID == request.DestinationID)
        {
            bool response;
            //TODO: Set this up with burla's new non node-based system
            //if (myNode.router.NetworkMap[myNode].Contains(request.NodeToCheck) == false)
            //{
            //    //link is broken, we cannot communicate
            //    response = true;
            //}
            //else
            //{
            //    // Get response
                
            //    //TODO: Set this up with the new comms system
            //    //response = router.NextHop(this, deadNode).Communicate(Request.Commands.DetectFailure, source, target, deadNode, isDead, isChecked);
            //}

            // Relay response opposite way
            //TODO: Set this up with the new comms system
            //router.NextHop(this, source).Communicate(Request.Commands.DetectFailure, source, target, deadNode, response, true);
        }
        

    }

    //TODO: set this up with non node-based system and new comms system
    public void FailureDetected(INode myNode, uint? failedNode)
    {
        // myNode.router.DeleteEdge(myNode.ID, failedNode);

        //uint? neighbourID = myNode.router.NetworkMap[failedNode][0];

        //myNode.router.NextHop(this, neighbour).Communicate(Request.Commands.DetectFailure, this, neighbour, failedNode, false, false);
    }

}
