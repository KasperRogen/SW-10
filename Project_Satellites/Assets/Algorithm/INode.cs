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

    void GenerateRouter();
    void Communicate(Constants.Commands command);
    void Communicate(Constants.Commands command, ConstellationPlan plan);

}
