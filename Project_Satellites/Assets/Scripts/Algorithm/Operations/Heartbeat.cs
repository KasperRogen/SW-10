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
        foreach (uint? node in myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours.ToList()) //TODO: Should just communicate with reachable nodes instead of using networkmap
        {
            Request request = new Request()
            {
                SourceID = myNode.ID,
                DestinationID = node,
                SenderID = myNode.ID,
                Command = Request.Commands.HEARTBEAT,
                AckExpected = true,
                ResponseExpected = true
            };

            Response response = await myNode.CommsModule.SendAsync(node, request, 3000, 3);

            if (response.ResponseCode == Response.ResponseCodes.TIMEOUT) {
                break;
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
            myNode.ThreadCount++;
            if (request.DestinationID != myNode.ID)
                return;

            Thread.Sleep(500 / Constants.TimeScale);
            Response response = new Response()
            { 
                SourceID = myNode.ID,
                DestinationID = request.SenderID,
                ResponseCode = Response.ResponseCodes.OK,
                MessageIdentifer = request.MessageIdentifer
            };
            myNode.CommsModule.Send(response.DestinationID, response);

            myNode.ThreadCount--;
        }).Start();
    }
}
