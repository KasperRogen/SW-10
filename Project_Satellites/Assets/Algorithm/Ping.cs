using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ping
{


    public static void RespondToPing(Node myNode, Request request)
    {
        if (request.DestinationID != myNode.ID)
            return;

        uint? nextHop = myNode.Router.NextHop(myNode.ID, request.SourceID);
        myNode.CommsModule.Send(nextHop, new Response(myNode.ID, request.SourceID, Response.ResponseCodes.OK, request.MessageIdentifer));
    }
}
