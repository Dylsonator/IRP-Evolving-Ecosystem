using System;
using UnityEngine;

[Serializable]
public class EvolutionGenome
{
    public const int BrainInputCount = 12;
    public const int BrainHiddenCount = 10;
    public const int BrainOutputCount = 3;

    [Header("Core Body Traits")]
    public float Speed = 4f;
    public float Acceleration = 8f;
    public float TurnRate = 180f;
    public float VerticalControl = 1f;
    public float BodySize = 1f;
    public float VisionRange = 15f;
    public float EnergyCapacity = 100f;

    [Header("Continuous Shape Modifiers")]
    public float BodyLength = 1f;
    public float BodyWidth = 1f;
    public float SensorSize = 1f;
    public float Muscle = 1f;
    public float Armour = 0.15f;
    public float FinSize = 1f;
    public float FinLength = 1f;
    public float FinWidth = 1f;
    public float TailSize = 1f;
    public float TailLength = 1f;
    public float TailWidth = 1f;
    public float JawSize = 1f;
    public float JawLength = 1f;
    public float JawWidth = 1f;
    public float DorsalFinSize = 0.75f;
    public float SpikeSize = 0.35f;
    public float SpikeLength = 0.35f;
    public float GillSize = 1f;

    [Header("Morph Part Genes")]
    public string BodyMorphId = "body_basic";
    public string TailMorphId = "tail_basic";
    public string FinMorphId = "fins_basic";
    public string JawMorphId = "jaw_filter";
    public string SensorMorphId = "sensors_basic";
    public string ArmourMorphId = "armour_none";
    public string DorsalFinMorphId = "dorsal_none";
    public string SpikeMorphId = "spikes_none";
    public string GillMorphId = "gills_basic";

    [Header("Diet Traits")]
    [Range(0f, 1f)] public float PlantDiet = 0.85f;
    [Range(0f, 1f)] public float MeatDiet = 0.05f;
    [Range(0f, 1f)] public float CarrionDiet = 0.10f;

    [Header("Behaviour Traits")]
    [Range(0f, 1f)] public float HungerDrive = 0.7f;
    [Range(0f, 1f)] public float AttractionRange = 0.3f;
    [Range(0f, 1f)] public float ThreatRange = 0.3f;
    [Range(0f, 1f)] public float GroupingChance = 0.2f;
    [Range(0f, 1f)] public float Aggression = 0.12f;
    [Range(0f, 1f)] public float RiskTolerance = 0.5f;
    [Range(0f, 5f)] public float DangerFactor = 0.15f;

    [Header("Reproduction / Mutation")]
    public float ReproductionEnergyThreshold = 80f;
    [Range(0f, 1f)] public float MutationRate = 0.08f;
    public float MutationStrength = 1f;
    [Range(0f, 1f)] public float MorphPartMutationRate = 0.035f;

    [Header("Evolvable Behaviour Controller")]
    public SimpleNeuralNetwork Brain;

