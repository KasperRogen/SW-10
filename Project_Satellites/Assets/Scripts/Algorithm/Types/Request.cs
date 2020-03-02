using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Request
{

    public enum Commands
    {
        Generate, Execute, DetectFailure, Heartbeat, Ping
    }

    public uint? SourceID { get; set; }
    public uint? DestinationID { get; set; }
    public Commands Command { get; set; }
    public string MessageIdentifer { get; set; }
}

public class PlanRequest : Request
{
    public ConstellationPlan Plan { get; set; }
}

public class DiscoveryRequest: Request
{
    public List<Tuple<uint?, uint?>> EdgeSet { get; set; }
}


public class DetectFailureRequest: Request
{
    public uint? NodeToCheck { get; set; }
    public bool? isDead { get; set; }
}