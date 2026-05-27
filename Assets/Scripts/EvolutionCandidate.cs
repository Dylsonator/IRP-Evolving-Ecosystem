using System;
using UnityEngine;

// Stores one creature genome plus its lifetime metrics, fitness calculation and debug identity.
[Serializable]
public class EvolutionCandidate
{
    public EvolutionGenome Genome;

    [Header("Debug Identity")]
    public int Id;
    public int ParentId;
    public int GenerationBorn;
    public string DisplayName;
    public CreatureBehaviourType BehaviourType;

    // Lifetime metrics are stored here so selection can judge what the creature actually did.
    [Header("Evaluation")]
    public float SurvivalTime;
    public float FinalEnergy;
    public float FinalHealth;
    public float FinalStomachFullness;
    public float EnergyGained;
    public int FoodEaten;
    public int CarrionEaten;
    public float FoodMassConsumed;
    public int PreyBites;
    public float PlantEnergyConsumed;
    public float MeatEnergyConsumed;
    public float CarrionEnergyConsumed;
    public float StarvationDamageTaken;
    public int FoodMemoryUses;
    public int LeaderFollowEvents;
    public float RestingTime;
    public float ExploringTime;
    public float SchoolingTime;
    public float ForagingTime;
    public float FeedingTime;
    public float MateSeekingTime;
    public float HuntingTime;
    public float FleeingTime;
    public float RecoveryTime;
    public int BrainModeSwitches;
    public int PreyKills;
    public float BiteDamageDealt;
    public int ReproductionCount;
    public int EggsLaid;
    public int EggsHatched;
    public int EggsEaten;
    public float DistanceTravelled;
    public float AverageSpeedUsed;
    public float AverageFoodDistance;
    public float AveragePreyDistance;
    public float AverageCarrionDistance;

    [Header("Neural / Survival Evidence")]
    public int BrainOutputSamples;
    public float AverageBrainFoodBias;
    public float AverageBrainHuntBias;
    public float AverageBrainFleeBias;
    public float AverageBrainMateSocialBias;
    public float AverageBrainExploreHomeBias;
    public float AverageBrainRestBias;
    public float AverageBrainSprintBias;
    public float SurvivalEmergencyTime;
    public int SurvivalEmergencyActivations;
    public float PredatorTypeAvoidanceTime;
    public float GroupDefenceDamageDealt;
    public int GroupDefenceEvents;

    [Header("Ancestral Spawn Memory")]
    public bool HasPreferredSpawnArea;
    public Vector3 PreferredSpawnArea;
    public string PreferredSpawnNiche;
    [Range(0f, 1f)] public float PreferredSpawnConfidence;

    // Creates a candidate around a genome and makes sure identity data exists
    public EvolutionCandidate(EvolutionGenome genome)
    {
        Genome = genome;
        RefreshDebugIdentity();
    }

    // Sets the runtime ID and display name used by logs and labels
    public void AssignRuntimeIdentity(int id, int generationBorn, int parentId = 0)
    {
        Id = id;
        GenerationBorn = generationBorn;
        ParentId = parentId;
        RefreshDebugIdentity();
    }

    // Updates the readable behaviour type and display name from the genome
    public void RefreshDebugIdentity()
    {
        BehaviourType = CreatureDebugTypeUtility.GetBehaviourType(Genome);

        if (Id > 0)
        {
            DisplayName = CreatureDebugTypeUtility.BuildReadableName(Genome, Id);
        }
        else
        {
            DisplayName = CreatureDebugTypeUtility.GetTypeName(BehaviourType);
        }
    }

