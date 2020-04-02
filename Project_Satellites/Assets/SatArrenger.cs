using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SatArrenger : MonoBehaviour
{

    TextMeshPro text;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponentInChildren<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        text.transform.position = transform.position * 1.15f;
    }
}
