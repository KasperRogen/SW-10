using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Discovery
{
    //TODO: THIS IS OLD AF, WILL NOT WORK.


    //TODO: Avoid responding to same discovery message repeatedly



    /// <summary>THIS DOES NOT WORK, IT NEEDS TO BE REWRITTEN
    /// <para>  </para>
    /// </summary>
    public void Discover(INode MyNode, DiscoveryRequest request)
    {
        bool newKnowledge = false;
        bool alteredSet = false;


        foreach (uint? node in MyNode.ReachableNodes)
        {
            Tuple<uint?, uint?> edge = new Tuple<uint?, uint?>(MyNode.ID, node);

            edge = edge.Item1 > edge.Item2 ? new Tuple<uint?, uint?>(edge.Item2, edge.Item1) : edge;


            if (request.EdgeSet.Contains(edge) == false)
            {
                request.EdgeSet.Add(edge);
                alteredSet = true;
            }
        }

        request.EdgeSet = request.EdgeSet.OrderBy(tuple => tuple.Item1).ThenBy(tuple => tuple.Item2).ToList();
        MyNode.KnownEdges = request.EdgeSet;

        if (alteredSet)
        {
            foreach(uint? node in MyNode.ReachableNodes)
            {
                request.SourceID = MyNode.ID;
                request.DestinationID = node;
                //TODO: Set this up with new comms system
                //node.Communicate(request);
            }
        }
        else if (newKnowledge)
        {
            uint? SenderID = request.SourceID;
            foreach (uint? node in MyNode.ReachableNodes)
            {
                if(node != SenderID) { 
                    request.SourceID = MyNode.ID;
                    request.DestinationID = node;
                    //TODO: set this up with new comms system
                    //node.Communicate(request);
                }
            }
        }




    }



}
