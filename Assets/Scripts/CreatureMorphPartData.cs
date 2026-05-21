using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "IRP Evolution/Creature Morph Part", fileName = "MorphPart_New")]
public class CreatureMorphPartData : ScriptableObject
{
    [Header("Identity")]
    public string PartId = "part_id";
    public string DisplayName = "New Morph Part";
    public CreatureMorphSlot Slot = CreatureMorphSlot.Body;
    public CreatureMorphFamily Family = CreatureMorphFamily.Basic;

    [Header("Visual Prefab")]
    public GameObject PartPrefab;

    [Tooltip("For body parts, this is the body placement. For other parts, this is an offset from the chosen body socket.")]
    public Vector3 LocalPosition;
    public Vector3 LocalRotationEuler;
    public Vector3 LocalScale = Vector3.one;
    public bool MirrorOnX;
    public Color DebugColour = Color.white;
    public bool OverrideTypeColour;

    [Header("Body Socket Nodes")]
    [Tooltip("Only used when this part is a Body. These nodes tell other parts where to attach on this body type.")]
    public List<CreatureMorphSocketDefinition> SocketNodes = new List<CreatureMorphSocketDefinition>();

    [Header("Socket Use")]
    [Tooltip("Only used by non-body parts. If true, this part will try to attach to a socket on the current body type.")]
    public bool UseBodySocket = true;

    [Tooltip("Optional named socket. Leave blank to use the first socket matching this part's slot.")]
    public string PreferredSocketName;

    [Tooltip("If true, LocalPosition is added after the socket position. This is useful for small prefab correction offsets.")]
    public bool AddLocalOffsetToSocket = true;

    [Tooltip("If true, LocalRotationEuler is added after the socket rotation.")]
    public bool AddLocalRotationToSocket = true;

    [Tooltip("If true, LocalScale is multiplied by the socket scale.")]
    public bool MultiplyLocalScaleBySocketScale = false;

    [Tooltip("If true, the runtime shape modifier also scales this part. Keep true for evolved size variation.")]
    public bool ApplyGenomeShapeScale = false;

    [Header("Direct Stat Modifiers")]
    public float SpeedModifier;
    public float AccelerationModifier;
    public float TurnRateModifier;
    public float VerticalControlModifier;
    public float BodySizeModifier;
    public float VisionRangeModifier;
    public float EnergyCapacityModifier;
    public float BiteDamageModifier;
    public float MouthRadiusModifier;
    public float DefenceModifier;
    public float DangerFactorModifier;

    [Header("Multipliers")]
    public float EnergyDrainMultiplier = 1f;
    public float SpeedMultiplier = 1f;
    public float TurnRateMultiplier = 1f;
    public float BiteDamageMultiplier = 1f;
    public float VisionRangeMultiplier = 1f;

    [Header("Behaviour / Diet Bias")]
    public float PlantDietBias;
    public float MeatDietBias;
    public float CarrionDietBias;
    public float AggressionModifier;
    public float RiskToleranceModifier;
    public float GroupingModifier;
    public float ThreatModifier;

    [Header("Mutation")]
    [Range(0.01f, 10f)] public float MutationWeight = 1f;

    [ContextMenu("IRP/Fill Basic Body Socket Nodes")]
    public void FillBasicBodySocketNodes()
    {
        Slot = CreatureMorphSlot.Body;
        SocketNodes.Clear();
        SocketNodes.Add(CreatureMorphSocketDefinition.Create(CreatureMorphSlot.Tail, new Vector3(0f, 0f, -0.85f), Vector3.zero, Vector3.one, false));
        SocketNodes.Add(CreatureMorphSocketDefinition.Create(CreatureMorphSlot.Fins, new Vector3(-0.62f, 0f, -0.05f), new Vector3(0f, 0f, -16f), Vector3.one, true));
        SocketNodes.Add(CreatureMorphSocketDefinition.Create(CreatureMorphSlot.Jaw, new Vector3(0f, 0f, 0.78f), Vector3.zero, Vector3.one, false));
        SocketNodes.Add(CreatureMorphSocketDefinition.Create(CreatureMorphSlot.Sensors, new Vector3(-0.24f, 0.23f, 0.52f), Vector3.zero, Vector3.one, true));
        SocketNodes.Add(CreatureMorphSocketDefinition.Create(CreatureMorphSlot.Armour, new Vector3(0f, 0.02f, -0.05f), Vector3.zero, Vector3.one, false));
        SocketNodes.Add(CreatureMorphSocketDefinition.Create(CreatureMorphSlot.DorsalFin, new Vector3(0f, 0.42f, -0.12f), Vector3.zero, Vector3.one, false));
        SocketNodes.Add(CreatureMorphSocketDefinition.Create(CreatureMorphSlot.Spikes, new Vector3(0f, 0.52f, -0.05f), Vector3.zero, Vector3.one, false));
        SocketNodes.Add(CreatureMorphSocketDefinition.Create(CreatureMorphSlot.Gills, new Vector3(-0.48f, 0.05f, 0.30f), Vector3.zero, Vector3.one, true));

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("IRP/Clear Socket Nodes")]
    public void ClearSocketNodes()
    {
        SocketNodes.Clear();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

}
