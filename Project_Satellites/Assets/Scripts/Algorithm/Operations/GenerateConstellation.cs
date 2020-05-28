using System.Collections;
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


        //Generate the target positions
        for (int i = 0; i < satCount; i++)
        {
            //Create random angle for position of Targetposition
            float angle = (360 / satCount) * i;
            
            angle = (float)(Math.PI / 180) * angle;

            Vector3 instantiationPos = Vector3.Transform(new Vector3(0, 0, 1), Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), angle));

            //Set it relative to the earth
            Vector3 instantiationVector = Vector3.Normalize(instantiationPos) * constellationAltitude * rng.Next(1, 1);

            //Store for propagation
            TargetPositions.Add(instantiationVector);
        }

        List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();

        //Create a plan containing the taraget positions
        foreach (Vector3 pos in TargetPositions)
        {
            Vector3 position = new Vector3(pos.X, pos.Y, pos.Z);
            List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => x.CompareTo(y)) };
            ConstellationPlanEntry entry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
            entries.Add(entry);
        }

        ConstellationPlan plan = new ConstellationPlan(entries);
        //Send the targetconstellation to random sat
        return plan;


    }

    public static ConstellationPlan GenerateRecoveryConstellation(Vector3 midpoint, int nodeCount)
    {
        // Calculate positions evenly spread starting from midpoint
        List<Vector3> targetPositions = CalculatePositions(midpoint, nodeCount);

        // Create constellation plan based on target positions
        ConstellationPlan plan = CreatePlan(targetPositions);

        return plan;
    }

    public static List<Vector3> CalculatePositions(Vector3 midpoint, int nodeCount) {

        List<Vector3> targetPositions = new List<Vector3> { midpoint };
        int middleCount = nodeCount / 2;
        float radius = Vector3.Distance(Vector3.Zero, midpoint);
        float angle = Constants.ScaleToSize(Constants.SatCommsRange) / radius;

        Vector3 tempVector = midpoint;

        for (int i = middleCount - 1; i >= 0; i--) {
            tempVector = Vector3.Transform(tempVector, Quaternion.CreateFromAxisAngle(Vector3.UnitY, -angle));
            targetPositions.Add(tempVector);
        }

        tempVector = midpoint;

        for (int i = middleCount + 1; i < nodeCount; i++) {
            tempVector = Vector3.Transform(tempVector, Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle));
            targetPositions.Add(tempVector);
        }

        return targetPositions;
    }

    public static ConstellationPlan CreatePlan(List<Vector3> targetPositions) {
        List<ConstellationPlanEntry> entries = new List<ConstellationPlanEntry>();

        foreach (Vector3 targetPosition in targetPositions) {
            List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => x.CompareTo(y)) };
            ConstellationPlanEntry entry = new ConstellationPlanEntry(targetPosition, fields, (x, y) => 1);
            entries.Add(entry);
        }

        ConstellationPlan plan = new ConstellationPlan(entries);

        return plan;
    }
}
