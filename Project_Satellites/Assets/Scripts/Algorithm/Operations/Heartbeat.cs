using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Heartbeat
{


    /// <summary>Performs aliveness check on immidiate neighbours
    /// <para>  </para>
    /// </summary>
    public async static void CheckHeartbeat(INode myNode)
    {
        Node.NodeState previousState = myNode.State;
        myNode.State = Node.NodeState.HEARTBEAT;

        //Loop through all immidate neightbours
        foreach (uint? node in myNode.Router.NetworkMap[myNode.ID].ToList()) //TODO: Should just communicate with reachable nodes instead of using networkmap
        {
            Request request = new Request();
            request.SourceID = myNode.ID;
            request.Command = Request.Commands.Heartbeat;
            request.DestinationID = node;

            Response response = await myNode.CommsModule.SendAsync(node, request, 5000);
            if (response == null || response.ResponseCode == Response.ResponseCodes.ERROR)
            {
                FailureDetection.FailureDetected(myNode, node);
            }
        }

        myNode.State = previousState;
    }


    /// <summary>Responds responsecode "OK" to heartbeat
    /// <para>  </para>
    /// </summary>
    internal static void RespondToHeartbeat(Node myNode, Request request)
    {
        new Thread(() =>
        {
            if (request.DestinationID != myNode.ID)
                return;
            Response response = new Response();
            response.DestinationID = request.SourceID;
            response.ResponseCode = Response.ResponseCodes.OK;
            myNode.CommsModule.Send(response.DestinationID, response);
        }).Start();

    }
}
