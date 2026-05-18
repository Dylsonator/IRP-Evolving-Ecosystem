using System;
using UnityEngine;

[Serializable]
public class EvolutionGenome
{
    [Header("Body Traits")]
    public float Speed = 4f;
    public float Acceleration = 8f;
    public float TurnRate = 180f;
    public float BodySize = 1f;
    public float VisionRange = 15f;
    public float EnergyCapacity = 100f;

    [Header("Phenotype / Marine Body Shape Traits")]
    [Tooltip("Visual and sensing trait. Higher values can be used to show larger sensory organs.")]
    public float SensorSize = 1f;

    [Tooltip("Represents body power. Higher values can support strong movement but cost more energy.")]
    public float Muscle = 1f;

    [Tooltip("Represents protection or hard body structure. Higher values cost more energy.")]
    public float Armour = 0.2f;

    [Tooltip("Visual steering trait for fins. Useful for debug/type readability.")]
    public float FinSize = 1f;

    [Tooltip("Visual propulsion trait for tail. Useful for debug/type readability.")]
    public float TailSize = 1f;

    [Tooltip("Controls the mouth/eating area scale. Higher values give a larger forward mouth zone.")]
    public float JawSize = 1f;

    [Header("Diet Traits")]
    [Tooltip("How strongly this creature is adapted to eating plant/food resources.")]
    [Range(0f, 1f)] public float PlantDiet = 0.8f;

    [Tooltip("How strongly this creature is adapted to hunting/eating other creatures. Mostly used by debug/species grouping until predator logic is added.")]
    [Range(0f, 1f)] public float MeatDiet = 0.1f;

    [Tooltip("How strongly this creature is adapted to scavenging. Mostly used by debug/species grouping until carrion logic is added.")]
    [Range(0f, 1f)] public float CarrionDiet = 0.1f;

    [Header("Behaviour Traits")]
    [Range(0f, 1f)] public float HungerDrive = 0.7f;
    [Range(0f, 1f)] public float AttractionRange = 0.3f;
    [Range(0f, 1f)] public float ThreatRange = 0.3f;
    [Range(0f, 1f)] public float GroupingChance = 0.2f;
    [Range(0f, 1f)] public float Aggression = 0.2f;
    [Range(0f, 1f)] public float RiskTolerance = 0.5f;

    [Header("Reproduction")]
    public float ReproductionEnergyThreshold = 80f;
    [Range(0f, 1f)] public float MutationRate = 0.08f;
    public float MutationStrength = 1f;

    [Header("Evolvable Behaviour Controller")]
    public SimpleNeuralNetwork Brain;

    public static EvolutionGenome CreateRandom(int inputCount = 8, int hiddenCount = 8, int outputCount = 2)
    {
        EvolutionGenome genome = new EvolutionGenome();

        genome.Speed = UnityEngine.Random.Range(2.5f, 6f);
        genome.Acceleration = UnityEngine.Random.Range(5f, 14f);
        genome.TurnRate = UnityEngine.Random.Range(90f, 260f);
        genome.BodySize = UnityEngine.Random.Range(0.7f, 1.5f);
        genome.VisionRange = UnityEngine.Random.Range(8f, 25f);
        genome.EnergyCapacity = UnityEngine.Random.Range(75f, 140f);

        genome.SensorSize = UnityEngine.Random.Range(0.65f, 1.6f);
        genome.Muscle = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.Armour = UnityEngine.Random.Range(0f, 0.8f);
        genome.FinSize = UnityEngine.Random.Range(0.65f, 1.6f);
        genome.TailSize = UnityEngine.Random.Range(0.65f, 1.6f);
        genome.JawSize = UnityEngine.Random.Range(0.65f, 1.6f);

        genome.PlantDiet = UnityEngine.Random.Range(0.45f, 1f);
        genome.MeatDiet = UnityEngine.Random.Range(0f, 0.45f);
        genome.CarrionDiet = UnityEngine.Random.Range(0f, 0.35f);
        genome.NormaliseDietTraits();

        genome.HungerDrive = UnityEngine.Random.Range(0.35f, 1f);
        genome.AttractionRange = UnityEngine.Random.Range(0f, 1f);
        genome.ThreatRange = UnityEngine.Random.Range(0f, 1f);
        genome.GroupingChance = UnityEngine.Random.Range(0f, 0.8f);
        genome.Aggression = UnityEngine.Random.Range(0f, 0.8f);
        genome.RiskTolerance = UnityEngine.Random.Range(0.15f, 1f);

        genome.ReproductionEnergyThreshold = UnityEngine.Random.Range(65f, 110f);
        genome.MutationRate = UnityEngine.Random.Range(0.04f, 0.14f);
        genome.MutationStrength = UnityEngine.Random.Range(0.65f, 1.4f);

        genome.Brain = SimpleNeuralNetwork.CreateRandom(inputCount, hiddenCount, outputCount);

        genome.ClampValues();
        return genome;
    }

