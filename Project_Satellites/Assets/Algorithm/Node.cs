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

    private Position _targetPosition;

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
            }            State = Node.NodeState.EXECUTING;            TargetPosition = _targetPosition;            if (executingPlan)            {                return; // Ignore Execute command if already executing which stops the execute communication loop
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

    public void Communicate(Constants.Commands command, ConstellationPlan plan)
    {
        Plan = plan;

        new Thread(delegate ()        {
            if (command != Constants.Commands.Generate)            {                throw new Exception("Wrong command"); // Only accept Generate command
            }
            State = Node.NodeState.PLANNING;

            executingPlan = false;

            Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

            for (int i = 0; i < plan.entries.Count; i++)
            {
                float requiredDeltaV = Position.Distance(Position, plan.entries[i].Position);
                fieldDeltaVPairs.Add(i, requiredDeltaV);
            }



            foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))

            {

                if (plan.entries.Any(entry => entry.Node != null && entry.Node.ID == this.ID) && plan.entries.Any(entry => entry.Node == null)){
                    break;
                }

                if (plan.ReduceBy("DeltaV", pair.Key, pair.Value))

                {

                    if (plan.entries.Any(entry => entry.Node != null && entry.Node.ID == this.ID))
                    {
                        //plan.entries.Find(entry => entry.Node.ID == this.ID).Fields["DeltaV"].Value = 100f;
                        plan.entries.Find(entry => entry.Node.ID == this.ID).Node = null;
                    }

                    if (plan.entries[pair.Key].Node != null && plan.entries[pair.Key].Node.ID != ID)
                    {
                        State = NodeState.OVERRIDE;
                    }



                    plan.entries[pair.Key].Node = this;

                    plan.entries[pair.Key].Fields["DeltaV"].Value = pair.Value;

                    _targetPosition = plan.entries[pair.Key].Position;

                    plan.lastEditedBy = ID;

                    justChangedPlan = true;

                    this.Plan = plan;

                    break;

                }

            }
                        if(plan.entries.Any(entry => entry.Node != null && entry.Node.ID == this.ID) == false && plan.entries.Any(entry => entry.Node == null))
            {
                int index = plan.entries.IndexOf(plan.entries.Find(entry => entry.Node == null));
                plan.entries[index].Node = this;
                plan.entries[index].Fields["DeltaV"].Value = Position.Distance(Position, plan.entries[index].Position);
                _targetPosition = plan.entries[index].Position;
                plan.lastEditedBy = ID;
                justChangedPlan = true;
                this.Plan = plan;
            }

            if (justChangedPlan)
            {
                Thread.Sleep(1000);
            }
            else
            {
                Thread.Sleep(250);
            }


            if (plan.lastEditedBy == ID && justChangedPlan == false)
            {
                justChangedPlan = false;
                State = Node.NodeState.EXECUTING;
                Communicate(Constants.Commands.Execute);
            }
            else
            {
                justChangedPlan = false;
                State = Node.NodeState.PASSIVE;
                router.NextHop(this).Communicate(Constants.Commands.Generate, plan);
            }
        }).Start();

    }

    private bool executingPlan;
    private bool justChangedPlan;
    private IRouter router;
}
