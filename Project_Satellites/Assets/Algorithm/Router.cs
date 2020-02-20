using System.Collections.Generic;
using System.Linq;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;

public class Router : IRouter
{
    public Dictionary<INode, List<INode>> NetworkMap = new Dictionary<INode, List<INode>>();
    Graph<INode, string> graph;
    Dictionary<INode, uint> nodeToNodeIDMapping = new Dictionary<INode, uint>();

    float satRange = 5f;

    public Router(ConstellationPlan plan)
    {

        NetworkMap = new Dictionary<INode, List<INode>>();
        if (plan.entries.TrueForAll(entry => entry.Node != null))
        {

            foreach (ConstellationPlanEntry entry in plan.entries)
            {
                NetworkMap.Add(entry.Node, new List<INode>());
                nodeToNodeIDMapping.Add(entry.Node, 0);
            }


        }


        UpdateNetworkMap(plan);

    }


    public INode NextSequential(INode source)
    {
        List<INode> nodes = new List<INode>();
        List<INode> lastNodes = new List<INode>();

        NetworkMap[source].ForEach(node => nodes.Add(node));
        nodes.Add(source);

        do
        {
            lastNodes.Clear();
            nodes.ForEach(node => lastNodes.Add(node));

            List<INode> newNodes = new List<INode>();
            nodes.ForEach(node => node.router.NetworkMap[node].ForEach(newNode => newNodes.Add(newNode)));
            nodes.AddRange(newNodes);
            
            nodes = nodes.Distinct().ToList();
            nodes = nodes.OrderBy((x) => x.ID).ToList();
            lastNodes = lastNodes.Distinct().ToList();
            lastNodes = lastNodes.OrderBy((x) => x.ID).ToList();

            List<INode> diff = lastNodes.Except(nodes).ToList();

        } while (nodes.TrueForAll(node => lastNodes.Contains(node)) == false);

        nodes = nodes.OrderBy((x) => x.ID).ToList();

        INode destination;
        int index = nodes.IndexOf(source);

        if (index == nodes.Count - 1)
        {
            destination = nodes[0];
        }
        else
        {
            destination = nodes[index + 1];
        }

        return destination;
    }

    public override INode NextHop(INode source, INode destination)
    {
        List<INode> nodes = new List<INode>(); 
        
        NetworkMap[source].ForEach(node => nodes.Add(node));
        nodes.Add(source);
        nodes = nodes.OrderBy((x) => x.ID).ToList();

        
        ShortestPathResult result = graph.Dijkstra(nodeToNodeIDMapping[source], nodeToNodeIDMapping[destination]);

        IEnumerable<uint> path = result.GetPath();

        return nodeToNodeIDMapping.ToList().Find((x) => x.Value == path.ElementAt(1)).Key;
    }

    public override void UpdateNetworkMap(ConstellationPlan plan)
    {
        foreach (ConstellationPlanEntry entry in plan.entries)
        {
            List<INode> neighbors = new List<INode>();

            foreach (ConstellationPlanEntry innerEntry in plan.entries.Where((x) => x != entry))
            {
                if (Position.Distance(entry.Position, innerEntry.Position) < satRange) // 100 = Range for Satellite communication
                {
                    if (innerEntry.Node != null)
                    neighbors.Add(innerEntry.Node);
                }
            }

            if(entry.Node != null)
                NetworkMap[entry.Node] = neighbors;
        }

        Graph<INode, string> updatedGraph = new Graph<INode, string>();

        foreach (KeyValuePair<INode, List<INode>> pair in NetworkMap)
        {
            uint nodeID = updatedGraph.AddNode(pair.Key);
            nodeToNodeIDMapping[pair.Key] = nodeID;
        }

        foreach (KeyValuePair<INode, List<INode>> pair in NetworkMap)
        {
            foreach (INode neighbor in pair.Value)
            {
                updatedGraph.Connect(nodeToNodeIDMapping[pair.Key], nodeToNodeIDMapping[neighbor], 1, "");
            }
        }

        graph = updatedGraph;
    }
}