using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SatManager : MonoBehaviour
{
    public static SatManager _instance;

    public List<SatelliteComms> satellites = new List<SatelliteComms>();
    public List<Tuple<Vector3, Vector3, Color>> SentMessages = new List<Tuple<Vector3, Vector3, Color>>();
    public int SatIndex = 0;

    private void Start()
    {
        _instance = this;


    }

}
