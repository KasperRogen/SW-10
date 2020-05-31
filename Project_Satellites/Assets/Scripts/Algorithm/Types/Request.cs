using System;
using System.Collections.Generic;
using System.Linq;



public class Request
{
    public enum Commands
    {
        GENERATE, EXECUTE, DETECTFAILURE, HEARTBEAT, PING, DISCOVER, POSITION, ADDITION, UPDATENETWORKMAP
    }

    public uint? SourceID { get; set; }
    public uint? DestinationID { get; set; }
    public uint? SenderID { get; set; }
    public Commands Command { get; set; }

    public string MessageIdentifer { get; set; }

    public List<Request> DependencyRequests = new  List<Request>();


    public bool AckExpected { get; set; }
    public bool ResponseExpected { get; set; }

    public Router.CommDir Dir { get; set; }



    public Request() {

        MessageIdentifer = DateTime.Now.ToString() + " milli " + DateTime.Now.Millisecond;

    }



    public Request(Request other) {

        SourceID = other.SourceID;

        DestinationID = other.DestinationID;
        

        Command = other.Command;

        MessageIdentifer = string.Copy(other.MessageIdentifer);

        ResponseExpected = other.ResponseExpected;
        AckExpected = other.AckExpected;

        Dir = other.Dir;

        DependencyRequests = other.DependencyRequests;

    }



    public Request(uint? sourceID, uint? destinationID, Commands command)

    {

        SourceID = sourceID;
        

        DestinationID = destinationID;

        Command = command;

        Dir = Router.CommDir.CW;

        MessageIdentifer = DateTime.Now.ToString() + " milli " + DateTime.Now.Millisecond;

    }



    public Request DeepCopy()

    {

        return new Request(this);

    }

}

public class PlanRequest : Request

{

    public ConstellationPlan Plan { get; set; }



    public PlanRequest() {



    }



    public PlanRequest(PlanRequest other) : base(other) {

        Plan = other.Plan.DeepCopy();

    }



    public new PlanRequest DeepCopy()

    {

        return new PlanRequest(this);

    }

}

public class DiscoveryRequest : Request

{

    public List<NetworkMapAlteration> Alterations { get; set; }
    public bool firstPassDone { get; set; }
    public bool requireFullSync { get; set; }
    public bool EdgeDetected { get; set; }




    public DiscoveryRequest(DiscoveryRequest other) : base(other) {

        Alterations = other.Alterations;
        firstPassDone = other.firstPassDone;
        requireFullSync = other.requireFullSync;
        EdgeDetected = other.EdgeDetected;
    }



    public DiscoveryRequest() {

        
        firstPassDone = false;
    }



    public new DiscoveryRequest DeepCopy()

    {

        return new DiscoveryRequest(this);

    }

}

public class DetectFailureRequest : Request
{
    public uint? NodeToCheck { get; set; }
    public List<Tuple<uint?, uint?>> DeadEdges { get; set; }
    public List<uint?> FailedNeighbours { get; set; } // Neighbours to NodeToCheck that have already tried contacting it without success

    public DetectFailureRequest() {

    }

    public DetectFailureRequest(DetectFailureRequest other) : base(other) {
        NodeToCheck = other.NodeToCheck;
        DeadEdges = other.DeadEdges.ToList().ConvertAll(x => new Tuple<uint?, uint?>(x.Item1, x.Item2));
        FailedNeighbours = new List<uint?>(other.FailedNeighbours);
    }



    public new DetectFailureRequest DeepCopy()
    {
        return new DetectFailureRequest(this);
    }



    



}


public class NetworkUpdateRequest : Request
{
    public List<uint?> DeadNodes;

    public new NetworkUpdateRequest DeepCopy()
    {
        return new NetworkUpdateRequest(this);
    }

    public NetworkUpdateRequest(List<uint?> deadNodes)
    {
        DeadNodes = deadNodes;
        Command = Commands.UPDATENETWORKMAP;
    }

    public NetworkUpdateRequest(NetworkUpdateRequest other) : base(other)
    {
        DeadNodes = other.DeadNodes;
    }
}


public class AdditionRequest : Request
{
    public ConstellationPlan plan;
    public AdditionRequest(AdditionRequest other) : base(other)
    {
        plan = other.plan;
    }

    public AdditionRequest()
    {
    }

    public new AdditionRequest DeepCopy()
    {
        return new AdditionRequest(this);
    }
}

