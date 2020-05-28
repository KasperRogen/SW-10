using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public static class Heartbeat {
    public async static void CheckHeartbeat(INode myNode)
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
                AckExpected = true, // TODO: Neccesary to both expect ack and response? Maybe just expect response?
                ResponseExpected = true
            };

            Response response = await myNode.CommsModule.SendAsync(node, request, 3000, 3);

            if (response.ResponseCode == Response.ResponseCodes.TIMEOUT) {
                break;
            }
        }

        myNode.State = previousState;
    }

    public static void RespondToHeartbeat(Node myNode, Request request)
    {
        if (request.DestinationID != myNode.Id) {
            return;
        }

        Thread.Sleep(500 / Constants.TIME_SCALE);
        Response response = new Response() {
            SourceID = myNode.Id,
            DestinationID = request.SenderID,
            ResponseCode = Response.ResponseCodes.OK,
            MessageIdentifer = request.MessageIdentifer
        };
        myNode.CommsModule.Send(response.DestinationID, response);

        new Thread(() =>
        {
            
            
        }).Start();
    }
}
