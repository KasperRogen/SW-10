using System.Collections.Generic;

public class ConstellationPlan
{
    public int lastEditedBy;
    public List<ConstellationPlanField> fields;
    private object mathf;

    public float TotalDeltaV()
    {
        float sum = 0;

        foreach(ConstellationPlanField field in fields)
        {
            sum += field.deltaV;
        }

        return sum;
    }

    public float TotalDeltaVWithChange(int index, float newDeltaV)
    {
        List<float> tempDeltaVs = new List<float>();
        foreach(ConstellationPlanField field in this.fields)
        {
            tempDeltaVs.Add(field.deltaV);
        }
        
        float currentSum = 0;
        float newSum = 0;

        foreach(float deltaV in tempDeltaVs)
        {
            currentSum += deltaV;
        }

        tempDeltaVs[index] = newDeltaV;

        foreach(float deltaV in tempDeltaVs)
        {
            newSum += deltaV;
        }


        if(currentSum <= newSum)
        {
            return currentSum;
        } else
        {
            return newSum;
        }
    }

}
