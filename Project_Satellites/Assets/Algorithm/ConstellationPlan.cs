using System;
using System.Collections.Generic;

public class ConstellationPlan
{
    public int lastEditedBy;
    public List<ConstellationPlanField> fields;

    public ConstellationPlan(List<ConstellationPlanField> fields)
    {
        this.fields = fields;
    }



}
