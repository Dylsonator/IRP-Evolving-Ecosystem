using System;
using UnityEngine;

[Serializable]
public class EvolutionGenome
{
    public const int BrainInputCount = 15;
    public const int BrainHiddenCount = 10;
    public const int BrainOutputCount = 3;

    [Header("Core Body Traits")]
    public float BaseSpeed = 4f;
    public float Acceleration = 8f;
    public float TurnRate = 180f;
    public float BodySize = 1f;
    public float EnergyCapacity = 100f;

    [Header("Evolving Body Parts")]
    public float TailSize = 1f;
    public float FinSize = 1f;
    public float JawSize = 1f;
    public float SensorSize = 1f;
    public float Armour = 0.2f;
    public float Muscle = 1f;
    public float DigestiveEfficiency = 1f;

    [Header("Diet Traits")]
    [Range(0f, 1f)] public float PlantDiet = 0.7f;
    [Range(0f, 1f)] public float MeatDiet = 0.2f;
    [Range(0f, 1f)] public float CarrionDiet = 0.2f;

    [Header("Behaviour Traits")]
    [Range(0f, 1f)] public float HungerDrive = 0.7f;
    [Range(0f, 1f)] public float GroupingChance = 0.25f;
    [Range(0f, 1f)] public float Aggression = 0.2f;
    [Range(0f, 1f)] public float RiskTolerance = 0.5f;
    [Range(0f, 1f)] public float SeparationDrive = 0.45f;

    [Header("Reproduction")]
    public float ReproductionEnergyThreshold = 95f;
    [Range(0f, 1f)] public float MutationRate = 0.08f;
    public float MutationStrength = 1f;

    [Header("Evolvable Behaviour Controller")]
    public SimpleNeuralNetwork Brain;

