using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Numerics;

public class Discovery
{
    
    public static void StartDiscovery(INode myNode, bool requireFullSync)
    {
        DiscoveryRequest discoveryRequest = new DiscoveryRequest()
        {
            Command = Request.Commands.DISCOVER,
            DestinationID = myNode.ID,
            SourceID = myNode.ID,
            SenderID = myNode.ID,
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
        bool _isIntroduced = true;

        if (MyNode.LastDiscoveryID == "" || MyNode.LastDiscoveryID == null)
            _isIntroduced = false;


        List<uint?> DiscoveredNeighbours = MyNode.CommsModule.Discover();
        if(MyNode.Router.NetworkMap.GetEntryByID(MyNode.ID).Neighbours.Any(neighbour => DiscoveredNeighbours.Contains(neighbour) == false)){
            Heartbeat.CheckHeartbeat(MyNode);
            return;
        }
            
        bool newKnowledge = false;
        bool alteredSet = false;
        MyNode.State = Node.NodeState.DISCOVERY;
        MyNode.LastDiscoveryID = request.MessageIdentifer;

        



        foreach (NetworkMapAlteration alteration in request.Alterations)
        {
            if(alteration.GetType() == typeof(NetworkMapAddition))
            {
                NetworkMapAddition addition = alteration as NetworkMapAddition;
                if(MyNode.Router.NetworkMap.GetEntryByID(addition.Entry.ID) == null)
                {
                    newKnowledge = true;
                    MyNode.Router.NetworkMap.Entries.Add(addition.Entry);
                }
            }
        }

        foreach (NetworkMapAlteration alteration in request.Alterations.ToList())
        {
            if (alteration.GetType() == typeof(NetworkMapAddition))
            {
                NetworkMapAddition netAddition = alteration as NetworkMapAddition;
                if (MyNode.Router.NetworkMap.GetEntryByID(netAddition.Entry.ID) != null)
                {
                    
                    foreach (uint? newNodeNeighbour in netAddition.Entry.Neighbours)
                    {
                        if(MyNode.Router.NetworkMap.GetEntryByID(newNodeNeighbour)?.Neighbours.Contains(netAddition.Entry.ID) == false)
                        {
                            newKnowledge = true;
                            MyNode.Router.NetworkMap.GetEntryByID(newNodeNeighbour)?.Neighbours.Add(netAddition.Entry.ID);
                        }
                        
                    }

                }
            }
        }



        //Additions
        foreach (uint? node in MyNode.CommsModule.Discover().Except(MyNode.Router.NetworkMap.Entries.Select(entry => entry.ID)))
        {
            if (MyNode.Router.NetworkMap.GetEntryByID(node) == null)
            {


                AdditionRequest additionRequest = new AdditionRequest()
                {
                    SourceID = MyNode.ID,
                    SenderID = MyNode.ID,
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


                newKnowledge = true;
                alteredSet = true;

            }
        }




        MyNode.Router.UpdateGraph();

        
        if (alteredSet || newKnowledge || _isIntroduced == false || request.requireFullSync)
        {
            List<ConstellationPlanEntry> newEntries = new List<ConstellationPlanEntry>();

            foreach(NetworkMapEntry entry in MyNode.Router.NetworkMap.Entries)
            {
                Vector3 position = entry.Position;
                List<ConstellationPlanField> fields = new List<ConstellationPlanField> { new ConstellationPlanField("DeltaV", 0, (x, y) => { return x.CompareTo(y); }) };
                ConstellationPlanEntry planEntry = new ConstellationPlanEntry(position, fields, (x, y) => 1);
                planEntry.NodeID = entry.ID; // NodeID must also be set as it caused problems executing a new plan generated after discovery
                newEntries.Add(planEntry);
            }

            MyNode.ActivePlan = new ConstellationPlan(newEntries);

            DiscoveryRequest newRequest = request.DeepCopy();

            newRequest.DestinationID = MyNode.Router.NextSequential(MyNode, request.Dir);

            if(newRequest.DestinationID == null)
            {
                if(request.firstPassDone == true)
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
                } else
                {
                    newRequest.firstPassDone = true;
                    newRequest.DestinationID = MyNode.Router.NextSequential(MyNode, Router.CommDir.CCW);
                    newRequest.Dir = Router.CommDir.CCW;
                }
            }

            newRequest.SourceID = MyNode.ID;
            newRequest.SenderID = MyNode.ID;
            newRequest.AckExpected = true;
            uint? nextHop = MyNode.Router.NextHop(MyNode.ID, newRequest.DestinationID);
            await MyNode.CommsModule.SendAsync(nextHop, newRequest, 1000, 3);
        } else
        {
            if(request.Alterations.Any(alteration => alteration.GetType() == typeof(NetworkMapAddition)))
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
