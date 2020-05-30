using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class PlanGenerator
{
    public static void GeneratePlan(INode myNode, PlanRequest request)
    {
        // Remove failure detection requests in queue as we are planning to make changes to network structure anyway, which might solve the failure
        myNode.CommsModule.requestList.RemoveAll(x => x.Command == Request.Commands.DETECTFAILURE);

        if (request.DestinationID != myNode.ID)
        {
            return;
        }
        else
        {
            myNode.executingPlan = false;
            myNode.State = Node.NodeState.PLANNING;
            myNode.GeneratingPlan = request.Plan;

            PlanRequest newRequest = request.DeepCopy();

            List<NodeLocationMatch> matches = CalculatePositions(myNode, newRequest);
            ConstellationPlan newPlan = ProcessPlan(myNode, newRequest, matches);
            Transmit(myNode, newRequest);
        }
    }

    private static List<NodeLocationMatch> CalculatePositions(INode myNode, PlanRequest Request) {
        List<NodeLocationMatch> matches = new List<NodeLocationMatch>();

        foreach (NetworkMapEntry node in myNode.Router.NetworkMap.Entries.OrderBy(entry => entry.ID)) {
            if (node.ID == myNode.ID || Request.Plan.Entries.Any(entry => entry.NodeID == node.ID)) {
                continue;
            }

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

    private class NodeLocationMatch {
        public uint? NodeID;
        public Vector3 Position;
        public float Distance;

        public NodeLocationMatch(uint? nodeID, Vector3 position, float distance) {
            NodeID = nodeID;
            Position = position;
            Distance = distance;
        }
    }

    private static ConstellationPlan ProcessPlan(INode myNode, PlanRequest request, List<NodeLocationMatch> matches) {
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
        bestEntry.NodeID = myNode.ID;
        bestEntry.Fields["DeltaV"].Value = Vector3.Distance(bestEntry.Position, myNode.Position);

        myNode.GeneratingPlan = request.Plan;

        //Return the plan
        return request.Plan;
    }

    private static void Transmit(INode myNode, PlanRequest request)
    {
        // If last location is filled, execute the plan
        if (request.Plan.Entries.All(x => x.NodeID != null))
        {
            request.Command = Request.Commands.EXECUTE;
            request.SourceID = myNode.ID;

            PlanExecuter.ExecutePlan(myNode, request);
        }
        else
        {
            ForwardRequest(myNode, request);
        }
    }

    private static void ForwardRequest(INode myNode, PlanRequest request) {
        uint? nextSeq = myNode.Router.NextSequential(myNode, request.Dir);

        if (nextSeq == null) {
            request.Dir = request.Dir == Router.CommDir.CW ? Router.CommDir.CCW : Router.CommDir.CW;
            nextSeq = myNode.Router.NextSequential(myNode, request.Dir);
        }

        request.DestinationID = nextSeq;

        if (myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours.Contains(nextSeq)) {
            myNode.CommsModule.SendAsync(nextSeq, request, Constants.COMMS_TIMEOUT, Constants.COMMS_ATTEMPTS);
        } else {
            uint? nextHop = myNode.Router.NextHop(myNode.ID, nextSeq);
            myNode.CommsModule.SendAsync(nextHop, request, Constants.COMMS_TIMEOUT, Constants.COMMS_ATTEMPTS);
        }
    }
}
