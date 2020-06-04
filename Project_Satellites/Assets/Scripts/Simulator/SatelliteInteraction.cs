using UnityEngine;

public class SatelliteKiller : MonoBehaviour
{
    public LayerMask SatelliteLayer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) &&
            Physics.SphereCast(Camera.main.ScreenPointToRay(Input.mousePosition), 0.33f, out RaycastHit hit, float.MaxValue, SatelliteLayer, QueryTriggerInteraction.Ignore))
        {
            SatelliteComms comms = hit.transform.gameObject.GetComponent<SatelliteComms>();
            comms.Node.Active = !comms.Node.Active;
        }
    }
}
