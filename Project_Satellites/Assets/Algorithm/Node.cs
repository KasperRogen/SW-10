using System;
using System.Collections.Generic;
using System.Linq;

public class Node : INode
{
    public int ID { get; set; }
    public List<INode> ReachableNodes { get; set; }
    public Position TargetPosition { get; set; }

    public Node(int ID)
    {
        this.ID = ID;
    }

    public void Communicate(Constants.Commands command)
    {
        if (command != Constants.Commands.Execute)
        {
            throw new Exception("Wrong command"); // Only accept Execute command
        }

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
        if (command != Constants.Commands.Generate)
        {
            throw new Exception("Wrong command"); // Only accept Generate command
        }

        executingPlan = false;

        Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

        for (int i = 0; i < plan.entries.Count; i++)
        {
            float requiredDeltaV = Position.Distance(TargetPosition, plan.entries[i].Position);
            fieldDeltaVPairs.Add(i, requiredDeltaV);
        }

        if (plan.entries.Any(x => x.NodeID == ID) == false)
        {
            foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))
            {
                if (plan.TotalDeltaVWithChange(pair.Key, pair.Value) < plan.TotalDeltaV())
                {
                    plan.entries[pair.Key].NodeID = ID;
                    plan.entries[pair.Key].deltaV = pair.Value;
                    TargetPosition = plan.entries[pair.Key].Position;
                    plan.lastEditedBy = ID;
                    justChangedPlan = true;
                    break;
                }
            }
        }

        if (plan.lastEditedBy == ID && justChangedPlan == false)
        {
            justChangedPlan = false;
            Communicate(Constants.Commands.Execute);
        }
        else
        {
            justChangedPlan = false;
            NextNode(ReachableNodes).Communicate(Constants.Commands.Generate, plan);
        }
    }

    private bool executingPlan;
    private bool justChangedPlan;

    private INode NextNode(List<INode> nodes)
    {
        int nextNodeID = (ID + 1) % Constants.NodesPerCycle;
        return nodes.Find((x) => x.ID == nextNodeID);
    }
}
