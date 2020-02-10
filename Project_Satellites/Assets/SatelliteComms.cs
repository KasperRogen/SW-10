using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatelliteComms : MonoBehaviour
{

    [Header("Satellite Settings"), Space(10)]
    [Min(0)] public float CommRadius;

    public List<GameObject> ReachableSats = new List<GameObject>();
    List<LineRenderer> lineRenderers = new List<LineRenderer>();

    List<Vector3> linerendererPositions = new List<Vector3>();
    LineRenderer lineRenderer;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {

        linerendererPositions.Clear();

        for (int i = 0; i < ReachableSats.Count; i++)
        {
            linerendererPositions.Add(transform.position);
            linerendererPositions.Add(ReachableSats[i].transform.position);
        }
        lineRenderer.positionCount = linerendererPositions.Count;
        lineRenderer.SetPositions(linerendererPositions.ToArray());

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || other.gameObject.layer != gameObject.layer)
            return;
        ReachableSats.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        ReachableSats.Remove(other.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Constants.ScaleToSize(CommRadius));
    }



    public void ReceiveMessage(List<Vector3> TargetConstellation)
    {

    }




}
