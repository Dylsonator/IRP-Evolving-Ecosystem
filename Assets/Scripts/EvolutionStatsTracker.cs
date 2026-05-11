using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class EvolutionStatsTracker : MonoBehaviour
{
    [Header("Sampling")]
    public float SampleInterval = 5f;
    public bool WriteCsvLog = true;
    public string CsvFileName = "irp_evolution_continuous_stats.csv";

    [Header("Runtime Counters")]
    public int SampleIndex;
    public int TotalBirths;
    public int TotalDeaths;
    public int TotalKills;
    public int TotalFoodEaten;
    public int TotalPlantsEaten;
    public int TotalMeatEaten;
    public int TotalCarrionEaten;
    public int TotalFoodRotted;
    public int TotalExtinctionEvents;

    [Header("Current Snapshot")]
    public int CurrentPopulation;
    public int CurrentSpeciesCount;
    public int CurrentPlantFood;
    public int CurrentFreshMeat;
    public int CurrentRottenMeat;

    [Header("Average Traits")]
    public float AverageFitness;
    public float AverageBodySize;
    public float AverageSpeed;
    public float AverageVision;
    public float AverageJawSize;
    public float AverageTailSize;
    public float AverageFinSize;
    public float AverageSensorSize;
    public float AverageAggression;
    public float AverageGrouping;
    public float AveragePlantDiet;
    public float AverageMeatDiet;
    public float AverageCarrionDiet;
    public float SpeciesDiversity;

    private readonly Dictionary<string, int> speciesCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, string> speciesNames = new Dictionary<string, string>();
    private float sampleTimer;
    private string csvPath;

    private void Start()
    {
        csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);

        if (WriteCsvLog)
        {
            CreateCsv();
        }
    }

    private void Update()
    {
        sampleTimer += Time.deltaTime;

        if (sampleTimer >= SampleInterval)
        {
            sampleTimer = 0f;
            SampleNow();
        }
    }

    public void SampleNow()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        SampleIndex++;
        speciesCounts.Clear();
        speciesNames.Clear();

        IReadOnlyList<MarineCreatureAgent> creatures = EvolutionEcosystemManager.Instance.ActiveCreatures;
        IReadOnlyList<FoodSource> foodSources = EvolutionEcosystemManager.Instance.ActiveFood;

        CurrentPopulation = creatures.Count;
        CountFood(foodSources);

        if (creatures.Count == 0)
        {
            ClearAverages();
            if (WriteCsvLog) AppendCsvLine();
            return;
        }

        float totalFitness = 0f;
        float totalBody = 0f;
        float totalSpeed = 0f;
        float totalVision = 0f;
        float totalJaw = 0f;
        float totalTail = 0f;
        float totalFin = 0f;
        float totalSensor = 0f;
        float totalAggression = 0f;
        float totalGrouping = 0f;
        float totalPlantDiet = 0f;
        float totalMeatDiet = 0f;
        float totalCarrionDiet = 0f;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent creature = creatures[i];
            if (creature == null || creature.Candidate == null || creature.Genome == null)
            {
                continue;
            }

            EvolutionGenome genome = creature.Genome;

            totalFitness += creature.Candidate.GetFitness();
            totalBody += genome.BodySize;
            totalSpeed += genome.GetEffectiveSpeed();
            totalVision += genome.GetVisionRange();
            totalJaw += genome.JawSize;
            totalTail += genome.TailSize;
            totalFin += genome.FinSize;
            totalSensor += genome.SensorSize;
            totalAggression += genome.Aggression;
            totalGrouping += genome.GroupingChance;
            totalPlantDiet += genome.PlantDiet;
            totalMeatDiet += genome.MeatDiet;
            totalCarrionDiet += genome.CarrionDiet;

            string key = SpeciesUtility.GetSpeciesKey(genome);
            if (!speciesCounts.ContainsKey(key))
            {
                speciesCounts.Add(key, 0);
                speciesNames.Add(key, SpeciesUtility.GetDisplayName(genome));
            }
            speciesCounts[key]++;
        }

        float count = Mathf.Max(1, creatures.Count);

        AverageFitness = totalFitness / count;
        AverageBodySize = totalBody / count;
        AverageSpeed = totalSpeed / count;
        AverageVision = totalVision / count;
        AverageJawSize = totalJaw / count;
        AverageTailSize = totalTail / count;
        AverageFinSize = totalFin / count;
        AverageSensorSize = totalSensor / count;
        AverageAggression = totalAggression / count;
        AverageGrouping = totalGrouping / count;
        AveragePlantDiet = totalPlantDiet / count;
        AverageMeatDiet = totalMeatDiet / count;
        AverageCarrionDiet = totalCarrionDiet / count;

        CurrentSpeciesCount = speciesCounts.Count;
        SpeciesDiversity = CalculateShannonDiversity(speciesCounts, creatures.Count);

        Debug.Log(
            "Sample " + SampleIndex +
            " | Pop: " + CurrentPopulation +
            " | Species: " + CurrentSpeciesCount +
            " | Avg Aggro: " + AverageAggression.ToString("F2") +
            " | Avg Group: " + AverageGrouping.ToString("F2") +
            " | Diversity: " + SpeciesDiversity.ToString("F2")
        );

        if (WriteCsvLog)
        {
            AppendCsvLine();
        }
    }

    public void RegisterBirth()
    {
        TotalBirths++;
    }

    public void RegisterDeath(bool killedByCreature)
    {
        TotalDeaths++;

        if (killedByCreature)
        {
            TotalKills++;
        }
    }

    public void RegisterFoodEaten(EcosystemFoodType foodType)
    {
        TotalFoodEaten++;

        if (foodType == EcosystemFoodType.Plant) TotalPlantsEaten++;
        else if (foodType == EcosystemFoodType.FreshMeat) TotalMeatEaten++;
        else TotalCarrionEaten++;
    }

    public void RegisterFoodRotted()
    {
        TotalFoodRotted++;
    }

    public void RegisterExtinctionEvent()
    {
        TotalExtinctionEvents++;
    }

    public string GetTopSpeciesText(int maxEntries)
    {
        if (speciesCounts.Count == 0)
        {
            return "No species sampled yet.";
        }

        List<KeyValuePair<string, int>> entries = new List<KeyValuePair<string, int>>(speciesCounts);
        entries.Sort((a, b) => b.Value.CompareTo(a.Value));

        StringBuilder builder = new StringBuilder();
        int amount = Mathf.Min(maxEntries, entries.Count);

        for (int i = 0; i < amount; i++)
        {
            string key = entries[i].Key;
            string name = speciesNames.ContainsKey(key) ? speciesNames[key] : key;
            builder.Append(i + 1).Append(". ").Append(name).Append(" (").Append(entries[i].Value).Append(")");

            if (i < amount - 1)
            {
                builder.Append("\n");
            }
        }

        return builder.ToString();
    }

    public string GetCsvPath()
    {
        return csvPath;
    }

    private void CountFood(IReadOnlyList<FoodSource> foodSources)
    {
        CurrentPlantFood = 0;
        CurrentFreshMeat = 0;
        CurrentRottenMeat = 0;

        for (int i = 0; i < foodSources.Count; i++)
        {
            FoodSource food = foodSources[i];
            if (food == null || food.IsConsumed)
            {
                continue;
            }

            if (food.FoodType == EcosystemFoodType.Plant) CurrentPlantFood++;
            else if (food.FoodType == EcosystemFoodType.FreshMeat) CurrentFreshMeat++;
            else CurrentRottenMeat++;
        }
    }

    private float CalculateShannonDiversity(Dictionary<string, int> counts, int total)
    {
        if (counts.Count <= 0 || total <= 0)
        {
            return 0f;
        }

        float diversity = 0f;

        foreach (KeyValuePair<string, int> pair in counts)
        {
            float p = pair.Value / (float)total;
            if (p > 0f)
            {
                diversity -= p * Mathf.Log(p);
            }
        }

        return diversity;
    }

    private void ClearAverages()
    {
        AverageFitness = 0f;
        AverageBodySize = 0f;
        AverageSpeed = 0f;
        AverageVision = 0f;
        AverageJawSize = 0f;
        AverageTailSize = 0f;
        AverageFinSize = 0f;
        AverageSensorSize = 0f;
        AverageAggression = 0f;
        AverageGrouping = 0f;
        AveragePlantDiet = 0f;
        AverageMeatDiet = 0f;
        AverageCarrionDiet = 0f;
        CurrentSpeciesCount = 0;
        SpeciesDiversity = 0f;
    }

    private void CreateCsv()
    {
        StringBuilder header = new StringBuilder();
        header.Append("Sample,Time,Population,SpeciesCount,SpeciesDiversity,");
        header.Append("PlantFood,FreshMeat,RottenMeat,");
        header.Append("TotalBirths,TotalDeaths,TotalKills,TotalFoodEaten,PlantsEaten,MeatEaten,CarrionEaten,FoodRotted,ExtinctionEvents,");
        header.Append("AverageFitness,AverageBodySize,AverageSpeed,AverageVision,AverageJawSize,AverageTailSize,AverageFinSize,AverageSensorSize,");
        header.Append("AverageAggression,AverageGrouping,AveragePlantDiet,AverageMeatDiet,AverageCarrionDiet\n");

        File.WriteAllText(csvPath, header.ToString());
        Debug.Log("IRP CSV log created at: " + csvPath);
    }

    private void AppendCsvLine()
    {
        if (string.IsNullOrEmpty(csvPath))
        {
            return;
        }

        float time = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance.Runtime : Time.time;

        StringBuilder line = new StringBuilder();
        line.Append(SampleIndex).Append(",");
        line.Append(time.ToString("F2")).Append(",");
        line.Append(CurrentPopulation).Append(",");
        line.Append(CurrentSpeciesCount).Append(",");
        line.Append(SpeciesDiversity.ToString("F4")).Append(",");
        line.Append(CurrentPlantFood).Append(",");
        line.Append(CurrentFreshMeat).Append(",");
        line.Append(CurrentRottenMeat).Append(",");
        line.Append(TotalBirths).Append(",");
        line.Append(TotalDeaths).Append(",");
        line.Append(TotalKills).Append(",");
        line.Append(TotalFoodEaten).Append(",");
        line.Append(TotalPlantsEaten).Append(",");
        line.Append(TotalMeatEaten).Append(",");
        line.Append(TotalCarrionEaten).Append(",");
        line.Append(TotalFoodRotted).Append(",");
        line.Append(TotalExtinctionEvents).Append(",");
        line.Append(AverageFitness.ToString("F3")).Append(",");
        line.Append(AverageBodySize.ToString("F3")).Append(",");
        line.Append(AverageSpeed.ToString("F3")).Append(",");
        line.Append(AverageVision.ToString("F3")).Append(",");
        line.Append(AverageJawSize.ToString("F3")).Append(",");
        line.Append(AverageTailSize.ToString("F3")).Append(",");
        line.Append(AverageFinSize.ToString("F3")).Append(",");
        line.Append(AverageSensorSize.ToString("F3")).Append(",");
        line.Append(AverageAggression.ToString("F3")).Append(",");
        line.Append(AverageGrouping.ToString("F3")).Append(",");
        line.Append(AveragePlantDiet.ToString("F3")).Append(",");
        line.Append(AverageMeatDiet.ToString("F3")).Append(",");
        line.Append(AverageCarrionDiet.ToString("F3")).Append("\n");

        File.AppendAllText(csvPath, line.ToString());
    }
}
