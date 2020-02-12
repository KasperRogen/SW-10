using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstellationVIsualiser : MonoBehaviour
{

    List<Vector3> linerendererPositions = new List<Vector3>();
    LineRenderer lineRenderer;

    SatelliteComms comms;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        comms = GetComponent<SatelliteComms>();
    }

    // Update is called once per frame
    void Update()
    {
        linerendererPositions.Clear();

        for (int i = 0; i < comms.ReachableSats.Count; i++)
        {
            linerendererPositions.Add(transform.position);
            linerendererPositions.Add(comms.ReachableSats[i].transform.position);
        }
        lineRenderer.positionCount = linerendererPositions.Count;
        lineRenderer.SetPositions(linerendererPositions.ToArray());
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Constants.ScaleToSize(comms.CommRadius));
    }

}
