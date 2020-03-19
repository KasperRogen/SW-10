using System.Collections.Generic;

public abstract class IRouter
{
    public NetworkMap NetworkMap { get; set; }

    public abstract uint? NextHop(uint? source, uint? destination);
    public abstract void UpdateNetworkMap(ConstellationPlan plan);
}
