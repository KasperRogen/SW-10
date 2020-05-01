using System;
using System.Collections.Generic;
using System.Linq;



public class Request
{
    public enum Commands
    {
        GENERATE, EXECUTE, DETECTFAILURE, HEARTBEAT, PING, DISCOVER, POSITION, ADDITION
    }

    public uint? SourceID { get; set; }
    public uint? DestinationID { get; set; }
    public uint? SenderID { get; set; }
    public Commands Command { get; set; }

    public string MessageIdentifer { get; set; }


    public bool AckExpected { get; set; }
    public bool ResponseExpected { get; set; }

    public Router.CommDir Dir { get; set; }



    public Request() {

        MessageIdentifer = DateTime.Now.ToString() + " milli " + DateTime.Now.Millisecond;

    }



    public Request(Request other) {

        SourceID = other.SourceID;

        DestinationID = other.DestinationID;

        SenderID = other.SenderID;

        Command = other.Command;

        MessageIdentifer = string.Copy(other.MessageIdentifer);

        ResponseExpected = other.ResponseExpected;
        AckExpected = other.AckExpected;

        Dir = other.Dir;

    }



    public Request(uint? sourceID, uint? destinationID, Commands command)

    {

        SourceID = sourceID;

        SenderID = sourceID;

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



    public PlanRequest DeepCopy()

    {

        return new PlanRequest(this);

    }

}

public class DiscoveryRequest : Request

{

    public List<NetworkMapAlteration> Alterations { get; set; }




    public DiscoveryRequest(DiscoveryRequest other) : base(other) {

        Alterations = other.Alterations; //TODO: THIS MIGHT BREAK STUFF

    }



    public DiscoveryRequest() {


        SenderID = SourceID;
    }



    public DiscoveryRequest DeepCopy()

    {

        return new DiscoveryRequest(this);

    }

}

public class DetectFailureRequest : Request

{

    public uint? NodeToCheck { get; set; }

    public List<Tuple<uint?, uint?>> DeadEdges { get; set; }



    public DetectFailureRequest() {



    }



    public DetectFailureRequest(DetectFailureRequest other) : base(other) {

        NodeToCheck = other.NodeToCheck;

        DeadEdges = other.DeadEdges.ToList().ConvertAll(x => new Tuple<uint?, uint?>(x.Item1, x.Item2));

    }



    public DetectFailureRequest DeepCopy()

    {

        return new DetectFailureRequest(this);

    }



    



}

public class AdditionRequest : Request
{
    public ConstellationPlan plan;
    public AdditionRequest(AdditionRequest other) : base(other)
    {
        plan = other.plan; //TODO: THIS MIGHT BREAK STUFF
    }

    public AdditionRequest()
    {
        SenderID = SourceID;
    }

    public AdditionRequest DeepCopy()
    {
        return new AdditionRequest(this);
    }
}

