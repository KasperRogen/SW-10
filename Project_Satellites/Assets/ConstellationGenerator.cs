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


        for (int i = 0; i < PlaneNum; i++)
        {

            float yAngle = Mathf.PI / PlaneNum * i;

            for(int j = 0; j < SatellitesPerPlane; j++)
            {
                float angle = j * Mathf.PI * 2f / SatellitesPerPlane;
                //float yAngle = j * Mathf.PI * 2f / SatellitesPerPlane;
                Vector3 instantiationPos = new Vector3(
                    Mathf.Cos(angle) * constellationRadius,
                    Mathf.Sin(yAngle / SatellitesPerPlane * j) * constellationRadius, 
                    Mathf.Sin(angle) * constellationRadius);
                Vector3 instantiationVector = (instantiationPos - transform.position).normalized * constellationAltitude;
                GameObject satellite = Instantiate(SatellitePrefab, transform.position + instantiationVector, Quaternion.identity);
                satellite.GetComponent<SatelliteProperties>().SatID = j;
                satellite.GetComponent<SatelliteProperties>().PlaneNum = 1;
                satellite.GetComponent<SatelliteProperties>().SatsPerPlane = SatellitesPerPlane;
                satellite.name = "Satellite " + j;

                INode node = new Node(j);
                satellite.GetComponent<SatelliteComms>().Node = node;
            }

        }
        


    }




}
