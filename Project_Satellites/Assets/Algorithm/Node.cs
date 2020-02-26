using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;using System.Timers;

public class Node : INode
{
    public enum NodeState { PASSIVE, PLANNING, OVERRIDE, EXECUTING, DEAD, HEARTBEAT };
    public override uint? ID { get; set; }
    public override List<uint?> ReachableNodes { get; set; } // Future Work: Make part of the algorithm that reachable nodes are calculated based on position and a communication distance
    public override Position Position { get; set; }
    public override Position TargetPosition { get; set; }
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
    public override ConstellationPlan Plan { get; set; }
    public override NodeState State { get; set; }
    public override Router Router { get; set; }

    private List<Tuple<uint?, uint?>> EdgeSet;
    private List<Tuple<uint?, uint?>> CurrentKnownEdges = new List<Tuple<uint?, uint?>>();
    private int LastDiscoverID = -1;
    private bool executingPlan;
    private bool justChangedPlan;
    private System.Timers.Timer timer;
    private bool active;

    public Node(uint? ID, Position position)
    {
        this.ID = ID;
        State = Node.NodeState.PASSIVE;
        Position = position;
        Active = true;
        timer = new System.Timers.Timer(5000);
        timer.Elapsed += OnTimedEvent;
        timer.AutoReset = true;
        timer.Enabled = true;
    }
    public override bool Communicate(Constants.Commands command)
    {
        if (!Active)
        {
            return false;
        }

        return true;
    }
    public override bool Communicate(Constants.Commands command, uint? target)
    {

        if (!Active)
        {
            return false;
        }

        new Thread(delegate ()
        {


            if (command != Constants.Commands.Execute)
            {
                throw new Exception("Wrong command"); // Only accept Execute command
            }

            if(target == ID)
            {

                State = Node.NodeState.EXECUTING;
                TargetPosition = Plan.Entries.Find(entry => entry.NodeID == ID).Position;

                if (executingPlan)
                {
                    return; // Ignore Execute command if already executing which stops the execute communication loop
                }
                else
                {
                    executingPlan = true;
                }

                if (Router == null)
                {
                    Router = new Router(Plan);
                }

                uint? nextSeq = Router.NextSequential(ID);
                TryCommunicate(Constants.Commands.Execute, nextSeq, nextSeq);

                Router.UpdateNetworkMap(Plan);

            }
            else
            {
                uint? nextHop = Router.NextHop(ID, target);
                TryCommunicate(command, nextHop, target);
            }


        }).Start();

        return true;
    }

