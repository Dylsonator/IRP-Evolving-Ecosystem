using UnityEngine;

public static class EvolutionNicheUtility
{
    public static string BuildNicheKey(EvolutionCandidate candidate)
    {
        if (candidate == null || candidate.Genome == null)
        {
            return "unknown";
        }

        EvolutionGenome g = candidate.Genome;
        string diet = GetDietNiche(g);
        string movement = GetMovementNiche(g);
        string social = GetSocialNiche(g);
        string defence = GetDefenceNiche(g);
        string reproduction = GetReproductionNiche(g);

        return diet + "|" + movement + "|" + social + "|" + defence + "|" + reproduction;
    }

    public static string BuildSelectionKey(EvolutionCandidate candidate, int bins)
    {
        if (candidate == null || candidate.Genome == null)
        {
            return "unknown";
        }

        bins = Mathf.Clamp(bins, 2, 8);
        Vector2 behaviour = candidate.GetBehaviourDescriptor();
        int movementBin = Mathf.Clamp(Mathf.FloorToInt(behaviour.x * bins), 0, bins - 1);
        int feedingBin = Mathf.Clamp(Mathf.FloorToInt(behaviour.y * bins), 0, bins - 1);

        // Keep selection buckets broad enough to preserve ecological roles, then use feature bins
        // inside those roles. The older full key was too fragmented in some runs, so a dominant
        // role could still fill most of the next generation through tournament fallback.
        return BuildCoreNicheKey(candidate) + "|Move" + movementBin + "|Feed" + feedingBin;
    }

    public static string BuildCoreNicheKey(EvolutionCandidate candidate)
    {
        if (candidate == null || candidate.Genome == null)
        {
            return "unknown";
        }

        return BuildCoreNicheKey(candidate.Genome);
    }

    public static string BuildCoreNicheKey(EvolutionGenome g)
    {
        if (g == null)
        {
            return "unknown";
        }

        bool plantFocused = g.PlantDiet >= g.MeatDiet && g.PlantDiet >= g.CarrionDiet;
        bool meatFocused = g.MeatDiet >= g.PlantDiet && g.MeatDiet >= g.CarrionDiet;
        bool carrionFocused = g.CarrionDiet >= g.PlantDiet && g.CarrionDiet >= g.MeatDiet;
        bool defensive = g.Armour >= 0.50f || g.SpikeSize >= 0.70f || g.DangerFactor >= 0.70f;
        bool schooling = g.GroupingChance >= 0.55f && g.SchoolTightness >= 0.38f;
        bool eggGuardian = g.EggProtection >= 0.58f && g.NestingDrive >= 0.42f;
        bool predator = g.MeatDiet >= 0.34f && g.Aggression >= 0.20f;

        if (predator && IsLikelyAmbusher(g)) return "ambush_predator";
        if (predator && meatFocused) return "active_predator";
        if (carrionFocused && g.CarrionDiet >= 0.34f) return "scavenger";
        if (plantFocused && defensive) return "defensive_grazer";
        if (plantFocused && schooling) return "schooling_grazer";
        if (eggGuardian) return "egg_guardian";
        if (g.MeatDiet >= 0.22f && g.PlantDiet >= 0.22f && g.CarrionDiet >= 0.12f) return "omnivore";
        if (plantFocused) return "grazer";
        if (g.Speed >= 6.2f || g.TurnRate >= 230f) return "mobile_generalist";
        return "generalist";
    }

    public static bool IsLikelyAmbusher(EvolutionGenome g)
    {
        if (g == null)
        {
            return false;
        }

        return g.MeatDiet >= 0.34f && g.Stealth >= 0.48f && (g.Territoriality >= 0.35f || g.Speed < 5.2f);
    }

    private static string GetDietNiche(EvolutionGenome g)
    {
        if (g.MeatDiet >= 0.52f && g.MeatDiet >= g.PlantDiet && g.MeatDiet >= g.CarrionDiet) return "active_predator";
        if (g.CarrionDiet >= 0.46f && g.CarrionDiet >= g.PlantDiet) return "scavenger";
        if (g.PlantDiet >= 0.55f && g.PlantDiet >= g.MeatDiet) return "grazer";
        if (g.MeatDiet >= 0.25f && g.PlantDiet >= 0.25f) return "omnivore";
        return "mixed_diet";
    }

    private static string GetMovementNiche(EvolutionGenome g)
    {
        if (g.Speed >= 6.3f && g.TailLength >= 1.08f) return "sprinter";
        if (g.TurnRate >= 230f || g.FinWidth >= 1.25f) return "agile";
        if (g.BodySize >= 1.45f || g.Armour >= 0.55f) return "heavy";
        if (IsLikelyAmbusher(g)) return "ambusher";
        return "general_mover";
    }

    private static string GetSocialNiche(EvolutionGenome g)
    {
        if (g.Territoriality >= 0.55f) return "territorial";
        if (g.GroupingChance >= 0.58f && g.SchoolTightness >= 0.45f) return "schooling";
        if (g.Selfishness >= 0.55f) return "solitary";
        return "loose_social";
    }

    private static string GetDefenceNiche(EvolutionGenome g)
    {
        if (g.DangerFactor >= 0.75f || g.SpikeSize >= 0.85f) return "warning_defence";
        if (g.Armour >= 0.55f) return "armoured";
        if (g.RiskTolerance <= 0.28f || g.Bravery <= 0.28f) return "skittish";
        return "normal_defence";
    }

    private static string GetReproductionNiche(EvolutionGenome g)
    {
        if (g.EggProtection >= 0.62f && g.NestingDrive >= 0.45f) return "egg_guardian";
        if (g.MateDrive >= 0.65f) return "high_mate_drive";
        return "normal_reproduction";
    }
}
