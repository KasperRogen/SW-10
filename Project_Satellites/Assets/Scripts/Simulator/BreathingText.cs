using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BreathingText : MonoBehaviour
{
    private TMP_Text TMP;
    private bool risingDir = true;

    public Color currentColor;
    // Start is called before the first frame update
    void Start()
    {
        TMP = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {

        currentColor = TMP.color;
        currentColor.a += risingDir ? 1 * Time.deltaTime : -1 * Time.deltaTime;
        TMP.color = currentColor;

        if (currentColor.a > 1)
            risingDir = false;
        if (currentColor.a < 0.1f)
            risingDir = true;
    }
}
