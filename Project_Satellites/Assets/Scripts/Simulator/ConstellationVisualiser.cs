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

    Dictionary<uint?, LineRenderer> commLineRenderes = new Dictionary<uint?, LineRenderer>();
    uint? lastActiveComm;

    LineRenderer targetPositionLineRenderer;

    SatelliteComms comms;
    CommsSim commsSim;
    MeshRenderer meshRenderer;

    Node.NodeState lastState = Node.NodeState.PASSIVE;

    private List<SatelliteComms> reachableSats = new List<SatelliteComms>();
    public List<uint?> KnownNeighbours = new List<uint?>();


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

        SatManager.MessageProps[] messageSnapshot = SatManager._instance.SentMessages.ToArray();

        if (messageSnapshot.Length > 0)
        {
            foreach (SatManager.MessageProps props in messageSnapshot)
            {
                StartCoroutine(DisplayMessageSent(props.StartVect + Vector3.up, props.EndVect + Vector3.up, props.Duration, 0f, props.Color));
            }
        }


        SatManager._instance.SentMessages.Clear();

        reachableSats.Clear();



        KnownNeighbours = comms.Node.Router.NetworkMap.GetEntryByID(comms.Node.Id)?.Neighbours;

        reachableSats.Clear();


        foreach (SatelliteComms node in reachableSats.Where(sat => KnownNeighbours.Contains(sat.Node.Id) == false))
        {
            reachableSats.Remove(node);
        }

        uint? toRemove = null;

        foreach (uint? node in KnownNeighbours)
        {
            if (reachableSats.Select(sat => sat.Node.Id).Contains(node) == false)
            {
                if (SatManager._instance.satellites.Any(sat => sat.Node.Id == node) == false)
                {
                    toRemove = node;
                }
                else
                {
                    Transform nodeTransform = SatManager._instance.satellites.Find(sat => sat.Node.Id == node).transform;
                    reachableSats.Add(nodeTransform.GetComponent<SatelliteComms>());
                }

            }
        }

        if (toRemove != null)
        {
            KnownNeighbours.Remove(toRemove);
        }

        List<uint?> reachableSatsID = new List<uint?>();

        for (int i = 0; i < reachableSats?.Count; i++)
        {
            uint? id = reachableSats[i].Node.Id;
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

                commLineRenderes.Add(id, lineRenderer);
            }
            else
            {
                Transform _transform = reachableSats[i].transform;

                Vector3 commLineDir = new Vector3
                {
                    x = _transform.position.x - comms.Node.Position.X,
                    y = _transform.position.y - comms.Node.Position.Y,
                    z = _transform.position.z - comms.Node.Position.Z,
                };

                LineRenderer lineRenderer = commLineRenderes[id];
                lineRenderer.SetPosition(1, _transform.position);
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
                commLineRenderes[key].SetPosition(0, this.transform.position);
            }
        }

        uint? nextSeq = comms.Node.Router.NextSequential(comms.Node, Router.CommDir.CW);

        if (nextSeq != null && commLineRenderes.ContainsKey(nextSeq))
        {
            LineRenderer nextSeqCommLine = commLineRenderes[nextSeq];
            nextSeqCommLine.startWidth = 0.1f;
            nextSeqCommLine.endWidth = 0.1f;
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
        if (commsSim.ActiveCommSat != null && commLineRenderes.ContainsKey(commsSim.ActiveCommSat.Node.Id))
        {
            uint? id = commsSim.ActiveCommSat.Node.Id;
            commLineRenderes[id].material.SetColor("_BaseColor", CommsActiveMat);
            lastActiveComm = id;
        }
        else if (lastActiveComm != null && commLineRenderes.ContainsKey(lastActiveComm))
        {
            commLineRenderes[lastActiveComm].material.SetColor("_BaseColor", CommsMat);
        }

        // Remove all non-reachable links, then update position



        if (comms.Node.GeneratingPlan != null)
        {

            Vector3 plannedposition = transform.position;

            foreach (ConstellationPlanEntry e in comms.Node.GeneratingPlan.Entries)
            {
                if (e.NodeID != null && e.NodeID == comms.Node.Id)
                {
                    plannedposition = BackendHelpers.UnityVectorFromNumerics(e.Position);
                }
            }

            targetPositionLineRenderer.positionCount = 2;
            targetPositionLineRenderer.SetPositions(new Vector3[] { transform.position, plannedposition });
        }
    }



    public IEnumerator DisplayMessageSent(Vector3 Origin, Vector3 Destination, float duration, float delay, Color color)
    {
        yield return new WaitForSeconds(delay / Constants.TimeScale);
        GameObject Message = Instantiate(MessageGO, Origin, Quaternion.identity);
        Message.transform.rotation = Camera.main.transform.parent.rotation;
        Message.transform.GetChild(0).GetComponent<Renderer>().material.SetColor("_BaseColor", color);
        float Dist = Vector3.Distance(Origin, Destination);
        while (Vector3.Distance(Message.transform.position, Destination) > 0.1f)
        {
            Message.transform.position = Vector3.MoveTowards(Message.transform.position, Destination, (Dist / (duration / 1000)) * Time.deltaTime);
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
        if (Constants.EnableDebug)
            Debug.DrawRay(pos, dir, Color.red, 1);
    }
}
