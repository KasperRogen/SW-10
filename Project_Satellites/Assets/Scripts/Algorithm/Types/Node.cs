using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Threading.Timer;

public class Node : INode
{
    public enum NodeState { PASSIVE, PLANNING, OVERRIDE, EXECUTING, DEAD, HEARTBEAT, DISCOVERY };
    public override uint? Id { get; set; }
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

            State = value ? NodeState.PASSIVE : NodeState.DEAD;
        }
    }

    public override bool AutoChecksAllowed { get; set; }
    public override int SleepCount { get; set; }
    public override ConstellationPlan ActivePlan { get; set; }
    public override ConstellationPlan GeneratingPlan { get; set; }
    public override NodeState State { get; set; }
    public override Router Router { get; set; }

    private bool active;

    private System.Timers.Timer heartbeatTimer, discoveryTimer;

    public Node(uint? ID, Vector3 position)
    {
        this.Id = ID;
        State = Node.NodeState.PASSIVE;
        Position = position;
        Active = true;
        GenerateRouter();
        MainThread();

        heartbeatTimer = new System.Timers.Timer();
        discoveryTimer = new System.Timers.Timer();
    }

    private void MainThread()
    {
        new Thread(() =>
        {
            Task.Delay((int)Id * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TimeScale).ContinueWith(t => SetupHeartbeat());
            Task.Delay((int)Id * Constants.ONE_MINUTE_IN_MILLISECONDS * 2 / Constants.TimeScale).ContinueWith(t => SetupDiscovery());


            bool run = true;
            while (run)
            {
                Thread.Sleep(1000 / Constants.TimeScale);
                Request request = CommsModule.FetchNextRequest();
                if (request != null)
                {
                    Communicate(request);
                }
            }
        }).Start();
    }

    private void SetupHeartbeat()
    {
        heartbeatTimer.Interval = Constants.NODES_PER_CYCLE * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TimeScale;
        heartbeatTimer.Elapsed += OnHeartbeatEvent;
        heartbeatTimer.Enabled = true;
    }

    private void OnHeartbeatEvent(Object source, ElapsedEventArgs e)
    {
        if (State == NodeState.PASSIVE && AutoChecksAllowed)
        {
            Heartbeat.CheckHeartbeat(this);
        }
    }

    private void SetupDiscovery()
    {
        discoveryTimer.Interval = Constants.NODES_PER_CYCLE * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TimeScale;
        discoveryTimer.Elapsed += OnDiscoveryEvent;
        discoveryTimer.Enabled = true;
    }

    private void OnDiscoveryEvent(Object source, ElapsedEventArgs e)
    {
        if (State == NodeState.PASSIVE && AutoChecksAllowed)
        {
            Discovery.StartDiscovery(this, false);
        }
    }

    public override void GenerateRouter()
    {
        Router = new Router(this, ActivePlan);
        if (Router.NetworkMap.Entries.Select(entry => entry.ID).Contains(Id) == false)
        {
            Router.NetworkMap.Entries.Add(new NetworkMapEntry(Id, new List<uint?>(), Position));
        }
    }


    public override async void Communicate(Request request)
    {

        if (active == false)
            return;

            if (request.DependencyRequests != null && request.DependencyRequests.Any())
            {
                foreach (Request dependencyRequest in request.DependencyRequests)
                {
                    ExecuteRequest(dependencyRequest, true);
                }
            }



            ExecuteRequest(request, false);

            if (request.DestinationID != Id)
            {
                if (Router.NetworkMap.GetEntryByID(Id).Neighbours.Contains(request.DestinationID))
                {
                    await CommsModule.SendAsync(request.DestinationID, request, Constants.COMMS_TIMEOUT, 3);
                }
                else
                {
                    uint? nextHop = Router.NextHop(Id, request.DestinationID);

                    await CommsModule.SendAsync(nextHop, request, Constants.COMMS_TIMEOUT, 3);
                }
            }

    }

    public override void ResetTimers()
    {
        discoveryTimer.Stop();
        heartbeatTimer.Stop();

        Task.Delay((int)Id * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TimeScale);
        Task.Delay((int)Id * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TimeScale);


        discoveryTimer.Interval = Constants.NODES_PER_CYCLE * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TimeScale;
        discoveryTimer.Start();


        heartbeatTimer.Interval = Constants.NODES_PER_CYCLE * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TimeScale;
        heartbeatTimer.Start();
    }

    private async void ExecuteRequest(Request request, bool isDependency)
    {
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
                PositionResponse response = new PositionResponse(Id, request.SourceID, Response.ResponseCodes.OK, request.MessageIdentifer, Position);
                CommsModule.Send(request.SourceID, response);
                break;

            case Request.Commands.ADDITION:

                List<uint?> neighbours = CommsModule.Discover();
                AdditionRequest addition = (request as AdditionRequest).DeepCopy();

                List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => x.CompareTo(y)) };
                addition.plan.Entries.Add(new ConstellationPlanEntry(Id, Position, fields, (x, y) => 1));
                ActivePlan = addition.plan;
                Router.UpdateNetworkMap(addition.plan);

                NodeAdditionResponse additionResponse = new NodeAdditionResponse(Id, request.SourceID, Response.ResponseCodes.OK, request.MessageIdentifer, Position, neighbours);
                CommsModule.Send(request.SourceID, additionResponse);
                break;

            case Request.Commands.UPDATENETWORKMAP:
                NetworkUpdateRequest updateRequest = request as NetworkUpdateRequest;

                //Remove any dead nodes from the networkmap
                Router.NetworkMap.Entries.RemoveAll(entry => updateRequest.DeadNodes.Contains(entry.ID));

                //Remove any dead nodes from neighbour lists
                foreach (NetworkMapEntry entry in Router.NetworkMap.Entries)
                {
                    foreach (uint? deadNode in updateRequest.DeadNodes)
                    {
                        entry.Neighbours.Remove(deadNode);
                    }
                }
                break;

            default:
                throw new NotImplementedException(request.Command.ToString() + " was not implemented.");
        }
    }
}
