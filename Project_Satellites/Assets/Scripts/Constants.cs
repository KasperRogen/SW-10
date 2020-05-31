using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public static int NODES_PER_CYCLE = 11;
    public static float EarthRadius;
    public const int TIME_SCALE = 25;
    public const int ONE_SECOND_IN_MILLISECONDS = 1000;
    public const int COMMS_TIMEOUT = 1000;
    public const int COMMS_ATTEMPTS = 3;
    public const int ONE_MINUTE_IN_MILLISECONDS = 60000;

    public static bool EnableDebug = true;

    public static float ScaleToSize(float input)
    {
        return input / 1000000;
    }

    public static float SatCommsRange = 5000000;


}
