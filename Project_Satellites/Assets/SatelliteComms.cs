using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SatelliteComms : MonoBehaviour
{

    [Header("Satellite Settings"), Space(10)]
    [Min(0)] public float CommRadius;

    public List<GameObject> ReachableSats = new List<GameObject>();

    List<Vector3> linerendererPositions = new List<Vector3>();
    LineRenderer lineRenderer;

    public INode Node;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        GetComponents<SphereCollider>().ToList().Find(col => col.isTrigger).radius = (transform.localScale.x * CommRadius);
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
        UpdateReachableNodes();
    }

    private void OnTriggerExit(Collider other)
    {
        ReachableSats.Remove(other.gameObject);
        UpdateReachableNodes();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Constants.ScaleToSize(CommRadius));
    }

    private void UpdateReachableNodes()
    {
        Node.ReachableNodes = ReachableSats.Select((x) => x.GetComponent<SatelliteComms>().Node).ToList();
    }
}
