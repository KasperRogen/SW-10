/*
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Communication : MonoBehaviour
{

    public List<GameObject> ReachableSats = new List<GameObject>();


    int satID;
    Vector3 newPosition;
    bool executeReceived;
    bool justChangedPlan;

    private void Start()
    {
        satID = GetComponent<SatelliteProperties>().SatID;
    }


    private void OnTriggerEnter(Collider other)
    {
        ReachableSats.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        ReachableSats.Remove(other.gameObject);
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
                if (plan.TotalDeltaVWithChange(pair.Key, pair.Value) < plan.TotalDeltaV())
                {
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
        }
        else
        {
            executeReceived = true;
        }

        GetComponent<SatelliteMovement>().TargetPosition = newPosition;
        NextSat().GetComponent<SatelliteComms>().ReceiveMessage(Constants.Commands.Execute);
    }
}
*/