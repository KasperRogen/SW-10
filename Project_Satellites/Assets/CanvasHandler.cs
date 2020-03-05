using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasHandler : MonoBehaviour
{
    LineRenderer lineRenderer;
    public static CanvasHandler _instance;
    public LayerMask SatelliteLayer;
    public GameObject CallingNode { get; set; }
    public GameObject SatelliteButtons;

    bool interactionButtonsActive;
    // Start is called before the first frame update
    void Start()
    {

        _instance = this;
        lineRenderer = GetComponent<LineRenderer>();
        SatelliteButtons.SetActive(false);
        lineRenderer.positionCount = 2;

    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;

        if (Input.GetMouseButton(1) &&
          Physics.SphereCast(Camera.main.ScreenPointToRay(Input.mousePosition), 0.33f, out hit, float.MaxValue, SatelliteLayer, QueryTriggerInteraction.Ignore))
        {
            SatelliteButtons.SetActive(true);
            CallingNode = hit.transform.gameObject;
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
            CallingNode = null;
        }
    }

    public void DiscoverNode()
    {
        if (CallingNode != null)
        {
            Discovery.StartDiscovery(CallingNode.GetComponent<SatelliteComms>().Node);
            SatelliteButtons.SetActive(false);
            CallingNode = null;
        }
    }

    public void ToggleNode()
    {
        SatelliteComms comms = CallingNode.GetComponent<SatelliteComms>();
        comms.Node.Active = !comms.Node.Active;
        SatelliteButtons.SetActive(false);
        CallingNode = null;
    }


}
