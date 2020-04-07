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
    public int requestlistcount;
    List<Request> requestList = new List<Request>();
    public SatelliteComms ActiveCommSat = null;
    ConstellationVisualiser visualiser;
    public int nodethreads;

    SatManager satMan;
    private void Start()
    {
        comms = GetComponent<SatelliteComms>();
        satMan = GameObject.FindGameObjectWithTag("SatelliteManager").GetComponent<SatManager>();
        visualiser = GetComponent<ConstellationVisualiser>();
    }

    private void Update()
    {
        nodethreads = comms.Node.ThreadCount;
    }

    public void Receive(Request request)
    {

        if (comms.Node.Active == false)
            return;

        new Thread(() =>
        {
            Thread.Sleep(250);
            requestList.Add(request);
            requestlistcount = requestList.Count;
        }).Start();

    }

    public void Send(uint? nextHop, Request request)
    {
        Debug.Log(comms.Node.ID + " -> " + nextHop + "\t : " + request.Command.ToString() + "\t dst: " + request.DestinationID);

        SatelliteComms hop = SatManager._instance.satellites.Find(sat => sat.Node.ID == nextHop);

        satMan.SentMessages.Add(new Tuple<Vector3, Vector3, Color>(BackendHelpers.UnityVectorFromNumerics(comms.Node.Position), BackendHelpers.UnityVectorFromNumerics(hop.Node.Position), Color.yellow));



        if (System.Numerics.Vector3.Distance(comms.Node.Position, hop.Node.Position) < Constants.ScaleToSize(comms.CommRadius))
        {
            ActiveCommSat = hop;

            if(request.MessageIdentifer == null)
            request.MessageIdentifer = DateTime.Now.ToString() + " milli " + DateTime.Now.Millisecond;

            hop.Node.CommsModule.Receive(request);
            ActiveCommSat = null;
        }
    }

    public async Task<Response> SendAsync(uint? nextHop, Request request, int timeout)
    {

        

        var tcs = new TaskCompletionSource<Response>();


        void GetResponse(object sender, ResponseEventArgs e)
        {
            if (e.Response.MessageIdentifer == request.MessageIdentifer)
            {
                OnResponseReceived -= GetResponse;
                tcs.SetResult(e.Response);
            }
        }

        OnResponseReceived += GetResponse;

        Send(nextHop, request);

        new Thread(() =>
        {
            comms.Node.ThreadCount++;
            Thread.Sleep(timeout);
            if (tcs.Task.IsCompleted == false)
                tcs.SetResult(null);
            comms.Node.ThreadCount--;
        }).Start();

        await tcs.Task;

        return tcs.Task.Result;



    }

    public List<uint?> Discover()
    {

        List<SatelliteComms> commsList = new List<SatelliteComms>();

        int desiredSatCount = 2;


        foreach (SatelliteComms sat in satMan.satellites
            .Where(sat => sat.Node.ID != comms.Node.ID)
            .OrderBy(sat => System.Numerics.Vector3.Distance(sat.Node.Position, comms.Node.Position))
            .ToList())
        {
            float dist = System.Numerics.Vector3.Distance(sat.Node.Position, comms.Node.Position);
            float range = Constants.ScaleToSize(comms.CommRadius);
            if (dist < range)
                commsList.Add(sat);
        }

        List<uint?> newCommsList = new List<uint?>();


        return commsList.Select(col => col.Node.ID).ToList();
    }



    public void Send(uint? nextHop, Response response)
    {
        Debug.Log(comms.Node.ID + " -> " + nextHop + "\t : Response" + "\t dst: " + response.DestinationID);

        SatelliteComms hop = SatManager._instance.satellites.Find(sat => sat.Node.ID == nextHop);

        satMan.SentMessages.Add(new Tuple<Vector3, Vector3, Color>(BackendHelpers.UnityVectorFromNumerics(comms.Node.Position), BackendHelpers.UnityVectorFromNumerics(hop.Node.Position), Color.blue));

        if (System.Numerics.Vector3.Distance(comms.Node.Position, hop.Node.Position) < Constants.ScaleToSize(comms.CommRadius))
        {
            hop.Node.CommsModule.Receive(response);
            ActiveCommSat = hop;
            ActiveCommSat = null;
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

        if (response.GetType() == typeof(FailureDetectionResponse))
        {
            (response as FailureDetectionResponse).DeadEdges.ForEach(edge => comms.Node.Router.DeleteEdge(edge.Item1, edge.Item2));
        }




        new Thread(() =>
        {
            Thread.Sleep(450);

            if (response.DestinationID == comms.Node.ID)
            {
                OnResponseReceived?.Invoke(this, new ResponseEventArgs(response));
            }
            else
            {
                comms.Node.ThreadCount++;
                uint? nextHop = comms.Node.Router.NextHop(comms.Node.ID, response.DestinationID);
                Send(nextHop, response);
                comms.Node.ThreadCount--;

            }
        }).Start();



    }


    public Request FetchNextRequest()
    {
        if (requestList.Count > 0)
        {
            Request request = requestList[0];
            requestList.RemoveAt(0);
            return request;
        }

        return null;
    }
}
