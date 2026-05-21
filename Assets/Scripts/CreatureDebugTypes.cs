using UnityEngine;

public enum CreatureBehaviourType
{
    Balanced,
    Grazer,
    Predator,
    Scavenger,
    Omnivore,
    Sprinter,
    Scout,
    Schooling,
    Skittish,
    Aggressive,
    Heavy
}

public static class CreatureDebugTypeUtility
{
    public static CreatureBehaviourType GetBehaviourType(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return CreatureBehaviourType.Balanced;
        }

        if (genome.MeatDiet >= 0.45f && genome.Aggression >= 0.28f)
        {
            return CreatureBehaviourType.Predator;
        }

        if (genome.CarrionDiet >= 0.45f)
        {
            return CreatureBehaviourType.Scavenger;
        }

        if (genome.PlantDiet >= 0.25f && genome.MeatDiet >= 0.25f && genome.CarrionDiet >= 0.15f)
        {
            return CreatureBehaviourType.Omnivore;
        }

        if (genome.Aggression >= 0.65f && genome.RiskTolerance >= 0.45f)
        {
            return CreatureBehaviourType.Aggressive;
        }

        if (genome.ThreatRange >= 0.65f && genome.RiskTolerance <= 0.55f)
        {
            return CreatureBehaviourType.Skittish;
        }

        if (genome.GroupingChance >= 0.6f && genome.AttractionRange >= 0.45f)
        {
            return CreatureBehaviourType.Schooling;
        }

        if (genome.Speed >= 7f)
        {
            return CreatureBehaviourType.Sprinter;
        }

        if (genome.VisionRange >= 25f)
        {
            return CreatureBehaviourType.Scout;
        }

        if (genome.BodySize >= 1.55f || genome.EnergyCapacity >= 155f)
        {
            return CreatureBehaviourType.Heavy;
        }

        if (genome.HungerDrive >= 0.72f && genome.PlantDiet >= 0.45f && genome.Aggression <= 0.45f)
        {
            return CreatureBehaviourType.Grazer;
        }

        return CreatureBehaviourType.Balanced;
    }

    public static string GetTypeName(CreatureBehaviourType type)
    {
        switch (type)
        {
            case CreatureBehaviourType.Grazer:
                return "Grazer";
            case CreatureBehaviourType.Predator:
                return "Predator";
            case CreatureBehaviourType.Scavenger:
                return "Scavenger";
            case CreatureBehaviourType.Omnivore:
                return "Omnivore";
            case CreatureBehaviourType.Sprinter:
                return "Sprinter";
            case CreatureBehaviourType.Scout:
                return "Scout";
            case CreatureBehaviourType.Schooling:
                return "Schooling";
            case CreatureBehaviourType.Skittish:
                return "Skittish";
            case CreatureBehaviourType.Aggressive:
                return "Aggressive";
            case CreatureBehaviourType.Heavy:
                return "Heavy";
            default:
                return "Balanced";
        }
    }

    public static Color GetTypeColour(CreatureBehaviourType type)
    {
        switch (type)
        {
            case CreatureBehaviourType.Grazer:
                return new Color(0.35f, 0.95f, 0.45f, 1f);
            case CreatureBehaviourType.Predator:
                return new Color(1f, 0.08f, 0.08f, 1f);
            case CreatureBehaviourType.Scavenger:
                return new Color(0.65f, 0.35f, 0.12f, 1f);
            case CreatureBehaviourType.Omnivore:
                return new Color(0.4f, 1f, 0.85f, 1f);
            case CreatureBehaviourType.Sprinter:
                return new Color(0.95f, 0.85f, 0.25f, 1f);
            case CreatureBehaviourType.Scout:
                return new Color(0.3f, 0.75f, 1f, 1f);
            case CreatureBehaviourType.Schooling:
                return new Color(0.65f, 0.55f, 1f, 1f);
            case CreatureBehaviourType.Skittish:
                return new Color(1f, 0.55f, 0.25f, 1f);
            case CreatureBehaviourType.Aggressive:
                return new Color(1f, 0.25f, 0.25f, 1f);
            case CreatureBehaviourType.Heavy:
                return new Color(0.75f, 0.75f, 0.75f, 1f);
            default:
                return new Color(0.95f, 0.95f, 0.95f, 1f);
        }
    }

    public static string BuildReadableName(EvolutionGenome genome, int id)
    {
        CreatureBehaviourType type = GetBehaviourType(genome);
        string typeName = GetTypeName(type);
        return typeName + " #" + id.ToString("000");
    }
}
