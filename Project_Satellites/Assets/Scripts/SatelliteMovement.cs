using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatelliteMovement : MonoBehaviour
{

    public float OrbitalPeriodInMinutes;
    public Vector3 TargetPosition;


    // Start is called before the first frame update
    void Start()
    {
        TargetPosition = transform.position;
    }


    // Update is called once per frame
    void Update()
    {



        if (Vector3.Distance(transform.position, TargetPosition) > 0.01f)
        {
            Debug.DrawLine(transform.position, TargetPosition, Color.green);
            float slerpSpeed = 1;
            float distance = Vector3.Distance(transform.position, TargetPosition);
            float finalSpeed = (distance / slerpSpeed);
            transform.position = Vector3.Slerp(transform.position, TargetPosition, Time.deltaTime / finalSpeed);
        }



    }
}


