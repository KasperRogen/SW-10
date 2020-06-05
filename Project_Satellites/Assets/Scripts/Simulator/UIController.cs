using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public TMP_Text TimescaleLabel;
    public TMP_Text TimescaleSliderLabel;
    public TMP_Text TimescaleSyncText;
    public Slider TimescaleSlider;
    private bool timeScaleSynchronizerRunning = false;
    private int desiredTimeScale;

    private void Start()
    {
        TimescaleSlider.value = Constants.TimeScale;
        desiredTimeScale = Constants.TimeScale;
        TimescaleSliderLabel.text = Constants.TimeScale.ToString();
    }

    IEnumerator TimeScaleSynchronizer()
    {
        timeScaleSynchronizerRunning = true;
        TimescaleSyncText.enabled = true;
        bool resettingTimers = SatManager._instance.satellites.Any(sat => sat.Node.ResettingTimers);
        bool sleepCount = SatManager._instance.satellites.Any(sat => sat.Node.SleepCount > 0);
        while (resettingTimers || sleepCount || Input.GetMouseButton(0))
        {
            yield return new WaitForSeconds(1);
            resettingTimers = SatManager._instance.satellites.Any(sat => sat.Node.ResettingTimers);
            sleepCount = SatManager._instance.satellites.Any(sat => sat.Node.SleepCount > 0);
        }

        Constants.TimeScale = desiredTimeScale;

        SatManager._instance.satellites.ForEach(sat => StartCoroutine(sat.Node.ResetTimers()));

        timeScaleSynchronizerRunning = false;
        TimescaleSyncText.enabled = false;
    }

    private void Update()
    {
        TimescaleLabel.text = "Current Timescale: " + Constants.TimeScale.ToString();
    }

    public void ToggleAnimate(bool state)
    {
        CamRotation._instance.RotationEnabled = state;
    }

    public void ToggleAutoChecks(bool state)
    {
        SatManager._instance.satellites.ForEach(sat => sat.Node.AutoChecksAllowed = state);
    }

    public void ResetScene()
    {
        System.Diagnostics.Process.Start(Application.dataPath.Replace("_Data", ".exe")); //new program
        Application.Quit(); //kill current process
    }

    public void OnTimescaleChanged(Single value)
    {
        desiredTimeScale = (int) value;
        TimescaleSliderLabel.text = desiredTimeScale.ToString();

        if (timeScaleSynchronizerRunning == false)
        {
            StartCoroutine(TimeScaleSynchronizer());
        }
    }

}
