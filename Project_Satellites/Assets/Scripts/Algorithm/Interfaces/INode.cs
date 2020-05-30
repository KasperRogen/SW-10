using System;
using System.Collections.Generic;
using System.Numerics;

public abstract class INode
{
    public abstract Node.NodeState State { get; set; }
    public abstract ConstellationPlan ActivePlan { get; set; }
    public abstract ConstellationPlan GeneratingPlan { get; set; }
    public abstract uint? Id { get; set; }
    public abstract Vector3 Position { get; set; }
    public abstract Vector3 TargetPosition { get; set; }
    public abstract Router Router { get; set; }
    public abstract bool Active { get; set; }
    public ICommunicate CommsModule { get; set; }
    public bool ExecutingPlan;
    public string LastDiscoveryId;
    public Vector3 PlaneNormalDir { get; set; }

    public abstract void GenerateRouter();
    public abstract void Communicate(Request message);
    public int ReachableNodeCount;
}
