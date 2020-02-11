

using System;
using System.Collections.Generic;
using System.Linq;

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

        foreach(ConstellationPlanField CPF in fields)
        {
            Fields.Add(CPF.Key, CPF);
        }
    
    
    }




//    ConstellationPlanField<float> a = new ConstellationPlanField<float>((x, y) => { return x.CompareTo(y); });

    public int CompareTo(object obj)
    {
        return compareFunction(this, obj as ConstellationPlanEntry);
    }
}




public class ConstellationPlanField : IComparable
{
    public string Key { get; set; }
    public float Value { get; set; }

    public delegate int CompareFunction(float obj1, float obj2);

    CompareFunction compareFunction;

    public ConstellationPlanField(string key, CompareFunction func)
    {
        Key = key;
        compareFunction = func;
    }

    public int CompareTo(object obj)
    {
        return compareFunction(this.Value, (obj as ConstellationPlanField).Value);
    }


}
