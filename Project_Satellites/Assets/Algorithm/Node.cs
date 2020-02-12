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
            }

            State = Node.NodeState.EXECUTING;

            TargetPosition = _targetPosition;

            if (executingPlan)
            {
                return; // Ignore Execute command if already executing which stops the execute communication loop
            }
            else
            {
                executingPlan = true;
            }


            NextNode(ReachableNodes).Communicate(Constants.Commands.Execute);
        }).Start();

        TargetPosition = intermediateTargetPosition;

        if (executingPlan)
        {
            return; // Ignore Execute command if already executing which stops the execute communication loop
        }
        else
        {
            executingPlan = true;
        }

        NextNode(ReachableNodes).Communicate(Constants.Commands.Execute);
    }

    public void Communicate(Constants.Commands command, ConstellationPlan plan)
    {

        Plan = plan;

        new Thread(delegate ()
        {


            if (command != Constants.Commands.Generate)
            {
                throw new Exception("Wrong command"); // Only accept Generate command
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
                    if (plan.ReduceBy("DeltaV", pair.Key, pair.Value))
                    {
                        if(plan.entries[pair.Key].NodeID != ID && plan.entries[pair.Key].NodeID != -1)
                        { 
                            State = NodeState.OVERRIDE;
                        }

                        plan.entries[pair.Key].NodeID = ID;
                        plan.entries[pair.Key].Fields["DeltaV"].Value = pair.Value;
                        _targetPosition = plan.entries[pair.Key].Position;
                        plan.lastEditedBy = ID;
                        justChangedPlan = true;
                        this.Plan = plan;
                        break;
                    }
                }


            if (justChangedPlan)
            {
                System.Threading.Thread.Sleep(1000);
            } else
            {
                System.Threading.Thread.Sleep(250);
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
                NextNode(ReachableNodes).Communicate(Constants.Commands.Generate, plan);
            }
        }).Start();
        
    }

    private bool executingPlan;
    private bool justChangedPlan;
    private Position intermediateTargetPosition;

    private INode NextNode(List<INode> nodes)
    {
        int nextNodeID = (ID + 1) % Constants.NodesPerCycle;
        return nodes.Find((x) => x.ID == nextNodeID);
    }
}
