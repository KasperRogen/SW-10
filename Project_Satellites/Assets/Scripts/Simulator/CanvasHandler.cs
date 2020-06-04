using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasHandler : MonoBehaviour
{
    LineRenderer lineRenderer;
    public static CanvasHandler _instance;
    public LayerMask SatelliteLayer;
    public LayerMask BackGroundLayer;
    public GameObject CallingNode { get; set; }
    public GameObject SatelliteButtons;
    public GameObject SatelliteToggles;
    public static bool AutoChecksAllowed = false;

    bool interactionButtonsActive;
    // Start is called before the first frame update
    void Start()
    {

        _instance = this;
        lineRenderer = GetComponent<LineRenderer>();
        SatelliteButtons.SetActive(false);
        SatelliteToggles.SetActive(false);
        lineRenderer.positionCount = 2;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1) &&
            Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, float.MaxValue, SatelliteLayer, QueryTriggerInteraction.Ignore))
        {
            SatelliteToggles.SetActive(true);
            CallingNode = hit.transform.gameObject;
            SatelliteButtons.SetActive(CallingNode.GetComponent<SatelliteComms>().Node.State != Node.NodeState.DEAD);
        }
        else if (Input.GetMouseButtonDown(1) && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, BackGroundLayer, QueryTriggerInteraction.Ignore))
        {
            if(CallingNode == null) { 
                Debug.Log("Spawning");
                ConstellationGenerator.InstantiateSatellite(hit.point);
            } else
            {
                CallingNode = null;
                SatelliteButtons.SetActive(false);
                SatelliteToggles.SetActive(false);
            }
        }

        lineRenderer.SetPositions(new Vector3[] { Vector3.up * 10,
        CallingNode == null ? Vector3.up * 10 : CallingNode.transform.position + Vector3.up * 10 });
    }



    public void HeartbeatNode()
    {
        if (CallingNode != null)
        {
            Heartbeat.CheckHeartbeat(CallingNode.GetComponent<SatelliteComms>().Node);
            SatelliteButtons.SetActive(false);
            SatelliteToggles.SetActive(false);
            CallingNode = null;
        }
    }

    public void DiscoverNode()
    {
        if (CallingNode != null)
        {
            Discovery.StartDiscovery(CallingNode.GetComponent<SatelliteComms>().Node, true);
            SatelliteButtons.SetActive(false);
            SatelliteToggles.SetActive(false);
            CallingNode = null;
        }
    }

    public void ToggleNode()
    {
        if (CallingNode != null)
        {
            SatelliteComms comms = CallingNode.GetComponent<SatelliteComms>();
            comms.Node.Active = !comms.Node.Active;
            SatelliteButtons.SetActive(false);
            SatelliteToggles.SetActive(false);
            CallingNode = null;
        }
    }

    public void GenerateNode()
    {
        if (CallingNode != null)
        {
            SatelliteComms comms = CallingNode.GetComponent<SatelliteComms>();
            TargetConstellationGenerator.instance.GenerateTargetConstellation(CallingNode.GetComponent<SatelliteComms>().Node);
            SatelliteButtons.SetActive(false);
            SatelliteToggles.SetActive(false);
            CallingNode = null;
        }
    }

    public void RemoveNode()
    {
        if (CallingNode != null)
        {
            uint? CallingNodeID = CallingNode.GetComponent<SatelliteComms>().Node.Id;
            foreach (SatelliteComms satellite in SatManager._instance.satellites)
            {
                satellite.Node.Router.NetworkMap.Entries.RemoveAll(entry =>
                    entry.ID == CallingNodeID);

                foreach (NetworkMapEntry entry in satellite.Node.Router.NetworkMap.Entries)
                {
                    entry.Neighbours.RemoveAll(neighbour =>
                        neighbour == CallingNodeID);
                }

                satellite.Node.ActivePlan?.Entries?.RemoveAll(entry => entry.NodeID == CallingNodeID);

                satellite.Node.Router.NodeToNodeIDMapping.Clear();
                satellite.Node.Router.UpdateGraph();
            }

            SatManager._instance.satellites.RemoveAll(sat => sat.Node.Id == CallingNodeID);
            Destroy(CallingNode);
            SatelliteButtons.SetActive(false);
            SatelliteToggles.SetActive(false);
            CallingNode = null;
        }
    }


}
