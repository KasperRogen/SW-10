using System;

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
    public static double NumericsVectorSignedAngle(System.Numerics.Vector3 a, System.Numerics.Vector3 b, System.Numerics.Vector3 normal)
    {
        return Math.Atan2(System.Numerics.Vector3.Dot(System.Numerics.Vector3.Cross(a, b), normal), System.Numerics.Vector3.Dot(a, b));
    }
    
}
