using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class SatelliteComms : MonoBehaviour
{

    [Header("Satellite Settings"), Space(10)]
    [Min(0)] public float CommRadius;

    TextMeshPro text;

    SatelliteMovement movement;

    [SerializeField]
    public INode Node;

    private void Start()
    {
        GetComponents<SphereCollider>().ToList().Find(col => col.isTrigger).radius = (transform.localScale.x * Constants.ScaleToSize(CommRadius));
        movement = GetComponent<SatelliteMovement>();
        text = GetComponentInChildren<TextMeshPro>();
    }

    private void Update()
    {
        if (Node.TargetPosition != null)
            movement.TargetPosition = BackendHelpers.UnityVectorFromNumerics(Node.TargetPosition);

        Node.Position = BackendHelpers.NumericsVectorFromUnity(transform.position);
        text.text = Node.ID.ToString() + "\n" + Node.State.ToString();
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
