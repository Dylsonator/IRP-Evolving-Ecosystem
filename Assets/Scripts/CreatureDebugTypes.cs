using UnityEngine;

public enum CreatureBehaviourType
{
    Balanced,
    Grazer,
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

        if (genome.HungerDrive >= 0.72f && genome.Aggression <= 0.45f)
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
