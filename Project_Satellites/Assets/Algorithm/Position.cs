using System;

public class Position
{
    public float X, Y, Z;

    public Position(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static float Distance(Position a, Position b)
    {
        return (float)Math.Sqrt(Math.Pow((b.X - a.X), 2) + Math.Pow((b.Y - a.Y), 2) + Math.Pow((b.Z - a.Z), 2));
    }
}
