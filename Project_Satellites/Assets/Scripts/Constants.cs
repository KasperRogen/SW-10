﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    
    public static int NodesPerCycle = 11;

    public static float EarthRadius;

    public static int TimeScale = 1;

    public static float ScaleToSize(float input)
    {
        return input / 1000000;
    }


}
