using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ICommunicate
{
    void Send(uint? nextHop, Request request);
    void Receive(Request request);
    Task<Response> SendAsync(uint? nextHop, Request request, int timeout);
    void Send(uint? nextHop, Response response);
    void Receive(Response response);
    List<uint?> Discover();
    Request FetchNextRequest();
}
