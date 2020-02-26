using System;
using System.Collections.Generic;

public abstract class INode
{
    public abstract Node.NodeState State { get; set; }
    public abstract ConstellationPlan Plan { get; set; }
    public abstract uint? ID { get; set; }
    public abstract List<uint?> ReachableNodes { get; set; }
    public abstract Position Position { get; set; }
    public abstract Position TargetPosition { get; set; }
    public abstract Router Router { get; set; }
    public abstract bool Active { get; set; }

    public abstract void Discover(List<Tuple<uint?, uint?>> receivedEdgeSet, uint? sender, int discoverID);
    public abstract void GenerateRouter();
    public abstract bool Communicate(Constants.Commands command);
    public abstract bool Communicate(Constants.Commands command, uint? destination);
    public abstract bool Communicate(Constants.Commands command, ConstellationPlan plan, uint? destination);
    public abstract bool Communicate(Constants.Commands command, uint? node, uint? neighbour, uint? failedNode, bool v1, bool v2);
}
