using UnityEngine;

// Shared morph slots, families and socket data.
public enum CreatureMorphSlot
{
    Body,
    Tail,
    Fins,
    Jaw,
    Sensors,
    Armour,
    DorsalFin,
    Spikes,
    Gills
}

public enum CreatureMorphFamily
{
    Basic,
    Streamlined,
    Armoured,
    Bulky,
    Agile,
    Predator,
    Grazer,
    Scavenger,
    SensorFocused,
    Defensive,
    Reduced
}

[System.Serializable]
public struct CreatureMorphSocketDefinition
{
    [Header("Socket Identity")]
    public string SocketName;
    public CreatureMorphSlot Slot;

    [Header("Local Transform")]
    public Vector3 LocalPosition;
    public Vector3 LocalRotationEuler;
    public Vector3 LocalScale;

    [Header("Mirroring")]
    public bool MirrorOnX;

    [Header("Body Shape Scaling")]
    public bool ScalePositionByBodyShape;
    public bool ScaleSocketByBodyShape;

    // Creates the object or data needed here
    public static CreatureMorphSocketDefinition Create(CreatureMorphSlot slot, Vector3 position, Vector3 rotation, Vector3 scale, bool mirrorOnX)
    {
        CreatureMorphSocketDefinition socket = new CreatureMorphSocketDefinition();
        socket.SocketName = slot.ToString();
        socket.Slot = slot;
        socket.LocalPosition = position;
        socket.LocalRotationEuler = rotation;
        socket.LocalScale = scale == Vector3.zero ? Vector3.one : scale;
        socket.MirrorOnX = mirrorOnX;
        socket.ScalePositionByBodyShape = true;
        socket.ScaleSocketByBodyShape = false;
        return socket;
    }
}
