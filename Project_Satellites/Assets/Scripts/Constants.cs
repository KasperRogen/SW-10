using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public static int NodesPerCycle = 11;
    public static float EarthRadius;
    public const int TIME_SCALE = 4;
    public const int ONE_SECOND_IN_MILLISECONDS = 1000;

    public static float ScaleToSize(float input)
    {
        return input / 1000000;
    }


}
