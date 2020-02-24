using System;
using System.Collections.Generic;

public abstract class INode
{


    public abstract Node.NodeState State { get; set; }
    public abstract ConstellationPlan Plan { get; set; }
    public abstract int ID { get; set; }
    public abstract List<INode> ReachableNodes { get; set; }
    public abstract Position Position { get; set; }
    public abstract Position TargetPosition { get; set; }
    public abstract Router router { get; set; }
    public abstract bool Active { get; set; }

    public abstract void Discover(List<Tuple<INode, INode>> ReceivedEdgeSet, INode sender, int discoverID);
    public abstract void GenerateRouter();
    public abstract bool Communicate(Constants.Commands command);
    public abstract bool Communicate(Constants.Commands command, INode Target);
    public abstract bool Communicate(Constants.Commands command, ConstellationPlan plan, INode Target);
    public abstract bool Communicate(Constants.Commands detectFailure, INode node, INode neighbour, INode failedNode, bool v1, bool v2);
}
