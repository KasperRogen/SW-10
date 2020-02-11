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

    int satID;
    Vector3 newPosition;
    bool executeReceived;
    bool justChangedPlan;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        satID = GetComponent<SatelliteProperties>().SatID;
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
    }

    private void OnTriggerExit(Collider other)
    {
        ReachableSats.Remove(other.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Constants.ScaleToSize(CommRadius));
    }

    GameObject NextSat()
    {
        int NextSatId = (satID + 1) % GetComponent<SatelliteProperties>().SatsPerPlane;
        return ReachableSats.Find(x => x.GetComponent<SatelliteProperties>().SatID == NextSatId);
    }

    public void ReceiveMessage(Constants.Commands command, ConstellationPlan plan)
    {
        if (command != Constants.Commands.Generate)
        {
            Debug.LogError("Wrong command");
            return;
        }

        executeReceived = false;

        Dictionary<int, float> fieldDeltaVPairs = new Dictionary<int, float>();

        for (int i = 0; i < plan.fields.Count; i++)
        {
            float requiredDeltaV = Mathf.Pow(Vector3.Distance(transform.position, plan.fields[i].position), 3);
            fieldDeltaVPairs.Add(i, requiredDeltaV);
        }

        if (plan.fields.Any(x => x.satID == satID) == false)
        {
            foreach (KeyValuePair<int, float> pair in fieldDeltaVPairs.OrderBy(x => x.Value))
            {
                //if (pair.Value < plan.fields[pair.Key].deltaV)
                //{
                //    plan.fields[pair.Key].satID = satID;
                //    plan.fields[pair.Key].deltaV = pair.Value;
                //    newPosition = plan.fields[pair.Key].position;
                //    break;
                //}
                if (plan.TotalDeltaVWithChange(pair.Key, pair.Value) < plan.TotalDeltaV()){
                    plan.fields[pair.Key].satID = satID;
                    plan.fields[pair.Key].deltaV = pair.Value;
                    newPosition = plan.fields[pair.Key].position;
                    plan.lastEditedBy = satID;
                    justChangedPlan = true;
                    break;
                }
            }
        }

        if (plan.lastEditedBy == satID && justChangedPlan == false)
        {
            justChangedPlan = false;
            ReceiveMessage(Constants.Commands.Execute);
        }
        else
        {
            justChangedPlan = false;
            NextSat().GetComponent<SatelliteComms>().ReceiveMessage(Constants.Commands.Generate, plan);
        }
    }

    public void ReceiveMessage(Constants.Commands command)
    {
        if (command != Constants.Commands.Execute)
        {
            Debug.LogError("Wrong command");
            return;
        }

        if (executeReceived)
        {
            return;
        } else
        {
            executeReceived = true;
        }

        GetComponent<SatelliteMovement>().TargetPosition = newPosition;
        NextSat().GetComponent<SatelliteComms>().ReceiveMessage(Constants.Commands.Execute);
    }
}
