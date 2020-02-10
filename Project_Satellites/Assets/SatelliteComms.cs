using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SatelliteComms : MonoBehaviour
{

    [Header("Satellite Settings"), Space(10)]
    [Min(0)] public float CommRadius;

    public List<GameObject> ReachableSats = new List<GameObject>();
    List<LineRenderer> lineRenderers = new List<LineRenderer>();

    List<Vector3> linerendererPositions = new List<Vector3>();
    LineRenderer lineRenderer;

    Vector3 newPosition;

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

    public void ReceiveMessage(Constants.Commands command, ConstellationPlan plan)
    {
        if (command != Constants.Commands.Generate)
        {
            Debug.LogError("Wrong command");
            return;
        }

        Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

        for (int i = 0; i < plan.fields.Count; i++)
        {
            float requiredDeltaV = Vector3.Distance(transform.position, plan.fields[i].position);
            fieldDeltaVPairs.Add(i, requiredDeltaV);
        }

        foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))
        {
            if (pair.Value < plan.fields[pair.Key].deltaV)
            {
                plan.fields[pair.Key].deltaV = pair.Value;
                newPosition = plan.fields[pair.Key].position;
                break;
            }
        }
    }

    public void ReceiveMessage(Constants.Commands command)
    {
        if (command != Constants.Commands.Execute)
        {
            Debug.LogError("Wrong command");
            return;
        }

        GetComponent<SatelliteMovement>().TargetPosition = newPosition;
    }
}
