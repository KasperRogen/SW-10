using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMapAlteration
{
    public enum Type
    {
        ADDITION, DELETION, REMOVAL
    }

    uint? ObserverNode;
    uint? AnalysedNode;
}

public class NetworkMapAddition : NetworkMapAlteration
{
    public NetworkMapEntry Entry;
    public NetworkMapAddition(NetworkMapEntry _entry)
    {
        Entry = _entry;
    }
}

public class NetworkMapRemoval : NetworkMapAlteration
{
    public NetworkMapEntry Entry;
    public NetworkMapRemoval(NetworkMapEntry _entry)
    {
        Entry = _entry;
    }
}
