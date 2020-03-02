﻿using System;
using System.Linq;
using System.Collections.Generic;

public class ConstellationPlan
{
    public uint? lastEditedBy;
    public List<ConstellationPlanEntry> Entries;

    public ConstellationPlan(List<ConstellationPlanEntry> entries)
    {
        lastEditedBy = null;
        Entries = entries;
    }

    /// <summary>Calculates whether a proposed change in plan is a better solution
    /// <para>  </para>
    /// </summary>
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