using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatManager : MonoBehaviour
{
    public static SatManager _instance;

    public List<SatelliteComms> satellites = new List<SatelliteComms>();

    private void Start()
    {
        _instance = this;
    }

}
