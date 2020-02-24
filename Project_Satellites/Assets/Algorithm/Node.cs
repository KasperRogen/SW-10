using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;using System.Timers;



public class Node : INode
{
    
    public enum NodeState { PASSIVE, PLANNING, OVERRIDE, EXECUTING, DEAD, HEARTBEAT };

    public override int ID { get; set; }
    public override List<INode> ReachableNodes { get; set; } // Future Work: Make part of the algorithm that reachable nodes are calculated based on position and a communication distance
    public override Position Position { get; set; }
    public override Position TargetPosition { get; set; }

    private bool active;

    public override bool Active { get { return active; } set
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
    public override Router router { get; set; }



    List<Tuple<INode, INode>> EdgeSet;

    List<Tuple<INode, INode>> CurrentKnownEdges = new List<Tuple<INode, INode>>();

    private int LastDiscoverID = -1;

    public Node(int ID, Position position)
    {
        this.ID = ID;
        State = Node.NodeState.PASSIVE;
        this.Position = position;
        Active = true;
        timer = new System.Timers.Timer(15000);
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
    public override bool Communicate(Constants.Commands command, INode target)
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

            if(target == this)
            {

                State = Node.NodeState.EXECUTING;
                TargetPosition = Plan.entries.Find(entry => entry.Node == this).Position;

                if (executingPlan)
                {
                    return; // Ignore Execute command if already executing which stops the execute communication loop
                }
                else
                {
                    executingPlan = true;
                }

                if (router == null)
                {
                    router = new Router(Plan);
                }

                INode nextSeq = router.NextSequential(this);
                TryCommunicate(Constants.Commands.Execute, nextSeq, nextSeq);

                router.UpdateNetworkMap(Plan);

            }
            else
            {
                INode nextHop = router.NextHop(this, target);
                TryCommunicate(command, nextHop, target);
            }


        }).Start();

