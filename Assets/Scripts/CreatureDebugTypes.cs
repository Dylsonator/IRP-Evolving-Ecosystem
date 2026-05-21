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
    Heavy,
    ArmouredGrazer,
    StreamlinedHunter,
    DefensiveHerbivore,
    SensorScavenger
}

public static class CreatureDebugTypeUtility
{
    public static CreatureBehaviourType GetBehaviourType(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return CreatureBehaviourType.Balanced;
        }

        CreatureEffectiveStats stats = CreatureEffectiveStats.Build(genome, CreatureMorphLibrary.ActiveLibrary);

        bool meatFocused = genome.MeatDiet >= genome.PlantDiet && genome.MeatDiet >= genome.CarrionDiet;
        bool plantFocused = genome.PlantDiet >= genome.MeatDiet && genome.PlantDiet >= genome.CarrionDiet;
        bool carrionFocused = genome.CarrionDiet >= genome.PlantDiet && genome.CarrionDiet >= genome.MeatDiet;
        bool streamlined = !string.IsNullOrEmpty(genome.BodyMorphId) && genome.BodyMorphId.Contains("streamlined");
        bool armoured = stats.Defence >= 1.25f || (!string.IsNullOrEmpty(genome.ArmourMorphId) && !genome.ArmourMorphId.Contains("none"));
        bool spiked = stats.DangerFactor >= 1.15f || (!string.IsNullOrEmpty(genome.SpikeMorphId) && !genome.SpikeMorphId.Contains("none"));

        if (meatFocused && streamlined && stats.Speed >= 5.3f)
        {
            return CreatureBehaviourType.StreamlinedHunter;
        }

        if (plantFocused && armoured)
        {
            return CreatureBehaviourType.ArmouredGrazer;
        }

        if (plantFocused && spiked)
        {
            return CreatureBehaviourType.DefensiveHerbivore;
        }

        if (carrionFocused && stats.VisionRange >= 24f)
        {
            return CreatureBehaviourType.SensorScavenger;
        }

        if (genome.MeatDiet >= 0.45f && genome.Aggression >= 0.24f)
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

        if (stats.Speed >= 7f)
        {
            return CreatureBehaviourType.Sprinter;
        }

        if (stats.VisionRange >= 27f)
        {
            return CreatureBehaviourType.Scout;
        }

        if (stats.BodySize >= 1.65f || stats.EnergyCapacity >= 170f)
        {
            return CreatureBehaviourType.Heavy;
        }

        if (genome.HungerDrive >= 0.7f && genome.PlantDiet >= 0.45f && genome.Aggression <= 0.45f)
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
            case CreatureBehaviourType.ArmouredGrazer:
                return "Armoured Grazer";
            case CreatureBehaviourType.StreamlinedHunter:
                return "Streamlined Hunter";
            case CreatureBehaviourType.DefensiveHerbivore:
                return "Defensive Herbivore";
            case CreatureBehaviourType.SensorScavenger:
                return "Sensor Scavenger";
            default:
                return "Balanced";
        }
    }

    public static string GetMorphologyName(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return "Unknown Morph";
        }

        CreatureEffectiveStats stats = CreatureEffectiveStats.Build(genome, CreatureMorphLibrary.ActiveLibrary);

        if (stats.DangerFactor >= 1.4f && genome.PlantDiet >= genome.MeatDiet)
        {
            return "Defensive";
        }

        if (!string.IsNullOrEmpty(genome.BodyMorphId) && genome.BodyMorphId.Contains("streamlined"))
        {
            return "Streamlined";
        }

        if (stats.Defence >= 1.2f || (!string.IsNullOrEmpty(genome.ArmourMorphId) && !genome.ArmourMorphId.Contains("none")))
        {
            return "Armoured";
        }

        if (stats.VisionRange >= 27f || (!string.IsNullOrEmpty(genome.SensorMorphId) && !genome.SensorMorphId.Contains("basic")))
        {
            return "Sensor";
        }

        if (genome.BodySize >= 1.45f || (!string.IsNullOrEmpty(genome.BodyMorphId) && genome.BodyMorphId.Contains("bulky")))
        {
            return "Bulky";
        }

        if (stats.TurnRate >= 235f || genome.FinWidth > 1.3f)
        {
            return "Agile";
        }

        return "Basic";
    }

    public static string GetSpeciesGroupName(EvolutionGenome genome)
    {
        CreatureBehaviourType type = GetBehaviourType(genome);
        return GetTypeName(type) + " - " + GetMorphologyName(genome);
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
            case CreatureBehaviourType.ArmouredGrazer:
                return new Color(0.55f, 0.95f, 0.55f, 1f);
            case CreatureBehaviourType.StreamlinedHunter:
                return new Color(1f, 0.18f, 0.15f, 1f);
            case CreatureBehaviourType.DefensiveHerbivore:
                return new Color(0.35f, 0.8f, 0.35f, 1f);
            case CreatureBehaviourType.SensorScavenger:
                return new Color(0.75f, 0.45f, 0.2f, 1f);
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
