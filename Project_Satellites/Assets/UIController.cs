using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public TMP_Text TimescaleLabel;
    public TMP_Text TimescaleSliderLabel;
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

        while (SatManager._instance.satellites.Any(sat => sat.Node.SleepCount > 0))
        {
            yield return new WaitForEndOfFrame();
        }

        Constants.TimeScale = desiredTimeScale;

        timeScaleSynchronizerRunning = false;
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
