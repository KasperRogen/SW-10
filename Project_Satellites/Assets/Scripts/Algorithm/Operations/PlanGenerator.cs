using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Numerics;

public class PlanGenerator
{
    /// <summary>Performs work in order to find optimum location in new constellation for given node
    /// <para>Recieves planrequest, finds best free location, or trades locations with other node in order to optimize net cost</para>
    /// </summary>
    public static void GeneratePlan(INode myNode, PlanRequest request)
    {
        //If the request isn't meant for this node, just return. Node.cs will relay the message   61674131 
        if (request.DestinationID != myNode.Id)
        {
            return;
        }
        else
        {
            // Remove failure detection requests in queue as we are planning to make changes to network structure anyway, which might solve the failure

            if (myNode.Id == 11)
            {
                int a = 2;
            }

            myNode.CommsModule.RequestList.RemoveAll(x => x.Command == Request.Commands.DETECTFAILURE);

            myNode.ExecutingPlan = false;
            myNode.State = Node.NodeState.PLANNING;
            myNode.GeneratingPlan = request.Plan;

            PlanRequest newRequest = request.DeepCopy();
            newRequest.AckExpected = true;

            List<NodeLocationMatch> matches = CalculatePositions(myNode, newRequest);
            ConstellationPlan newPlan = ProcessPlan(matches, newRequest, myNode);
            newRequest.Plan = newPlan;
            Transmit(newRequest, myNode);


        }
    }

    private static void Transmit(PlanRequest request, INode myNode)
    {


        // If last location is filled, execute the plan
        if (request.Plan.Entries.All(x => x.NodeID != null))
        {
            request.Command = Request.Commands.EXECUTE;
            request.SourceID = myNode.Id;

            PlanExecuter.ExecutePlan(myNode, request);
        }
        else
        {
            uint? nextSeq = myNode.Router.NextSequential(myNode, request.Dir);

            if (nextSeq == null)
            {
                Router.CommDir newDir = request.Dir == Router.CommDir.CW ? Router.CommDir.CCW : Router.CommDir.CW;
                request.Dir = newDir;
                nextSeq = myNode.Router.NextSequential(myNode, newDir);
            }

            request.DestinationID = nextSeq;

            if (myNode.Router.NetworkMap.GetEntryByID(myNode.Id).Neighbours.Contains(nextSeq))
            {
                myNode.CommsModule.Send(nextSeq, request);
            }
            else
            {
                uint? nextHop = myNode.Router.NextHop(myNode.Id, nextSeq);
                myNode.CommsModule.Send(nextHop, request);
            }
        }
    }

    private static ConstellationPlan ProcessPlan(List<NodeLocationMatch> matches, PlanRequest request, INode myNode)
    {

        //Find entries not taken by other nodes
        List<ConstellationPlanEntry> FreeEntries = request.Plan.Entries
            .Where(entry => entry.NodeID == null && matches.Select(match => match.Position) //Select positions from matches
            .Contains(entry.Position) == false).ToList();

        //Order by distance to my node
        FreeEntries.OrderBy(entry => Vector3.Distance(myNode.Position, entry.Position));

        //Lowest distance is my best entry.
        ConstellationPlanEntry bestEntry = FreeEntries.First();
        bestEntry = request.Plan.Entries.Find(entry => entry.Position == bestEntry.Position);

        //Take the location in the plan
        bestEntry.NodeID = myNode.Id;
        bestEntry.Fields["DeltaV"].Value = Vector3.Distance(bestEntry.Position, myNode.Position);

        myNode.GeneratingPlan = request.Plan;

        //Return the plan
        return request.Plan;
    }

    private static List<NodeLocationMatch> CalculatePositions(INode myNode, PlanRequest Request)
    {
        List<NodeLocationMatch> matches = new List<NodeLocationMatch>();

        List<NetworkMapEntry> ReachableSats = myNode.Router.NetworkMap.Entries
            .Where(entry => myNode.Router.ReachableSats(myNode).Contains(entry.ID)).ToList();

        foreach (NetworkMapEntry node in ReachableSats)
        {
            if (node.ID == myNode.Id || Request.Plan.Entries.Any(entry => entry.NodeID == node.ID))
                continue;


            //Find all locations
            List<Vector3> OrderedPositions = Request.Plan.Entries
                .Where(entry => entry.NodeID == null)
                .Select(entry => entry.Position).ToList();


            //Find closest location for given node
            OrderedPositions = OrderedPositions.OrderBy(position => Vector3.Distance(node.Position, position)).ToList();


            //Save node-location match
            matches.Add(new NodeLocationMatch(node.ID, OrderedPositions.First(), Vector3.Distance(node.Position, OrderedPositions.First())));
        }

        return matches;
    }


    private class NodeLocationMatch
    {
        public uint? NodeID;
        public Vector3 Position;
        public float Distance;

        public NodeLocationMatch(uint? nodeID, Vector3 position, float distance)
        {
            NodeID = nodeID;
            Position = position;
            Distance = distance;
        }
    }
}