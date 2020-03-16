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

    bool autotestRunning = false;
    ConstellationPlan plan = null;
    public int RandomSeed;
    public GameObject SatLocationPlaceholderPrefab;

    List<GameObject> Sats = new List<GameObject>();
    List<INode> nodes = new List<INode>();
    string ConstellationAltitudeInput = "781000";

    public static float CurrentDeltaVSum;


    List<Vector3> TargetPositions = new List<Vector3>();

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 50, 200, 30), "Generate Target Constellation"))
        {
            GenerateTargetConstellation();
        }
        GUI.TextField(new Rect(10, 10, 200, 20), "Target Altitude", 25);
        ConstellationAltitudeInput = GUI.TextField(new Rect(10, 30, 200, 20), ConstellationAltitudeInput, 25);

        GUI.TextField(new Rect(10, 80, 200, 20), CurrentDeltaVSum.ToString(), 25);
    }

    public void GenerateTargetConstellation()
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
        if (Sats.Count == 0)
            Sats = GameObject.FindGameObjectsWithTag("Satellite").ToList();



        //Remove old location Placeholders
        GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().ForEach(GO => Destroy(GO));

        float angle = 0;

        for (int i = 0; i < Sats.Count; i++)
        {
            //Create random angle for position of Targetposition
            angle = (360 / Sats.Count) * i;
            angle += UnityEngine.Random.Range(-5, 5);

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
        INode targetSat = Sats[UnityEngine.Random.Range(0, Sats.Count - 1)].GetComponent<SatelliteComms>().Node;
        PlanRequest request = new PlanRequest {
            Command = Request.Commands.Generate,
            SourceID = targetSat.ID,
            DestinationID = targetSat.ID,
            SenderID = targetSat.ID,
            MessageIdentifer = "42",
            Plan = plan
        };
        targetSat.Communicate(request);

        if (autotestRunning == false && EnableAutotest == true)
            StartCoroutine(RestartGenerator());
        

    }


    public static void Clear()
    {
        //Remove old location Placeholders
        GameObject.FindGameObjectsWithTag("LocationPlaceholder")?.ToList().ForEach(GO => Destroy(GO));
    }


    IEnumerator RestartGenerator()
    {
        autotestRunning = true;
        yield return new WaitForSeconds(5);

        while (EnableAutotest)
        {
            if(nodes.Count == 0)
                Sats.ForEach(sat => nodes.Add(sat.GetComponent<SatelliteComms>().Node));


            if (plan != null && plan.Entries.TrueForAll(entry => nodes.Any(node => System.Numerics.Vector3.Distance(node.Position, entry.Position) < 0.1f))) {
                int newSeed = RandomSeed;
                do
                {
                    newSeed = (int)(DateTime.Now.DayOfYear * DateTime.Now.Minute * Time.time * UnityEngine.Random.Range(1, 5));
                } while (RandomSeed == newSeed);

                RandomSeed = newSeed;
                GenerateTargetConstellation();
            }
            yield return new WaitForSeconds(1);
        }
        autotestRunning = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        Sats = GameObject.FindGameObjectsWithTag("Satellite").ToList();

        Sats.ForEach(sat => nodes.Add(sat.GetComponent<SatelliteComms>().Node));

        StartCoroutine(RestartGenerator());

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
                request.Command = Request.Commands.Generate;
                request.DestinationID = targetSat.ID;
                request.Plan = plan;
                request.MessageIdentifer = "42";
                targetSat.Communicate(request);


                EnableManualDesign = false;
            }


        }
    }
}
