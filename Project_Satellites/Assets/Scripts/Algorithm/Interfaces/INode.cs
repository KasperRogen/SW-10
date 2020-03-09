using System;
using System.Collections.Generic;
using System.Numerics;

public abstract class INode
{
    public abstract Node.NodeState State { get; set; }
    public abstract ConstellationPlan ActivePlan { get; set; }
    public abstract ConstellationPlan GeneratingPlan { get; set; }
    public abstract uint? ID { get; set; }
    public abstract List<uint?> ReachableNodes { get; set; }
    public abstract Vector3 Position { get; set; }
    public abstract Vector3 TargetPosition { get; set; }
    public abstract Router Router { get; set; }
    public abstract bool Active { get; set; }
    public ICommunicate CommsModule { get; set; }
    public List<Tuple<uint?, uint?>> KnownEdges { get; set; }

    public bool IsBusy { get; set; }
    public bool executingPlan;
    public bool justChangedPlan;

    public abstract void GenerateRouter();
    public abstract void Communicate(Request message);
}
