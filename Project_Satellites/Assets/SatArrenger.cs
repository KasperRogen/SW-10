using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SatArrenger : MonoBehaviour
{
    private Camera cam;
    TextMeshPro text;
    public float XOffsetScale, YOffsetScale = 0.1f;
    public Transform ImageTransform;
    public float ImageRotationOffset;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponentInChildren<TextMeshPro>();
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 baseOffset = transform.position;
        Vector3 verticalOffset = new Vector3(0, 0, transform.position.z * text.GetRenderedValues(true).y) * YOffsetScale;
        Vector3 horizontalOffset = new Vector3(transform.position.x * text.GetRenderedValues(true).x, 0, 0) * XOffsetScale;
        text.transform.position = baseOffset + verticalOffset + horizontalOffset;
        Vector3 currentRot = text.transform.rotation.eulerAngles;
        currentRot.y = cam.transform.parent.rotation.eulerAngles.y;
        text.transform.rotation = Quaternion.Euler(currentRot);

        if (Constants.EnableDebug)
        {
            Debug.DrawLine(Vector3.zero, baseOffset);
            Debug.DrawLine(baseOffset, baseOffset + verticalOffset);
            Debug.DrawLine(baseOffset + verticalOffset, horizontalOffset);
        }
        
        Quaternion imageRotation = Quaternion.LookRotation(Vector3.zero - transform.position, Vector3.up);
        Vector3 imageRotationVector = imageRotation.eulerAngles;

        imageRotationVector.x = 90;
        imageRotationVector.z = 0;
        imageRotationVector.y += ImageRotationOffset;

        ImageTransform.rotation = Quaternion.Euler(imageRotationVector);
    }
    
}
