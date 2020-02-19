using System;
using System.Collections.Generic;

public interface INode
{


    Node.NodeState State { get; set; }
    ConstellationPlan Plan { get; set; }
    int ID { get; set; }
    List<INode> ReachableNodes { get; set; }
    Position Position { get; set; }
    Position TargetPosition { get; set; }
    Router router { get; set; }

    void GenerateRouter();
    void Communicate(Constants.Commands command, INode Target);
    void Communicate(Constants.Commands command, ConstellationPlan plan, INode Target);
    void Communicate(Constants.Commands command, INode source, INode target, INode deadSat, bool isDead, bool isChecked);

}