    public EvolutionGenome CreateMutatedCopy(float environmentMutationMultiplier)
    {
        EvolutionGenome child = new EvolutionGenome();

        float finalMutationRate = Mathf.Clamp01(MutationRate * environmentMutationMultiplier);
        float finalMutationStrength = Mathf.Max(0.05f, MutationStrength);

        child.Speed = MutateFloat(Speed, 0.6f, finalMutationRate, finalMutationStrength);
        child.Acceleration = MutateFloat(Acceleration, 1.5f, finalMutationRate, finalMutationStrength);
        child.TurnRate = MutateFloat(TurnRate, 25f, finalMutationRate, finalMutationStrength);
        child.BodySize = MutateFloat(BodySize, 0.15f, finalMutationRate, finalMutationStrength);
        child.VisionRange = MutateFloat(VisionRange, 3f, finalMutationRate, finalMutationStrength);
        child.EnergyCapacity = MutateFloat(EnergyCapacity, 12f, finalMutationRate, finalMutationStrength);

        child.SensorSize = MutateFloat(SensorSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.Muscle = MutateFloat(Muscle, 0.18f, finalMutationRate, finalMutationStrength);
        child.Armour = MutateFloat(Armour, 0.12f, finalMutationRate, finalMutationStrength);
        child.FinSize = MutateFloat(FinSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.TailSize = MutateFloat(TailSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.JawSize = MutateFloat(JawSize, 0.18f, finalMutationRate, finalMutationStrength);

        child.PlantDiet = MutateFloat(PlantDiet, 0.12f, finalMutationRate, finalMutationStrength);
        child.MeatDiet = MutateFloat(MeatDiet, 0.12f, finalMutationRate, finalMutationStrength);
        child.CarrionDiet = MutateFloat(CarrionDiet, 0.12f, finalMutationRate, finalMutationStrength);

        child.HungerDrive = MutateFloat(HungerDrive, 0.12f, finalMutationRate, finalMutationStrength);
        child.AttractionRange = MutateFloat(AttractionRange, 0.12f, finalMutationRate, finalMutationStrength);
        child.ThreatRange = MutateFloat(ThreatRange, 0.12f, finalMutationRate, finalMutationStrength);
        child.GroupingChance = MutateFloat(GroupingChance, 0.12f, finalMutationRate, finalMutationStrength);
        child.Aggression = MutateFloat(Aggression, 0.12f, finalMutationRate, finalMutationStrength);
        child.RiskTolerance = MutateFloat(RiskTolerance, 0.12f, finalMutationRate, finalMutationStrength);

        child.ReproductionEnergyThreshold = MutateFloat(ReproductionEnergyThreshold, 10f, finalMutationRate, finalMutationStrength);
        child.MutationRate = MutateFloat(MutationRate, 0.02f, finalMutationRate, finalMutationStrength);
        child.MutationStrength = MutateFloat(MutationStrength, 0.15f, finalMutationRate, finalMutationStrength);

        child.Brain = Brain != null
            ? Brain.CreateMutatedCopy(finalMutationRate, finalMutationStrength)
            : SimpleNeuralNetwork.CreateRandom(8, 8, 2);

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
        Speed = Mathf.Clamp(Speed, 0.75f, 12f);
        Acceleration = Mathf.Clamp(Acceleration, 1f, 25f);
        TurnRate = Mathf.Clamp(TurnRate, 35f, 420f);
        BodySize = Mathf.Clamp(BodySize, 0.4f, 2.5f);
        VisionRange = Mathf.Clamp(VisionRange, 3f, 45f);
        EnergyCapacity = Mathf.Clamp(EnergyCapacity, 40f, 220f);

        SensorSize = Mathf.Clamp(SensorSize, 0.35f, 2.5f);
        Muscle = Mathf.Clamp(Muscle, 0.25f, 2.5f);
        Armour = Mathf.Clamp(Armour, 0f, 2f);
        FinSize = Mathf.Clamp(FinSize, 0.35f, 2.5f);
        TailSize = Mathf.Clamp(TailSize, 0.35f, 2.5f);
        JawSize = Mathf.Clamp(JawSize, 0.35f, 2.5f);

        PlantDiet = Mathf.Clamp01(PlantDiet);
        MeatDiet = Mathf.Clamp01(MeatDiet);
        CarrionDiet = Mathf.Clamp01(CarrionDiet);
        NormaliseDietTraits();

        HungerDrive = Mathf.Clamp01(HungerDrive);
        AttractionRange = Mathf.Clamp01(AttractionRange);
        ThreatRange = Mathf.Clamp01(ThreatRange);
        GroupingChance = Mathf.Clamp01(GroupingChance);
        Aggression = Mathf.Clamp01(Aggression);
        RiskTolerance = Mathf.Clamp01(RiskTolerance);

        ReproductionEnergyThreshold = Mathf.Clamp(ReproductionEnergyThreshold, 30f, EnergyCapacity);
        MutationRate = Mathf.Clamp(MutationRate, 0.005f, 0.35f);
        MutationStrength = Mathf.Clamp(MutationStrength, 0.1f, 3f);

        if (Brain == null)
        {
            Brain = SimpleNeuralNetwork.CreateRandom(8, 8, 2);
        }
    }

    public void NormaliseDietTraits()
    {
        PlantDiet = Mathf.Clamp01(PlantDiet);
        MeatDiet = Mathf.Clamp01(MeatDiet);
        CarrionDiet = Mathf.Clamp01(CarrionDiet);

        float total = PlantDiet + MeatDiet + CarrionDiet;

        if (total <= 0.001f)
        {
            PlantDiet = 0.8f;
            MeatDiet = 0.1f;
            CarrionDiet = 0.1f;
            return;
        }

        PlantDiet /= total;
        MeatDiet /= total;
        CarrionDiet /= total;
    }

    public float GetEnergyDrainMultiplier()
    {
        float speedCost = Speed / 4f;
        float visionCost = VisionRange / 15f;
        float sizeCost = BodySize;
        float aggressionCost = 1f + Aggression * 0.35f;
        float phenotypeCost = 1f + (Armour * 0.18f) + (Muscle * 0.08f) + (JawSize * 0.04f);
        float meatCost = 1f + (MeatDiet * 0.08f);

        return Mathf.Max(0.1f, (speedCost + visionCost + sizeCost) / 3f * aggressionCost * phenotypeCost * meatCost);
    }
}
