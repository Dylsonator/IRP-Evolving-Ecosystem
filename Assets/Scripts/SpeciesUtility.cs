using UnityEngine;

public static class SpeciesUtility
{
    public static string GetSpeciesKey(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return "Unknown";
        }

        string diet = GetDietCode(genome);
        int bodyBin = Bin(genome.BodySize, 0.35f, 2.75f, 4);
        int jawBin = Bin(genome.JawSize, 0.2f, 2.75f, 4);
        int sensorBin = Bin(genome.SensorSize, 0.25f, 2.75f, 4);
        int aggressionBin = Bin(genome.Aggression, 0f, 1f, 4);
        int groupingBin = Bin(genome.GroupingChance, 0f, 1f, 4);

        return diet + "-B" + bodyBin + "-J" + jawBin + "-S" + sensorBin + "-A" + aggressionBin + "-G" + groupingBin;
    }

    public static string GetDisplayName(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return "Unknown Species";
        }

        string diet = GetDietName(genome);
        string body = genome.BodySize > 1.45f ? "Large" : genome.BodySize < 0.8f ? "Small" : "Mid";
        string mood = genome.Aggression > 0.65f ? "Hunter" : genome.GroupingChance > 0.65f ? "Shoaler" : "Drifter";

        return body + " " + diet + " " + mood;
    }

    public static bool AreSimilarEnoughForGrouping(EvolutionGenome a, EvolutionGenome b)
    {
        return GetSimilarity(a, b) >= 0.68f;
    }

    public static float GetSimilarity(EvolutionGenome a, EvolutionGenome b)
    {
        if (a == null || b == null)
        {
            return 0f;
        }

        float difference = 0f;
        difference += NormalisedDifference(a.BodySize, b.BodySize, 0.35f, 2.75f);
        difference += NormalisedDifference(a.TailSize, b.TailSize, 0.25f, 2.75f);
        difference += NormalisedDifference(a.FinSize, b.FinSize, 0.25f, 2.75f);
        difference += NormalisedDifference(a.JawSize, b.JawSize, 0.2f, 2.75f);
        difference += NormalisedDifference(a.SensorSize, b.SensorSize, 0.25f, 2.75f);
        difference += Mathf.Abs(a.PlantDiet - b.PlantDiet);
        difference += Mathf.Abs(a.MeatDiet - b.MeatDiet);
        difference += Mathf.Abs(a.CarrionDiet - b.CarrionDiet);
        difference += Mathf.Abs(a.Aggression - b.Aggression);
        difference += Mathf.Abs(a.GroupingChance - b.GroupingChance);

        float averageDifference = difference / 10f;
        return Mathf.Clamp01(1f - averageDifference);
    }

    public static Color GetBodyColour(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return Color.white;
        }

        Color plant = new Color(0.25f, 0.8f, 0.45f);
        Color meat = new Color(0.85f, 0.25f, 0.2f);
        Color carrion = new Color(0.55f, 0.45f, 0.75f);
        Color neutral = new Color(0.45f, 0.75f, 0.9f);

        float total = Mathf.Max(0.01f, genome.PlantDiet + genome.MeatDiet + genome.CarrionDiet);
        Color mixed = (plant * genome.PlantDiet + meat * genome.MeatDiet + carrion * genome.CarrionDiet) / total;
        mixed = Color.Lerp(neutral, mixed, 0.75f);
        return mixed;
    }

    private static string GetDietCode(EvolutionGenome genome)
    {
        if (genome.MeatDiet > genome.PlantDiet && genome.MeatDiet >= genome.CarrionDiet)
        {
            return "M";
        }

        if (genome.CarrionDiet > genome.PlantDiet && genome.CarrionDiet > genome.MeatDiet)
        {
            return "C";
        }

        if (genome.PlantDiet > 0.55f && genome.MeatDiet > 0.45f)
        {
            return "O";
        }

        return "P";
    }

    private static string GetDietName(EvolutionGenome genome)
    {
        string code = GetDietCode(genome);

        if (code == "M") return "Meat";
        if (code == "C") return "Carrion";
        if (code == "O") return "Omni";
        return "Plant";
    }

    private static int Bin(float value, float min, float max, int bins)
    {
        float normalised = Mathf.InverseLerp(min, max, value);
        int bin = Mathf.FloorToInt(normalised * bins);
        return Mathf.Clamp(bin, 0, bins - 1);
    }

    private static float NormalisedDifference(float a, float b, float min, float max)
    {
        float range = Mathf.Max(0.0001f, max - min);
        return Mathf.Clamp01(Mathf.Abs(a - b) / range);
    }
}
