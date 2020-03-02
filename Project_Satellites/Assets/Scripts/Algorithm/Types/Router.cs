using System.Collections.Generic;
using System.Linq;
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
    public uint? NextSequential(uint? source)
    {
        List<uint?> nodes = new List<uint?>();
        List<uint?> lastNodes = new List<uint?>();
        NetworkMap[source].ForEach(node => nodes.Add(node));
        nodes.Add(source);


        do
        {
            //Store the nodes i could reach before this pass
            lastNodes.Clear();
            nodes.ForEach(node => lastNodes.Add(node));
            List<uint?> newNodes = new List<uint?>();

            //Add the nodes my nodes can reach as nodes i can reach
            foreach(uint? node in nodes)
            {
                NetworkMap[node].ForEach(newNode => newNodes.Add(newNode));
            }
            nodes.AddRange(newNodes);
            
            nodes = nodes.Distinct().ToList();
            nodes = nodes.OrderBy((x) => x).ToList();
            lastNodes = lastNodes.Distinct().ToList();
            lastNodes = lastNodes.OrderBy((x) => x).ToList();

            //Repeat the process untill no new nodes were added in a pass
        } while (nodes.TrueForAll(node => lastNodes.Contains(node)) == false);
        nodes = nodes.OrderBy((x) => x).ToList();
        uint? destination;

        //If i am last node in the list, take the first index
        if (source == nodes[nodes.Count-1])
        {
            destination = nodes[0];
        }
        else
        {
            //Else take the next
            destination = nodes[nodes.IndexOf(source) +1];
        }
        return destination;
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
            List<uint?> neighbors = new List<uint?>();

            foreach (ConstellationPlanEntry innerEntry in plan.Entries.Where((x) => x != entry))
            {
                if (Position.Distance(entry.Position, innerEntry.Position) < satRange) // 100 = Range for Satellite communication
                {
                    if (innerEntry.NodeID != null)
                    neighbors.Add(innerEntry.NodeID);
                }
            }

            if(entry.NodeID != null)
                NetworkMap[entry.NodeID] = neighbors;
        }

        UpdateGraph();
    }

    public void DeleteEdge(uint? n1, uint? n2)
    {
        NetworkMap[n1].Remove(n2);
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