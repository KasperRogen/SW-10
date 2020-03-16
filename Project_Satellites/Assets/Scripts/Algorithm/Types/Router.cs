using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;

public class Router : IRouter
{
    public override Dictionary<uint?, List<uint?>> NetworkMap { get; set; }
    
    private Graph<uint?, string> graph;
    private Dictionary<uint?, uint> nodeToNodeIDMapping = new Dictionary<uint?, uint>();
    private float satRange = 5f;

    public Router(ConstellationPlan plan)
    {
        NetworkMap = new Dictionary<uint?, List<uint?>>();

        if (plan.Entries.TrueForAll(entry => entry.NodeID != null))
        {
            foreach (ConstellationPlanEntry entry in plan.Entries)
            {
                NetworkMap.Add(entry.NodeID, new List<uint?>());
                nodeToNodeIDMapping.Add(entry.NodeID, 0);
            }
        }
        UpdateNetworkMap(plan);
    }
    // Returns next sequential neighbour node based on who sent a request.
    // The other neighbour is then returned in order to send the message "forward".
    public uint? NextSequential(uint? source, uint? sender)
    {
        return NetworkMap[source].Where(x => x != sender).ToList()[0];
    }

    // Returns next sequential neighbour node based on current plan.
    // Always sends clockwise or counterclockwise (cant remember which one).
    public uint? NextSequential(uint? source, ConstellationPlan plan)
    {
        // Assumption: Always 2 neighbours, if not the case it is handled by fault mechanisms.
        ConstellationPlanEntry sourceEntry = plan.Entries.Single(x => x.NodeID == source);
        List<ConstellationPlanEntry> neighbourEntries = plan.Entries.Where(x => NetworkMap[source].Contains(x.NodeID)).ToList();
        Vector3 normalVector = Vector3.Normalize(Vector3.Cross(neighbourEntries[0].Position, neighbourEntries[1].Position));
        List<double> angles = neighbourEntries.Select(x => BackendHelpers.NumericsVectorSignedAngle(sourceEntry.Position, x.Position, normalVector)).ToList();
        return neighbourEntries[angles.IndexOf(angles.Max())].NodeID;
    }
    public override uint? NextHop(uint? source, uint? destination)
    {
        List<uint?> nodes = new List<uint?>(); 
        
        NetworkMap[source].ForEach(node => nodes.Add(node));
        nodes.Add(source);
        nodes = nodes.OrderBy((x) => x).ToList();
        
        ShortestPathResult result = graph.Dijkstra(nodeToNodeIDMapping[source], nodeToNodeIDMapping[destination]);

        IEnumerable<uint> path = result.GetPath();
        int a = path.Count();
        uint? nextHop = nodeToNodeIDMapping.ToList().Find((x) => x.Value == path.ElementAt(1)).Key;
        return nextHop;
    }

    public override void UpdateNetworkMap(ConstellationPlan plan)
    {
        foreach (ConstellationPlanEntry entry in plan.Entries)
        {
            List<Tuple<uint?, float>> neighbors = new List<Tuple<uint?, float>>();

            foreach (ConstellationPlanEntry innerEntry in plan.Entries.Where((x) => x != entry))
            {
                float dist = Vector3.Distance(entry.Position, innerEntry.Position);
                if (dist < satRange) // 100 = Range for Satellite communication
                {

                    if (innerEntry.NodeID != null)
                    neighbors.Add(new Tuple<uint?, float>(innerEntry.NodeID, dist));
                }
            }

            //Order sats by distance to myself
            neighbors = neighbors.OrderBy(sat => sat.Item2).ToList();

            int desiredSatNum = 2;

            if(entry.NodeID != null)
            {
                if (neighbors.Count > 2)
                {
                    NetworkMap[entry.NodeID] = neighbors.GetRange(0, desiredSatNum).Select(sat => sat.Item1).ToList();
                } else
                {
                    NetworkMap[entry.NodeID] = neighbors.Select(sat => sat.Item1).ToList();
                }
            }
                
        }

        UpdateGraph();
    }

    public void DeleteEdge(uint? n1, uint? n2)
    {
        if(NetworkMap[n1].Contains(n2))
        NetworkMap[n1].Remove(n2);

        if(NetworkMap[n2].Contains(n1))
        NetworkMap[n2].Remove(n1);

        UpdateGraph();
    }

    private void UpdateGraph()
    {
        Graph<uint?, string> updatedGraph = new Graph<uint?, string>();

        foreach (KeyValuePair<uint?, List<uint?>> pair in NetworkMap)
        {
            uint nodeID = updatedGraph.AddNode(pair.Key);
            nodeToNodeIDMapping[pair.Key] = nodeID;
        }

        foreach (KeyValuePair<uint?, List<uint?>> pair in NetworkMap)
        {
            foreach (uint? neighbor in pair.Value)
            {
                updatedGraph.Connect(nodeToNodeIDMapping[pair.Key], nodeToNodeIDMapping[neighbor], 1, "");
            }
        }

        graph = updatedGraph;
    }
}