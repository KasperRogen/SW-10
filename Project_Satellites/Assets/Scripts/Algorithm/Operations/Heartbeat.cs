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
    public static async void CheckHeartbeat(INode myNode)
    {
        Node.NodeState previousState = myNode.State;
        myNode.State = Node.NodeState.HEARTBEAT;

        List<uint?> Neighbours = myNode.Router.NetworkMap.GetEntryByID(myNode.Id).Neighbours.ToList();
        //Loop through all immidate neightbours
        foreach (uint? node in Neighbours)
        {
            Request request = new Request()
            {
                SourceID = myNode.Id,
                DestinationID = node,
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
            if (request.DestinationID != myNode.Id)
                return;

            Thread.Sleep(500 / Constants.TimeScale);
            Response response = new Response()
            { 
                SourceID = myNode.Id,
                DestinationID = request.SenderID,
                ResponseCode = Response.ResponseCodes.OK,
                MessageIdentifer = request.MessageIdentifer
            };
            myNode.CommsModule.Send(response.DestinationID, response);
            
        }).Start();
    }
}
