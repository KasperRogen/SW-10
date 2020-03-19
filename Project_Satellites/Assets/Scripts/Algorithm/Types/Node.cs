using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;using System.Timers;
public class Node : INode
{
    public enum NodeState { PASSIVE, PLANNING, OVERRIDE, EXECUTING, DEAD, HEARTBEAT, DISCOVERY };
    public override uint? ID { get; set; }
    public override List<uint?> ReachableNodes { get; set; } // Future Work: Make part of the algorithm that reachable nodes are calculated based on position and a communication distance
    public override Vector3 Position { get; set; }
    public override Vector3 TargetPosition { get; set; }

    public override bool Active
    {
        get 
        {
            return active;
        }
        set
        {
            active = value;

            if (value)
            {
                State = NodeState.PASSIVE;
            }
            else
            {
                State = NodeState.DEAD;
            }
        }
    }
    public override ConstellationPlan ActivePlan { get; set; }
    public override ConstellationPlan GeneratingPlan { get; set; }
    public override NodeState State { get; set; }
    public override Router Router { get; set; }

    private bool active;

    public Node(uint? ID, Vector3 position)
    {
        this.ID = ID;
        State = Node.NodeState.PASSIVE;
        Position = position;
        Active = true;
        MainThread();
    }
    private void MainThread()
    {
        new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(1000);
                Request request = CommsModule.FetchNextRequest();
                if(request != null)
                {
                    Communicate(request);
                }
            }
        }).Start();
    }
   
   
    public override void GenerateRouter()
    {
        Router = new Router(this, ActivePlan);
        if(Router.NetworkMap.Entries.Select(entry => entry.ID).Contains(ID) == false)
        {
            Router.NetworkMap.Entries.Add(new NetworkMapEntry(ID, new List<uint?>(), Position));
        }
    }


    public override bool Equals(object obj)
    {
        return ID == (obj as INode).ID;
    }


    public override void Communicate(Request request)
    {
        new Thread(async () =>
        {
            ThreadCount++;
            IsBusy = true;
            switch (request.Command)
            {
                case Request.Commands.GENERATE:
                    PlanGenerator.GeneratePlan(this, request as PlanRequest);
                    break;

                case Request.Commands.EXECUTE:
                    PlanExecuter.ExecutePlan(this, request as PlanRequest);
                    break;

                case Request.Commands.HEARTBEAT:
                    Heartbeat.RespondToHeartbeat(this, request);
                    break;

                case Request.Commands.PING:
                    Ping.RespondToPing(this, request);
                    break;

                case Request.Commands.DETECTFAILURE:
                    FailureDetection.DetectFailure(this, request as DetectFailureRequest);
                    break;

                case Request.Commands.DISCOVER:
                    await Discovery.DiscoverAsync(this, request as DiscoveryRequest);
                    break;

                case Request.Commands.POSITION:
                    Router.UpdateGraph();
                    PositionResponse response = new PositionResponse(ID, request.SourceID, Response.ResponseCodes.OK, request.MessageIdentifer, Position);
                    Router.AddNodeToGraph(request.SourceID);
                    uint? nextHop = Router.NextHop(ID, response.DestinationID);
                    CommsModule.Send(nextHop, response);
                    break;

                default:
                    throw new NotImplementedException(request.Command.ToString() + " was not implemented.");
            }


            if (request.DestinationID != ID)
            {
                if (Router.NetworkMap.GetEntryByID(ID).Neighbours.Contains(request.DestinationID))
                {
                    CommsModule.Send(request.DestinationID, request);
                }
                else
                {
                    uint? nextHop = Router.NextHop(ID, request.DestinationID);

                    if (nextHop == null)
                        throw new Exception("CANNOT FIND THE GUY");

                    CommsModule.Send(nextHop, request);
                }
            }
            IsBusy = false;

            ThreadCount--;
        }).Start();
            
    }
}
