using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SatArrenger : MonoBehaviour
{
    private Camera cam;
    TextMeshPro text;
    public Transform ImageTransform;
    public float ImageRotationOffset;
    public float TextDisplacementScale = 0.1f;
    

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponentInChildren<TextMeshPro>();
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dirVect = (transform.position - Vector3.zero).normalized;


        float xDot = Mathf.Abs(Vector3.Dot(text.transform.right, dirVect));
        float xAmount = text.GetRenderedValues(true).x * TextDisplacementScale * xDot;

        float zDot = Mathf.Abs(Vector3.Dot(text.transform.up, dirVect));
        float zAmount = text.GetRenderedValues(true).y * TextDisplacementScale * zDot;

        text.transform.position = transform.position + dirVect * (xAmount + zAmount);


        Vector3 currentRot = text.transform.rotation.eulerAngles;
        currentRot.y = cam.transform.parent.rotation.eulerAngles.y;
        text.transform.rotation = Quaternion.Euler(currentRot);


        
        Quaternion imageRotation = Quaternion.LookRotation(Vector3.zero - transform.position, Vector3.up);
        Vector3 imageRotationVector = imageRotation.eulerAngles;

        imageRotationVector.x = 90;
        imageRotationVector.z = 0;
        imageRotationVector.y += ImageRotationOffset;

        ImageTransform.rotation = Quaternion.Euler(imageRotationVector);
    }


    
}
