using UnityEngine;

// Reusable stat modifier asset, like agile, armoured, swift or hunter.
[CreateAssetMenu(menuName = "IRP Evolution/Creature Morph Modifier", fileName = "MorphModifier_New")]
public class CreatureMorphModifierData : ScriptableObject
{
    public string ModifierId = "modifier_id";
    public string DisplayName = "New Modifier";

    [Header("Shape")]
    public Vector3 ScaleMultiplier = Vector3.one;

    [Header("Stats")]
    public float SpeedModifier;
    public float TurnRateModifier;
    public float EnergyCapacityModifier;
    public float EnergyDrainMultiplier = 1f;
    public float BiteDamageModifier;
    public float DefenceModifier;
    public float DangerFactorModifier;
}
