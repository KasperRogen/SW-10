using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class TargetConstellationGenerator : MonoBehaviour
{
    public bool EnableAutotest = false;
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
        UnityEngine.Random.InitState(RandomSeed);
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
            Vector3 instantiationVector = (instantiationPos - Vector3.zero).normalized * constellationAltitude * UnityEngine.Random.Range(0.5f, 1f);

            //Store for propagation
            TargetPositions.Add(instantiationVector);
            Instantiate(SatLocationPlaceholderPrefab, instantiationVector, Quaternion.identity);
        }

        List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();

        foreach (Vector3 pos in TargetPositions)
        {
            Position position = new Position(pos.x, pos.y, pos.z);
            List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => { return x.CompareTo(y); }) };
            ConstellationPlanEntry entry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
            entries.Add(entry);
        }

        plan = new ConstellationPlan(entries);

        //Send the targetconstellation to random sat
        Sats[UnityEngine.Random.Range(0, Sats.Count - 1)].GetComponent<SatelliteComms>().Node.Communicate(Constants.Commands.Generate, plan);

        if (autotestRunning == false && EnableAutotest == true)
            StartCoroutine(RestartGenerator());
        

    }


    IEnumerator RestartGenerator()
    {
        autotestRunning = true;
        yield return new WaitForSeconds(5);

        while (EnableAutotest)
        {
            if(nodes.Count == 0)
                Sats.ForEach(sat => nodes.Add(sat.GetComponent<SatelliteComms>().Node));


            if (plan != null && plan.entries.TrueForAll(entry => nodes.Any(node => Position.Distance(node.Position, entry.Position) < 0.1f))) {
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
    }
}
