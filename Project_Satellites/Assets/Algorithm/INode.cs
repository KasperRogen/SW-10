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
    

    public abstract void Discover(List<Tuple<INode, INode>> ReceivedEdgeSet, INode sender, int discoverID);
    public abstract void GenerateRouter();
    public abstract void Communicate(Constants.Commands command, INode Target);
    public abstract void Communicate(Constants.Commands command, ConstellationPlan plan, INode Target);


}
