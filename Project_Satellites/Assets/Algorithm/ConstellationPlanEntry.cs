using System.Collections;
using System.Collections.Generic;
using System;

public class ConstellationPlanEntry : IComparable
{
    public Position Position;
    public int NodeID;

    public Dictionary<string, ConstellationPlanField> Fields = new Dictionary<string, ConstellationPlanField>();

    public delegate int CompareFunction(ConstellationPlanEntry obj1, ConstellationPlanEntry obj2);

    CompareFunction compareFunction;


    public ConstellationPlanEntry(Position position, List<ConstellationPlanField> fields, CompareFunction func)

    {
        Position = position;
        NodeID = -1;

        compareFunction = func;

        foreach (ConstellationPlanField CPF in fields)

        {
            Fields.Add(CPF.Key, CPF);
        }
    }


    public int CompareTo(object obj)

    {
        return compareFunction(this, obj as ConstellationPlanEntry);
    }
}
