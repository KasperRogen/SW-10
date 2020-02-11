

using System;
using System.Collections.Generic;
using System.Linq;

public class ConstellationPlanEntry : IComparable
{
    public Dictionary<string, ConstellationPlanField> Fields = new Dictionary<string, ConstellationPlanField>();

    public delegate int CompareFunction(ConstellationPlanEntry obj1, ConstellationPlanEntry obj2);

    CompareFunction compareFunction;

    public ConstellationPlanEntry(List<ConstellationPlanField> fields, CompareFunction func)
    {

        compareFunction = func;

        foreach(ConstellationPlanField CPF in fields)
        {
            Fields.Add(CPF.key, CPF);
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
    public string key { get; set; }
    public float value { get; set; }

    public delegate int CompareFunction(float obj1, float obj2);

    CompareFunction compareFunction;

    public ConstellationPlanField(CompareFunction func)
    {
        compareFunction = func;
    }

    public int CompareTo(object obj)
    {
        return compareFunction(this.value, (obj as ConstellationPlanField).value);
    }


}
