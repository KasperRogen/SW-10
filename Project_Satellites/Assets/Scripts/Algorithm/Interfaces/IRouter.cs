using System.Collections.Generic;

public abstract class IRouter
{
    public abstract NetworkMap NetworkMap { get; set; }

    public abstract uint? NextHop(uint? source, uint? destination);
    public abstract void UpdateNetworkMap(ConstellationPlan plan);
    public abstract void ClearNetworkMap();
}
