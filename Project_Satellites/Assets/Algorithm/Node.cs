using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


public class Node : INode
{

    public enum NodeState { PASSIVE, PLANNING, OVERRIDE, EXECUTING };


    public int ID { get; set; }
    public List<INode> ReachableNodes { get; set; } // Future Work: Make part of the algorithm that reachable nodes are calculated based on position and a communication distance
    public Position Position { get; set; }
    public Position TargetPosition { get; set; }
    public NodeState State { get; set; }
    public ConstellationPlan Plan { get; set; }

  
    
    public Node(int ID)
    {
        this.ID = ID;
        State = Node.NodeState.PASSIVE;
    }

    public void Communicate(Constants.Commands command)
    {


        new Thread(delegate ()
        {
            if (command != Constants.Commands.Execute)
            {
                throw new Exception("Wrong command"); // Only accept Execute command
            }            State = Node.NodeState.EXECUTING;            TargetPosition = Plan.entries.Find(entry => entry.Node == this).Position;                        if (executingPlan)            {                return; // Ignore Execute command if already executing which stops the execute communication loop
            }            else            {                executingPlan = true;            }
            if (router == null)
            {
                router = new Router(Plan);
            }

            router.NextHop(this).Communicate(Constants.Commands.Execute);
            router.UpdateNetworkMap(Plan);

            
        }).Start();
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

    public void Communicate(Constants.Commands command, ConstellationPlan plan)
    {

        new Thread(delegate ()        {
            if (command != Constants.Commands.Generate)            {                throw new Exception("Wrong command"); // Only accept Generate command
            }
            executingPlan = false;

            Plan = plan;

            State = Node.NodeState.PLANNING;
            Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();
            for (int i = 0; i < plan.entries.Count; i++)            {                float requiredDeltaV = Position.Distance(Position, plan.entries[i].Position);                fieldDeltaVPairs.Add(i, requiredDeltaV);            }

            ConstellationPlan newPlan = null;

            if (plan.entries.Any(entry => entry.Node?.ID == this.ID) == false)
            {
                ConstellationPlanEntry slotToTake = plan.entries.Where(entry => entry.Node == null) //Only allow satellite to take free locations
                .Aggregate((CurrentBest, currentTest) => //Iterate each entry
                Position.Distance(currentTest.Position, this.Position) <=  //This entry currently being tested to improve over current best
                Position.Distance(CurrentBest.Position, this.Position) ?  //current best 
                currentTest : CurrentBest); //return best candidate of currenttest and currentbest

                newPlan = TakeSlot(plan, plan.entries.IndexOf(slotToTake), Position.Distance(slotToTake.Position, this.Position));
            } else
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
                Thread.Sleep(100);
            } else
            {
                Thread.Sleep(25);
            }



            if (plan.lastEditedBy == ID && justChangedPlan == false)            {                State = Node.NodeState.EXECUTING;                Communicate(Constants.Commands.Execute);            }
            else            {                justChangedPlan = false;                State = Node.NodeState.PASSIVE;                router.NextHop(this).Communicate(Constants.Commands.Generate, plan);            }
        }).Start();

    }

    private bool executingPlan;
    private bool justChangedPlan;
    private IRouter router;
}
