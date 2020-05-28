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
    public List<Request> RequestList { get; set; } = new List<Request>();
    public SatelliteComms ActiveCommSat = null;

    public static List<string> logs = new List<string>();

    SatManager satMan;
    private void Start()
    {
        comms = GetComponent<SatelliteComms>();
        satMan = GameObject.FindGameObjectWithTag("SatelliteManager").GetComponent<SatManager>();
    }

    private void Update()
    {
        comms.Node.Position = BackendHelpers.NumericsVectorFromUnity(transform.position);





        if (Constants.EnableDebug)
        {
            logs.ForEach(Debug.LogWarning);
            logs.Clear();
        }
    }

    public void Receive(Request request)
    {
        if (comms.Node.Active == false)
            return;

        new Thread(() =>
        {



            Thread.Sleep(250 / Constants.TimeScale);
            
            comms.Node.Router.AddNodeToGraph(comms.Node.Id);
            if(request.SenderID != null)
                comms.Node.Router.AddNodeToGraph(request.SenderID);

            RequestList.Add(request);


            if (request.AckExpected)
            {

                Response response = new Response()
                {
                    SourceID = comms.Node.Id,
                    DestinationID = request.SenderID,
                    ResponseCode = Response.ResponseCodes.ACK,
                    MessageIdentifer = request.MessageIdentifer
                };

                uint? nextHop = comms.Node.Router.NextHop(comms.Node.Id, response.DestinationID);
                Send(nextHop, response);
            }






        }).Start();

    }

    public void Send(uint? nextHop, Request request)
    {
        request.SenderID = comms.Node.Id;
        SatelliteComms hop = SatManager._instance.satellites.Find(sat => sat.Node.Id == nextHop);

        satMan.SentMessages.Add(new Tuple<Vector3, Vector3, Color>(BackendHelpers.UnityVectorFromNumerics(comms.Node.Position), BackendHelpers.UnityVectorFromNumerics(hop.Node.Position), Color.yellow));

        if (System.Numerics.Vector3.Distance(comms.Node.Position, hop.Node.Position) < Constants.ScaleToSize(comms.CommRadius))
        {
            ActiveCommSat = hop;

            if (request.MessageIdentifer == null)
                request.MessageIdentifer = DateTime.Now.ToString() + " milli " + DateTime.Now.Millisecond;

            if(Constants.EnableDebug)
                Debug.Log(request.Dir + ": " + comms.Node.Id + " -> " + nextHop + "\t : " + request.Command.ToString() + "\t dst: " + request.DestinationID + "\t msgID: " + request.MessageIdentifer);

            hop.Node.CommsModule.Receive(request);
            ActiveCommSat = null;
        }
        else
        {
            if (Constants.EnableDebug)
                Debug.Log(request.Dir + ": " + comms.Node.Id + " -> " + nextHop + "\t : " + request.Command.ToString() + "\t dst: " + request.DestinationID + "\t msgID: " + request.MessageIdentifer);
        }
    }

    public async Task<Response> SendAsync(uint? nextHop, Request request, int timeout, int attempts)
    {
        int retryDelay = 1000;
        TaskCompletionSource<Response> tcs = new TaskCompletionSource<Response>();
        bool AckReceived = false;

        void GetResponse(object sender, ResponseEventArgs e)
        {
            if (e.Response.MessageIdentifer == request.MessageIdentifer)
            {
                if(request.SourceID != comms.Node.Id)
                {
                    if(request.AckExpected && e.Response.ResponseCode == Response.ResponseCodes.ACK)
                    {
                        OnResponseReceived -= GetResponse;
                        tcs.SetResult(e.Response);
                        return;
                    }

                }

                if (request.ResponseExpected)
                {
                    if (e.Response.ResponseCode == Response.ResponseCodes.ACK)
                    {
                        AckReceived = true;
                    } else
                    {
                        OnResponseReceived -= GetResponse;
                        tcs.SetResult(e.Response);
                        return;
                    }    
                } else if(request.AckExpected)
                {
                    OnResponseReceived -= GetResponse;
                    tcs.SetResult(e.Response);
                    return;
                }
            }
        }

        new Thread(() =>
        {

            OnResponseReceived += GetResponse;


            // Attempt to send multiple times, break loop if response
            for (int i = 0; i < attempts; i++)
            {

                if (AckReceived == false)
                {
                    Send(nextHop, request);
                    Thread.Sleep(timeout / Constants.TimeScale);

                    // Delay and retry with increasing delay
                    if (tcs.Task.IsCompleted == false)
                    {
                        Thread.Sleep(retryDelay / Constants.TimeScale);
                        retryDelay *= 2;
                    }
                    else
                    {
                        break;
                    }
                }

            }

            if (tcs.Task.IsCompleted == false) {
                Response response = new Response() {
                    SourceID = request.DestinationID,
                    DestinationID = request.SourceID,
                    MessageIdentifer = request.MessageIdentifer,
                    ResponseCode = Response.ResponseCodes.TIMEOUT
                };
                tcs.SetResult(response);
            }
        }).Start();

        await tcs.Task;

        // Trigger failure handling if no response after several attempts
        if (request.GetType() != typeof(DetectFailureRequest) && request.Command != Request.Commands.PING && tcs.Task.Result.ResponseCode == Response.ResponseCodes.TIMEOUT)
        {
            FailureDetection.FailureDetected(comms.Node, nextHop);
        }
        // Trigger recovery if no response after several attempts and already failure handling and attempted node is not the one to be checked via failure handling
        else if (request.GetType() == typeof(DetectFailureRequest) && tcs.Task.Result.ResponseCode == Response.ResponseCodes.TIMEOUT && (request as DetectFailureRequest).NodeToCheck != nextHop)
        {
            FailureDetection.Recovery(comms.Node, nextHop);
        }

        return tcs.Task.Result;
    }

    public List<uint?> Discover()
    {

        List<SatelliteComms> commsList = new List<SatelliteComms>();


        foreach (SatelliteComms sat in satMan.satellites
            .Where(sat => sat.Node.Id != comms.Node.Id && sat.Node.Active)
            .OrderBy(sat => System.Numerics.Vector3.Distance(sat.Node.Position, comms.Node.Position))
            .ToList())
        {
            float dist = System.Numerics.Vector3.Distance(sat.Node.Position, comms.Node.Position);
            float range = Constants.ScaleToSize(comms.CommRadius);
            if (dist < range)
                commsList.Add(sat);
        }

        List<uint?> newCommsList = new List<uint?>();


        return commsList.Select(col => col.Node.Id).ToList();
    }



    public void Send(uint? nextHop, Response response)
    {
        if (Constants.EnableDebug)
            Debug.Log(comms.Node.Id + " -> " + nextHop + "\t : " + response.GetType() + ": " + response.ResponseCode + "." + "\t dst: " + response.DestinationID);

        SatelliteComms hop = SatManager._instance.satellites.Find(sat => sat.Node.Id == nextHop);

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
            Thread.Sleep(450 / Constants.TimeScale);

            if (response.DestinationID == comms.Node.Id)
            {
                OnResponseReceived?.Invoke(this, new ResponseEventArgs(response));
            }
            else
            {
                uint? nextHop = comms.Node.Router.NextHop(comms.Node.Id, response.DestinationID);
                Send(nextHop, response);

            }
        }).Start();
    }

    public Request FetchNextRequest()
    {
        if (RequestList.Count > 0)
        {
            Request request = RequestList[0];
            RequestList.RemoveAt(0);
            return request;
        }

        return null;
    }
}
