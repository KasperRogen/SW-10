using System.Collections.Generic;

public interface INode
{
    int ID { get; set; }
    List<INode> ReachableNodes { get; set; }
    Position TargetPosition { get; set; }

    void Communicate(Constants.Commands command);
    void Communicate(Constants.Commands command, ConstellationPlan plan);
}
