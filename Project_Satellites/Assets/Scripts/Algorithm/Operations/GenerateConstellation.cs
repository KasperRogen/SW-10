﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Numerics;

public class GenerateConstellation
{

    public static ConstellationPlan GenerateTargetConstellation(int satCount, float constellationAltitude)
    {
        Random rng = new Random();
        List<Vector3> TargetPositions = new List<Vector3>();
        ConstellationPlan plan = null;
        float constellationRadius = constellationAltitude / 2;

        float angle = 0;

        for (int i = 0; i < satCount; i++)
        {
            //Create random angle for position of Targetposition
            angle = (360 / satCount) * i;
            
            angle = (float)(Math.PI / 180) * angle;

            Vector3 instantiationPos = Vector3.Transform(new Vector3(0, 0, 1), Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), angle));

            //Set it relative to the earth
            Vector3 instantiationVector = Vector3.Normalize(instantiationPos) * constellationAltitude * rng.Next(1, 1);

            //Store for propagation
            TargetPositions.Add(instantiationVector);
        }

        List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();

        foreach (Vector3 pos in TargetPositions)
        {
            Vector3 position = new Vector3(pos.X, pos.Y, pos.Z);
            List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => { return x.CompareTo(y); }) };
            ConstellationPlanEntry entry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
            entries.Add(entry);
        }

        plan = new ConstellationPlan(entries);
        //Send the targetconstellation to random sat
        return plan;


    }


}