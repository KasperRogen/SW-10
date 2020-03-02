﻿using System;
using System.Collections.Generic;

public abstract class INode
{
    public abstract Node.NodeState State { get; set; }
    public abstract ConstellationPlan Plan { get; set; }
    public abstract uint? ID { get; set; }
    public abstract List<uint?> ReachableNodes { get; set; }
    public abstract Position Position { get; set; }
    public abstract Position TargetPosition { get; set; }
    public abstract Router Router { get; set; }
    public abstract bool Active { get; set; }
    public ICommunicate CommsModule { get; set; }
    public List<Tuple<uint?, uint?>> KnownEdges { get; set; }
    public bool executingPlan;
    public bool justChangedPlan;

    public abstract void GenerateRouter();
    public abstract void Communicate(Request message);
}