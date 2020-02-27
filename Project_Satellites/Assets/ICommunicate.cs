using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommunicate
{
    void Send(Request request);
    void Receive(Request request);
}