    // Scores how well this creature survived and used its niche during the run
    public float GetFitness()
    {
        // Fitness is intentionally multi-factor, but it must not over-reward the safest food source.
        // Earlier versions rewarded raw plant count too much, so grazers could out-score every other
        // niche simply by surviving and eating safe buds. This version scores niche success instead:
        // plant specialists are rewarded for plant energy, predators for meat/prey success, and
        // scavengers for carrion. Wrong-diet feeding still keeps a creature alive, but it is not
        // treated as strong evolutionary proof.
        if (Genome == null)
        {
            return 0f;
        }

        float plantDiet = Mathf.Clamp01(Genome.PlantDiet);
        float meatDiet = Mathf.Clamp01(Genome.MeatDiet);
        float carrionDiet = Mathf.Clamp01(Genome.CarrionDiet);
        float dietTotal = Mathf.Max(0.001f, plantDiet + meatDiet + carrionDiet);
        plantDiet /= dietTotal;
        meatDiet /= dietTotal;
        carrionDiet /= dietTotal;

        float fitness = 0f;

        // Baseline biological success. Survival matters, but not enough to let slow starving armour win forever.
        fitness += SurvivalTime * 0.82f;
        fitness += EnergyGained * 0.18f;
        fitness += FinalHealth * 0.18f;
        fitness += FinalStomachFullness * 5.0f;

        // Diet-aligned feeding. These are the main feeding rewards.
        float plantAlignment = Mathf.Lerp(0.18f, 1.0f, plantDiet);
        float meatAlignment = Mathf.Lerp(0.20f, 1.08f, meatDiet);
        float carrionAlignment = Mathf.Lerp(0.20f, 1.05f, carrionDiet);

        fitness += PlantEnergyConsumed * 0.082f * plantAlignment;
        fitness += MeatEnergyConsumed * 0.140f * meatAlignment;
        fitness += CarrionEnergyConsumed * 0.105f * carrionAlignment;

        // Counts are now small evidence bonuses, not the main score driver.
        fitness += FoodEaten * Mathf.Lerp(0.8f, 3.8f, plantDiet);
        fitness += CarrionEaten * Mathf.Lerp(1.0f, 6.0f, carrionDiet);
        fitness += FoodMassConsumed * 0.035f * plantAlignment;

        // Predator success. Bites are useful proof, but kills/own meat intake matter more.
        float predatorProof = Mathf.Clamp01(meatDiet * 1.35f + Genome.Aggression * 0.45f);
        fitness += PreyBites * Mathf.Lerp(1.2f, 8.5f, predatorProof);
        fitness += PreyKills * Mathf.Lerp(10f, 42f, predatorProof);
        fitness += BiteDamageDealt * Mathf.Lerp(0.035f, 0.18f, predatorProof);

        // Scavengers should have a real path that is not just failed predation.
        if (carrionDiet > plantDiet && carrionDiet >= meatDiet)
        {
            fitness += CarrionEaten * 3.5f;
            fitness += CarrionEnergyConsumed * 0.030f;
        }

        // Plant-led creatures need a fair route to selection that is not just raw food count.
        // Reward stable grazing/schooling slightly when it produces real plant energy and survival.
        if (plantDiet >= 0.52f && plantDiet >= meatDiet && PlantEnergyConsumed > 0f)
        {
            fitness += Mathf.Min(18f, PlantEnergyConsumed * 0.018f + Mathf.Min(SchoolingTime, SurvivalTime) * 0.018f);
        }

        // Reproduction and offspring remain the strongest evidence of a viable strategy.
        fitness += ReproductionCount * 56f;
        fitness += EggsLaid * 6f;
        fitness += EggsHatched * 20f;

        // Behaviour evidence. Small values only, so movement spam does not become a goal.
        fitness += FoodMemoryUses * 1.0f;
        fitness += LeaderFollowEvents * 0.6f;
        fitness += Mathf.Min(ForagingTime, SurvivalTime) * Mathf.Lerp(0.015f, 0.055f, plantDiet);
        fitness += Mathf.Min(MateSeekingTime, SurvivalTime) * 0.06f;
        fitness += Mathf.Min(SchoolingTime, SurvivalTime) * Mathf.Lerp(0.015f, 0.050f, Genome.GroupingChance);
        fitness += DistanceTravelled * 0.010f;

        // Wrong-diet penalties. These do not stop emergency survival, but they stop selection from
        // treating a predator that grazes all day, or a grazer that randomly hunts, as a better version
        // of that niche.
        float wrongPlantForPredator = Mathf.Clamp01((meatDiet - plantDiet) * 1.8f) * PlantEnergyConsumed;
        float wrongHuntingForGrazer = Mathf.Clamp01((plantDiet - meatDiet) * 1.8f) * (PreyBites + PreyKills * 3f);
        fitness -= wrongPlantForPredator * 0.045f;
        fitness -= wrongHuntingForGrazer * 4.0f;

        // A hunter that spends a long time hunting but gets no bite is not a good hunter.
        if (HuntingTime > 2f)
        {
            float huntProof = PreyBites + PreyKills * 2f + MeatEnergyConsumed * 0.018f;
            float wastedHunt = Mathf.Max(0f, HuntingTime - huntProof * 5f);
            fitness -= wastedHunt * Mathf.Lerp(0.03f, 0.16f, meatDiet);
        }

        // Natural anti-convergence support: a creature that only survives by slowly starving should not dominate selection.
        fitness -= StarvationDamageTaken * 0.75f;
        if (FinalHealth > 0f && FinalHealth < 35f)
        {
            fitness -= (35f - FinalHealth) * 0.50f;
        }

        // Heavy armour should survive through useful strategy, not just by taking ages to die.
        if (Genome.Armour > 0.65f && StarvationDamageTaken > 5f)
        {
            fitness -= (Genome.Armour - 0.65f) * StarvationDamageTaken * 0.35f;
        }

        return Mathf.Max(0f, fitness);
    }

