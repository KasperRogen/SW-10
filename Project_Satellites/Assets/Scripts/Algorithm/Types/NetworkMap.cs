using System.Collections;
using System.Collections.Generic;
using System.Numerics;

public class NetworkMap
{
    public List<NetworkMapEntry> Entries { get; set; }

    public NetworkMap()
    {
        Entries = new List<NetworkMapEntry>();
    }

    public NetworkMapEntry GetEntryByID(uint? ID)
    {
        return Entries.Find(entry => entry.ID == ID);
    }
}

public class NetworkMapEntry
{
    public uint? ID { get; set; }
    public List<uint?> Neighbours { get; set; }
    public Vector3 Position { get; set; }

    public NetworkMapEntry(uint? _id, Vector3 _position)
    {
        ID = _id;
        Position = _position;
        Neighbours = new List<uint?>();
    }

    public NetworkMapEntry(uint? _id, List<uint?> _neighbours, Vector3 _position)
    {
        ID = _id;
        Neighbours = _neighbours;
        Position = _position;
    }
}
