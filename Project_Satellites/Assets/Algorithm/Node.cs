﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;using System.Timers;

public class Node : INode
{
    public enum NodeState { PASSIVE, PLANNING, OVERRIDE, EXECUTING, DEAD, HEARTBEAT };
    public override uint? ID { get; set; }
    public override List<uint?> ReachableNodes { get; set; } // Future Work: Make part of the algorithm that reachable nodes are calculated based on position and a communication distance
    public override Position Position { get; set; }
    public override Position TargetPosition { get; set; }
    public override bool Active
    {
        get 
        {
            return active;
        }
        set
        {
            active = value;

            if (value)
            {
                State = NodeState.PASSIVE;
            }
            else
            {
                State = NodeState.DEAD;
            }
        }
    }
    public override ConstellationPlan Plan { get; set; }
    public override NodeState State { get; set; }
    public override Router Router { get; set; }

    private List<Tuple<uint?, uint?>> EdgeSet;
    private List<Tuple<uint?, uint?>> CurrentKnownEdges = new List<Tuple<uint?, uint?>>();
    private int LastDiscoverID = -1;
    private System.Timers.Timer timer;
    private bool active;

    public Node(uint? ID, Position position)
    {
        this.ID = ID;
        State = Node.NodeState.PASSIVE;
        Position = position;
        Active = true;
    }
   
   
    public override void GenerateRouter()
    {
        Router = new Router(Plan);
    }


    public override bool Equals(object obj)
    {
        return ID == (obj as INode).ID;
    }


    public override void Communicate(Request request)
    {
        new Thread(() => {

            if (request.DestinationID != ID)
            {
                Thread.Sleep(500);
                if (Router.NetworkMap[ID].Contains(request.DestinationID))
                {
                    CommsModule.Send(request.DestinationID, request);
                }
                else
                {
                    uint? nextHop = Router.NextHop(ID, request.DestinationID);

                    if (nextHop == null)
                        throw new Exception("CANNOT FIND THE GUY");

                    CommsModule.Send(nextHop, request);
                }
                return;
            }

            switch (request.Command)
            {
                case Request.Commands.Generate:
                    PlanGenerator.GeneratePlan(this, request as PlanRequest);
                    break;

                case Request.Commands.Execute:
                    PlanExecuter.ExecutePlan(this, request as PlanRequest);
                    break;
            }

        }).Start();
        
    }
}
