using UnityEngine;

public class ConstellationPlanField
{
    public Position Position;
    public int satID;
    public float deltaV;

    public ConstellationPlanField(Position position)
    {
        Position = position;
        satID = -1;
        deltaV = float.MaxValue;
    }
}
