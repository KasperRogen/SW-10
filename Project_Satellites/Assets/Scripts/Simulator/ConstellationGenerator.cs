using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstellationGenerator : MonoBehaviour
{
    public GameObject SatellitePrefab;
    private static GameObject _satellitePrefab;

    [Header("Constellation Settings"), Space(10)]
    [Min(1)] public int PlaneNum;
    [Min(1)] public int SatellitesPerPlane;
    [Min(0)] public float SatelliteAltitude;


    public static void InstantiateSatellite(Vector3 instantiationPoisition)
    {
        GameObject satellite = Instantiate(_satellitePrefab, instantiationPoisition, Quaternion.identity);
        CommsSim sim = satellite.AddComponent<CommsSim>();

        int satIndex = SatManager._instance.SatIndex;

        INode node = new Node((uint?)satIndex, BackendHelpers.NumericsVectorFromUnity(satellite.transform.position));
        node.TargetPosition = node.Position;
        node.CommsModule = sim;
        node.PlaneNormalDir = BackendHelpers.NumericsVectorFromUnity(Vector3.up);

        satellite.name = "P(" + 0 + "), S(" + (satIndex) + ")";
        satellite.GetComponent<SatelliteComms>().Node = node;
        SatManager._instance.SatIndex++;
        node.AutoChecksAllowed = CanvasHandler.AutoChecksAllowed;
        node.GenerateRouter();
    }

    // Start is called before the first frame update
    void Start()
    {
        _satellitePrefab = SatellitePrefab; 

        Constants.EarthRadius = (GetComponent<SphereCollider>().radius * transform.localScale.x);
        float constellationAltitude = Constants.EarthRadius + Constants.ScaleToSize(SatelliteAltitude);
        float constellationRadius = constellationAltitude / 2;


        List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();

        List<INode> nodes = new List<INode>();

        for (int i = 0; i < PlaneNum; i++)
        {
            float yAngle = Mathf.PI / PlaneNum * i;

            
            for(uint? j = 0; j < SatellitesPerPlane; j++)
            {
                float angle = (int)j * Mathf.PI * 2f / SatellitesPerPlane;
            

                Vector3 instantiationPos = new Vector3(
                    Mathf.Cos(angle) * constellationRadius,
                    Mathf.Sin(yAngle / SatellitesPerPlane * (int)j) * constellationRadius, 
                    Mathf.Sin(angle) * constellationRadius);
                
                //Create a vector from earth center to the desired position
                Vector3 instantiationVector = (instantiationPos - transform.position).normalized * constellationAltitude;

                GameObject satellite = Instantiate(SatellitePrefab, transform.position + instantiationVector, Quaternion.identity);
                CommsSim sim = satellite.AddComponent<CommsSim>();

                INode node = new Node(j, BackendHelpers.NumericsVectorFromUnity(satellite.transform.position));
                node.TargetPosition = node.Position;
                node.CommsModule = sim;
                node.PlaneNormalDir = BackendHelpers.NumericsVectorFromUnity(Vector3.up);

                satellite.name = "P(" + i + "), S(" + j + ")";
                satellite.GetComponent<SatelliteComms>().Node = node;
                satellite.GetComponent<SatelliteComms>().Node.AutoChecksAllowed = CanvasHandler.AutoChecksAllowed;

                List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => x.CompareTo(y)) };
                ConstellationPlanEntry entry = new ConstellationPlanEntry(node.Position, fields, (x, y) => 1);
                entry.NodeID = node.Id;
                entries.Add(entry);
                nodes.Add(node);

                SatManager._instance.SatIndex++;
            }

        }

        ConstellationPlan plan = new ConstellationPlan(entries);
        nodes.ForEach(node => { node.ActivePlan = plan; node.GenerateRouter(); });
    }




}
