using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{

    public static SimulationManager _instance;

    public bool SimMinutesToSeconds;


    private void Start()
    {
        _instance = this;
    }

}
