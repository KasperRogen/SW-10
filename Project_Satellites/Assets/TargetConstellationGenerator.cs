using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TargetConstellationGenerator : MonoBehaviour
{
    public GameObject SatLocationPlaceholderPrefab;

    List<GameObject> Sats = new List<GameObject>();
    string ConstellationAltitudeInput = "781000";


    List<Vector3> TargetPositions = new List<Vector3>();

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 50, 200, 30), "Generate Target Constellation"))
        {
            GenerateTargetConstellation();
        }
        GUI.TextField(new Rect(10, 10, 200, 20), "Target Altitude", 25);
        ConstellationAltitudeInput = GUI.TextField(new Rect(10, 30, 200, 20), ConstellationAltitudeInput, 25);
    }

    void GenerateTargetConstellation()
    {
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
            //float angle = UnityEngine.Random.Range(0, Mathf.PI * 2f);

            //angle += (Mathf.PI * 2f / Sats.Count) * i;
            angle = (360 / Sats.Count) * i;
            angle += Random.Range(-50, 50);


            //Create the targetposition
            //Vector3 instantiationPos = new Vector3(
            //    Mathf.Cos(angle) * constellationRadius,
            //    0,
            //    Mathf.Sin(angle) * constellationRadius);

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
            Position position = new Position(pos.x, pos.y, pos.z);
            List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField<float>("DeltaV", (x, y) => x.CompareTo(y)) };
            ConstellationPlanEntry entry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
            entries.Add(entry);
        }

        ConstellationPlan plan = new ConstellationPlan(entries);

        //Send the targetconstellation to random sat
        Sats[Random.Range(0, Sats.Count - 1)].GetComponent<SatelliteComms>().Node.Communicate(Constants.Commands.Generate, plan);
    }



    // Start is called before the first frame update
    void Start()
    {
        Sats = GameObject.FindGameObjectsWithTag("Satellite").ToList();
    }
}
