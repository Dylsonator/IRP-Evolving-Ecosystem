using System;
using UnityEngine;

// The genes for body stats, morphs, diet, behaviour and the neural brain.
[Serializable]
public class EvolutionGenome
{
    public const int BrainInputCount = 24;
    public const int BrainHiddenCount = 14;
    public const int BrainOutputCount = 10;
    public const int BrainMaxHiddenCount = 28;

    // Physical traits that can evolve and trade off against each other.
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
    public string BodyMorphId = "basic";
    public string TailMorphId = "basic";
    public string FinMorphId = "basic";
    public string JawMorphId = "basic";
    public string SensorMorphId = "basic";
    public string ArmourMorphId = "none";
    public string DorsalFinMorphId = "none";
    public string SpikeMorphId = "none";
    public string GillMorphId = "basic";

    // Diet values decide what food this fish is actually good at using.
    [Header("Diet Traits")]
    [Range(0f, 1f)] public float PlantDiet = 0.85f;
    [Range(0f, 1f)] public float MeatDiet = 0.05f;
    [Range(0f, 1f)] public float CarrionDiet = 0.10f;

    // Behaviour traits bias decisions without fully hardcoding them.
    [Header("Behaviour Traits")]
    [Range(0f, 1f)] public float HungerDrive = 0.7f;
    [Range(0f, 1f)] public float AttractionRange = 0.3f;
    [Range(0f, 1f)] public float ThreatRange = 0.3f;
    [Range(0f, 1f)] public float GroupingChance = 0.2f;
    [Range(0f, 1f)] public float Aggression = 0.12f;
    [Range(0f, 1f)] public float RiskTolerance = 0.5f;
    [Range(0f, 5f)] public float DangerFactor = 0.15f;



    [Header("Schooling / Habitat Traits")]
    [Range(0f, 1f)] public float PreferredDepth01 = 0.5f;
    [Range(0f, 1f)] public float DepthFlexibility = 0.35f;
    [Range(0f, 1f)] public float SchoolTightness = 0.45f;
    [Range(0f, 1f)] public float Leadership = 0.35f;
    [Range(0f, 1f)] public float FoodSharing = 0.45f;
    [Range(0f, 1f)] public float Territoriality = 0.08f;
    [Range(0f, 1f)] public float ActivityCycle = 0.65f;

    [Header("Digestion / Memory / Autonomous Group Traits")]
    [Range(0f, 1f)] public float HungerThreshold = 0.48f;
    [Range(0.25f, 2.5f)] public float StomachSize = 1f;
    [Range(0.25f, 2.5f)] public float Metabolism = 1f;
    [Range(0f, 1f)] public float Bravery = 0.45f;
    [Range(0f, 1f)] public float Selfishness = 0.18f;
    [Range(0f, 1f)] public float ExplorationDrive = 0.45f;
    [Range(0f, 1f)] public float FoodMemoryStrength = 0.55f;

    [Header("Life Cycle / Sensing Traits")]
    [Range(0f, 1f)] public float SexGene = 0.5f;
    [Range(0f, 1f)] public float NestingDrive = 0.45f;
    [Range(0f, 1f)] public float EggProtection = 0.35f;
    [Range(0f, 1f)] public float MateDrive = 0.45f;
    [Range(0f, 1f)] public float Stealth = 0.25f;
    [Range(0f, 1f)] public float HearingSensitivity = 0.45f;

    public bool PlantDietLocked;
    public bool MeatDietLocked;
    public bool CarrionDietLocked;

    [Header("Reproduction / Mutation")]
    public float ReproductionEnergyThreshold = 80f;
    [Range(0f, 1f)] public float MutationRate = 0.08f;
    public float MutationStrength = 1f;
    [Range(0f, 1f)] public float MorphPartMutationRate = 0.035f;

    // Neural weights mutate so decision habits can change over generations.
    [Header("Evolvable Behaviour Controller")]
    [Tooltip("Chance for NEAT-lite structural growth. This can add hidden nodes over generations while keeping old weights.")]
    [Range(0f, 0.2f)] public float BrainStructuralMutationRate = 0.025f;
    [Tooltip("How strongly short-term neural memory carries over between decisions. Higher values make agents less twitchy and more context aware.")]
    [Range(0f, 0.98f)] public float BrainMemoryDecay = 0.82f;
    public SimpleNeuralNetwork Brain;

