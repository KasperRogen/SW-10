using System.Collections.Generic;
using System.Linq;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;

public class Router : IRouter
{
    Dictionary<INode, List<INode>> networkMap;
    Graph<INode, string> graph;
    Dictionary<INode, uint> nodeToNodeIDMapping = new Dictionary<INode, uint>();

    public Router(ConstellationPlan plan)
    {

        networkMap = new Dictionary<INode, List<INode>>();
        //if (plan.entries.TrueForAll(entry => entry.Node != null)) { 

        //    foreach (ConstellationPlanEntry entry in plan.entries)
        //    {
        //        networkMap.Add(entry.Node, new List<INode>());
        //        nodeToNodeIDMapping.Add(entry.Node, 0);
        //    }


        //}


        UpdateNetworkMap(plan);

    }

    public INode NextHop(INode source)
    {
        List<INode> nodes = new List<INode>(); 
        
        networkMap[source].ForEach(node => nodes.Add(node));
        nodes.Add(source);
        nodes = nodes.OrderBy((x) => x.ID).ToList();

        INode destination;
        int index = nodes.IndexOf(source);

        if (index == nodes.Count - 1)
        {
            destination = nodes[0];
        } else
        {
            destination = nodes[index + 1];
        }

        ShortestPathResult result = graph.Dijkstra(nodeToNodeIDMapping[source], nodeToNodeIDMapping[destination]);

        IEnumerable<uint> path = result.GetPath();

        return nodeToNodeIDMapping.ToList().Find((x) => x.Value == path.ElementAt(1)).Key;
    }

    public void UpdateNetworkMap(ConstellationPlan plan)
    {
        foreach (ConstellationPlanEntry entry in plan.entries)
        {
            List<INode> neighbors = new List<INode>();

            foreach (ConstellationPlanEntry innerEntry in plan.entries.Where((x) => x != entry))
            {
                if (Position.Distance(entry.Position, innerEntry.Position) < 100) // 100 = Range for Satellite communication
                {
                    neighbors.Add(innerEntry.Node);
                }
            }

            networkMap[entry.Node] = neighbors;
        }

        Graph<INode, string> updatedGraph = new Graph<INode, string>();

        foreach (KeyValuePair<INode, List<INode>> pair in networkMap)
        {
            uint nodeID = updatedGraph.AddNode(pair.Key);
            nodeToNodeIDMapping[pair.Key] = nodeID;
        }

        foreach (KeyValuePair<INode, List<INode>> pair in networkMap)
        {
            foreach (INode neighbor in pair.Value)
            {
                updatedGraph.Connect(nodeToNodeIDMapping[pair.Key], nodeToNodeIDMapping[neighbor], 1, "");
            }
        }

        graph = updatedGraph;
    }
}