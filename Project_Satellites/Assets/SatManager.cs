using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SatManager : MonoBehaviour
{
    public static SatManager _instance;

    public List<SatelliteComms> satellites = new List<SatelliteComms>();

    private void Start()
    {
        _instance = this;


        new Thread(() =>
        {
            Thread.Sleep(5000);
            Heartbeat.CheckHeartbeat(satellites[0].Node);
        }).Start();
    }

}
