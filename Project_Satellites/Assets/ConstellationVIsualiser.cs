using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ConstellationVisualiser : MonoBehaviour
{

    public Material PassiveMat;
    public Material LeaderMat;
    public Material OverrideMat;
    public Material CommsMat;

    List<Vector3> linerendererPositions = new List<Vector3>();
    LineRenderer commLineRenderer;
    LineRenderer targetPositionLineRenderer;

    SatelliteComms comms;
    MeshRenderer meshRenderer;

    
    private void Awake()
    {
        GameObject commLineGO = Instantiate(new GameObject(), transform);
        GameObject targetLineGO = Instantiate(new GameObject(), transform);

        commLineRenderer = commLineGO.AddComponent<LineRenderer>();
        targetPositionLineRenderer = targetLineGO.AddComponent<LineRenderer>();

    }


    // Start is called before the first frame update
    void Start()
    {
        commLineRenderer.startWidth = 0.025f;
        commLineRenderer.endWidth = 0.025f;
        commLineRenderer.material = CommsMat;

        targetPositionLineRenderer.startWidth = 0.1f;
        targetPositionLineRenderer.endWidth = 0.1f;
        targetPositionLineRenderer.material = PassiveMat;
        

        comms = GetComponent<SatelliteComms>();

        meshRenderer = GetComponent<MeshRenderer>();


    }



    // Update is called once per frame
    void Update()
    {
        if (comms == null || comms.Node == null)
            return;
        switch (comms.Node.State)
        {
            case Node.NodeState.PASSIVE:
                meshRenderer.material = PassiveMat;
                targetPositionLineRenderer.material = PassiveMat;
                break;

            case Node.NodeState.PLANNING:
                meshRenderer.material = LeaderMat;
                targetPositionLineRenderer.material = LeaderMat;
                break;

            case Node.NodeState.EXECUTING:

                break;

            case Node.NodeState.OVERRIDE:
                meshRenderer.material = OverrideMat;
                targetPositionLineRenderer.material = OverrideMat;
                break;
        }

        linerendererPositions.Clear();

        for (int i = 0; i < comms.ReachableSats.Count; i++)
        {
            linerendererPositions.Add(transform.position);
            linerendererPositions.Add(comms.ReachableSats[i].transform.position);
        }
        commLineRenderer.positionCount = linerendererPositions.Count;
        commLineRenderer.SetPositions(linerendererPositions.ToArray());


        if(comms.Node.Plan != null) { 

            Vector3 plannedposition = transform.position;

            foreach(ConstellationPlanEntry e in comms.Node.Plan.entries)
            {
                if(e.NodeID == comms.Node.ID)
                {
                    plannedposition = BackendHelpers.Vector3FromPosition(e.Position);
                }
            }

            targetPositionLineRenderer.positionCount = 2;
            targetPositionLineRenderer.SetPositions(new Vector3[] { transform.position, plannedposition });


            TargetConstellationGenerator.CurrentDeltaVSum = comms.Node.Plan.entries.Sum(entry => entry.Fields["DeltaV"].Value);
        }


    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Constants.ScaleToSize(comms.CommRadius));
    }

}
