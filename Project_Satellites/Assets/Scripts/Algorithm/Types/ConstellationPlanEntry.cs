using System.Collections.Generic;
using System;
using System.Numerics;
using System.Linq;

public class ConstellationPlanEntry : IComparable
{
    public Vector3 Position { get; set; }
    public uint? NodeID { get; set; }
    public Dictionary<string, ConstellationPlanField> Fields { get; set; }

    public delegate int CompareFunction(ConstellationPlanEntry obj1, ConstellationPlanEntry obj2);

    private CompareFunction compareFunction;

    public ConstellationPlanEntry(Vector3 position, List<ConstellationPlanField> fields, CompareFunction func)
    {
        Position = position;
        NodeID = null;
        Fields = new Dictionary<string, ConstellationPlanField>();
        
        foreach (ConstellationPlanField field in fields)
        {
            Fields.Add(field.Key, field);
        }

        compareFunction = func;
    }

    public ConstellationPlanEntry(uint? nodeID, Vector3 position, List<ConstellationPlanField> fields, CompareFunction func)
    {
        Position = position;
        NodeID = nodeID;
        Fields = new Dictionary<string, ConstellationPlanField>();

        foreach (ConstellationPlanField field in fields)
        {
            Fields.Add(field.Key, field);
        }

        compareFunction = func;
    }

    public int CompareTo(object obj)
    {
        return compareFunction(this, obj as ConstellationPlanEntry);
    }

    public override string ToString()
    {
        return $"{{(ConstellationPlanEntry)\n" +
            $"Position: {Position},\n" +
            $"NodeID: {NodeID},\n" +
            $"Fields: {{\n" +
            $"{Fields.Select(x => $"{x.Key}: {x.Value}" ).Aggregate((x, y) => x+ ",\n" + y)}}}}}";
    }

    public ConstellationPlanEntry DeepCopy()
    {
        ConstellationPlanEntry copy = new ConstellationPlanEntry(Position, Fields.Values.ToList().ConvertAll(x => x.DeepCopy()), compareFunction);
        copy.NodeID = NodeID;
        return copy;
    }
}