    public static EvolutionGenome CreateBaseline()
    {
        EvolutionGenome genome = new EvolutionGenome();
        genome.Speed = 4.2f;
        genome.Acceleration = 9f;
        genome.TurnRate = 185f;
        genome.VerticalControl = 1.05f;
        genome.BodySize = 1f;
        genome.VisionRange = 18f;
        genome.EnergyCapacity = 125f;

        genome.BodyLength = 1f;
        genome.BodyWidth = 1f;
        genome.SensorSize = 1f;
        genome.Muscle = 1f;
        genome.Armour = 0.12f;
        genome.FinSize = 1f;
        genome.FinLength = 1f;
        genome.FinWidth = 1f;
        genome.TailSize = 1f;
        genome.TailLength = 1f;
        genome.TailWidth = 1f;
        genome.JawSize = 0.9f;
        genome.JawLength = 0.9f;
        genome.JawWidth = 0.9f;
        genome.DorsalFinSize = 0.65f;
        genome.SpikeSize = 0.2f;
        genome.SpikeLength = 0.2f;
        genome.GillSize = 1f;

        genome.BodyMorphId = "body_basic";
        genome.TailMorphId = "tail_basic";
        genome.FinMorphId = "fins_basic";
        genome.JawMorphId = "jaw_filter";
        genome.SensorMorphId = "sensors_basic";
        genome.ArmourMorphId = "armour_none";
        genome.DorsalFinMorphId = "dorsal_none";
        genome.SpikeMorphId = "spikes_none";
        genome.GillMorphId = "gills_basic";

        genome.PlantDiet = 0.85f;
        genome.MeatDiet = 0.05f;
        genome.CarrionDiet = 0.10f;
        genome.HungerDrive = 0.72f;
        genome.AttractionRange = 0.25f;
        genome.ThreatRange = 0.28f;
        genome.GroupingChance = 0.18f;
        genome.Aggression = 0.12f;
        genome.RiskTolerance = 0.5f;
        genome.DangerFactor = 0.12f;
        genome.ReproductionEnergyThreshold = 82f;
        genome.MutationRate = 0.08f;
        genome.MutationStrength = 1f;
        genome.MorphPartMutationRate = 0.035f;
        genome.Brain = SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);
        genome.ClampValues();
        return genome;
    }

    public static EvolutionGenome CreateRandom(int inputCount = BrainInputCount, int hiddenCount = BrainHiddenCount, int outputCount = BrainOutputCount)
    {
        EvolutionGenome genome = CreateBaseline();

        genome.Speed = UnityEngine.Random.Range(2.5f, 6.5f);
        genome.Acceleration = UnityEngine.Random.Range(5f, 15f);
        genome.TurnRate = UnityEngine.Random.Range(90f, 280f);
        genome.VerticalControl = UnityEngine.Random.Range(0.65f, 1.8f);
        genome.BodySize = UnityEngine.Random.Range(0.7f, 1.55f);
        genome.VisionRange = UnityEngine.Random.Range(8f, 28f);
        genome.EnergyCapacity = UnityEngine.Random.Range(80f, 160f);

        genome.BodyLength = UnityEngine.Random.Range(0.75f, 1.45f);
        genome.BodyWidth = UnityEngine.Random.Range(0.75f, 1.45f);
        genome.SensorSize = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.Muscle = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.Armour = UnityEngine.Random.Range(0f, 0.9f);
        genome.FinSize = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.FinLength = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.FinWidth = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.TailSize = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.TailLength = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.TailWidth = UnityEngine.Random.Range(0.65f, 1.7f);
        genome.JawSize = UnityEngine.Random.Range(0.55f, 1.7f);
        genome.JawLength = UnityEngine.Random.Range(0.55f, 1.7f);
        genome.JawWidth = UnityEngine.Random.Range(0.55f, 1.7f);
        genome.DorsalFinSize = UnityEngine.Random.Range(0.25f, 1.7f);
        genome.SpikeSize = UnityEngine.Random.Range(0f, 1.2f);
        genome.SpikeLength = UnityEngine.Random.Range(0f, 1.4f);
        genome.GillSize = UnityEngine.Random.Range(0.65f, 1.7f);

        genome.BodyMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.Body, genome.BodyMorphId);
        genome.TailMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.Tail, genome.TailMorphId);
        genome.FinMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.Fins, genome.FinMorphId);
        genome.JawMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.Jaw, genome.JawMorphId);
        genome.SensorMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.Sensors, genome.SensorMorphId);
        genome.ArmourMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.Armour, genome.ArmourMorphId);
        genome.DorsalFinMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.DorsalFin, genome.DorsalFinMorphId);
        genome.SpikeMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.Spikes, genome.SpikeMorphId);
        genome.GillMorphId = CreatureMorphLibrary.GetFallbackRandomPartId(CreatureMorphSlot.Gills, genome.GillMorphId);

        genome.PlantDiet = UnityEngine.Random.Range(0.45f, 1f);
        genome.MeatDiet = UnityEngine.Random.Range(0f, 0.65f);
        genome.CarrionDiet = UnityEngine.Random.Range(0f, 0.55f);
        genome.HungerDrive = UnityEngine.Random.Range(0.35f, 1f);
        genome.AttractionRange = UnityEngine.Random.Range(0f, 1f);
        genome.ThreatRange = UnityEngine.Random.Range(0f, 1f);
        genome.GroupingChance = UnityEngine.Random.Range(0f, 0.8f);
        genome.Aggression = UnityEngine.Random.Range(0f, 0.8f);
        genome.RiskTolerance = UnityEngine.Random.Range(0.15f, 1f);
        genome.DangerFactor = UnityEngine.Random.Range(0f, 0.75f);
        genome.ReproductionEnergyThreshold = UnityEngine.Random.Range(60f, 105f);
        genome.MutationRate = UnityEngine.Random.Range(0.04f, 0.14f);
        genome.MutationStrength = UnityEngine.Random.Range(0.65f, 1.4f);
        genome.MorphPartMutationRate = UnityEngine.Random.Range(0.02f, 0.06f);
        genome.Brain = SimpleNeuralNetwork.CreateRandom(inputCount, hiddenCount, outputCount);
        genome.ClampValues();
        return genome;
    }

    public EvolutionGenome Clone()
    {
        EvolutionGenome copy = (EvolutionGenome)MemberwiseClone();
        copy.Brain = Brain != null ? Brain.CreateMutatedCopy(0f, 0f) : SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);
        copy.ClampValues();
        return copy;
    }

    public EvolutionGenome CreateInitialVariant(float variationStrength)
    {
        if (variationStrength <= 0f)
        {
            return Clone();
        }

        EvolutionGenome child = Clone();
        float oldMutationRate = child.MutationRate;
        float oldMorphRate = child.MorphPartMutationRate;
        child.MutationRate = Mathf.Clamp01(variationStrength);
        child.MorphPartMutationRate = 0f;
        child = child.CreateMutatedCopy(1f);
        child.MutationRate = oldMutationRate;
        child.MorphPartMutationRate = oldMorphRate;
        child.ClampValues();
        return child;
    }

    public EvolutionGenome CreateMutatedCopy(float environmentMutationMultiplier)
    {
        EvolutionGenome child = new EvolutionGenome();
        float finalMutationRate = Mathf.Clamp01(MutationRate * environmentMutationMultiplier);
        float finalMutationStrength = Mathf.Max(0.05f, MutationStrength);
        float morphChance = Mathf.Clamp01(MorphPartMutationRate * environmentMutationMultiplier);

        child.Speed = MutateFloat(Speed, 0.65f, finalMutationRate, finalMutationStrength);
        child.Acceleration = MutateFloat(Acceleration, 1.6f, finalMutationRate, finalMutationStrength);
        child.TurnRate = MutateFloat(TurnRate, 28f, finalMutationRate, finalMutationStrength);
        child.VerticalControl = MutateFloat(VerticalControl, 0.18f, finalMutationRate, finalMutationStrength);
        child.BodySize = MutateFloat(BodySize, 0.16f, finalMutationRate, finalMutationStrength);
        child.VisionRange = MutateFloat(VisionRange, 3.2f, finalMutationRate, finalMutationStrength);
        child.EnergyCapacity = MutateFloat(EnergyCapacity, 13f, finalMutationRate, finalMutationStrength);

        child.BodyLength = MutateFloat(BodyLength, 0.16f, finalMutationRate, finalMutationStrength);
        child.BodyWidth = MutateFloat(BodyWidth, 0.16f, finalMutationRate, finalMutationStrength);
        child.SensorSize = MutateFloat(SensorSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.Muscle = MutateFloat(Muscle, 0.18f, finalMutationRate, finalMutationStrength);
        child.Armour = MutateFloat(Armour, 0.14f, finalMutationRate, finalMutationStrength);
        child.FinSize = MutateFloat(FinSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.FinLength = MutateFloat(FinLength, 0.18f, finalMutationRate, finalMutationStrength);
        child.FinWidth = MutateFloat(FinWidth, 0.18f, finalMutationRate, finalMutationStrength);
        child.TailSize = MutateFloat(TailSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.TailLength = MutateFloat(TailLength, 0.18f, finalMutationRate, finalMutationStrength);
        child.TailWidth = MutateFloat(TailWidth, 0.18f, finalMutationRate, finalMutationStrength);
        child.JawSize = MutateFloat(JawSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.JawLength = MutateFloat(JawLength, 0.18f, finalMutationRate, finalMutationStrength);
        child.JawWidth = MutateFloat(JawWidth, 0.18f, finalMutationRate, finalMutationStrength);
        child.DorsalFinSize = MutateFloat(DorsalFinSize, 0.18f, finalMutationRate, finalMutationStrength);
        child.SpikeSize = MutateFloat(SpikeSize, 0.2f, finalMutationRate, finalMutationStrength);
        child.SpikeLength = MutateFloat(SpikeLength, 0.22f, finalMutationRate, finalMutationStrength);
        child.GillSize = MutateFloat(GillSize, 0.18f, finalMutationRate, finalMutationStrength);

        child.BodyMorphId = MutateMorphPartId(BodyMorphId, CreatureMorphSlot.Body, morphChance);
        child.TailMorphId = MutateMorphPartId(TailMorphId, CreatureMorphSlot.Tail, morphChance);
        child.FinMorphId = MutateMorphPartId(FinMorphId, CreatureMorphSlot.Fins, morphChance);
        child.JawMorphId = MutateMorphPartId(JawMorphId, CreatureMorphSlot.Jaw, morphChance);
        child.SensorMorphId = MutateMorphPartId(SensorMorphId, CreatureMorphSlot.Sensors, morphChance);
        child.ArmourMorphId = MutateMorphPartId(ArmourMorphId, CreatureMorphSlot.Armour, morphChance);
        child.DorsalFinMorphId = MutateMorphPartId(DorsalFinMorphId, CreatureMorphSlot.DorsalFin, morphChance);
        child.SpikeMorphId = MutateMorphPartId(SpikeMorphId, CreatureMorphSlot.Spikes, morphChance);
        child.GillMorphId = MutateMorphPartId(GillMorphId, CreatureMorphSlot.Gills, morphChance);

        child.PlantDiet = MutateFloat(PlantDiet, 0.18f, finalMutationRate, finalMutationStrength);
        child.MeatDiet = MutateFloat(MeatDiet, 0.18f, finalMutationRate, finalMutationStrength);
        child.CarrionDiet = MutateFloat(CarrionDiet, 0.18f, finalMutationRate, finalMutationStrength);
        child.HungerDrive = MutateFloat(HungerDrive, 0.12f, finalMutationRate, finalMutationStrength);
        child.AttractionRange = MutateFloat(AttractionRange, 0.12f, finalMutationRate, finalMutationStrength);
        child.ThreatRange = MutateFloat(ThreatRange, 0.12f, finalMutationRate, finalMutationStrength);
        child.GroupingChance = MutateFloat(GroupingChance, 0.12f, finalMutationRate, finalMutationStrength);
        child.Aggression = MutateFloat(Aggression, 0.12f, finalMutationRate, finalMutationStrength);
        child.RiskTolerance = MutateFloat(RiskTolerance, 0.12f, finalMutationRate, finalMutationStrength);
        child.DangerFactor = MutateFloat(DangerFactor, 0.16f, finalMutationRate, finalMutationStrength);
        child.ReproductionEnergyThreshold = MutateFloat(ReproductionEnergyThreshold, 10f, finalMutationRate, finalMutationStrength);
        child.MutationRate = MutateFloat(MutationRate, 0.02f, finalMutationRate, finalMutationStrength);
        child.MutationStrength = MutateFloat(MutationStrength, 0.15f, finalMutationRate, finalMutationStrength);
        child.MorphPartMutationRate = MutateFloat(MorphPartMutationRate, 0.015f, finalMutationRate, finalMutationStrength);

        child.Brain = Brain != null
            ? Brain.CreateMutatedCopy(finalMutationRate, finalMutationStrength)
            : SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);

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

    private string MutateMorphPartId(string currentId, CreatureMorphSlot slot, float chance)
    {
        if (UnityEngine.Random.value > chance)
        {
            return currentId;
        }

        return CreatureMorphLibrary.GetRandomPartIdFromActive(slot, currentId);
    }

    public void ClampValues()
    {
        Speed = Mathf.Clamp(Speed, 0.75f, 14f);
        Acceleration = Mathf.Clamp(Acceleration, 1f, 30f);
        TurnRate = Mathf.Clamp(TurnRate, 35f, 520f);
        VerticalControl = Mathf.Clamp(VerticalControl, 0.2f, 3.5f);
        BodySize = Mathf.Clamp(BodySize, 0.4f, 2.8f);
        VisionRange = Mathf.Clamp(VisionRange, 3f, 55f);
        EnergyCapacity = Mathf.Clamp(EnergyCapacity, 40f, 260f);

        BodyLength = Mathf.Clamp(BodyLength, 0.45f, 2.5f);
        BodyWidth = Mathf.Clamp(BodyWidth, 0.45f, 2.5f);
        SensorSize = Mathf.Clamp(SensorSize, 0.35f, 2.5f);
        Muscle = Mathf.Clamp(Muscle, 0.25f, 2.5f);
        Armour = Mathf.Clamp(Armour, 0f, 2f);
        FinSize = Mathf.Clamp(FinSize, 0.35f, 2.5f);
        FinLength = Mathf.Clamp(FinLength, 0.35f, 2.5f);
        FinWidth = Mathf.Clamp(FinWidth, 0.35f, 2.5f);
        TailSize = Mathf.Clamp(TailSize, 0.35f, 2.5f);
        TailLength = Mathf.Clamp(TailLength, 0.35f, 2.5f);
        TailWidth = Mathf.Clamp(TailWidth, 0.35f, 2.5f);
        JawSize = Mathf.Clamp(JawSize, 0.35f, 2.5f);
        JawLength = Mathf.Clamp(JawLength, 0.35f, 2.5f);
        JawWidth = Mathf.Clamp(JawWidth, 0.35f, 2.5f);
        DorsalFinSize = Mathf.Clamp(DorsalFinSize, 0f, 2.5f);
        SpikeSize = Mathf.Clamp(SpikeSize, 0f, 2.5f);
        SpikeLength = Mathf.Clamp(SpikeLength, 0f, 2.5f);
        GillSize = Mathf.Clamp(GillSize, 0.35f, 2.5f);

        EnsureMorphIds();

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
        DangerFactor = Mathf.Clamp(DangerFactor, 0f, 5f);
        ReproductionEnergyThreshold = Mathf.Clamp(ReproductionEnergyThreshold, 25f, EnergyCapacity * 0.95f);
        MutationRate = Mathf.Clamp(MutationRate, 0.005f, 0.35f);
        MutationStrength = Mathf.Clamp(MutationStrength, 0.1f, 3f);
        MorphPartMutationRate = Mathf.Clamp(MorphPartMutationRate, 0f, 0.25f);

        if (Brain == null || Brain.InputCount != BrainInputCount || Brain.OutputCount != BrainOutputCount)
        {
            Brain = SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);
        }
    }

    private void EnsureMorphIds()
    {
        if (string.IsNullOrEmpty(BodyMorphId)) BodyMorphId = "body_basic";
        if (string.IsNullOrEmpty(TailMorphId)) TailMorphId = "tail_basic";
        if (string.IsNullOrEmpty(FinMorphId)) FinMorphId = "fins_basic";
        if (string.IsNullOrEmpty(JawMorphId)) JawMorphId = "jaw_filter";
        if (string.IsNullOrEmpty(SensorMorphId)) SensorMorphId = "sensors_basic";
        if (string.IsNullOrEmpty(ArmourMorphId)) ArmourMorphId = "armour_none";
        if (string.IsNullOrEmpty(DorsalFinMorphId)) DorsalFinMorphId = "dorsal_none";
        if (string.IsNullOrEmpty(SpikeMorphId)) SpikeMorphId = "spikes_none";
        if (string.IsNullOrEmpty(GillMorphId)) GillMorphId = "gills_basic";
    }

    public void NormaliseDietTraits()
    {
        PlantDiet = Mathf.Clamp01(PlantDiet);
        MeatDiet = Mathf.Clamp01(MeatDiet);
        CarrionDiet = Mathf.Clamp01(CarrionDiet);

        float total = PlantDiet + MeatDiet + CarrionDiet;

        if (total <= 0.001f)
        {
            PlantDiet = 0.85f;
            MeatDiet = 0.05f;
            CarrionDiet = 0.10f;
            return;
        }

        PlantDiet /= total;
        MeatDiet /= total;
        CarrionDiet /= total;
    }

    public float GetEnergyDrainMultiplier()
    {
        float speedCost = Mathf.Lerp(0.75f, 1.25f, Mathf.InverseLerp(0.75f, 14f, Speed));
        float visionCost = Mathf.Lerp(0.85f, 1.18f, Mathf.InverseLerp(3f, 55f, VisionRange));
        float sizeCost = Mathf.Lerp(0.75f, 1.35f, Mathf.InverseLerp(0.4f, 2.8f, BodySize));
        float aggressionCost = 1f + Aggression * 0.12f;
        float morphologyCost = 1f + (Armour * 0.08f) + (Muscle * 0.04f) + (JawSize * 0.025f) + (SpikeSize * 0.045f);
        float meatCost = 1f + (MeatDiet * 0.04f);
        float gillSaving = Mathf.Clamp(1f - (GillSize - 1f) * 0.04f, 0.85f, 1.08f);

        return Mathf.Max(0.1f, (speedCost + visionCost + sizeCost) / 3f * aggressionCost * morphologyCost * meatCost * gillSaving);
    }
}
