using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SatArrenger : MonoBehaviour
{

    TextMeshPro text;
    public float XOffsetScale, YOffsetScale = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponentInChildren<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 baseOffset = transform.position;
        Vector3 verticalOffset = new Vector3(0, 0, transform.position.z * text.GetRenderedValues(true).y) * YOffsetScale;
        Vector3 horizontalOffset = new Vector3(transform.position.x * text.GetRenderedValues(true).x, 0, 0) * XOffsetScale;
        text.transform.position = baseOffset + verticalOffset + horizontalOffset;

        if (Constants.EnableDebug)
        {
            Debug.DrawLine(Vector3.zero, baseOffset);
            Debug.DrawLine(baseOffset, baseOffset + verticalOffset);
            Debug.DrawLine(baseOffset + verticalOffset, horizontalOffset);
        }
    }
}
