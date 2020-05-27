using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{

    public static SimulationManager _instance;

    public bool SimMinutesToSeconds;

    private void Awake()
    {
#if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 150;
#endif
    }
    private void Start()
    {
        _instance = this;
    }

}
