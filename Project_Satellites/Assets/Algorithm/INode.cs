using System;
using System.Collections.Generic;

public abstract class INode
{


    public Node.NodeState State { get; set; }
    public ConstellationPlan Plan { get; set; }
    public int ID { get; set; }
    public List<INode> ReachableNodes { get; set; }
    public Position Position { get; set; }
    public Position TargetPosition { get; set; }
    public Router router { get; set; }

    public abstract void Discover(List<Tuple<INode, INode>> ReceivedEdgeSet, INode sender, int discoverID);
    public abstract void GenerateRouter();
    public abstract void Communicate(Constants.Commands command, INode Target);
    public abstract void Communicate(Constants.Commands command, ConstellationPlan plan, INode Target);


}
