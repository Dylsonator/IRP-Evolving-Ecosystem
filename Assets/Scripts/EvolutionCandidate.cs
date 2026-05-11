using System;
using UnityEngine;

[Serializable]
public class EvolutionCandidate
{
    public EvolutionGenome Genome;
    public string SpeciesKeyAtBirth;

    [Header("Evaluation")]
    public float SurvivalTime;
    public float EnergyGained;
    public int FoodEaten;
    public int PlantMeals;
    public int MeatMeals;
    public int CarrionMeals;
    public int Kills;
    public int ReproductionCount;
    public float DamageDealt;
    public float DamageTaken;
    public float DistanceTravelled;

    public EvolutionCandidate(EvolutionGenome genome)
    {
        Genome = genome;
        if (Genome == null)
        {
            Genome = EvolutionGenome.CreateRandom();
        }

        Genome.ClampValues();
        SpeciesKeyAtBirth = SpeciesUtility.GetSpeciesKey(Genome);
    }

    public float GetFitness()
    {
        // This is kept as an evaluation value only. The continuous ecosystem does not force a fixed winner each generation.
        float fitness = 0f;
        fitness += SurvivalTime * 1.0f;
        fitness += EnergyGained * 0.25f;
        fitness += FoodEaten * 10f;
        fitness += ReproductionCount * 55f;
        fitness += Kills * 18f;
        fitness += DistanceTravelled * 0.01f;
        fitness -= DamageTaken * 0.1f;
        return Mathf.Max(0f, fitness);
    }

    public Vector2 GetGroupingAggressionDescriptor()
    {
        if (Genome == null)
        {
            return Vector2.zero;
        }

        return new Vector2(Genome.GroupingChance, Genome.Aggression);
    }

    public EvolutionCandidate CreateChild(float mutationMultiplier)
    {
        if (Genome == null)
        {
            return new EvolutionCandidate(EvolutionGenome.CreateRandom());
        }

        return new EvolutionCandidate(Genome.CreateMutatedCopy(mutationMultiplier));
    }
}
