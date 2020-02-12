using System;
using System.Collections.Generic;
using System.Linq;

public class ConstellationPlanField : IComparable
{
    public string Key { get; set; }
    public float Value { get; set; }
    public delegate int CompareFunction(float obj1, float obj2);
    CompareFunction compareFunction;
    public ConstellationPlanField(string key, float defaultValue, CompareFunction func)
    {
        Key = key;        Value = defaultValue;
        compareFunction = func;
    }

    public int CompareTo(object obj)
    {
        return compareFunction(this.Value, (obj as ConstellationPlanField).Value);
    }
}