    // Creates the baseline object or data needed here
    public static EvolutionGenome CreateBaseline()
    {
        EvolutionGenome genome = new EvolutionGenome();
        genome.Speed = 4.2f;
        genome.Acceleration = 9f;
        genome.TurnRate = 185f;
        genome.VerticalControl = 1.05f;
        genome.BodySize = 1f;
        genome.VisionRange = 18f;
        genome.EnergyCapacity = 150f;

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

        genome.BodyMorphId = "basic";
        genome.TailMorphId = "basic";
        genome.FinMorphId = "basic";
        genome.JawMorphId = "basic";
        genome.SensorMorphId = "basic";
        genome.ArmourMorphId = "none";
        genome.DorsalFinMorphId = "none";
        genome.SpikeMorphId = "none";
        genome.GillMorphId = "basic";

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
        genome.PreferredDepth01 = 0.5f;
        genome.DepthFlexibility = 0.42f;
        genome.SchoolTightness = 0.45f;
        genome.Leadership = 0.35f;
        genome.FoodSharing = 0.55f;
        genome.Territoriality = 0.08f;
        genome.ActivityCycle = 0.65f;
        genome.HungerThreshold = 0.42f;
        genome.StomachSize = 1f;
        genome.Metabolism = 1f;
        genome.Bravery = 0.45f;
        genome.Selfishness = 0.18f;
        genome.ExplorationDrive = 0.45f;
        genome.FoodMemoryStrength = 0.55f;
        genome.SexGene = UnityEngine.Random.value;
        genome.NestingDrive = 0.45f;
        genome.EggProtection = 0.35f;
        genome.MateDrive = 0.58f;
        genome.Stealth = 0.25f;
        genome.HearingSensitivity = 0.45f;
        genome.PlantDietLocked = false;
        genome.MeatDietLocked = false;
        genome.CarrionDietLocked = false;
        genome.ReproductionEnergyThreshold = 62f;
        genome.MutationRate = 0.08f;
        genome.MutationStrength = 1f;
        genome.MorphPartMutationRate = 0.035f;
        genome.BrainStructuralMutationRate = 0.025f;
        genome.BrainMemoryDecay = 0.82f;
        genome.Brain = SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);
        genome.ClampValues();
        return genome;
    }

    // Creates the random object or data needed here
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

        genome.BodyMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.Body, genome.BodyMorphId);
        genome.TailMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.Tail, genome.TailMorphId);
        genome.FinMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.Fins, genome.FinMorphId);
        genome.JawMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.Jaw, genome.JawMorphId);
        genome.SensorMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.Sensors, genome.SensorMorphId);
        genome.ArmourMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.Armour, genome.ArmourMorphId);
        genome.DorsalFinMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.DorsalFin, genome.DorsalFinMorphId);
        genome.SpikeMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.Spikes, genome.SpikeMorphId);
        genome.GillMorphId = CreatureMorphLibrary.GetRandomPartIdFromActive(CreatureMorphSlot.Gills, genome.GillMorphId);

        genome.PlantDiet = UnityEngine.Random.Range(0.10f, 1f);
        genome.MeatDiet = UnityEngine.Random.Range(0f, 0.85f);
        genome.CarrionDiet = UnityEngine.Random.Range(0f, 0.70f);
        genome.HungerDrive = UnityEngine.Random.Range(0.35f, 1f);
        genome.AttractionRange = UnityEngine.Random.Range(0f, 1f);
        genome.ThreatRange = UnityEngine.Random.Range(0f, 1f);
        genome.GroupingChance = UnityEngine.Random.Range(0f, 0.8f);
        genome.Aggression = UnityEngine.Random.Range(0f, 0.95f);
        genome.RiskTolerance = UnityEngine.Random.Range(0.15f, 1f);
        genome.DangerFactor = UnityEngine.Random.Range(0f, 0.75f);
        genome.PreferredDepth01 = UnityEngine.Random.Range(0.15f, 0.85f);
        genome.DepthFlexibility = UnityEngine.Random.Range(0.15f, 0.85f);
        genome.SchoolTightness = UnityEngine.Random.Range(0.15f, 0.95f);
        genome.Leadership = UnityEngine.Random.Range(0.05f, 0.95f);
        genome.FoodSharing = UnityEngine.Random.Range(0.05f, 0.95f);
        genome.Territoriality = UnityEngine.Random.Range(0f, 0.75f);
        genome.ActivityCycle = UnityEngine.Random.Range(0.15f, 1f);
        genome.HungerThreshold = UnityEngine.Random.Range(0.28f, 0.62f);
        genome.StomachSize = UnityEngine.Random.Range(0.65f, 1.45f);
        genome.Metabolism = UnityEngine.Random.Range(0.65f, 1.45f);
        genome.Bravery = UnityEngine.Random.Range(0.1f, 0.9f);
        genome.Selfishness = UnityEngine.Random.Range(0f, 0.45f);
        genome.ExplorationDrive = UnityEngine.Random.Range(0.15f, 0.85f);
        genome.FoodMemoryStrength = UnityEngine.Random.Range(0.15f, 0.85f);
        genome.SexGene = UnityEngine.Random.value;
        genome.NestingDrive = UnityEngine.Random.Range(0.15f, 0.85f);
        genome.EggProtection = UnityEngine.Random.Range(0.05f, 0.85f);
        genome.MateDrive = UnityEngine.Random.Range(0.32f, 0.95f);
        genome.Stealth = UnityEngine.Random.Range(0.05f, 0.85f);
        genome.HearingSensitivity = UnityEngine.Random.Range(0.15f, 0.9f);
        genome.ReproductionEnergyThreshold = UnityEngine.Random.Range(45f, 90f);
        genome.MutationRate = UnityEngine.Random.Range(0.04f, 0.14f);
        genome.MutationStrength = UnityEngine.Random.Range(0.65f, 1.4f);
        genome.MorphPartMutationRate = UnityEngine.Random.Range(0.02f, 0.06f);
        genome.BrainStructuralMutationRate = UnityEngine.Random.Range(0.01f, 0.055f);
        genome.BrainMemoryDecay = UnityEngine.Random.Range(0.68f, 0.94f);
        genome.Brain = SimpleNeuralNetwork.CreateRandom(inputCount, hiddenCount, outputCount);
        genome.ClampValues();
        return genome;
    }

    // Handles clone
    public EvolutionGenome Clone()
    {
        EvolutionGenome copy = (EvolutionGenome)MemberwiseClone();
        copy.Brain = Brain != null ? Brain.CreateMutatedCopy(0f, 0f) : SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);
        copy.ClampValues();
        return copy;
    }

    // Creates the initial variant object or data needed here
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

    // Creates the mutated copy object or data needed here
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
        child.PreferredDepth01 = MutateFloat(PreferredDepth01, 0.08f, finalMutationRate, finalMutationStrength);
        child.DepthFlexibility = MutateFloat(DepthFlexibility, 0.08f, finalMutationRate, finalMutationStrength);
        child.SchoolTightness = MutateFloat(SchoolTightness, 0.10f, finalMutationRate, finalMutationStrength);
        child.Leadership = MutateFloat(Leadership, 0.10f, finalMutationRate, finalMutationStrength);
        child.FoodSharing = MutateFloat(FoodSharing, 0.10f, finalMutationRate, finalMutationStrength);
        child.Territoriality = MutateFloat(Territoriality, 0.10f, finalMutationRate, finalMutationStrength);
        child.ActivityCycle = MutateFloat(ActivityCycle, 0.10f, finalMutationRate, finalMutationStrength);
        child.HungerThreshold = MutateFloat(HungerThreshold, 0.08f, finalMutationRate, finalMutationStrength);
        child.StomachSize = MutateFloat(StomachSize, 0.16f, finalMutationRate, finalMutationStrength);
        child.Metabolism = MutateFloat(Metabolism, 0.14f, finalMutationRate, finalMutationStrength);
        child.Bravery = MutateFloat(Bravery, 0.10f, finalMutationRate, finalMutationStrength);
        child.Selfishness = MutateFloat(Selfishness, 0.10f, finalMutationRate, finalMutationStrength);
        child.ExplorationDrive = MutateFloat(ExplorationDrive, 0.10f, finalMutationRate, finalMutationStrength);
        child.FoodMemoryStrength = MutateFloat(FoodMemoryStrength, 0.10f, finalMutationRate, finalMutationStrength);
        child.SexGene = SexGene;
        if (UnityEngine.Random.value < finalMutationRate * 0.25f)
        {
            child.SexGene = UnityEngine.Random.value;
        }
        child.NestingDrive = MutateFloat(NestingDrive, 0.10f, finalMutationRate, finalMutationStrength);
        child.EggProtection = MutateFloat(EggProtection, 0.12f, finalMutationRate, finalMutationStrength);
        child.MateDrive = MutateFloat(MateDrive, 0.10f, finalMutationRate, finalMutationStrength);
        child.Stealth = MutateFloat(Stealth, 0.10f, finalMutationRate, finalMutationStrength);
        child.HearingSensitivity = MutateFloat(HearingSensitivity, 0.10f, finalMutationRate, finalMutationStrength);
        child.PlantDietLocked = PlantDietLocked;
        child.MeatDietLocked = MeatDietLocked;
        child.CarrionDietLocked = CarrionDietLocked;
        child.ReproductionEnergyThreshold = MutateFloat(ReproductionEnergyThreshold, 10f, finalMutationRate, finalMutationStrength);
        child.MutationRate = MutateFloat(MutationRate, 0.02f, finalMutationRate, finalMutationStrength);
        child.MutationStrength = MutateFloat(MutationStrength, 0.15f, finalMutationRate, finalMutationStrength);
        child.MorphPartMutationRate = MutateFloat(MorphPartMutationRate, 0.015f, finalMutationRate, finalMutationStrength);
        child.BrainStructuralMutationRate = MutateFloat(BrainStructuralMutationRate, 0.012f, finalMutationRate, finalMutationStrength);
        child.BrainMemoryDecay = MutateFloat(BrainMemoryDecay, 0.045f, finalMutationRate, finalMutationStrength);

        child.Brain = Brain != null
            ? Brain.CreateMutatedCopy(finalMutationRate, finalMutationStrength, BrainStructuralMutationRate * environmentMutationMultiplier, BrainMaxHiddenCount)
            : SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);

        child.ClampValues();
        return child;
    }

    // Handles mutate float
    private float MutateFloat(float value, float amount, float mutationRate, float mutationStrength)
    {
        if (UnityEngine.Random.value > mutationRate)
        {
            return value;
        }

        return value + UnityEngine.Random.Range(-amount, amount) * mutationStrength;
    }

    // Handles mutate morph part id
    private string MutateMorphPartId(string currentId, CreatureMorphSlot slot, float chance)
    {
        if (UnityEngine.Random.value > chance)
        {
            return currentId;
        }

        return CreatureMorphLibrary.GetRandomPartIdFromActive(slot, currentId);
    }

    // Handles clamp values
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
        PreferredDepth01 = Mathf.Clamp01(PreferredDepth01);
        DepthFlexibility = Mathf.Clamp01(DepthFlexibility);
        SchoolTightness = Mathf.Clamp01(SchoolTightness);
        Leadership = Mathf.Clamp01(Leadership);
        FoodSharing = Mathf.Clamp01(FoodSharing);
        Territoriality = Mathf.Clamp01(Territoriality);
        ActivityCycle = Mathf.Clamp01(ActivityCycle);
        HungerThreshold = Mathf.Clamp01(HungerThreshold);
        StomachSize = Mathf.Clamp(StomachSize, 0.25f, 3.5f);
        Metabolism = Mathf.Clamp(Metabolism, 0.25f, 3.5f);
        Bravery = Mathf.Clamp01(Bravery);
        Selfishness = Mathf.Clamp01(Selfishness);
        ExplorationDrive = Mathf.Clamp01(ExplorationDrive);
        FoodMemoryStrength = Mathf.Clamp01(FoodMemoryStrength);
        SexGene = Mathf.Clamp01(SexGene);
        NestingDrive = Mathf.Clamp01(NestingDrive);
        EggProtection = Mathf.Clamp01(EggProtection);
        MateDrive = Mathf.Clamp01(MateDrive);
        Stealth = Mathf.Clamp01(Stealth);
        HearingSensitivity = Mathf.Clamp01(HearingSensitivity);
        ApplyDietLocks();
        ReproductionEnergyThreshold = Mathf.Clamp(ReproductionEnergyThreshold, 25f, EnergyCapacity * 0.95f);
        MutationRate = Mathf.Clamp(MutationRate, 0.005f, 0.35f);
        MutationStrength = Mathf.Clamp(MutationStrength, 0.1f, 3f);
        MorphPartMutationRate = Mathf.Clamp(MorphPartMutationRate, 0f, 0.25f);
        BrainStructuralMutationRate = Mathf.Clamp(BrainStructuralMutationRate, 0f, 0.2f);
        BrainMemoryDecay = Mathf.Clamp(BrainMemoryDecay, 0f, 0.98f);

        if (Brain == null)
        {
            Brain = SimpleNeuralNetwork.CreateRandom(BrainInputCount, BrainHiddenCount, BrainOutputCount);
        }
        else if (Brain.InputCount != BrainInputCount || Brain.OutputCount != BrainOutputCount || Brain.HiddenCount < 1 || Brain.HiddenCount > BrainMaxHiddenCount)
        {
            int targetHidden = Mathf.Clamp(Brain.HiddenCount > 0 ? Brain.HiddenCount : BrainHiddenCount, BrainHiddenCount, BrainMaxHiddenCount);
            Brain = Brain.ResizeTo(BrainInputCount, targetHidden, BrainOutputCount);
        }
    }

    // Handles ensure morph ids
    private void EnsureMorphIds()
    {
        BodyMorphId = NormaliseMorphIdForSlot(BodyMorphId, CreatureMorphSlot.Body, "basic");
        TailMorphId = NormaliseMorphIdForSlot(TailMorphId, CreatureMorphSlot.Tail, "basic");
        FinMorphId = NormaliseMorphIdForSlot(FinMorphId, CreatureMorphSlot.Fins, "basic");
        JawMorphId = NormaliseMorphIdForSlot(JawMorphId, CreatureMorphSlot.Jaw, "basic");
        SensorMorphId = NormaliseMorphIdForSlot(SensorMorphId, CreatureMorphSlot.Sensors, "basic");
        ArmourMorphId = NormaliseMorphIdForSlot(ArmourMorphId, CreatureMorphSlot.Armour, "none");
        DorsalFinMorphId = NormaliseMorphIdForSlot(DorsalFinMorphId, CreatureMorphSlot.DorsalFin, "none");
        SpikeMorphId = NormaliseMorphIdForSlot(SpikeMorphId, CreatureMorphSlot.Spikes, "none");
        GillMorphId = NormaliseMorphIdForSlot(GillMorphId, CreatureMorphSlot.Gills, "basic");
    }

    // Handles normalise morph id for slot
    private string NormaliseMorphIdForSlot(string id, CreatureMorphSlot slot, string fallback)
    {
        if (string.IsNullOrEmpty(id))
        {
            return fallback;
        }

        return CreatureMorphLibrary.NormalisePartIdForSlot(slot, id);
    }

    // Handles normalise diet traits
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

    // Handles reinforce diet usage
    public void ReinforceDietUsage(float plantAmount, float meatAmount, float carrionAmount, float learningRate)
    {
        float total = Mathf.Max(0.0001f, Mathf.Abs(plantAmount) + Mathf.Abs(meatAmount) + Mathf.Abs(carrionAmount));
        float rate = Mathf.Clamp01(learningRate);

        if (!PlantDietLocked && !MeatDietLocked && !CarrionDietLocked)
        {
            PlantDiet = Mathf.Lerp(PlantDiet, Mathf.Clamp01(plantAmount / total), rate);
            MeatDiet = Mathf.Lerp(MeatDiet, Mathf.Clamp01(meatAmount / total), rate);
            CarrionDiet = Mathf.Lerp(CarrionDiet, Mathf.Clamp01(carrionAmount / total), rate);
            NormaliseDietTraits();
        }

        TryLockDiet(total);
        ApplyDietLocks();
    }

    // Tries to lock diet and returns whether it worked
    private void TryLockDiet(float lifetimeConsumed)
    {
        if (PlantDietLocked || MeatDietLocked || CarrionDietLocked)
        {
            return;
        }

        // Locks are deliberately conservative. A fish must be strongly biased and have actually
        // lived on that diet before the lineage becomes specialised. Once locked, evolution pushes
        // supportive traits instead of unlocking randomly.
        if (lifetimeConsumed < 120f)
        {
            return;
        }

        float strongest = Mathf.Max(PlantDiet, Mathf.Max(MeatDiet, CarrionDiet));
        if (strongest < 0.82f)
        {
            return;
        }

        PlantDietLocked = PlantDiet >= strongest;
        MeatDietLocked = MeatDiet >= strongest && !PlantDietLocked;
        CarrionDietLocked = CarrionDiet >= strongest && !PlantDietLocked && !MeatDietLocked;
    }

    // Applies diet locks to the current object
    public void ApplyDietLocks()
    {
        if (PlantDietLocked)
        {
            PlantDiet = Mathf.Max(0.88f, PlantDiet);
            MeatDiet = Mathf.Min(MeatDiet, 0.07f);
            CarrionDiet = Mathf.Min(CarrionDiet, 0.08f);
        }
        else if (MeatDietLocked)
        {
            MeatDiet = Mathf.Max(0.88f, MeatDiet);
            PlantDiet = Mathf.Min(PlantDiet, 0.07f);
            CarrionDiet = Mathf.Min(CarrionDiet, 0.10f);
            Aggression = Mathf.Max(Aggression, 0.35f);
            Selfishness = Mathf.Max(Selfishness, 0.35f);
        }
        else if (CarrionDietLocked)
        {
            CarrionDiet = Mathf.Max(0.88f, CarrionDiet);
            PlantDiet = Mathf.Min(PlantDiet, 0.08f);
            MeatDiet = Mathf.Min(MeatDiet, 0.12f);
            FoodMemoryStrength = Mathf.Max(FoodMemoryStrength, 0.55f);
        }
    }

    // Handles decay unused behaviour traits
    public void DecayUnusedBehaviourTraits(float rate)
    {
        float r = Mathf.Clamp01(rate);
        Aggression = Mathf.Lerp(Aggression, 0.08f, r * Mathf.Clamp01(1f - MeatDiet));
        ThreatRange = Mathf.Lerp(ThreatRange, 0.22f, r * Mathf.Clamp01(1f - RiskTolerance));
        Territoriality = Mathf.Lerp(Territoriality, 0.05f, r * Mathf.Clamp01(1f - Aggression));
        GroupingChance = Mathf.Lerp(GroupingChance, 0.45f, r * Mathf.Clamp01(PlantDiet + CarrionDiet));
        ClampValues();
    }

    // Gets the morph signature used by the sim
    public string GetMorphSignature()
    {
        return BodyMorphId + "|" + TailMorphId + "|" + FinMorphId + "|" + JawMorphId + "|" + SensorMorphId + "|" + ArmourMorphId + "|" + DorsalFinMorphId + "|" + SpikeMorphId + "|" + GillMorphId;
    }

    // Gets the morph similarity used by the sim
    public float GetMorphSimilarity(EvolutionGenome other)
    {
        if (other == null)
        {
            return 0f;
        }

        int matches = 0;
        int total = 9;
        if (BodyMorphId == other.BodyMorphId) matches++;
        if (TailMorphId == other.TailMorphId) matches++;
        if (FinMorphId == other.FinMorphId) matches++;
        if (JawMorphId == other.JawMorphId) matches++;
        if (SensorMorphId == other.SensorMorphId) matches++;
        if (ArmourMorphId == other.ArmourMorphId) matches++;
        if (DorsalFinMorphId == other.DorsalFinMorphId) matches++;
        if (SpikeMorphId == other.SpikeMorphId) matches++;
        if (GillMorphId == other.GillMorphId) matches++;

        float partSimilarity = matches / (float)total;
        float shapeDifference = 0f;
        shapeDifference += Mathf.Abs(BodySize - other.BodySize) / 2.8f;
        shapeDifference += Mathf.Abs(BodyLength - other.BodyLength) / 2.5f;
        shapeDifference += Mathf.Abs(BodyWidth - other.BodyWidth) / 2.5f;
        shapeDifference += Mathf.Abs(JawSize - other.JawSize) / 2.5f;
        shapeDifference += Mathf.Abs(FinSize - other.FinSize) / 2.5f;
        shapeDifference += Mathf.Abs(TailSize - other.TailSize) / 2.5f;
        shapeDifference += Mathf.Abs(SensorSize - other.SensorSize) / 2.5f;
        float shapeSimilarity = 1f - Mathf.Clamp01(shapeDifference / 7f);

        return Mathf.Clamp01(partSimilarity * 0.72f + shapeSimilarity * 0.28f);
    }

    // Gets the energy drain multiplier used by the sim
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
