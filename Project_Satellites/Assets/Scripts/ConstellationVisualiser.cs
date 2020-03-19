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
    public Material CommsActiveMat;
    public Material ExecuteMat;
    public Material DeadMat;
    public Material HeartbeatMat;

    Dictionary<uint?, GameObject> commLineRenderes = new Dictionary<uint?, GameObject>();
    uint? lastActiveComm;

    LineRenderer targetPositionLineRenderer;

    SatelliteComms comms;
    CommsSim commsSim;
    MeshRenderer meshRenderer;

    Node.NodeState lastState = Node.NodeState.PASSIVE;

    public List<Transform> reachableSats = new List<Transform>();

    private void Awake()
    {
        GameObject targetLineGO = new GameObject();

        targetLineGO.transform.parent = this.transform;

        targetPositionLineRenderer = targetLineGO.AddComponent<LineRenderer>();

    }


    // Start is called before the first frame update
    void Start()
    {
        //commLineRenderer.startWidth = 0.025f;
        //commLineRenderer.endWidth = 0.025f;
        //commLineRenderer.material = CommsMat;

        targetPositionLineRenderer.startWidth = 0.1f;
        targetPositionLineRenderer.endWidth = 0.1f;
        targetPositionLineRenderer.material = PassiveMat;

        comms = GetComponent<SatelliteComms>();
        commsSim = GetComponent<CommsSim>();

        meshRenderer = GetComponent<MeshRenderer>();
    }



    // Update is called once per frame
    void Update()
    {

        reachableSats.Clear();
        //TODO: THIS IS INSANELY EXPENSIVE. SHOULD BE IMPROVED

        if (comms.Node.Router.NetworkMap.Entries.Select(entry => entry.ID).Contains(comms.Node.ID))
        {
            foreach (uint? node in comms.Node.Router?.NetworkMap?.GetEntryByID(comms.Node.ID).Neighbours)
            {
                Transform nodeTransform = SatManager._instance.satellites.Find(sat => sat.GetComponent<SatelliteComms>().Node.ID == node).transform;
                reachableSats.Add(nodeTransform);
            }

        }



        //comms.ReachableSats = comms.Node.router.NetworkMap?[comms.Node].Select(node => BackendHelpers.Vector3FromPosition(node.Position)).ToList();


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

            case Node.NodeState.DEAD:
                meshRenderer.material = DeadMat;
                break;

            case Node.NodeState.HEARTBEAT:
                meshRenderer.material = HeartbeatMat;
                break;
        }
        //}

        lastState = comms.Node.State;

        List<uint?> reachableSatsID = new List<uint?>();

        // Create a gameobject with a linerendere for each reachable sat if not already added
        for (int i = 0; i < reachableSats?.Count; i++)
        {
            uint? id = reachableSats[i].GetComponent<SatelliteComms>().Node.ID;
            reachableSatsID.Add(id);

            if (commLineRenderes.ContainsKey(id) == false)
            {
                GameObject commlineGO = new GameObject();
                commlineGO.transform.parent = this.transform;
                LineRenderer lineRenderer = commlineGO.AddComponent<LineRenderer>();

                lineRenderer.startWidth = 0.025f;
                lineRenderer.endWidth = 0.025f;
                lineRenderer.material = CommsMat;

                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, this.transform.position);
                lineRenderer.SetPosition(1, reachableSats[i].transform.position);

                commLineRenderes.Add(id, commlineGO);
            }
            else
            {
                Vector3 commLineDir = new Vector3
                {
                    x = reachableSats[i].position.x - comms.Node.Position.X,
                    y = reachableSats[i].position.y - comms.Node.Position.Y,
                    z = reachableSats[i].position.z - comms.Node.Position.Z,
                };

                //TODO: something something vector3.right to commlinedir
                commLineRenderes[id].GetComponent<LineRenderer>().SetPosition(1, reachableSats[i].position/* + commLineDir.*/);
            }

        }

        // Turn active communication link on and off 
        if (commsSim.ActiveCommSat != null && commLineRenderes.ContainsKey(commsSim.ActiveCommSat.Node.ID))
        {
            uint? id = commsSim.ActiveCommSat.Node.ID;
            commLineRenderes[id].GetComponent<LineRenderer>().material = CommsActiveMat;
            lastActiveComm = id;
        }
        else if (lastActiveComm != null && commLineRenderes.ContainsKey(lastActiveComm))
        {
            commLineRenderes[lastActiveComm].GetComponent<LineRenderer>().material = CommsMat;
        }

        // Remove all non-reachable links, then update position
        for (int i = commLineRenderes.Count - 1; i >= 0; i--)
        {
            if (comms.Node.Active == false)
            {
                Destroy(commLineRenderes.ElementAt(i).Value);
                commLineRenderes.Remove(commLineRenderes.ElementAt(i).Key);
                continue;
            }

            uint? key = commLineRenderes.ElementAt(i).Key;

            if (reachableSatsID.Contains(key) == false)
            {
                Destroy(commLineRenderes[key]);
                commLineRenderes.Remove(key);
            }
            else
            {
                commLineRenderes[key].GetComponent<LineRenderer>().SetPosition(0, this.transform.position);
            }
        }

        //foreach (var commLine in commLineRenderes)
        //{
        //if (reachableSatsID.Contains(commLine.Key) == false)
        //{
        //    Destroy(commLine.Value);
        //    commLineRenderes.Remove(commLine.Key);
        //}

        //commLine.Value.GetComponent<LineRenderer>().SetPosition(0, this.transform.position);
        //}


        if (comms.Node.GeneratingPlan != null)
        {

            Vector3 plannedposition = transform.position;

            foreach (ConstellationPlanEntry e in comms.Node.GeneratingPlan.Entries)
            {
                if (e.NodeID != null && e.NodeID == comms.Node.ID)
                {
                    plannedposition = BackendHelpers.UnityVectorFromNumerics(e.Position);
                }
            }

            targetPositionLineRenderer.positionCount = 2;
            targetPositionLineRenderer.SetPositions(new Vector3[] { transform.position, plannedposition });

            float DeltaVSum = comms.Node.GeneratingPlan.Entries.Sum(entry => entry.Fields["DeltaV"].Value);

            if (DeltaVSum != TargetConstellationGenerator.CurrentDeltaVSum && DeltaVSum != 1100f)
                TargetConstellationGenerator.CurrentDeltaVSum = DeltaVSum;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Constants.ScaleToSize(comms.CommRadius));
    }

    public static void DrawLine(Vector3 pos, Vector3 dir)
    {
        Debug.DrawRay(pos, dir, Color.red, 1);
    }
}
