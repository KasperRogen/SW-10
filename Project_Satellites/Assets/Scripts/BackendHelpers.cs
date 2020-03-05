using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackendHelpers
{
    public static UnityEngine.Vector3 UnityVectorFromNumerics(System.Numerics.Vector3 pos)
    {
        return new UnityEngine.Vector3(pos.X, pos.Y, pos.Z);
    }

    public static System.Numerics.Vector3 NumericsVectorFromUnity(UnityEngine.Vector3 pos)
    {
        return new System.Numerics.Vector3(pos.x, pos.y, pos.z);
    }
}
