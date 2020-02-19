using System.Collections.Generic;

public interface IRouter
{
    INode NextHop(INode source, INode target);
    void UpdateNetworkMap(ConstellationPlan plan);
    void DeleteEdge(INode n1, INode n2);

    Dictionary<INode, List<INode>> NetworkMap { get; set; }
}
