using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ConstellationVisualiser : MonoBehaviour
{

    public Material CommLineMat;
    public Color PassiveMat;
    public Color LeaderMat;
    public Color OverrideMat;
    public Color CommsMat;
    public Color CommsActiveMat;
    public Color ExecuteMat;
    public Color DeadMat;
    public Color HeartbeatMat;

    
    public Node.NodeState state;
    public GameObject MessageGO;

    Dictionary<uint?, GameObject> commLineRenderes = new Dictionary<uint?, GameObject>();
    uint? lastActiveComm;

    LineRenderer targetPositionLineRenderer;

    SatelliteComms comms;
    CommsSim commsSim;
    MeshRenderer meshRenderer;

    Node.NodeState lastState = Node.NodeState.PASSIVE;

    public List<Transform> reachableSats = new List<Transform>();
    public List<uint?> KnownNeighbours = new List<uint?>();
    public List<uint?> KnownNeighbourEntryIDs = new List<uint?>();


    private void Awake()
    {
        GameObject targetLineGO = new GameObject();

        targetLineGO.transform.parent = this.transform;

        targetPositionLineRenderer = targetLineGO.AddComponent<LineRenderer>();
        targetPositionLineRenderer.material = CommLineMat;
    }


    // Start is called before the first frame update
    void Start()
    {
        //commLineRenderer.startWidth = 0.025f;
        //commLineRenderer.endWidth = 0.025f;
        //commLineRenderer.material = CommsMat;


        targetPositionLineRenderer.startWidth = 0.1f;
        targetPositionLineRenderer.endWidth = 0.1f;
        targetPositionLineRenderer.material.SetColor("_BaseColor", LeaderMat);
        targetPositionLineRenderer.material.SetColor("_EmissionColor", LeaderMat);

        comms = GetComponent<SatelliteComms>();
        commsSim = GetComponent<CommsSim>();

        meshRenderer = transform.GetComponentsInChildren<MeshRenderer>().ToList().Find(mesh => mesh.transform.tag == "SatIcon");
    }



    // Update is called once per frame
    void Update()
    {

        state = comms.Node.State;
        foreach (Tuple<Vector3, Vector3, Color> tuple in SatManager._instance.SentMessages)
        {
            StartCoroutine(DisplayMessageSent(tuple.Item1 + Vector3.up, tuple.Item2 + Vector3.up, 0.5f, 0f, tuple.Item3));
        }

        SatManager._instance.SentMessages.Clear();

        reachableSats.Clear();
        //TODO: THIS IS INSANELY EXPENSIVE. SHOULD BE IMPROVED

        //If i have an entry in the neworkmap




        KnownNeighbours = comms.Node.Router.NetworkMap.GetEntryByID(comms.Node.ID)?.Neighbours;
        KnownNeighbourEntryIDs = (comms.Node.Router.NetworkMap.Entries.Where(entry => KnownNeighbours.Contains(entry.ID)).Select(node => node.ID)).ToList();

        reachableSats.Clear();

        foreach (uint? node in KnownNeighbours)
        {
            Transform nodeTransform = SatManager._instance.satellites.Find(sat => sat.GetComponent<SatelliteComms>().Node.ID == node).transform;
            reachableSats.Add(nodeTransform);
        }

        List<uint?> reachableSatsID = new List<uint?>();

        for (int i = 0; i < reachableSats?.Count; i++)
        {
            uint? id = reachableSats[i].GetComponent<SatelliteComms>().Node.ID;
            reachableSatsID.Add(id);

            if (commLineRenderes.ContainsKey(id) == false)
            {
                GameObject commlineGO = new GameObject();
                commlineGO.transform.parent = this.transform;
                LineRenderer lineRenderer = commlineGO.AddComponent<LineRenderer>();

                lineRenderer.material = CommLineMat;
                lineRenderer.material.SetColor("_BaseColor", CommsMat);
                lineRenderer.startWidth = 0.025f;
                lineRenderer.endWidth = 0.025f;




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
                LineRenderer lineRenderer = commLineRenderes[id].GetComponent<LineRenderer>();
                lineRenderer.SetPosition(1, reachableSats[i].position/* + commLineDir.*/);
                lineRenderer.startWidth = 0.025f;
                lineRenderer.endWidth = 0.025f;
            }

        }

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


        if (KnownNeighbours.Count >= 2)
        {
            uint? nextSeq = comms.Node.Router.NextSequential(comms.Node, Router.CommDir.CW); //dies at 5th iteration

            if (nextSeq != null && commLineRenderes.ContainsKey(nextSeq))
            {
                LineRenderer nextSeqCommLine = commLineRenderes[nextSeq].GetComponent<LineRenderer>();
                nextSeqCommLine.startWidth = 0.1f;
                nextSeqCommLine.endWidth = 0.1f;
            }
        }



        switch (comms.Node.State)
        {
            case Node.NodeState.PASSIVE:
                meshRenderer.material.SetColor("_BaseColor", PassiveMat);
                //targetPositionLineRenderer.material.SetColor("_BaseColor", PassiveMat);
                break;

            case Node.NodeState.PLANNING:
                //meshRenderer.material.SetColor("_BaseColor", LeaderMat);
                //targetPositionLineRenderer.material.SetColor("_BaseColor", LeaderMat);
                break;

            case Node.NodeState.EXECUTING:
                meshRenderer.material.SetColor("_BaseColor", ExecuteMat);
                //targetPositionLineRenderer.material.SetColor("_BaseColor", ExecuteMat);
                break;

            case Node.NodeState.OVERRIDE:

                meshRenderer.material.SetColor("_BaseColor", OverrideMat);
                //targetPositionLineRenderer.material.SetColor("_BaseColor", OverrideMat);
                break;

            case Node.NodeState.DEAD:
                meshRenderer.material.SetColor("_BaseColor", DeadMat);
                break;

            case Node.NodeState.HEARTBEAT:
                meshRenderer.material.SetColor("_BaseColor", HeartbeatMat);
                break;
        }


        lastState = comms.Node.State;



        // Turn active communication link on and off 
        if (commsSim.ActiveCommSat != null && commLineRenderes.ContainsKey(commsSim.ActiveCommSat.Node.ID))
        {
            uint? id = commsSim.ActiveCommSat.Node.ID;
            commLineRenderes[id].GetComponent<LineRenderer>().material.SetColor("_BaseColor", CommsActiveMat);
            lastActiveComm = id;
        }
        else if (lastActiveComm != null && commLineRenderes.ContainsKey(lastActiveComm))
        {
            commLineRenderes[lastActiveComm].GetComponent<LineRenderer>().material.SetColor("_BaseColor", CommsMat);
        }

        // Remove all non-reachable links, then update position



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



    public IEnumerator DisplayMessageSent(Vector3 Origin, Vector3 Destination, float duration, float delay, Color color)
    {
        yield return new WaitForSeconds(delay);
        GameObject Message = Instantiate(MessageGO, Origin, Quaternion.identity);
        Message.transform.GetChild(0).GetComponent<Renderer>().material.SetColor("_BaseColor", color);
        float Dist = Vector3.Distance(Origin, Destination);
        while (Vector3.Distance(Message.transform.position, Destination) > 0.1f)
        {
            Message.transform.position = Vector3.MoveTowards(Message.transform.position, Destination, (Dist / duration) * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
        Destroy(Message);
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
