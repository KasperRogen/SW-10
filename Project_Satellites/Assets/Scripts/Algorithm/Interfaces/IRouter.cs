using System.Collections.Generic;
using UnityEngine;

public abstract class IRouter : MonoBehaviour
{
    public abstract NetworkMap NetworkMap { get; set; }

    public abstract uint? NextHop(uint? source, uint? destination);
    public abstract void UpdateNetworkMap(ConstellationPlan plan);
    public abstract void ClearNetworkMap();
}
