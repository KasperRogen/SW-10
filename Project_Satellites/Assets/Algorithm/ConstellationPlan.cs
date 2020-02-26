using System.Linq;
using System.Collections.Generic;

public class ConstellationPlan
{
    public uint? LastEditedBy { get; set; }
    public List<ConstellationPlanEntry> Entries { get; set; }

    public ConstellationPlan(List<ConstellationPlanEntry> entries)
    {
        LastEditedBy = null;
        Entries = entries;
    }

    public bool ReduceBy(string key, int index, float testValue, uint? initiator)
    {
        List<float> values = new List<float>();

        ConstellationPlanEntry oldSlot = Entries.Find(entry => entry.NodeID == initiator);

        foreach(ConstellationPlanEntry entry in Entries)
        {
            values.Add((entry.Fields[key] as ConstellationPlanField).Value);
        }

        float oldSum, newSum;

        oldSum = values.Sum();

        values[index] = testValue;
        int oldIndex = Entries.IndexOf(oldSlot);
        values[oldIndex] = Position.Distance(Entries[index].Position, Entries[oldIndex].Position);

        newSum = values.Sum();

        return newSum < oldSum;
    }

}
