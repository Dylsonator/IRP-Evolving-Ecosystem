using System;
using UnityEngine;

[Serializable]
public class EvolutionCandidate
{
    public EvolutionGenome Genome;

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
        if (Genome == null)
        {
            return new EvolutionCandidate(EvolutionGenome.CreateRandom());
        }

        return new EvolutionCandidate(Genome.CreateMutatedCopy(mutationMultiplier));
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
