using UnityEngine;

public class ConstellationPlanField
{
    public Vector3 position;
    public int satID;
    public float deltaV;

    public ConstellationPlanField()
    {
        satID = -1;
        deltaV = 200;
    }
}
