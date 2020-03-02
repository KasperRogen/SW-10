using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Threading;

public class CommsSim : MonoBehaviour, ICommunicate
{
    SatelliteComms comms;


    private void Start()
    {
        comms = GetComponent<SatelliteComms>();
    }

    public void Receive(Request request)
    {
        if (comms.Node.Active == false)
            return;

        comms.Node.Communicate(request);
    }

    public void Send(uint? nextHop, Request request)
    {
        Debug.Log(comms.Node.ID + " -> " + nextHop + "\t : " + request.Command.ToString() + "\t dst: " + request.DestinationID);

        SatelliteComms hop = SatManager._instance.satellites.Find(sat => sat.Node.ID == nextHop);

        

        if (Position.Distance(comms.Node.Position, hop.Node.Position) < comms.CommRadius)
        {
            request.MessageIdentifer = DateTime.Now.ToString() + " milli " + DateTime.Now.Millisecond;
            hop.Node.CommsModule.Receive(request);
        }
    }

    public async Task<Response> SendAsync(uint? nextHop, Request request, int timeout)
    {

        

        var tcs = new TaskCompletionSource<Response>();


        void GetResponse(object sender, ResponseEventArgs e)
        {
            if(e.Response.MessageIdentifer == request.MessageIdentifer)
            {
                OnResponseReceived -= GetResponse;
                tcs.SetResult(e.Response);
            }
        }

        OnResponseReceived += GetResponse;

        Send(nextHop, request);
        
        new Thread(() =>
        {
            Thread.Sleep(timeout);
            if (tcs.Task.IsCompleted == false)
                tcs.SetResult(null);
        }).Start();

        await tcs.Task;

        return tcs.Task.Result;

        

    }

    public void Send(uint? nextHop, Response response)
    {
        Debug.Log(comms.Node.ID + " -> " + nextHop + "\t : Response" + "\t dst: " + response.DestinationID);

        SatelliteComms hop = SatManager._instance.satellites.Find(sat => sat.Node.ID == nextHop);

        if (Position.Distance(comms.Node.Position, hop.Node.Position) < comms.CommRadius)
        {
            hop.Node.CommsModule.Receive(response);
        }
    }

    private class ResponseEventArgs : EventArgs
    {
        public ResponseEventArgs(Response response)
        {
            Response = response;
        }
        public Response Response;
    }

    private delegate void ResponseEventHandler(object sender, ResponseEventArgs e);
    private event ResponseEventHandler OnResponseReceived;

    public void Receive(Response response)
    {
        if(response.DestinationID == comms.Node.ID)
        {
            OnResponseReceived?.Invoke(this, new ResponseEventArgs(response));
        } else
        {
            new Thread(() => 
            {
                Thread.Sleep(250);
                uint? nextHop = comms.Node.Router.NextHop(comms.Node.ID, response.DestinationID);
                Send(nextHop, response);
            }).Start();
            
        }

        
    }
}
