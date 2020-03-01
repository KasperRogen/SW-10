using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class SatelliteComms : MonoBehaviour
{

    [Header("Satellite Settings"), Space(10)]
    [Min(0)] public float CommRadius;

    public List<Transform> ReachableSats = new List<Transform>();
    SatelliteMovement movement;

    [SerializeField]
    public INode Node;

    private void Start()
    {
        GetComponents<SphereCollider>().ToList().Find(col => col.isTrigger).radius = (transform.localScale.x * CommRadius);
        movement = GetComponent<SatelliteMovement>();
        GetComponentInChildren<TextMeshPro>().text = Node.ID.ToString();
    }

    private void Update()
    {
        if (Node.TargetPosition != null)
            movement.TargetPosition = BackendHelpers.Vector3FromPosition(Node.TargetPosition);

        Node.Position = BackendHelpers.PositionFromVector3(transform.position);        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || other.gameObject.layer != gameObject.layer)
            return;
        ReachableSats.Add(other.gameObject.transform);
        UpdateReachableNodes();
    }

    private void OnTriggerExit(Collider other)
    {
        ReachableSats.Remove(other.gameObject.transform);
        UpdateReachableNodes();
    }

    private void UpdateReachableNodes()
    {
        Node.ReachableNodes = ReachableSats.Select((x) => x.GetComponent<SatelliteComms>().Node.ID).ToList();
    }

    private void OnEnable()
    {
        SatManager._instance.satellites.Add(this);
    }

    private void OnDisable()
    {
        SatManager._instance.satellites.Remove(this);
    }
}
