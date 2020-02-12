using System.Collections.Generic;

public interface IRouter
{
    INode NextHop(INode source);
    void UpdateNetworkMap(ConstellationPlan plan);
}