        return true;
    }

    public override void GenerateRouter()
    {
        router = new Router(Plan);
    }

    ConstellationPlan TakeSlot(ConstellationPlan plan, int entryIndex, float newValue)
    {
        ConstellationPlan newPlan = new ConstellationPlan(plan.entries);

        ConstellationPlanEntry currentSlot = newPlan.entries.Find(entry => entry.Node != null && entry.Node == this);
        if(currentSlot != null && plan.entries[entryIndex].Node != null)
        {
            currentSlot.Node = plan.entries[entryIndex].Node;
            currentSlot.Fields["DeltaV"].Value = Position.Distance(currentSlot.Position, currentSlot.Node.Position);
        }

        newPlan.entries[entryIndex].Node = this;
        newPlan.entries[entryIndex].Fields["DeltaV"].Value = newValue;

        return newPlan;
    }

    public override bool Communicate(Constants.Commands command, ConstellationPlan plan, INode target)
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

            if (target == this)
            {
                executingPlan = false;

                Plan = plan;

                State = Node.NodeState.PLANNING;

                Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

                for (int i = 0; i < plan.entries.Count; i++)
                {
                    float requiredDeltaV = Position.Distance(Position, plan.entries[i].Position);
                    fieldDeltaVPairs.Add(i, requiredDeltaV);
                }

                ConstellationPlan newPlan = null;

                if (plan.entries.Any(entry => entry.Node?.ID == this.ID) == false)
                {
                    ConstellationPlanEntry slotToTake = plan.entries.Where(entry => entry.Node == null) //Only allow satellite to take free locations
                    .Aggregate((CurrentBest, currentTest) => //Iterate each entry
                    Position.Distance(currentTest.Position, this.Position) <=  //This entry currently being tested to improve over current best
                    Position.Distance(CurrentBest.Position, this.Position) ?  //current best 
                    currentTest : CurrentBest); //return best candidate of currenttest and currentbest

                    newPlan = TakeSlot(plan, plan.entries.IndexOf(slotToTake), Position.Distance(slotToTake.Position, this.Position));
                }
                else if (plan.entries.Any(entry => entry.Node == null) == false)
                {

                    foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))
                    {
                        if (plan.ReduceBy("DeltaV", pair.Key, pair.Value, this))
                        {
                            if (plan.entries[pair.Key].Node != null && plan.entries[pair.Key].Node.ID != ID)
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
                    plan.lastEditedBy = ID;

                    this.Plan = plan;
                    Thread.Sleep(1000);
                }
                else
                {
                    Thread.Sleep(250);
                }


                if (plan.lastEditedBy == ID && justChangedPlan == false)
                {
                    State = Node.NodeState.EXECUTING;
                    Communicate(Constants.Commands.Execute, this);
                }

                else
                {
                    justChangedPlan = false;
                    State = Node.NodeState.PASSIVE;
                    INode nextSeq = router.NextSequential(this);
                    if (router.NetworkMap[this].Contains(nextSeq)){
                        TryCommunicate(Constants.Commands.Generate, plan, nextSeq, nextSeq);
                    } else
                    {
                        TryCommunicate(Constants.Commands.Generate, plan, router.NextHop(this, nextSeq), nextSeq);
                    }
                    
                }


            }
            else
            {
                Thread.Sleep(500);
                State = Node.NodeState.PLANNING;
                INode nextHop = router.NextHop(this, target);
                TryCommunicate(command, plan, nextHop, target);
            }

        }).Start();

        return true;
    }

    private void TryCommunicate(Constants.Commands command, ConstellationPlan plan, INode source, INode destination)
    {
        if (!source.Communicate(command, plan, destination))
        {
            FailureDetection(source);
        }
    }

    private void TryCommunicate(Constants.Commands command, INode source, INode destination)
    {
        if (!source.Communicate(command, destination))
        {
            FailureDetection(source);
        }
    }

    private void FailureDetection(INode failedNode)
    {
        router.DeleteEdge(this, failedNode);

        INode neighbour = router.NetworkMap[failedNode][0];

        router.NextHop(this, neighbour).Communicate(Constants.Commands.DetectFailure, this, neighbour, failedNode, false, false);
    }

  
    public override bool Communicate(Constants.Commands command, INode source, INode target, INode deadNode, bool isDead, bool isChecked)
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
            router.DeleteEdge(source, deadNode);

            this.State = isChecked ? NodeState.PASSIVE : NodeState.EXECUTING;

            if (source.ID == ID)
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

            if (target.ID != ID) // check if other dead sat, otherwise relay.
            {
                INode nextHopTarget = isChecked ? source : target;
                bool response = router.NextHop(this, nextHopTarget).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, isDead, isChecked);

                // Handle additional dead satellites
            }
            else if (target.ID == ID)
            {
                bool response;
                if(router.NetworkMap[this].Contains(deadNode) == false)
                {
                    //link is broken, we cannot communicate
                    response = true;
                } else
                {
                    // Get response
                    response = !router.NextHop(this, deadNode).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, isDead, isChecked);
                }


                // Relay response opposite way
                router.NextHop(this, source).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, response, true);
            }
        }).Start();

        return true;
  
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

            foreach (INode node in router.NetworkMap[this].ToList()) // Should just communicate with reachable nodes instead of using networkmap
            {
                if (!node.Communicate(Constants.Commands.Heartbeat))
                {
                    FailureDetection(node);
                }
            }

            State = previousState;
        }).Start();
    }

    

    public override void Discover(List<Tuple<INode, INode>> ReceivedEdgeSet, INode sender, int discoverID)
    {
        bool newKnowledge = false;
        bool alteredSet = false;


        if(discoverID > LastDiscoverID)
        {
            CurrentKnownEdges.Clear();
            LastDiscoverID = discoverID;
        }



        //List<Tuple<INode, INode>> temp = ReceivedEdgeSet.Except(CurrentKnownEdges, (x y) => return 1).ToList();
        //newKnowledge = temp.Count() > 0;

        

        foreach (INode node in ReachableNodes)
        {
            Tuple<INode, INode> edge = new Tuple<INode, INode>(this, node);

            edge = edge.Item1.ID > edge.Item2.ID ? new Tuple<INode, INode>(edge.Item2, edge.Item1) : edge;


            if(ReceivedEdgeSet.Contains(edge) == false)
            {
                ReceivedEdgeSet.Add(edge);
                alteredSet = true;
            }
        }

        ReceivedEdgeSet = ReceivedEdgeSet.OrderBy(tuple => tuple.Item1.ID).ThenBy(tuple => tuple.Item2.ID).ToList();
        CurrentKnownEdges = ReceivedEdgeSet;

        if (alteredSet)
        {
            ReachableNodes.ForEach(node => node.Discover(ReceivedEdgeSet, this, discoverID));
        } else if (newKnowledge)
        {
            ReachableNodes.Where(node => node.ID != sender.ID).ToList().ForEach(node => node.Discover(ReceivedEdgeSet, this, discoverID));
        }




    }


    public override bool Equals(object obj)
    {
        return this.ID == (obj as INode).ID;
    }

    private bool executingPlan;
    private bool justChangedPlan;
    private System.Timers.Timer timer;
}
