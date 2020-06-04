using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SatManager : MonoBehaviour
{
    public static SatManager _instance;

    public List<SatelliteComms> satellites = new List<SatelliteComms>();
    public List<MessageProps> SentMessages = new List<MessageProps>();
    public int SatIndex = 0;

    private void Start()
    {
        _instance = this;


    }

    public class MessageProps
    {
        public MessageProps(Vector3 startvect, Vector3 endvect, Color color, float Duration)
        {
            this.StartVect = startvect;
            this.EndVect = endvect;
            this.Color = color;
            this.Duration = Duration;
        }

        public Vector3 StartVect, EndVect;
        public Color Color;
        public float Duration;
    }

}
