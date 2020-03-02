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
            transform.position = Vector3.MoveTowards(transform.position, TargetPosition, 2 * Time.deltaTime);
        }
    }
}


