using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


public class Node : INode
{
    
    public enum NodeState { PASSIVE, PLANNING, OVERRIDE, EXECUTING, DEAD };

    public int ID { get; set; }
    public List<INode> ReachableNodes { get; set; } // Future Work: Make part of the algorithm that reachable nodes are calculated based on position and a communication distance
    public Position Position { get; set; }
    public Position TargetPosition { get; set; }
    public ConstellationPlan Plan { get; set; }
    public NodeState State { get; set; }
    public Router router { get; set; }
    public bool Active
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
    private bool active;

    public Node(int ID)
    {
        this.ID = ID;
        State = Node.NodeState.PASSIVE;
        Active = true;
    }

    public bool Communicate(Constants.Commands command, INode target)
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
            }            if(target == this)
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

    public void GenerateRouter()
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

    public bool Communicate(Constants.Commands command, ConstellationPlan plan, INode target)
    {
        if (!Active)
        {
            return false;
        }

        new Thread(delegate ()        {            if (command != Constants.Commands.Generate)            {                throw new Exception("Wrong command"); // Only accept Generate command
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

    public bool Communicate(Constants.Commands command, INode source, INode target, INode deadNode, bool isDead, bool isChecked)
    {
        if (!Active)
        {
            return false;
        }

        new Thread(delegate ()
        {
            Thread.Sleep(500);
            router.DeleteEdge(source, deadNode);
            
            if (isChecked == true)
            {
                this.State = NodeState.PASSIVE;
            }
            else
            {
                this.State = NodeState.EXECUTING;
            }

            if (command != Constants.Commands.DetectFailure)
            {
                throw new Exception("Wrong command:: Expected DetectFailure command");
            }

            if (deadNode.ID == ID) // do nothing
            {
                return;
            }
            else if (target.ID != ID) // check if other dead sat, otherwise relay.
            {
                INode nextHopTarget = isChecked ? source : target;
                bool response = router.NextHop(this, nextHopTarget).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, isDead, isChecked);

                // Handle additional dead satellites
            }
            else if (target.ID == ID)
            {
                // Get response
                bool response = router.NextHop(this, deadNode).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, isDead, isChecked);

                // Relay response opposite way
                router.NextHop(this, source).Communicate(Constants.Commands.DetectFailure, source, target, deadNode, response, true);
            }
            else if (source.ID == ID)
            {
                if (isDead == true)
                {
                    TargetConstellationGenerator.instance.GenerateTargetConstellation();
                }
                else
                {
                    return;
                }
            }
        }).Start();

        return true;
  
    }


    private bool executingPlan;
    private bool justChangedPlan;
}
