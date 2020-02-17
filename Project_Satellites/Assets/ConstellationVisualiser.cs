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
    public Material ExecuteMat;

    List<Vector3> linerendererPositions = new List<Vector3>();
    LineRenderer commLineRenderer;
    LineRenderer targetPositionLineRenderer;

    SatelliteComms comms;
    MeshRenderer meshRenderer;

    Node.NodeState lastState = Node.NodeState.PASSIVE;
    
    private void Awake()
    {
        GameObject commLineGO = new GameObject();
        GameObject targetLineGO = new GameObject();

        commLineGO.transform.parent = this.transform;
        targetLineGO.transform.parent = this.transform;

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

        comms.ReachableSats = comms.Node.router.NetworkMap?[comms.Node].Select(node => BackendHelpers.Vector3FromPosition(node.Position)).ToList();


        if (comms == null || comms.Node == null)
            return;

        //if (comms.Node.State != lastState) { 
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
                    meshRenderer.material = ExecuteMat;
                    targetPositionLineRenderer.material = ExecuteMat;
                break;

                case Node.NodeState.OVERRIDE:

                    meshRenderer.material = OverrideMat;
                    targetPositionLineRenderer.material = OverrideMat;
                break;
            }
        //}

        lastState = comms.Node.State;

        linerendererPositions.Clear();

        for (int i = 0; i < comms.ReachableSats?.Count; i++)
        {
            linerendererPositions.Add(transform.position);
            linerendererPositions.Add(comms.ReachableSats[i]);
        }
        commLineRenderer.positionCount = linerendererPositions.Count;
        commLineRenderer.SetPositions(linerendererPositions.ToArray());


        if(comms.Node.Plan != null) 
        { 

            Vector3 plannedposition = transform.position;

            foreach(ConstellationPlanEntry e in comms.Node.Plan.entries)
            {
                if(e.Node != null && e.Node.ID == comms.Node.ID)
                {
                    plannedposition = BackendHelpers.Vector3FromPosition(e.Position);
                }
            }

            targetPositionLineRenderer.positionCount = 2;
            targetPositionLineRenderer.SetPositions(new Vector3[] { transform.position, plannedposition });

            float DeltaVSum = comms.Node.Plan.entries.Sum(entry => entry.Fields["DeltaV"].Value);

            if (DeltaVSum != TargetConstellationGenerator.CurrentDeltaVSum && DeltaVSum != 1100f)
                TargetConstellationGenerator.CurrentDeltaVSum = DeltaVSum;
        }


    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Constants.ScaleToSize(comms.CommRadius));
    }

}
