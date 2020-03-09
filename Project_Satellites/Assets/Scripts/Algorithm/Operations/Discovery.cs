using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Discovery
{

    public static void StartDiscovery(INode myNode)
    {
        DiscoveryRequest discoveryRequest = new DiscoveryRequest()
        {
            Command = Request.Commands.Discover,
            DestinationID = myNode.ID,
            SourceID = myNode.ID,
            EdgeSet = new List<Tuple<uint?, uint?>>()
        };

        myNode.CommsModule.Send(myNode.ID, discoveryRequest);
    }


    /// <summary>procedure for updating network map for the entire network
    /// <para>  </para>
    /// </summary>
    public static void Discover(INode MyNode, DiscoveryRequest request)
    {
        bool newKnowledge = false;
        bool alteredSet = false;

        //Request all nodes reachable by the commsModule
        List<uint?> ReachableNodes = MyNode.CommsModule.Discover();

        MyNode.Router.NetworkMap[MyNode.ID].AddRange(ReachableNodes);
        MyNode.Router.NetworkMap[MyNode.ID] = MyNode.Router.NetworkMap[MyNode.ID].Distinct().ToList();

        if (MyNode.Router.NetworkMap[MyNode.ID].Contains(MyNode.ID)) { 
            
            int i = 2;
        }

        foreach (uint? node in MyNode.Router.NetworkMap[MyNode.ID])
        {
            //Create an edge to each of my neighbours
            Tuple<uint?, uint?> edge = new Tuple<uint?, uint?>(MyNode.ID, node);

            //order the edge
            edge = edge.Item1 > edge.Item2 ? new Tuple<uint?, uint?>(edge.Item2, edge.Item1) : edge;

            //Only add it to the list if it doesn't already exist
            if (request.EdgeSet.Contains(edge) == false)
            {
                request.EdgeSet.Add(edge);
                alteredSet = true;
            } else if(MyNode.KnownEdges.Contains(edge) == false)
            {
                MyNode.KnownEdges.Add(edge);
                newKnowledge = true;
            }
        }

        request.EdgeSet = request.EdgeSet.OrderBy(tuple => tuple.Item1).ThenBy(tuple => tuple.Item2).ToList();
        //MyNode.KnownEdges = request.EdgeSet;

        if (alteredSet)
        {
            request.SourceID = MyNode.ID;
            foreach (uint? node in MyNode.Router.NetworkMap[MyNode.ID])
            {
                request.DestinationID = node;
                uint? nextHop = MyNode.Router.NextHop(MyNode.ID, node);
                MyNode.CommsModule.Send(nextHop, request);
            }
        }
        else if (newKnowledge)
        {
            uint? SenderID = request.SourceID;
            request.SourceID = MyNode.ID;
            foreach (uint? node in MyNode.Router.NetworkMap[MyNode.ID])
            {
                if(node != SenderID) { 
                    request.DestinationID = node;
                    uint? nextHop = MyNode.Router.NextHop(MyNode.ID, node);
                    MyNode.CommsModule.Send(nextHop, request);
                }
            }
        } else
        {
            int a = 2;
        }
    }
}
