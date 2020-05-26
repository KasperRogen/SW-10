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
        float constellationAltitude;
        float constellationRadius;

        TargetPositions.Clear();
        
        //If the constellation altitude input from textfield isn't numbers, return
        if (float.TryParse(ConstellationAltitudeInput, out constellationAltitude) == false)
            return;

        //Set the constellation altitude based on the input textfield
        constellationAltitude = Constants.EarthRadius + Constants.ScaleToSize(constellationAltitude);
        constellationRadius = constellationAltitude / 2;


        //Get reference to satellites
            Sats = GameObject.FindGameObjectsWithTag("Satellite").ToList();

        List<uint?> reachableNodes = RequesterNode.Router.ReachableSats(RequesterNode).ToList();

        for(int i = Sats.Count -1; i > 0; i--)
        {
            if(reachableNodes.Contains(Sats[i].GetComponent<SatelliteComms>().Node.ID) == false)
            {
                Sats.RemoveAt(i);
            }
        }


        //Remove old location Placeholders
        GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().ForEach(GO => Destroy(GO));

        float angle = 0;

        for (int i = 0; i < Sats.Count; i++)
        {
            //Create random angle for position of Targetposition
            angle = (360 / Sats.Count) * i;

            Vector3 instantiationPos = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            //Set it relative to the earth
            Vector3 instantiationVector = (instantiationPos - Vector3.zero).normalized * constellationAltitude * UnityEngine.Random.Range(1f, 1f);

            //Store for propagation
            TargetPositions.Add(instantiationVector);
            Instantiate(SatLocationPlaceholderPrefab, instantiationVector, Quaternion.identity);
        }

        List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();
            
        foreach (Vector3 pos in TargetPositions)
        {
            Vector3 position = new Vector3(pos.x, pos.y, pos.z);
            List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => { return x.CompareTo(y); }) };
            ConstellationPlanEntry entry = new ConstellationPlanEntry(BackendHelpers.NumericsVectorFromUnity(position), fields, (x, y) => 1);
            entries.Add(entry);
        }

        plan = new ConstellationPlan(entries);

        //Send the targetconstellation to random sat
        INode targetSat = RequesterNode;
        PlanRequest request = new PlanRequest {
            Command = Request.Commands.GENERATE,
            SourceID = targetSat.ID,
            DestinationID = targetSat.ID,
            MessageIdentifer = "42",
            Plan = plan
        };
        targetSat.Communicate(request);

    }


    public static void Clear()
    {
        //Remove old location Placeholders
        GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().ForEach(GO => Destroy(GO));
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


        if (nodes.Count == 0)
            Sats.ForEach(sat => nodes.Add(sat.GetComponent<SatelliteComms>().Node));


        RaycastHit hit;

        if (plan == null)
            return;

        if (Input.GetMouseButtonDown(0) && 
            Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, ManualDesignMask) &&  
            plan.Entries.TrueForAll(entry => nodes.Any(node => System.Numerics.Vector3.Distance(node.Position, entry.Position) < 0.1f))){

            if(EnableManualDesign == false)
            {
                Clear();
            }

            if(GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().Count < nodes.Count) { 
                Instantiate(SatLocationPlaceholderPrefab, hit.point, Quaternion.identity);


                EnableAutotest = false;
                EnableManualDesign = true;

            } else if(EnableManualDesign == true)
            {
                List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();

                foreach (Vector3 pos in GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().Select (loc => loc.transform.position))
                {
                    System.Numerics.Vector3 position = new System.Numerics.Vector3(pos.x, pos.y, pos.z);
                    List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => { return x.CompareTo(y); }) };
                    ConstellationPlanEntry entry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
                    entries.Add(entry);
                }

                plan = new ConstellationPlan(entries);

                //Send the targetconstellation to random sat
                INode targetSat = Sats[UnityEngine.Random.Range(0, Sats.Count - 1)].GetComponent<SatelliteComms>().Node;
                PlanRequest request = new PlanRequest();
                request.Command = Request.Commands.GENERATE;
                request.DestinationID = targetSat.ID;
                request.Plan = plan;
                request.MessageIdentifer = "42";
                targetSat.Communicate(request);


                EnableManualDesign = false;
            }


        }
    }
}
