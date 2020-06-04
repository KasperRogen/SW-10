using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;


public class TargetConstellationGenerator : MonoBehaviour
{
    public static TargetConstellationGenerator instance;



    public bool EnableAutotest = false;
    public bool EnableManualDesign = false;
    public LayerMask ManualDesignMask;
    
    ConstellationPlan plan = null;
    public int RandomSeed;
    public GameObject SatLocationPlaceholderPrefab;

    List<GameObject> Sats = new List<GameObject>();
    List<INode> nodes = new List<INode>();
    string ConstellationAltitudeInput = "781000";

    public static float CurrentDeltaVSum;


    List<Vector3> TargetPositions = new List<Vector3>();


    public void GenerateTargetConstellation(INode RequesterNode)
    {
        System.Random r = new System.Random(RandomSeed);

        TargetPositions.Clear();
        
        //If the constellation altitude input from textfield isn't numbers, return
        if (float.TryParse(ConstellationAltitudeInput, out float constellationAltitude) == false)
            return;

        //Set the constellation altitude based on the input textfield
        constellationAltitude = Constants.EarthRadius + Constants.ScaleToSize(constellationAltitude);


        //Get reference to satellites
            Sats = GameObject.FindGameObjectsWithTag("Satellite").ToList();

        List<uint?> reachableNodes = RequesterNode.Router.ReachableSats(RequesterNode).ToList();

        Sats.RemoveAll(sat => reachableNodes.Contains(sat.GetComponent<SatelliteComms>().Node.Id) == false);


        //Remove old location Placeholders
        GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().ForEach(Destroy);


        for (int i = 0; i < Sats.Count; i++)
        {
            //Create random angle for position of Targetposition
            float angle = (360 / Sats.Count) * i;

            Vector3 instantiationPos = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            //Set it relative to the earth
            Vector3 instantiationVector = (instantiationPos - Vector3.zero).normalized * constellationAltitude;

            //Store for propagation
            TargetPositions.Add(instantiationVector);
            Instantiate(SatLocationPlaceholderPrefab, instantiationVector, Quaternion.identity);
        }

        List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();
            
        foreach (Vector3 pos in TargetPositions)
        {
            Vector3 position = new Vector3(pos.x, pos.y, pos.z);
            List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => x.CompareTo(y)) };
            ConstellationPlanEntry entry = new ConstellationPlanEntry(BackendHelpers.NumericsVectorFromUnity(position), fields, (x, y) => 1);
            entries.Add(entry);
        }

        plan = new ConstellationPlan(entries);

        //Send the targetconstellation to random sat
        INode targetSat = RequesterNode;
        PlanRequest request = new PlanRequest {
            Command = Request.Commands.GENERATE,
            SourceID = targetSat.Id,
            DestinationID = targetSat.Id,
            SenderID = 42,
            Dir = Router.CommDir.CW,
            AckExpected = true,
            MessageIdentifer = "42",
            Plan = plan
        };
        PlanGenerator.GeneratePlan(targetSat, request);
    }


    public static void Clear()
    {
        //Remove old location Placeholders
        GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().ForEach(Destroy);
    }


    

    // Start is called before the first frame update
    void Start()
    {
        Sats = GameObject.FindGameObjectsWithTag("Satellite").ToList();

        Sats.ForEach(sat => nodes.Add(sat.GetComponent<SatelliteComms>().Node));



        instance = this;
    }

    private void Update()
    {

        if (SatManager._instance.satellites.TrueForAll(sat => sat.Node.State != Node.NodeState.PLANNING))
        {
            GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().ForEach(Destroy);
        }

        if(GameObject.FindGameObjectsWithTag("LocationPlaceholder").Length == 0 && SatManager._instance.satellites.Any(sat => sat.Node.State == Node.NodeState.PLANNING))
        {
            SatelliteComms planningNode =
                SatManager._instance.satellites.Find(sat => sat.Node.State == Node.NodeState.PLANNING);

            planningNode.Node.GeneratingPlan.Entries.ForEach(entry => Instantiate(SatLocationPlaceholderPrefab, BackendHelpers.UnityVectorFromNumerics(entry.Position), Quaternion.identity));
        }

        if (nodes.Count == 0)
            Sats.ForEach(sat => nodes.Add(sat.GetComponent<SatelliteComms>().Node));


        if (plan == null)
            return;

        if (Input.GetMouseButtonDown(0) && 
            Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, float.MaxValue, ManualDesignMask) &&  
            plan.Entries.TrueForAll(entry => nodes.Any(node => System.Numerics.Vector3.Distance(node.Position, entry.Position) < 0.1f))){

            if(EnableManualDesign == false)
            {
                Clear();
            }

            if(GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().Count < nodes.Count) { 
                Instantiate(SatLocationPlaceholderPrefab, hit.point, Quaternion.identity);


                EnableAutotest = false;
                EnableManualDesign = true;

            } else if(EnableManualDesign)
            {
                List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();

                foreach (Vector3 pos in GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().Select (loc => loc.transform.position))
                {
                    System.Numerics.Vector3 position = new System.Numerics.Vector3(pos.x, pos.y, pos.z);
                    List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => x.CompareTo(y)) };
                    ConstellationPlanEntry entry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
                    entries.Add(entry);
                }

                plan = new ConstellationPlan(entries);

                //Send the targetconstellation to random sat
                INode targetSat = Sats[UnityEngine.Random.Range(0, Sats.Count - 1)].GetComponent<SatelliteComms>().Node;
                PlanRequest request = new PlanRequest();
                request.Command = Request.Commands.GENERATE;
                request.DestinationID = targetSat.Id;
                request.Plan = plan;
                request.MessageIdentifer = "42";
                targetSat.Communicate(request);


                EnableManualDesign = false;
            }


        }
    }
}
