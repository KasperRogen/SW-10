using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatelliteMovement : MonoBehaviour
{

    public float OrbitalPeriodInMinutes;


    GameObject Earth;

    // Start is called before the first frame update
    void Start()
    {
        Earth = GameObject.FindGameObjectWithTag("Earth");
    }

    // Update is called once per frame
    void Update()
    {
        bool SimMinsToSecs = SimulationManager._instance.SimMinutesToSeconds;

        float orbitalPeriodInSeconds = OrbitalPeriodInMinutes * 60;
        float rotationAngle = (360 / (orbitalPeriodInSeconds / (SimMinsToSecs ? 60 : 1))) * Time.deltaTime;
        transform.RotateAround(Earth.transform.position, Vector3.up, rotationAngle);
    }
}


