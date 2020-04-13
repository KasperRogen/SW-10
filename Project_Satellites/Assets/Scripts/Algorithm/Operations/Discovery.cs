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
        if(MyNode.LastDiscoveryID != request.MessageIdentifer)
        {
            MyNode.ReachableNodeCount = MyNode.Router.ReachableSats(MyNode).Count;
            MyNode.ActivePlan = new ConstellationPlan(new List<ConstellationPlanEntry>());
            MyNode.LastDiscoveryID = request.MessageIdentifer;
            MyNode.Router.NetworkMap.Entries.Clear();
            MyNode.Router.NetworkMap.Entries.Add(new NetworkMapEntry(MyNode.ID, MyNode.Position));
            MyNode.Router.ClearNetworkMap();
        }
            
        bool newKnowledge = false;
        bool alteredSet = false;
        MyNode.State = Node.NodeState.DISCOVERY;
        

        //Request all nodes reachable by the commsModule
        List<uint?> ReachableNodes = MyNode.CommsModule.Discover();
        
        //Add these reachable nodes to my neighbours
        MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.AddRange(ReachableNodes);
        MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours = MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.Distinct().ToList();
        
        //If the request doesn't contain my node, add it
        if(request.EdgeSet.Entries.Select(set => set.ID).Contains(MyNode.ID) == false)
        {
            request.EdgeSet.Entries.Add(new NetworkMapEntry(MyNode.ID, new List<uint?>(), MyNode.Position));
        }


        NetworkMap networkmap = MyNode.Router.NetworkMap;


        //Add any nodes not in my networkmap
        foreach(NetworkMapEntry set in request.EdgeSet.Entries)
        {
            if(networkmap.Entries.Select(entry => entry.ID).Contains(set.ID) == false)
            {
                //I don't have this node as entry in my networkmap, add it
                networkmap.Entries.Add(set);
                newKnowledge = true;
            } else if(set.Neighbours.Any(neighbour => networkmap.GetEntryByID(set.ID).Neighbours.Contains(neighbour) == false)) //If we already know of it, add any neighbours
            {
                //A new neighbour is identified, for a node in my networkmap, add it.
                networkmap.GetEntryByID(set.ID).Neighbours.AddRange(set.Neighbours);
                networkmap.GetEntryByID(set.ID).Neighbours = networkmap.GetEntryByID(set.ID).Neighbours.Distinct().ToList();
                newKnowledge = true;
            }

        }

        foreach(NetworkMapEntry entry in networkmap.Entries)
        {
            if(request.EdgeSet.Entries.Select(set => set.ID).Contains(entry.ID) == false)
            {
                //The request edgeset doesn't contain this entry. add it
                request.EdgeSet.Entries.Add(entry);
                alteredSet = true;
            } else if(entry.Neighbours.Any(neighbour => request.EdgeSet.GetEntryByID(entry.ID).Neighbours.Contains(neighbour) == false))
            {
                //The request edgeset doesn't have this given neighbour for the entry. add it
                request.EdgeSet.GetEntryByID(entry.ID).Neighbours.AddRange(entry.Neighbours);
                request.EdgeSet.GetEntryByID(entry.ID).Neighbours = request.EdgeSet.GetEntryByID(entry.ID).Neighbours.Distinct().ToList();
                alteredSet = true;
            }

        }



        //For each of my neighbours
        foreach (uint? neighbour in MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours)
        {
            if(MyNode.Router.NetworkMap.Entries.Select(entry => entry.ID).Contains(neighbour) == false)
            {
                if (neighbour == MyNode.ID)
                    continue;

                MyNode.Router.AddNodeToGraph(neighbour);

                Request positionRequest = new Request(MyNode.ID, neighbour, Request.Commands.POSITION);
                positionRequest.AckExpected = true;
                positionRequest.ResponseExpected = true;
                uint? nextHop = MyNode.Router.NextHop(MyNode.ID, positionRequest.DestinationID);
                PositionResponse response = await MyNode.CommsModule.SendAsync(nextHop, positionRequest, 300000, 3) as PositionResponse;
                Vector3 position = response.Position;
                NetworkMapEntry neigbourEntry = new NetworkMapEntry(neighbour, position);
                MyNode.Router.NetworkMap.Entries.Add(neigbourEntry);
                request.EdgeSet.Entries.Add(neigbourEntry);
                newKnowledge = true;
                alteredSet = true;
            }
        }

    

        MyNode.Router.UpdateGraph();


        if (alteredSet || newKnowledge)
        {
            List<ConstellationPlanEntry> newEntries = new List<ConstellationPlanEntry>();

            foreach(NetworkMapEntry entry in MyNode.Router.NetworkMap.Entries)
            {
                Vector3 position = entry.Position;
                List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => { return x.CompareTo(y); }) };
                ConstellationPlanEntry planEntry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
                newEntries.Add(planEntry);
            }

            MyNode.ActivePlan = new ConstellationPlan(newEntries);

            DiscoveryRequest newRequest = request.DeepCopy();

            newRequest.DestinationID = MyNode.Router.NextSequential(MyNode, Router.CommDir.CW);
            newRequest.SourceID = MyNode.ID;
            uint? nextHop = MyNode.Router.NextHop(MyNode.ID, newRequest.DestinationID);
            MyNode.CommsModule.Send(nextHop, newRequest);
        } else
        {
            if(MyNode.Router.ReachableSats(MyNode).Count > MyNode.ReachableNodeCount)
            {
                ConstellationPlan RecoveryPlan = GenerateConstellation.GenerateTargetConstellation(MyNode.Router.ReachableSats(MyNode).Count, 7.152f);


                PlanRequest recoveryRequest = new PlanRequest
                {
                    SourceID = MyNode.ID,
                    DestinationID = MyNode.ID,
                    Command = Request.Commands.GENERATE,
                    Plan = RecoveryPlan
                };

                if (MyNode.Router.NextSequential(MyNode, Router.CommDir.CW) == null)
                {
                    recoveryRequest.Dir = Router.CommDir.CCW;
                }

                MyNode.CommsModule.Send(MyNode.ID, recoveryRequest);
                return;
            }
        }


        MyNode.State = Node.NodeState.PASSIVE;
    }
}
