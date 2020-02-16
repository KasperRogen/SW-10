using System;
using System.Linq;
using System.Collections.Generic;

public class ConstellationPlan
{
    public int lastEditedBy;
    public List<ConstellationPlanEntry> entries;

   
    public ConstellationPlan(List<ConstellationPlanEntry> entries)
    {
        this.entries = entries;
    }


    public bool ReduceBy(string key, int index, float testValue, Node initiator)
    {
        List<float> values = new List<float>();

        ConstellationPlanEntry oldSlot = entries.Find(entry => entry.Node == initiator);

        foreach(ConstellationPlanEntry entry in entries)
        {
            values.Add((entry.Fields[key] as ConstellationPlanField).Value);
        }

        float oldSum, newSum;

        oldSum = values.Sum();

        values[index] = testValue;
        int oldIndex = entries.IndexOf(oldSlot);
        values[oldIndex] = Position.Distance(entries[index].Node.Position, entries[oldIndex].Position);

        newSum = values.Sum();

        return newSum < oldSum;
    }

}
