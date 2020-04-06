﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;

public class Router : IRouter
{
    public enum CommDir
    {
        CW, CCW
    }


    INode node;
    private Graph<uint?, string> graph = new Graph<uint?, string>();
    private Dictionary<uint?, uint> nodeToNodeIDMapping = new Dictionary<uint?, uint>();
    private float satRange = 5f;

    public Router(INode _node, ConstellationPlan _plan)
    {
        node = _node;
        NetworkMap = new NetworkMap();

        if (_plan != null && _plan.Entries.TrueForAll(entry => entry.NodeID != null))
        {
            foreach (ConstellationPlanEntry entry in _plan.Entries)
            {
                NetworkMap.Entries.Add(new NetworkMapEntry(entry.NodeID, entry.Position));
                nodeToNodeIDMapping.Add(entry.NodeID, 0);
            }
        }

        UpdateNetworkMap(_plan);
    }



    // Returns next sequential neighbour node based on current plan.
    // Always sends clockwise or counterclockwise (cant remember which one).
    public uint? NextSequential(INode source, CommDir dir)
    {


        Vector3 EarthPosition = Vector3.Zero;
        
        if(NetworkMap.GetEntryByID(source.ID).Neighbours.Count < 2)
        {
            return null;
        }

        // Assumption: Always 2 neighbours, if not the case it is handled by fault mechanisms.

        //List<ConstellationPlanEntry> neighbourEntries = plan.Entries.Where(x => NetworkMap[source].Contains(x.NodeID)).ToList();

        List<NetworkMapEntry> neighbourEntries = source.Router.NetworkMap.Entries.Where(entry =>
        source.Router.NetworkMap.GetEntryByID(source.ID).Neighbours.Contains(entry.ID)).ToList();
        //
        //The "up" vector for the constellation plan is calculated.      //(B - A) cross (C - B)
        
        List<uint?> NeighbourIDs = NetworkMap.GetEntryByID(source.ID).Neighbours;
       
        Vector3 SatClockwiseVector = Vector3.Cross(EarthPosition - source.Position, source.PlaneNormalDir);


        
        if (neighbourEntries.Any() == false || neighbourEntries.Count <= 1)
        {
            return null;
        }

        // Assumption: Always 2 neighbours, if not the case it is handled by fault mechanisms.
        Vector3 normalVector = source.PlaneNormalDir;
        List<double> angles = neighbourEntries.Select(x => BackendHelpers.NumericsVectorSignedAngle(source.Position, x.Position, normalVector)).ToList();
        if(angles.Any(angle => angle > 0))
        {
            if(dir == CommDir.CW)
            {
                return neighbourEntries[angles.IndexOf(angles.Where(angle => angle > 0).Min())].ID;
            } else if(dir == CommDir.CCW)
            {
                return neighbourEntries[angles.IndexOf(angles.Where(angle => angle < 0).Max())].ID;
            }
        }

        return null;
    }

    public void AddNodeToGraph(uint? neighbour)
    {
        if(nodeToNodeIDMapping.ContainsKey(neighbour) == false)
        {
            uint nodeID = graph.AddNode(neighbour);          
            nodeToNodeIDMapping[neighbour] = nodeID;
            graph.Connect(nodeToNodeIDMapping[node.ID], nodeToNodeIDMapping[neighbour], 1, "");
            
        }
        
    }

    double AngleBetween(Vector3 u, Vector3 v)
    {
        var angleInRadians = Math.Acos(Vector3.Dot(v, u) / (v.Length() * u.Length()));

        return angleInRadians *= 360.0 / (2 * Math.PI);
    }


    public override uint? NextHop(uint? source, uint? destination)
    {
        ShortestPathResult result = graph.Dijkstra(nodeToNodeIDMapping[source], nodeToNodeIDMapping[destination]);

        IEnumerable<uint> path = result.GetPath();
        int a = path.Count();
        uint? nextHop = nodeToNodeIDMapping.ToList().Find((x) => x.Value == path.ElementAt(1)).Key;
        return nextHop;
    }


    public override void UpdateNetworkMap(ConstellationPlan plan)
    {
        if (plan == null)
            return;

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


            if (entry.NodeID != null)
            {

                NetworkMap.GetEntryByID(entry.NodeID).Neighbours = neighbors.Select(sat => sat.Item1).ToList();
            }

        }

        UpdateGraph();
    }

    public void DeleteEdge(uint? n1, uint? n2)
    {
        if (NetworkMap.GetEntryByID(n1).Neighbours.Contains(n2))
            NetworkMap.GetEntryByID(n1).Neighbours.Remove(n2);

        if (NetworkMap.GetEntryByID(n2).Neighbours.Contains(n1))
            NetworkMap.GetEntryByID(n2).Neighbours.Remove(n1);

        UpdateGraph();
    }

    public void UpdateGraph()
    {
        Graph<uint?, string> updatedGraph = new Graph<uint?, string>();

        foreach (NetworkMapEntry entry in NetworkMap.Entries)
        {
            uint nodeID = updatedGraph.AddNode(entry.ID);
            nodeToNodeIDMapping[entry.ID] = nodeID;
        }

        if(node.ID == 12)
        {
            int i = 0;
        }

        foreach (NetworkMapEntry entry in NetworkMap.Entries)
        {
            foreach (uint? neighbor in entry.Neighbours)
            {
                if (nodeToNodeIDMapping.ContainsKey(neighbor) == false)
                {
                    uint nodeID = updatedGraph.AddNode(neighbor);
                    nodeToNodeIDMapping[neighbor] = nodeID;
                }

                updatedGraph.Connect(nodeToNodeIDMapping[entry.ID], nodeToNodeIDMapping[neighbor], 1, "");
            }
        }

        graph = updatedGraph;
    }

    public override void ClearNetworkMap()
    {
        graph = new Graph<uint?, string>();
        nodeToNodeIDMapping.Clear();
        nodeToNodeIDMapping.Add(node.ID, 0);
        UpdateGraph();
    }
}