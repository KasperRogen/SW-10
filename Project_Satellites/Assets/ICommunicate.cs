using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommunicate
{
    void Send(uint? nextHop, Request request);
    void Receive(Request request);
}
