using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public static int NODES_PER_CYCLE = 11;
    public static float EarthRadius;
    public static int TimeScale = 5;
    public const int ONE_SECOND_IN_MILLISECONDS = 1000;
    public const int COMMS_TIMEOUT = 11000;
    public const int COMMS_ATTEMPTS = 3;
    public const int ONE_MINUTE_IN_MILLISECONDS = 60000;
    public const int SEND_DURATION_TIME = 5000;

    public static bool EnableDebug = false;

    public static float ScaleToSize(float input)
    {
        return input / 1000000;
    }

    public static float SatCommsRange = 5000000;


}
