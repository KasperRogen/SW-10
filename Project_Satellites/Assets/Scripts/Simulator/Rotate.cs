using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamRotation : MonoBehaviour
{

    public static CamRotation _instance;
    public bool RotationEnabled = true;

    // Start is called before the first frame update
    void Start()
    {
        _instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if(RotationEnabled)
        transform.Rotate(-Vector3.up, 0.2f * Time.deltaTime * Constants.TimeScale);
    }
}
