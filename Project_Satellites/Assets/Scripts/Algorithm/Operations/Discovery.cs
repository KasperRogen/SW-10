using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Numerics;

public class Discovery
{
    
    public static void StartDiscovery(INode myNode)
    {
        DiscoveryRequest discoveryRequest = new DiscoveryRequest()
        {
            Command = Request.Commands.DISCOVER,
            DestinationID = myNode.ID,
            SourceID = myNode.ID,
            EdgeSet = new NetworkMap()
        };

        myNode.CommsModule.Send(myNode.ID, discoveryRequest);
    }


    /// <summary>procedure for updating network map for the entire network
    /// <para>  </para>
    /// </summary>
    public static async System.Threading.Tasks.Task DiscoverAsync(INode MyNode, DiscoveryRequest request)
    {
        if(MyNode.State != Node.NodeState.DISCOVERY)
        {
            MyNode.Router.NetworkMap.Entries.ForEach(mapEntry => mapEntry.Neighbours.Clear());
        }


        bool newKnowledge = false;
        bool alteredSet = false;


        //Request all nodes reachable by the commsModule
        List<uint?> ReachableNodes = MyNode.CommsModule.Discover();

        MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.AddRange(ReachableNodes);
        
        request.EdgeSet.Entries.Add(new NetworkMapEntry(MyNode.ID, new List<uint?>(), MyNode.Position));

        foreach (var set in request.EdgeSet.Entries)
        {
            if(set.Neighbours.Any(neighbour => MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.Contains(neighbour) == false)){
                if(MyNode.Router.NetworkMap.Entries.Any(entry => entry.ID == set.ID) == false)
                {
                    MyNode.Router.NetworkMap.Entries.Add(set);
                } else
                {
                    MyNode.Router.NetworkMap.GetEntryByID(set.ID).Neighbours.AddRange(set.Neighbours);
                }

                MyNode.Router.NetworkMap.GetEntryByID(set.ID).Neighbours = MyNode.Router.NetworkMap.GetEntryByID(set.ID).Neighbours.Distinct().ToList();
                newKnowledge = true;
                continue;
            }
        }

        foreach (uint? node in MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours)
        {
            
            //Only add it to the list if it doesn't already exist
            if (request.EdgeSet.GetEntryByID(MyNode.ID).Neighbours.Contains(node) == false)
            {
                request.EdgeSet.GetEntryByID(MyNode.ID).Neighbours.Add(node);
                alteredSet = true;
            }
        }

        foreach(uint? node in request.EdgeSet.GetEntryByID(MyNode.ID).Neighbours)
        {
            if(MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.Contains(node) == false)
            {
                MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.Add(node);
                newKnowledge = true;
            }
        }

        MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours = MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.Distinct().ToList();

        foreach (uint? neighbour in MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours)
        {
            if(MyNode.Router.NetworkMap.Entries.Select(entry => entry.ID).Contains(neighbour) == false)
            {
                if (neighbour == MyNode.ID)
                    continue;

                MyNode.Router.AddNodeToGraph(neighbour);

                Request positionRequest = new Request(MyNode.ID, neighbour, Request.Commands.POSITION);
                uint? nextHop = MyNode.Router.NextHop(MyNode.ID, positionRequest.DestinationID);
                PositionResponse response = await MyNode.CommsModule.SendAsync(nextHop, positionRequest, 300000) as PositionResponse;
                Vector3 position = response.Position;
                MyNode.Router.NetworkMap.Entries.Add(new NetworkMapEntry(neighbour, position));
                newKnowledge = true;
                alteredSet = true;
            }
        }

    

        MyNode.Router.UpdateGraph();


        if (alteredSet || newKnowledge)
        {
            DiscoveryRequest newRequest = request.DeepCopy();

            newRequest.DestinationID = MyNode.Router.NextSequential(MyNode, MyNode.ActivePlan);
            newRequest.SourceID = MyNode.ID;
            uint? nextHop = MyNode.Router.NextHop(MyNode.ID, newRequest.DestinationID);
            MyNode.CommsModule.Send(nextHop, newRequest);
        }
    }
}
