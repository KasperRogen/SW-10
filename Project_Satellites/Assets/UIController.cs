using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public TMP_Text TimescaleLabel;
    public Slider TimescaleSlider;

    private void Start()
    {
        TimescaleSlider.value = Constants.TimeScale;
    }

    private void Update()
    {
        TimescaleLabel.text = "Timescale: " + Constants.TimeScale.ToString();
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
        Constants.TimeScale = (int)value;
    }

}