    // Builds a compact behaviour descriptor for novelty and archive scoring
    public Vector2 GetBehaviourDescriptor()
    {
        float movementDescriptor = Mathf.Clamp01(DistanceTravelled / 500f);
        float feedingScore = FoodEaten + CarrionEaten + FoodMassConsumed * 0.05f + PreyBites + PreyKills * 2f;
        float feedingDescriptor = Mathf.Clamp01(feedingScore / 14f);

        return new Vector2(movementDescriptor, feedingDescriptor);
    }

    // Returns the main diet label used for logs and selection
    public Vector2 GetDietDescriptor()
    {
        if (Genome == null)
        {
            return Vector2.zero;
        }

        // X = plant to meat axis, Y = scavenger tendency.
        return new Vector2(
            Mathf.Clamp01(Genome.MeatDiet),
            Mathf.Clamp01(Genome.CarrionDiet)
        );
    }

    // Creates a mutated child candidate from this candidate
    public EvolutionCandidate CreateChild(float mutationMultiplier)
    {
        EvolutionCandidate child;

        if (Genome == null)
        {
            child = new EvolutionCandidate(EvolutionGenome.CreateRandom());
        }
        else
        {
            child = new EvolutionCandidate(Genome.CreateMutatedCopy(mutationMultiplier));
        }

        child.ParentId = Id;
        child.HasPreferredSpawnArea = HasPreferredSpawnArea;
        child.PreferredSpawnArea = PreferredSpawnArea + UnityEngine.Random.insideUnitSphere * Mathf.Lerp(0.75f, 4.0f, 1f - Mathf.Clamp01(PreferredSpawnConfidence));
        child.PreferredSpawnNiche = PreferredSpawnNiche;
        child.PreferredSpawnConfidence = Mathf.Clamp01(PreferredSpawnConfidence * 0.92f);
        return child;
    }

