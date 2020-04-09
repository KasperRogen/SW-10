using System;
using System.Numerics;

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

    // https://stackoverflow.com/questions/5188561/signed-angle-between-two-3d-vectors-with-same-origin-within-the-same-plane
    public static double NumericsVectorSignedAngle(Vector3 b, Vector3 a, Vector3 normal)
    {
        Vector3 Cross = Vector3.Cross(b, a);
        return Math.Atan2(Vector3.Dot(Cross, normal), Vector3.Dot(a, b));
    }
    
}
