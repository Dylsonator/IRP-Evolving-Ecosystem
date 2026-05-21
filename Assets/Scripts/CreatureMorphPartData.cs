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
    public Vector3 LocalPosition;
    public Vector3 LocalRotationEuler;
    public Vector3 LocalScale = Vector3.one;
    public bool MirrorOnX;
    public Color DebugColour = Color.white;
    public bool OverrideTypeColour;

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
}