    // Copies runtime results from a fish back into this candidate
    public void AddMetricsFrom(EvolutionCandidate other)
    {
        if (other == null)
        {
            return;
        }

        SurvivalTime += other.SurvivalTime;
        EnergyGained += other.EnergyGained;
        FoodEaten += other.FoodEaten;
        CarrionEaten += other.CarrionEaten;
        FoodMassConsumed += other.FoodMassConsumed;
        PlantEnergyConsumed += other.PlantEnergyConsumed;
        MeatEnergyConsumed += other.MeatEnergyConsumed;
        CarrionEnergyConsumed += other.CarrionEnergyConsumed;
        StarvationDamageTaken += other.StarvationDamageTaken;
        FoodMemoryUses += other.FoodMemoryUses;
        LeaderFollowEvents += other.LeaderFollowEvents;
        RestingTime += other.RestingTime;
        ExploringTime += other.ExploringTime;
        SchoolingTime += other.SchoolingTime;
        ForagingTime += other.ForagingTime;
        FeedingTime += other.FeedingTime;
        MateSeekingTime += other.MateSeekingTime;
        HuntingTime += other.HuntingTime;
        FleeingTime += other.FleeingTime;
        RecoveryTime += other.RecoveryTime;
        BrainModeSwitches += other.BrainModeSwitches;
        PreyBites += other.PreyBites;
        PreyKills += other.PreyKills;
        BiteDamageDealt += other.BiteDamageDealt;
        ReproductionCount += other.ReproductionCount;
        EggsLaid += other.EggsLaid;
        EggsHatched += other.EggsHatched;
        EggsEaten += other.EggsEaten;
        DistanceTravelled += other.DistanceTravelled;
        AverageSpeedUsed += other.AverageSpeedUsed;
        AverageFoodDistance += other.AverageFoodDistance;
        AveragePreyDistance += other.AveragePreyDistance;
        AverageCarrionDistance += other.AverageCarrionDistance;

        int combinedBrainSamples = BrainOutputSamples + other.BrainOutputSamples;
        if (combinedBrainSamples > 0)
        {
            AverageBrainFoodBias = WeightedAverage(AverageBrainFoodBias, BrainOutputSamples, other.AverageBrainFoodBias, other.BrainOutputSamples);
            AverageBrainHuntBias = WeightedAverage(AverageBrainHuntBias, BrainOutputSamples, other.AverageBrainHuntBias, other.BrainOutputSamples);
            AverageBrainFleeBias = WeightedAverage(AverageBrainFleeBias, BrainOutputSamples, other.AverageBrainFleeBias, other.BrainOutputSamples);
            AverageBrainMateSocialBias = WeightedAverage(AverageBrainMateSocialBias, BrainOutputSamples, other.AverageBrainMateSocialBias, other.BrainOutputSamples);
            AverageBrainExploreHomeBias = WeightedAverage(AverageBrainExploreHomeBias, BrainOutputSamples, other.AverageBrainExploreHomeBias, other.BrainOutputSamples);
            AverageBrainRestBias = WeightedAverage(AverageBrainRestBias, BrainOutputSamples, other.AverageBrainRestBias, other.BrainOutputSamples);
            AverageBrainSprintBias = WeightedAverage(AverageBrainSprintBias, BrainOutputSamples, other.AverageBrainSprintBias, other.BrainOutputSamples);
            BrainOutputSamples = combinedBrainSamples;
        }

        SurvivalEmergencyTime += other.SurvivalEmergencyTime;
        SurvivalEmergencyActivations += other.SurvivalEmergencyActivations;
        PredatorTypeAvoidanceTime += other.PredatorTypeAvoidanceTime;
        GroupDefenceDamageDealt += other.GroupDefenceDamageDealt;
        GroupDefenceEvents += other.GroupDefenceEvents;

        if (other.HasPreferredSpawnArea && (!HasPreferredSpawnArea || other.PreferredSpawnConfidence > PreferredSpawnConfidence))
        {
            HasPreferredSpawnArea = true;
            PreferredSpawnArea = other.PreferredSpawnArea;
            PreferredSpawnNiche = other.PreferredSpawnNiche;
            PreferredSpawnConfidence = other.PreferredSpawnConfidence;
        }
    }

    // Blends parent values when creating child metrics
    private float WeightedAverage(float a, int aCount, float b, int bCount)
    {
        int total = aCount + bCount;
        if (total <= 0)
        {
            return 0f;
        }

        return (a * aCount + b * bCount) / total;
    }
}
