

using System;
using System.Collections.Generic;

public class ConstellationPlanEntry : IComparable
{
    Dictionary<string, ConstellationPlanField> Fields = new Dictionary<string, ConstellationPlanField>();

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



public abstract class ConstellationPlanField : IComparable
{
    public string key { get; set; }

    public abstract int CompareTo(object obj);

}

public class ConstellationPlanField<T> : ConstellationPlanField, IComparable
{
    public T value { get; set; }

    public delegate int CompareFunction(T obj1, T obj2);

    CompareFunction compareFunction;

    public ConstellationPlanField(CompareFunction func)
    {
        compareFunction = func;
    }

    public override int CompareTo(object obj)
    {
        return compareFunction(this.value, (obj as ConstellationPlanField<T>).value);
    }

    //public static 

    //public static Sum<T>(string key)
    //{
    //    float sum = 0;

    //    foreach (ConstellationPlanField field in fields)
    //    {
    //        sum += field.deltaV;
    //    }

    //    return sum;
    //}

    //public float TotalDeltaVWithChange(int index, float newDeltaV)
    //{
    //    List<float> tempDeltaVs = new List<float>();
    //    foreach (ConstellationPlanField field in this.fields)
    //    {
    //        tempDeltaVs.Add(field.deltaV);
    //    }

    //    float currentSum = 0;
    //    float newSum = 0;

    //    foreach (float deltaV in tempDeltaVs)
    //    {
    //        currentSum += deltaV;
    //    }

    //    tempDeltaVs[index] = newDeltaV;

    //    foreach (float deltaV in tempDeltaVs)
    //    {
    //        newSum += deltaV;
    //    }


    //    if (currentSum <= newSum)
    //    {
    //        return currentSum;
    //    }
    //    else
    //    {
    //        return newSum;
    //    }
    //}


}
