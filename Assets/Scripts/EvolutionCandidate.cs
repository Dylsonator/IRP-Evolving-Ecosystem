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
    public float EnergyGained;
    public int FoodEaten;
    public int ReproductionCount;
    public float DistanceTravelled;
    public float AverageSpeedUsed;
    public float AverageFoodDistance;

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
        float fitness = 0f;
        fitness += SurvivalTime * 1.0f;
        fitness += EnergyGained * 0.35f;
        fitness += FoodEaten * 12f;
        fitness += ReproductionCount * 45f;
        fitness += DistanceTravelled * 0.02f;

        return Mathf.Max(0f, fitness);
    }

    public Vector2 GetBehaviourDescriptor()
    {
        float movementDescriptor = Mathf.Clamp01(DistanceTravelled / 500f);
        float feedingDescriptor = Mathf.Clamp01(FoodEaten / 12f);

        return new Vector2(movementDescriptor, feedingDescriptor);
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
        ReproductionCount += other.ReproductionCount;
        DistanceTravelled += other.DistanceTravelled;
        AverageSpeedUsed += other.AverageSpeedUsed;
        AverageFoodDistance += other.AverageFoodDistance;
    }
}
