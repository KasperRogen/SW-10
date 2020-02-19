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
    bool Active { get; set; }

    void GenerateRouter();
    bool Communicate(Constants.Commands command, INode Target);
    bool Communicate(Constants.Commands command, ConstellationPlan plan, INode Target);
    bool Communicate(Constants.Commands command, INode source, INode target, INode deadSat, bool isDead, bool isChecked);

}
