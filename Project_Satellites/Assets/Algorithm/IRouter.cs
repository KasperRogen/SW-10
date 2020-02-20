using System.Collections.Generic;

public abstract class IRouter
{
    public abstract INode NextHop(INode source, INode target);
    public abstract void UpdateNetworkMap(ConstellationPlan plan);

    public Dictionary<INode, List<INode>> NetworkMap;
}
