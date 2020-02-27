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

    public void Send(Request request)
    {
        SatelliteComms destination = SatManager._instance.satellites.Find(sat => sat.Node.ID == request.DestinationID);
        
        if(Position.Distance(comms.Node.Position, destination.Node.Position) < comms.CommRadius)
        {
            destination.Node.CommsModule.Receive(request);
        }
    }



}