    public override bool Communicate(Constants.Commands command, ConstellationPlan plan, uint? target)
    {
        if (Active == false)
        {
            return false;
        }

        new Thread(delegate ()
        {
            if (command != Constants.Commands.Generate)
            {
                throw new Exception("Wrong command"); // Only accept Generate command
            }

            if (target == ID)
            {
                executingPlan = false;

                Plan = plan;

                State = Node.NodeState.PLANNING;

                Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

                for (int i = 0; i < plan.Entries.Count; i++)
                {
                    float requiredDeltaV = Position.Distance(Position, plan.Entries[i].Position);
                    fieldDeltaVPairs.Add(i, requiredDeltaV);
                }

                ConstellationPlan newPlan = null;

                if (plan.Entries.Any(entry => entry.NodeID == ID) == false)
                {
                    ConstellationPlanEntry slotToTake = plan.Entries.Where(entry => entry.NodeID == null) //Only allow satellite to take free locations
                    .Aggregate((CurrentBest, currentTest) => //Iterate each entry
                    Position.Distance(currentTest.Position, this.Position) <=  //This entry currently being tested to improve over current best
                    Position.Distance(CurrentBest.Position, this.Position) ?  //current best 
                    currentTest : CurrentBest); //return best candidate of currenttest and currentbest

                    newPlan = TakeSlot(plan, plan.Entries.IndexOf(slotToTake), Position.Distance(slotToTake.Position, this.Position));
                }
                else if (plan.Entries.Any(entry => entry.NodeID == null) == false)
                {

                    foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))
                    {
                        if (plan.ReduceBy("DeltaV", pair.Key, pair.Value, ID))
                        {
                            if (plan.Entries[pair.Key].NodeID != null && plan.Entries[pair.Key].NodeID != ID)
                            {
                                State = NodeState.OVERRIDE;
                            }

                            newPlan = TakeSlot(plan, pair.Key, pair.Value);
                            break;
                        }
                    }
                }

                if (newPlan != null && newPlan != plan)
                {
                    plan = newPlan;
                    justChangedPlan = true;
                    plan.LastEditedBy = ID;

                    this.Plan = plan;
                    Thread.Sleep(1000);
                }
                else
                {
                    Thread.Sleep(250);
                }


                if (plan.LastEditedBy == ID && justChangedPlan == false)
                {
                    State = Node.NodeState.EXECUTING;
                    Communicate(Constants.Commands.Execute, ID);
                }

                else
                {
                    justChangedPlan = false;
                    State = Node.NodeState.PASSIVE;
                    uint? nextSeq = Router.NextSequential(ID);
                    if (Router.NetworkMap[ID].Contains(nextSeq))
                    {
                        TryCommunicate(Constants.Commands.Generate, plan, nextSeq, nextSeq);
                    }
                    else
                    {
                        TryCommunicate(Constants.Commands.Generate, plan, Router.NextHop(ID, nextSeq), nextSeq);
                    }

                }


            }
            else
            {
                Thread.Sleep(500);
                State = Node.NodeState.PLANNING;
                uint? nextHop = Router.NextHop(ID, target);
                TryCommunicate(command, plan, nextHop, target);
            }

        }).Start();

        return true;
    }

    public override bool Communicate(Constants.Commands command, uint? source, uint? target, uint? deadNode, bool isDead, bool isChecked)
    {
        if (command != Constants.Commands.DetectFailure)
        {
            throw new Exception("Wrong command:: Expected DetectFailure command");
        }

        if (!Active)
        {
            return false;
        }

        new Thread(delegate ()
        {
            Thread.Sleep(500);
            Router.DeleteEdge(source, deadNode);

            this.State = isChecked ? NodeState.PASSIVE : NodeState.EXECUTING;

            if (source == ID)
            {
                if (isDead == true)
                {
                    TargetConstellationGenerator.instance.GenerateTargetConstellation();
                    return;
                }
                else
                {
                    return;
                }
            }

            if (target != ID) // check if other dead sat, otherwise relay.
            {
                uint? nextHopTarget = isChecked ? source : target;
                bool response = Router.NextHop(ID, nextHopTarget).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, isDead, isChecked);

                // Handle additional dead satellites
            }
            else if (target == ID)
            {
                bool response;
                if (Router.NetworkMap[ID].Contains(deadNode) == false)
                {
                    //link is broken, we cannot communicate
                    response = true;
                }
                else
                {
                    // Get response
                    response = !Router.NextHop(ID, deadNode).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, isDead, isChecked);
                }


                // Relay response opposite way
                Router.NextHop(ID, source).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, response, true);
            }
        }).Start();

        return true;

    }

    private void TryCommunicate(Constants.Commands command, ConstellationPlan plan, uint? source, uint? destination)
    {
        if (!source.Communicate(command, plan, destination))
        {
            FailureDetection(source);
        }
    }

    private void TryCommunicate(Constants.Commands command, uint? source, uint? destination)
    {
        if (!source.Communicate(command, destination))
        {
            FailureDetection(source);
        }
    }

    public override void GenerateRouter()
    {
        Router = new Router(Plan);
    }

    public override void Discover(List<Tuple<uint?, uint?>> ReceivedEdgeSet, uint? sender, int discoverID)
    {
        bool newKnowledge = false;
        bool alteredSet = false;

        if (discoverID > LastDiscoverID)
        {
            CurrentKnownEdges.Clear();
            LastDiscoverID = discoverID;
        }

        //List<Tuple<INode, INode>> temp = ReceivedEdgeSet.Except(CurrentKnownEdges, (x y) => return 1).ToList();
        //newKnowledge = temp.Count() > 0;

        foreach (uint? node in ReachableNodes)
        {
            Tuple<uint?, uint?> edge = new Tuple<uint?, uint?>(ID, node);

            edge = edge.Item1 > edge.Item2 ? new Tuple<uint?, uint?>(edge.Item2, edge.Item1) : edge;


            if (ReceivedEdgeSet.Contains(edge) == false)
            {
                ReceivedEdgeSet.Add(edge);
                alteredSet = true;
            }
        }

        ReceivedEdgeSet = ReceivedEdgeSet.OrderBy(tuple => tuple.Item1).ThenBy(tuple => tuple.Item2).ToList();
        CurrentKnownEdges = ReceivedEdgeSet;

        if (alteredSet)
        {
            ReachableNodes.ForEach(node => node.Discover(ReceivedEdgeSet, this, discoverID)); // *** This needs to be changed to use new comms system as well.
        }
        else if (newKnowledge)
        {
            ReachableNodes.Where(node => node != sender).ToList().ForEach(node => node.Discover(ReceivedEdgeSet, this, discoverID)); // *** This needs to be changed to use new comms system as well.
        }
    }

    public override bool Equals(object obj)
    {
        return ID == (obj as INode).ID;
    }

    private ConstellationPlan TakeSlot(ConstellationPlan plan, int entryIndex, float newValue)
    {
        ConstellationPlan newPlan = new ConstellationPlan(plan.Entries);

        ConstellationPlanEntry currentSlot = newPlan.Entries.Find(entry => entry.NodeID != null && entry.NodeID == ID);
        if(currentSlot != null && plan.Entries[entryIndex].NodeID != null)
        {
            currentSlot.NodeID = plan.Entries[entryIndex].NodeID;
            currentSlot.Fields["DeltaV"].Value = Position.Distance(currentSlot.Position, currentSlot.Node.Position); // *** What to do here? Can no longer access position of Node when changed to NodeID?.
        }

        newPlan.Entries[entryIndex].NodeID = ID;
        newPlan.Entries[entryIndex].Fields["DeltaV"].Value = newValue;

        return newPlan;
    }

    private void FailureDetection(uint? failedNode)
    {
        Router.DeleteEdge(ID, failedNode);

        uint? neighbour = Router.NetworkMap[failedNode][0];

        Router.NextHop(ID, neighbour).Communicate(Constants.Commands.DetectFailure, ID, neighbour, failedNode, false, false);
    }

    private void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        if (Active)
        {
            Heartbeat();
        }
    }

    private void Heartbeat()
    {
        new Thread(delegate () {
            NodeState previousState = State;
            State = NodeState.HEARTBEAT;
            Thread.Sleep(500);

            foreach (uint? node in Router.NetworkMap[ID].ToList()) // Should just communicate with reachable nodes instead of using networkmap
            {
                if (!node.Communicate(Constants.Commands.Heartbeat))
                {
                    FailureDetection(node);
                }
            }

            State = previousState;
        }).Start();
    }
}
