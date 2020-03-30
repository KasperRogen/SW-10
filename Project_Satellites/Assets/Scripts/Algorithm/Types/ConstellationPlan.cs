using System.Linq;
using System.Collections.Generic;
using System.Numerics;

public class ConstellationPlan
{
    public uint? LastEditedBy { get; set; }
    public List<ConstellationPlanEntry> Entries { get; set; }

    public ConstellationPlan(List<ConstellationPlanEntry> entries)
    {
        LastEditedBy = null;
        Entries = entries;
    }

    /// <summary>
    /// Returns whether or not swapping the two nodes yields a more cost-efficient constellation.
    /// </summary>
    /// <returns></returns>
    public bool TrySwapNodes(uint? nodeID1, Vector3 nodePosition1, uint? nodeID2, Vector3 nodePosition2, out ConstellationPlan newPlan)
    {
        ConstellationPlan planCopy = DeepCopy(); // Make a copy of the plan to avoid the method having side effects.
        ConstellationPlanEntry entry1 = planCopy.Entries.Find(x => x.NodeID == nodeID1);
        ConstellationPlanEntry entry2 = planCopy.Entries.Find(x => x.NodeID == nodeID2);
        entry1.NodeID = nodeID2;
        entry1.Fields["DeltaV"].Value = Vector3.Distance(nodePosition2, entry1.Position);
        entry2.NodeID = nodeID1;
        entry2.Fields["DeltaV"].Value = Vector3.Distance(nodePosition1, entry2.Position);
        float currentSum = Entries.Select(x => x.Fields["DeltaV"].Value).Sum();
        float newSum = planCopy.Entries.Select(x => x.Fields["DeltaV"].Value).Sum();

        if (newSum < currentSum)
        {
            newPlan = planCopy;
            return true;
        }
        else
        {
            newPlan = null;
            return false;
        }
    }

    public ConstellationPlan DeepCopy()
    {
        ConstellationPlan copy = new ConstellationPlan(Entries.ConvertAll(x => x.DeepCopy()));
        copy.LastEditedBy = LastEditedBy;
        return copy;
    }

    public override string ToString() {
        return $"{{(ConstellationPlan)\n" +
            $"LastEditedBy: {LastEditedBy},\n" +
            $"Entries: [\n" +
            $"{Entries.Select(x => x.ToString()).Aggregate((x, y) => x + ",\n" + y)}]}}";
    }
}
