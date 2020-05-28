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
            DestinationID = myNode.Id,
            SourceID = myNode.Id,
            requireFullSync = requireFullSync,
            Alterations = new List<NetworkMapAlteration>()
        };

        myNode.CommsModule.Send(myNode.Id, discoveryRequest);
    }


    /// <summary>procedure for updating network map for the entire network
    /// <para>  </para>
    /// </summary>
    public static async System.Threading.Tasks.Task DiscoverAsync(INode myNode, DiscoveryRequest request)
    {
        //Break References to request
        DiscoveryRequest requestClone = request;

        //Check in any neighbours has gone missing
        CheckNeighbours(myNode);

        //If the node hasn't been introducued yet, propagation is allowed
        bool propagationAllowed = (myNode.LastDiscoveryId == "" || myNode.LastDiscoveryId == null);

        myNode.State = Node.NodeState.DISCOVERY;
        myNode.LastDiscoveryId = requestClone.MessageIdentifer;

        //Propagation is allowed if a new node is added to the request, or a new node has been discovered
        propagationAllowed = ImplementAlterations(requestClone, myNode) || propagationAllowed;
        propagationAllowed = await CreateAdditions(myNode, requestClone) || propagationAllowed;

        myNode.Router.UpdateGraph();

        //If propagation is allowed, propagate.
        CheckPropagation(propagationAllowed, requestClone, myNode);
       

        myNode.State = Node.NodeState.PASSIVE;
    }

    private static async void CheckPropagation(bool propagationAllowed, DiscoveryRequest request, INode myNode)
    {
        if (propagationAllowed || (request.requireFullSync) && myNode.Id == request.SourceID)
        {
            List<ConstellationPlanEntry> newPlanEntries = new List<ConstellationPlanEntry>();

            //update my constellationplan based on the new knowledge
            foreach (NetworkMapEntry entry in myNode.Router.NetworkMap.Entries)
            {
                Vector3 position = entry.Position;
                List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => x.CompareTo(y)) };
                ConstellationPlanEntry planEntry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
                planEntry.NodeID = entry.ID;
                newPlanEntries.Add(planEntry);
            }

            myNode.ActivePlan = new ConstellationPlan(newPlanEntries);


            DiscoveryRequest newRequest = request.DeepCopy();

            newRequest.DestinationID = myNode.Router.NextSequential(myNode, request.Dir);

            if (newRequest.DestinationID == null)
            {
                if (request.firstPassDone)
                {
                    if (request.Alterations.Any(alteration => alteration.GetType() == typeof(NetworkMapAddition)))
                    {
                        ConstellationPlan recoveryPlan = GenerateConstellation.GenerateTargetConstellation(myNode.Router.ReachableSats(myNode).Count, 7.152f);


                        PlanRequest recoveryRequest = new PlanRequest
                        {
                            SourceID = myNode.Id,
                            DestinationID = myNode.Id,
                            Command = Request.Commands.GENERATE,
                            Plan = recoveryPlan
                        };

                        if (myNode.Router.NextSequential(myNode, Router.CommDir.CW) == null)
                        {
                            recoveryRequest.Dir = Router.CommDir.CCW;
                        }

                        myNode.CommsModule.Send(myNode.Id, recoveryRequest);
                        return;
                    }
                }
                else
                {
                    newRequest.firstPassDone = true;
                    newRequest.DestinationID = myNode.Router.NextSequential(myNode, Router.CommDir.CCW);
                    newRequest.Dir = Router.CommDir.CCW;
                }
            }

            newRequest.SenderID = myNode.Id;
            newRequest.AckExpected = true;
            uint? nextHop = myNode.Router.NextHop(myNode.Id, newRequest.DestinationID);
            await myNode.CommsModule.SendAsync(nextHop, newRequest, 1000, 3);
        }
        else
        {
            if (request.Alterations.Any(alteration => alteration.GetType() == typeof(NetworkMapAddition)))
            {
                ConstellationPlan recoveryPlan = GenerateConstellation.GenerateTargetConstellation(myNode.Router.ReachableSats(myNode).Count, 7.152f);


                PlanRequest recoveryRequest = new PlanRequest
                {
                    SourceID = myNode.Id,
                    DestinationID = myNode.Id,
                    Command = Request.Commands.GENERATE,
                    Plan = recoveryPlan
                };

                if (myNode.Router.NextSequential(myNode, Router.CommDir.CW) == null)
                {
                    recoveryRequest.Dir = Router.CommDir.CCW;
                }

                myNode.CommsModule.Send(myNode.Id, recoveryRequest);
            }
        }

    }

    private static async Task<bool> CreateAdditions(INode myNode, DiscoveryRequest request)
    {

        bool propagationAllowed = false;

        //Additions
        foreach (uint? node in myNode.CommsModule.Discover().Except(myNode.Router.NetworkMap.Entries.Select(entry => entry.ID)))
        {
            if (myNode.Router.NetworkMap.GetEntryByID(node) == null)
            {


                AdditionRequest additionRequest = new AdditionRequest()
                {
                    SourceID = myNode.Id,
                    DestinationID = node,
                    Command = Request.Commands.ADDITION,
                    AckExpected = false,
                    ResponseExpected = true,
                    plan = myNode.ActivePlan
                };
                NodeAdditionResponse response = await myNode.CommsModule.SendAsync(additionRequest.DestinationID, additionRequest, 2000, 3) as NodeAdditionResponse;
                Vector3 position = response.Position;
                List<uint?> nodeNeighbours = response.Neighbours;

                NetworkMapEntry neigbourEntry = new NetworkMapEntry(node, nodeNeighbours, position);
                NetworkMapEntry ent = new NetworkMapEntry(neigbourEntry.ID, neigbourEntry.Neighbours, neigbourEntry.Position);

                myNode.Router.NetworkMap.GetEntryByID(myNode.Id).Neighbours.Add(node);
                myNode.Router.NetworkMap.Entries.Add(ent);

                request.Alterations.Add(new NetworkMapAddition(ent));


                propagationAllowed = true;

            }
        }

        return propagationAllowed;
    }

    private static bool ImplementAlterations(DiscoveryRequest request, INode myNode)
    {
        bool propagationAllowed = false;

        //if any additions are new informatio to me, store in networkmap
        foreach (NetworkMapAlteration alteration in request.Alterations)
        {
            if (alteration.GetType() == typeof(NetworkMapAddition))
            {
                NetworkMapAddition addition = alteration as NetworkMapAddition;
                if (myNode.Router.NetworkMap.GetEntryByID(addition.Entry.ID) == null)
                {
                    propagationAllowed = true;
                    myNode.Router.NetworkMap.Entries.Add(addition.Entry);
                }

                if (myNode.Router.NetworkMap.GetEntryByID(addition.Entry.ID) != null)
                {

                    foreach (uint? newNodeNeighbour in addition.Entry.Neighbours)
                    {
                        if (myNode.Router.NetworkMap.GetEntryByID(newNodeNeighbour)?.Neighbours.Contains(addition.Entry.ID) == false)
                        {
                            propagationAllowed = true;
                            myNode.Router.NetworkMap.GetEntryByID(newNodeNeighbour)?.Neighbours.Add(addition.Entry.ID);
                        }

                    }

                }
            }
        }

        return propagationAllowed;

    }

    private static void CheckNeighbours(INode myNode)
    {
        //Fetch immidiate neighbours
        List<uint?> discoveredNeighbours = myNode.CommsModule.Discover();


        //Check my neighbours, if any "old" neighbours wasn't detected above, heartbeat them
        if (myNode.Router.NetworkMap.GetEntryByID(myNode.Id).Neighbours
            .Any(neighbour => discoveredNeighbours.Contains(neighbour) == false))
        {
            Heartbeat.CheckHeartbeat(myNode);
        }

    }
}
