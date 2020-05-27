using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Numerics;
using System.Threading.Tasks;

public class Discovery
{
    
    public static void StartDiscovery(INode myNode, bool requireFullSync)
    {
        DiscoveryRequest discoveryRequest = new DiscoveryRequest()
        {
            Command = Request.Commands.DISCOVER,
            DestinationID = myNode.ID,
            SourceID = myNode.ID,
            requireFullSync = requireFullSync,
            Alterations = new List<NetworkMapAlteration>()
        };

        myNode.CommsModule.Send(myNode.ID, discoveryRequest);
    }


    /// <summary>procedure for updating network map for the entire network
    /// <para>  </para>
    /// </summary>
    public static async System.Threading.Tasks.Task DiscoverAsync(INode MyNode, DiscoveryRequest request)
    {
        //Break References to request
        DiscoveryRequest requestClone = request;

        //Check in any neighbours has gone missing
        CheckNeighbours(MyNode);

        //If the node hasn't been introducued yet, propagation is allowed
        bool PropagationAllowed = (MyNode.LastDiscoveryID == "" || MyNode.LastDiscoveryID == null);

        MyNode.State = Node.NodeState.DISCOVERY;
        MyNode.LastDiscoveryID = requestClone.MessageIdentifer;

        //Propagation is allowed if a new node is added to the request, or a new node has been discovered
        PropagationAllowed = ImplementAlterations(requestClone, MyNode) || PropagationAllowed;
        PropagationAllowed = await CreateAdditions(MyNode, requestClone) || PropagationAllowed;

        MyNode.Router.UpdateGraph();

        //If propagation is allowed, propagate.
        CheckPropagation(PropagationAllowed, requestClone, MyNode);
       

        MyNode.State = Node.NodeState.PASSIVE;
    }

    private static async void CheckPropagation(bool PropagationAllowed, DiscoveryRequest request, INode MyNode)
    {
        if (PropagationAllowed || request.requireFullSync)
        {
            List<ConstellationPlanEntry> newPlanEntries = new List<ConstellationPlanEntry>();

            //update my constellationplan based on the new knowledge
            foreach (NetworkMapEntry entry in MyNode.Router.NetworkMap.Entries)
            {
                Vector3 position = entry.Position;
                List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => { return x.CompareTo(y); }) };
                ConstellationPlanEntry planEntry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
                planEntry.NodeID = entry.ID;
                newPlanEntries.Add(planEntry);
            }

            MyNode.ActivePlan = new ConstellationPlan(newPlanEntries);


            DiscoveryRequest newRequest = request.DeepCopy();

            newRequest.DestinationID = MyNode.Router.NextSequential(MyNode, request.Dir);

            if (newRequest.DestinationID == null)
            {
                if (request.firstPassDone == true)
                {
                    if (request.Alterations.Any(alteration => alteration.GetType() == typeof(NetworkMapAddition)))
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
                else
                {
                    newRequest.firstPassDone = true;
                    newRequest.DestinationID = MyNode.Router.NextSequential(MyNode, Router.CommDir.CCW);
                    newRequest.Dir = Router.CommDir.CCW;
                }
            }

            newRequest.SourceID = MyNode.ID;
            newRequest.AckExpected = true;
            uint? nextHop = MyNode.Router.NextHop(MyNode.ID, newRequest.DestinationID);
            await MyNode.CommsModule.SendAsync(nextHop, newRequest, 1000, 3);
        }
        else
        {
            if (request.Alterations.Any(alteration => alteration.GetType() == typeof(NetworkMapAddition)))
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

    }

    private static async Task<bool> CreateAdditions(INode MyNode, DiscoveryRequest request)
    {

        bool PropagationAllowed = false;

        //Additions
        foreach (uint? node in MyNode.CommsModule.Discover().Except(MyNode.Router.NetworkMap.Entries.Select(entry => entry.ID)))
        {
            if (MyNode.Router.NetworkMap.GetEntryByID(node) == null)
            {


                AdditionRequest additionRequest = new AdditionRequest()
                {
                    SourceID = MyNode.ID,
                    DestinationID = node,
                    Command = Request.Commands.ADDITION,
                    AckExpected = false,
                    ResponseExpected = true,
                    plan = MyNode.ActivePlan
                };
                NodeAdditionResponse response = await MyNode.CommsModule.SendAsync(additionRequest.DestinationID, additionRequest, 2000, 3) as NodeAdditionResponse;
                Vector3 position = response.Position;
                List<uint?> nodeNeighbours = response.Neighbours;

                NetworkMapEntry neigbourEntry = new NetworkMapEntry(node, nodeNeighbours, position);
                NetworkMapEntry ent = new NetworkMapEntry(neigbourEntry.ID, neigbourEntry.Neighbours, neigbourEntry.Position);

                MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.Add(node);
                MyNode.Router.NetworkMap.Entries.Add(ent);

                request.Alterations.Add(new NetworkMapAddition(ent));


                PropagationAllowed = true;

            }
        }

        return PropagationAllowed;
    }

    private static bool ImplementAlterations(DiscoveryRequest request, INode MyNode)
    {
        bool PropagationAllowed = false;

        //if any additions are new informatio to me, store in networkmap
        foreach (NetworkMapAlteration alteration in request.Alterations)
        {
            if (alteration.GetType() == typeof(NetworkMapAddition))
            {
                NetworkMapAddition addition = alteration as NetworkMapAddition;
                if (MyNode.Router.NetworkMap.GetEntryByID(addition.Entry.ID) == null)
                {
                    PropagationAllowed = true;
                    MyNode.Router.NetworkMap.Entries.Add(addition.Entry);
                }

                if (MyNode.Router.NetworkMap.GetEntryByID(addition.Entry.ID) != null)
                {

                    foreach (uint? newNodeNeighbour in addition.Entry.Neighbours)
                    {
                        if (MyNode.Router.NetworkMap.GetEntryByID(newNodeNeighbour)?.Neighbours.Contains(addition.Entry.ID) == false)
                        {
                            PropagationAllowed = true;
                            MyNode.Router.NetworkMap.GetEntryByID(newNodeNeighbour)?.Neighbours.Add(addition.Entry.ID);
                        }

                    }

                }
            }
        }

        return PropagationAllowed;

    }

    private static void CheckNeighbours(INode myNode)
    {
        //Fetch immidiate neighbours
        List<uint?> DiscoveredNeighbours = myNode.CommsModule.Discover();


        //Check my neighbours, if any "old" neighbours wasn't detected above, heartbeat them
        if (myNode.Router.NetworkMap.GetEntryByID(myNode.ID).Neighbours
            .Any(neighbour => DiscoveredNeighbours.Contains(neighbour) == false))
        {
            Heartbeat.CheckHeartbeat(myNode);
            return;
        }

    }
}
