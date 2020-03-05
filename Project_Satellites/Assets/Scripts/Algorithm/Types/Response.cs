using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Response 
{
    public enum ResponseCodes
    {
        OK, ERROR
    }

    public Response(uint? _sourceID, uint? _destinationID, ResponseCodes _responsecode, string _messageIdentifier)
    {
        this.SourceID = _sourceID;
        this.DestinationID = _destinationID;
        this.ResponseCode = _responsecode;
        this.MessageIdentifer = _messageIdentifier;
    }

    public Response()
    {

    }




    public ResponseCodes ResponseCode { get; set; }
    public uint? SourceID { get; set; }
    public uint? DestinationID { get; set; }
    public string MessageIdentifer { get; set; }


}


public class FailureDetectionResponse : Response
{

    public List<Tuple<uint?, uint?>> DeadEdges { get; set; }

    public FailureDetectionResponse(uint? _sourceID, uint? _destinationID, Response.ResponseCodes _responsecode, string _messageIdentifier, List<Tuple<uint?, uint?>> _deadEdges)
    {
        this.SourceID = _sourceID;
        this.DestinationID = _destinationID;
        this.ResponseCode = _responsecode;
        this.MessageIdentifer = _messageIdentifier;
        this.DeadEdges = _deadEdges;
    }
}
