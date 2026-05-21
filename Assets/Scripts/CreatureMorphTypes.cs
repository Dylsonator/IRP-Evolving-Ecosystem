using UnityEngine;

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
    public CreatureMorphSlot Slot;
    public Vector3 LocalPosition;
    public Vector3 LocalRotationEuler;
    public Vector3 LocalScale;
    public bool MirrorOnX;
}
