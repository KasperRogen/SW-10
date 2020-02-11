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


    public bool ReduceBy(string key, int index, float testValue)
    {
        List<float> values = new List<float>();

        foreach(ConstellationPlanEntry entry in entries)
        {
            values.Add((entry.Fields[key] as ConstellationPlanField).Value);
        }

        float oldSum, newSum;

        oldSum = values.Sum();

        values[index] = testValue;

        newSum = values.Sum();

        return newSum < oldSum;
    }

}
