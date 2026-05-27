using System;
using UnityEngine;

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

    public EvolutionCandidate(EvolutionGenome genome)
    {
        Genome = genome;
        RefreshDebugIdentity();
    }

    public void AssignRuntimeIdentity(int id, int generationBorn, int parentId = 0)
    {
        Id = id;
        GenerationBorn = generationBorn;
        ParentId = parentId;
        RefreshDebugIdentity();
    }

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

    public float GetFitness()
    {
        // Fitness is intentionally multi-factor. This avoids selecting only one "best" stat.
        // It rewards survival, energy collection, reproduction, movement and successful feeding strategy.
        float fitness = 0f;
        fitness += SurvivalTime * 1.0f;
        fitness += EnergyGained * 0.35f;
        fitness += FinalHealth * 0.25f;
        fitness += FinalStomachFullness * 8f;
        fitness += FoodEaten * 12f;
        fitness += CarrionEaten * 10f;
        fitness += FoodMassConsumed * 0.18f;
        fitness += PreyBites * 6f;
        fitness += PreyKills * 28f;
        fitness += BiteDamageDealt * 0.22f;
        fitness += ReproductionCount * 52f;
        fitness += EggsLaid * 5f;
        fitness += EggsHatched * 18f;
        fitness += FoodMemoryUses * 1.2f;
        fitness += LeaderFollowEvents * 0.6f;
        fitness += Mathf.Min(ForagingTime, SurvivalTime) * 0.08f;
        fitness += Mathf.Min(MateSeekingTime, SurvivalTime) * 0.05f;
        fitness += DistanceTravelled * 0.02f;

        // Natural anti-convergence support: a creature that only survives by slowly starving should not dominate selection.
        // This lets heavy armour remain useful, but prevents barely-functional armoured builds taking over every niche.
        fitness -= StarvationDamageTaken * 0.55f;
        if (FinalHealth > 0f && FinalHealth < 35f)
        {
            fitness -= (35f - FinalHealth) * 0.35f;
        }

        return Mathf.Max(0f, fitness);
    }

    public Vector2 GetBehaviourDescriptor()
    {
        float movementDescriptor = Mathf.Clamp01(DistanceTravelled / 500f);
        float feedingScore = FoodEaten + CarrionEaten + FoodMassConsumed * 0.05f + PreyBites + PreyKills * 2f;
        float feedingDescriptor = Mathf.Clamp01(feedingScore / 14f);

        return new Vector2(movementDescriptor, feedingDescriptor);
    }

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
