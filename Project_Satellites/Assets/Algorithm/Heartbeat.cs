using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Heartbeat
{
    public void CheckHeartbeat(INode myNode)
    {
            Node.NodeState previousState = myNode.State;
            myNode.State = Node.NodeState.HEARTBEAT;
        
            //TODO: Set up with new comms system
            //foreach (INode node in myNode.Router.NetworkMap[myNode].ToList()) // Should just communicate with reachable nodes instead of using networkmap
            //{
            //    if (node.Communicate(Request.Commands.Heartbeat) == false)
            //    {
            //        FailureDetection(node);
            //    }
            //}

            myNode.State = previousState;
    }

}
