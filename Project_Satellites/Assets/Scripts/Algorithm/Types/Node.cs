using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
    public override ConstellationPlan ActivePlan { get; set; }
    public override ConstellationPlan GeneratingPlan { get; set; }
    public override NodeState State { get; set; }
    public override Router Router { get; set; }

    private bool active;

    public Node(uint? ID, Vector3 position)
    {
        this.Id = ID;
        State = Node.NodeState.PASSIVE;
        Position = position;
        Active = true;
        GenerateRouter();
        MainThread();
    }

    private void MainThread()
    {
        new Thread(() =>
        {
            Task.Delay((int)Id * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TIME_SCALE).ContinueWith(t => SetupHeartbeat());
            Task.Delay((int)Id * Constants.ONE_MINUTE_IN_MILLISECONDS * 2 / Constants.TIME_SCALE).ContinueWith(t => SetupDiscovery());

            bool run = true;
            while (run)
            {
                Thread.Sleep(1000 / Constants.TIME_SCALE);
                Request request = CommsModule.FetchNextRequest();
                if(request != null)
                {
                    Communicate(request);
                }
            }
        }).Start();
    }

    private void SetupHeartbeat() {
        System.Timers.Timer timer = new System.Timers.Timer();
        timer.Interval = Constants.NODES_PER_CYCLE * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TIME_SCALE;
        timer.Elapsed += OnHeartbeatEvent;
        timer.Enabled = true;
    }

    private void OnHeartbeatEvent(Object source, ElapsedEventArgs e) {
        UnityEngine.Debug.Log("Heartbeat");
        Heartbeat.CheckHeartbeat(this);
    }

    private void SetupDiscovery() {
        System.Timers.Timer timer = new System.Timers.Timer();
        timer.Interval = Constants.NODES_PER_CYCLE * Constants.ONE_MINUTE_IN_MILLISECONDS / Constants.TIME_SCALE;
        timer.Elapsed += OnDiscoveryEvent;
        timer.Enabled = true;
    }

    private void OnDiscoveryEvent(Object source, ElapsedEventArgs e) {
        UnityEngine.Debug.Log("Discovery");
        Discovery.StartDiscovery(this, false);
    }

    public override void GenerateRouter()
    {
        Router = new Router(this, ActivePlan);
        if(Router.NetworkMap.Entries.Select(entry => entry.ID).Contains(Id) == false)
        {
            Router.NetworkMap.Entries.Add(new NetworkMapEntry(Id, new List<uint?>(), Position));
        }
    }


    public override void Communicate(Request request)
    {

        if (active == false)
            return;

        new Thread(async () =>
        {
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
                    await CommsModule.SendAsync(request.DestinationID, request, 1000, 3);
                }
                else
                {
                    uint? nextHop = Router.NextHop(Id, request.DestinationID);

                    await CommsModule.SendAsync(nextHop, request, 1000, 3);
                }
            }
        }).Start();
            
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