    public static EvolutionGenome CreateRandom()
    {
        EvolutionGenome genome = new EvolutionGenome();

        genome.BaseSpeed = UnityEngine.Random.Range(2.5f, 6f);
        genome.Acceleration = UnityEngine.Random.Range(5f, 14f);
        genome.TurnRate = UnityEngine.Random.Range(90f, 260f);
        genome.BodySize = UnityEngine.Random.Range(0.7f, 1.5f);
        genome.EnergyCapacity = UnityEngine.Random.Range(80f, 145f);

        genome.TailSize = UnityEngine.Random.Range(0.55f, 1.65f);
        genome.FinSize = UnityEngine.Random.Range(0.55f, 1.65f);
        genome.JawSize = UnityEngine.Random.Range(0.45f, 1.55f);
        genome.SensorSize = UnityEngine.Random.Range(0.55f, 1.7f);
        genome.Armour = UnityEngine.Random.Range(0f, 1.1f);
        genome.Muscle = UnityEngine.Random.Range(0.65f, 1.65f);
        genome.DigestiveEfficiency = UnityEngine.Random.Range(0.65f, 1.45f);

        genome.PlantDiet = UnityEngine.Random.Range(0.15f, 1f);
        genome.MeatDiet = UnityEngine.Random.Range(0f, 0.95f);
        genome.CarrionDiet = UnityEngine.Random.Range(0f, 0.95f);

        genome.HungerDrive = UnityEngine.Random.Range(0.35f, 1f);
        genome.GroupingChance = UnityEngine.Random.Range(0f, 0.85f);
        genome.Aggression = UnityEngine.Random.Range(0f, 0.85f);
        genome.RiskTolerance = UnityEngine.Random.Range(0.1f, 1f);
        genome.SeparationDrive = UnityEngine.Random.Range(0.1f, 1f);

        genome.ReproductionEnergyThreshold = UnityEngine.Random.Range(75f, 125f);
        genome.MutationRate = UnityEngine.Random.Range(0.04f, 0.14f);
        genome.MutationStrength = UnityEngine.Random.Range(0.65f, 1.4f);

        genome.Brain = SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);
        genome.ClampValues();
        return genome;
    }

    public EvolutionGenome CreateMutatedCopy(float environmentMutationMultiplier)
    {
        EvolutionGenome child = new EvolutionGenome();

        float finalMutationRate = Mathf.Clamp01(MutationRate * environmentMutationMultiplier);
        float finalMutationStrength = Mathf.Max(0.05f, MutationStrength);

        child.BaseSpeed = MutateFloat(BaseSpeed, 0.6f, finalMutationRate, finalMutationStrength);
        child.Acceleration = MutateFloat(Acceleration, 1.5f, finalMutationRate, finalMutationStrength);
        child.TurnRate = MutateFloat(TurnRate, 25f, finalMutationRate, finalMutationStrength);
        child.BodySize = MutateFloat(BodySize, 0.16f, finalMutationRate, finalMutationStrength);
        child.EnergyCapacity = MutateFloat(EnergyCapacity, 12f, finalMutationRate, finalMutationStrength);

        child.TailSize = MutateFloat(TailSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.FinSize = MutateFloat(FinSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.JawSize = MutateFloat(JawSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.SensorSize = MutateFloat(SensorSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.Armour = MutateFloat(Armour, 0.16f, finalMutationRate, finalMutationStrength);
        child.Muscle = MutateFloat(Muscle, 0.18f, finalMutationRate, finalMutationStrength);
        child.DigestiveEfficiency = MutateFloat(DigestiveEfficiency, 0.14f, finalMutationRate, finalMutationStrength);

        child.PlantDiet = MutateFloat(PlantDiet, 0.14f, finalMutationRate, finalMutationStrength);
        child.MeatDiet = MutateFloat(MeatDiet, 0.14f, finalMutationRate, finalMutationStrength);
        child.CarrionDiet = MutateFloat(CarrionDiet, 0.14f, finalMutationRate, finalMutationStrength);

        child.HungerDrive = MutateFloat(HungerDrive, 0.12f, finalMutationRate, finalMutationStrength);
        child.GroupingChance = MutateFloat(GroupingChance, 0.12f, finalMutationRate, finalMutationStrength);
        child.Aggression = MutateFloat(Aggression, 0.12f, finalMutationRate, finalMutationStrength);
        child.RiskTolerance = MutateFloat(RiskTolerance, 0.12f, finalMutationRate, finalMutationStrength);
        child.SeparationDrive = MutateFloat(SeparationDrive, 0.12f, finalMutationRate, finalMutationStrength);

        child.ReproductionEnergyThreshold = MutateFloat(ReproductionEnergyThreshold, 10f, finalMutationRate, finalMutationStrength);
        child.MutationRate = MutateFloat(MutationRate, 0.02f, finalMutationRate, finalMutationStrength);
        child.MutationStrength = MutateFloat(MutationStrength, 0.15f, finalMutationRate, finalMutationStrength);

        EnsureBrainIsValid();
        child.Brain = Brain.CreateMutatedCopy(finalMutationRate, finalMutationStrength);

        child.ClampValues();
        return child;
    }

    private float MutateFloat(float value, float amount, float mutationRate, float mutationStrength)
    {
        if (UnityEngine.Random.value > mutationRate)
        {
            return value;
        }

        return value + UnityEngine.Random.Range(-amount, amount) * mutationStrength;
    }

    public void ClampValues()
    {
        BaseSpeed = Mathf.Clamp(BaseSpeed, 0.75f, 12f);
        Acceleration = Mathf.Clamp(Acceleration, 1f, 25f);
        TurnRate = Mathf.Clamp(TurnRate, 35f, 420f);
        BodySize = Mathf.Clamp(BodySize, 0.35f, 2.75f);
        EnergyCapacity = Mathf.Clamp(EnergyCapacity, 40f, 240f);

        TailSize = Mathf.Clamp(TailSize, 0.25f, 2.75f);
        FinSize = Mathf.Clamp(FinSize, 0.25f, 2.75f);
        JawSize = Mathf.Clamp(JawSize, 0.2f, 2.75f);
        SensorSize = Mathf.Clamp(SensorSize, 0.25f, 2.75f);
        Armour = Mathf.Clamp(Armour, 0f, 2.5f);
        Muscle = Mathf.Clamp(Muscle, 0.35f, 2.75f);
        DigestiveEfficiency = Mathf.Clamp(DigestiveEfficiency, 0.25f, 2.5f);

        PlantDiet = Mathf.Clamp01(PlantDiet);
        MeatDiet = Mathf.Clamp01(MeatDiet);
        CarrionDiet = Mathf.Clamp01(CarrionDiet);

        HungerDrive = Mathf.Clamp01(HungerDrive);
        GroupingChance = Mathf.Clamp01(GroupingChance);
        Aggression = Mathf.Clamp01(Aggression);
        RiskTolerance = Mathf.Clamp01(RiskTolerance);
        SeparationDrive = Mathf.Clamp01(SeparationDrive);

        ReproductionEnergyThreshold = Mathf.Clamp(ReproductionEnergyThreshold, 45f, EnergyCapacity * 1.15f);
        MutationRate = Mathf.Clamp(MutationRate, 0.01f, 0.3f);
        MutationStrength = Mathf.Clamp(MutationStrength, 0.15f, 2.5f);

        EnsureBrainIsValid();
    }

    public void EnsureBrainIsValid()
    {
        if (Brain == null || Brain.InputCount != BrainInputCount || Brain.OutputCount != BrainOutputCount)
        {
            Brain = SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);
        }
    }

    public float GetVisionRange()
    {
        return Mathf.Clamp(7f + SensorSize * 9f + BodySize * 1.5f, 4f, 45f);
    }

    public float GetEffectiveSpeed()
    {
        float tailBonus = 0.65f + TailSize * 0.32f;
        float muscleBonus = 0.82f + Muscle * 0.14f;
        float armourPenalty = 1f / (1f + Armour * 0.18f);
        float sizePenalty = 1f / Mathf.Sqrt(Mathf.Max(0.35f, BodySize));
        return Mathf.Clamp(BaseSpeed * tailBonus * muscleBonus * armourPenalty * sizePenalty, 0.5f, 18f);
    }

    public float GetEffectiveAcceleration()
    {
        float finBonus = 0.7f + FinSize * 0.25f;
        float muscleBonus = 0.85f + Muscle * 0.16f;
        float armourPenalty = 1f / (1f + Armour * 0.14f);
        return Mathf.Clamp(Acceleration * finBonus * muscleBonus * armourPenalty, 0.5f, 35f);
    }

    public float GetEffectiveTurnRate()
    {
        float finBonus = 0.65f + FinSize * 0.45f;
        float bodyPenalty = 1f / Mathf.Sqrt(Mathf.Max(0.35f, BodySize));
        return Mathf.Clamp(TurnRate * finBonus * bodyPenalty, 30f, 540f);
    }

    public float GetMaxHealth()
    {
        return Mathf.Clamp(45f * BodySize + Armour * 25f + Muscle * 10f, 20f, 260f);
    }

    public float GetAttackDamage()
    {
        return Mathf.Clamp(8f + JawSize * 13f + Muscle * 5f + BodySize * 3f, 4f, 80f);
    }

    public float GetAttackRange()
    {
        return Mathf.Clamp((0.8f + JawSize * 0.7f) * BodySize, 0.6f, 5f);
    }

    public float GetEnergyDrainMultiplier()
    {
        float bodyCost = 0.7f + BodySize * 0.32f;
        float speedCost = 0.85f + BaseSpeed * 0.035f;
        float senseCost = 0.92f + SensorSize * 0.05f;
        float armourCost = 1f + Armour * 0.08f;
        float muscleCost = 1f + Muscle * 0.06f;
        float digestionHelp = 1f / Mathf.Max(0.35f, DigestiveEfficiency);
        return Mathf.Clamp(bodyCost * speedCost * senseCost * armourCost * muscleCost * digestionHelp, 0.35f, 4f);
    }

    public float GetDietPreference(EcosystemFoodType foodType)
    {
        switch (foodType)
        {
            case EcosystemFoodType.Plant:
                return PlantDiet;
            case EcosystemFoodType.FreshMeat:
                return MeatDiet;
            case EcosystemFoodType.RottenMeat:
                return CarrionDiet;
            default:
                return 0f;
        }
    }

    public float GetHunterScore()
    {
        return Mathf.Clamp01((MeatDiet * 0.42f) + (Aggression * 0.38f) + (JawSize / 2.75f * 0.2f));
    }

    public float GetScavengerScore()
    {
        return Mathf.Clamp01((CarrionDiet * 0.7f) + ((1f - Aggression) * 0.15f) + (DigestiveEfficiency / 2.5f * 0.15f));
    }

    public float GetGroupingScore()
    {
        return GroupingChance;
    }
}
