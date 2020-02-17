using System.Collections.Generic;

public interface IRouter
{
    INode NextHop(INode source, INode target);
    void UpdateNetworkMap(ConstellationPlan plan);

    Dictionary<INode, List<INode>> NetworkMap { get; set; }
}
