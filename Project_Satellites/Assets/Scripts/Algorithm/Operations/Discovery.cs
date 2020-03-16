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
            EdgeSet = new Dictionary<uint?, List<uint?>>()
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

        if(request.EdgeSet.ContainsKey(MyNode.ID) == false)
        {
            request.EdgeSet.Add(MyNode.ID, new List<uint?>());
        }

        foreach (uint? node in MyNode.Router.NetworkMap[MyNode.ID])
        {
            
            //Only add it to the list if it doesn't already exist
            if (request.EdgeSet[MyNode.ID].Contains(node) == false)
            {
                request.EdgeSet[MyNode.ID].Add(node);
                alteredSet = true;
            }
        }

        foreach(uint? node in request.EdgeSet[MyNode.ID])
        {
            if(MyNode.Router.NetworkMap[MyNode.ID].Contains(node) == false)
            {
                MyNode.Router.NetworkMap[MyNode.ID].Add(node);
                newKnowledge = true;
            }
        }

        
        if (alteredSet || newKnowledge)
        {
            request.SourceID = MyNode.ID;
            request.DestinationID = MyNode.Router.NextSequential(MyNode.ID, request.SenderID);
            uint? nextHop = MyNode.Router.NextHop(MyNode.ID, request.DestinationID);
            MyNode.CommsModule.Send(nextHop, request);
        }
    }
}
