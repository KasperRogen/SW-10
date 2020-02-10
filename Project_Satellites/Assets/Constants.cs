using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public enum Commands
    {
        Generate, Execute
    }

    public static float EarthRadius;

    public static float ScaleToSize(float input)
    {
        return input / 1000000;
    }


}
