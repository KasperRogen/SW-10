using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstellationGenerator : MonoBehaviour
{
    public GameObject SatellitePrefab;

    [Header("Constellation Settings"), Space(10)]
    [Min(1)] public int PlaneNum;
    [Min(1)] public int SatellitesPerPlane;
    [Min(0)] public float SatelliteAltitude;




    // Start is called before the first frame update
    void Start()
    {
        Constants.EarthRadius = (GetComponent<SphereCollider>().radius * transform.localScale.x);
        float constellationAltitude = Constants.EarthRadius + Constants.ScaleToSize(SatelliteAltitude);
        float constellationRadius = constellationAltitude / 2;


        List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();

        List<INode> nodes = new List<INode>();

        for (int i = 0; i < PlaneNum; i++)
        {
            float yAngle = Mathf.PI / PlaneNum * i;

            for(int j = 0; j < SatellitesPerPlane; j++)
            {
                float angle = j * Mathf.PI * 2f / SatellitesPerPlane;
            

                Vector3 instantiationPos = new Vector3(
                    Mathf.Cos(angle) * constellationRadius,
                    Mathf.Sin(yAngle / SatellitesPerPlane * j) * constellationRadius, 
                    Mathf.Sin(angle) * constellationRadius);
                
                //Create a vector from earth center to the desired position
                Vector3 instantiationVector = (instantiationPos - transform.position).normalized * constellationAltitude;

                GameObject satellite = Instantiate(SatellitePrefab, transform.position + instantiationVector, Quaternion.identity);

                INode node = new Node(j);
                node.Position = BackendHelpers.PositionFromVector3(satellite.transform.position);
                node.TargetPosition = node.Position;

                satellite.name = "P(" + i + "), S(" + j + ")";
                satellite.GetComponent<SatelliteComms>().Node = node;

                List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 100, (x, y) => { return x.CompareTo(y); }) };
                ConstellationPlanEntry entry = new ConstellationPlanEntry(node.Position, fields, (x, y) => 1);
                entry.Node = node;
                entries.Add(entry);
                nodes.Add(node);
            }

        }

        ConstellationPlan plan = new ConstellationPlan(entries);
        nodes.ForEach(node => { node.Plan = plan; node.GenerateRouter(); });
    }




}
