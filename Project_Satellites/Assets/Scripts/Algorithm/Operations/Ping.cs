using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ping
{



    /// <summary>Replies to requests with ping command
    /// <para>Used by sender node to check aliveness of node in failureDetection Operation</para>
    /// </summary>
    public static void RespondToPing(Node myNode, Request request)
    {
        if (request.DestinationID != myNode.Id)
            return;

        uint? nextHop = myNode.Router.NextHop(myNode.Id, request.SourceID);
        myNode.CommsModule.Send(nextHop, new Response(myNode.Id, request.SourceID, Response.ResponseCodes.OK, request.MessageIdentifer));
    }
}
