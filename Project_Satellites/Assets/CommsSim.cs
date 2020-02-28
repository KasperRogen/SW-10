using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CommsSim : MonoBehaviour, ICommunicate
{
    SatelliteComms comms;

    private void Start()
    {
        comms = GetComponent<SatelliteComms>();
    }

    public void Receive(Request request)
    {
        comms.Node.Communicate(request);
    }

    public void Send(uint? nextHop, Request request)
    {
        SatelliteComms hop = SatManager._instance.satellites.Find(sat => sat.Node.ID == nextHop);

        if(Position.Distance(comms.Node.Position, hop.Node.Position) < comms.CommRadius)
        {
            hop.Node.CommsModule.Receive(request);
        }
    }



}
