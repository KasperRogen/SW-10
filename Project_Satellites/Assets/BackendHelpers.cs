using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackendHelpers : MonoBehaviour
{
    public static Vector3 Vector3FromPosition(Position pos)
    {
        return new Vector3(pos.X, pos.Y, pos.Z);
    }

    public static Position PositionFromVector3(Vector3 pos)
    {
        return new Position(pos.x, pos.y, pos.z);
    }
}
