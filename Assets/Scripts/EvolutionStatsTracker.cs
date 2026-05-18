using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class EvolutionStatsTracker : MonoBehaviour
{
    [Header("Current Generation Stats")]
    public int CurrentGeneration;
    public int CurrentPopulation;
    public int OffspringPoolCount;

    public float AverageFitness;
    public float AverageSpeed;
    public float AverageVisionRange;
    public float AverageBodySize;
    public float AverageMutationRate;
    public float AverageFoodEaten;
    public float AverageSurvivalTime;
    public float BehaviourDiversity;

    [Header("Grouped Behaviour Counts")]
    public int BalancedCount;
    public int GrazerCount;
    public int SprinterCount;
    public int ScoutCount;
    public int SchoolingCount;
    public int SkittishCount;
    public int AggressiveCount;
    public int HeavyCount;
    public string DominantBehaviourGroup;
    public string BehaviourGroupSummary;

    [Header("Grouped Trait Averages")]
    public float AverageHungerDrive;
    public float AverageAggression;
    public float AverageRiskTolerance;
    public float AverageGroupingChance;
    public float AverageThreatRange;

    [Header("Diversity Breakdown")]
    public float MovementDiversity;
    public float FeedingDiversity;
    public float TraitDiversity;
    public float BehaviourTypeDiversity;

    [Header("Logging")]
    public bool WriteCsvLog = true;
    public string CsvFileName = "IRP_EvolutionStats.csv";

    private string csvPath;

    private void Start()
    {
        csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);

        if (WriteCsvLog && !File.Exists(csvPath))
        {
            File.WriteAllText(csvPath,
                "Generation,Population,Offspring,AverageFitness,AverageSpeed,AverageVisionRange,AverageBodySize,AverageMutationRate,AverageFoodEaten,AverageSurvivalTime,BehaviourDiversity,MovementDiversity,FeedingDiversity,TraitDiversity,BehaviourTypeDiversity,DominantGroup,Balanced,Grazer,Sprinter,Scout,Schooling,Skittish,Aggressive,Heavy,AverageHungerDrive,AverageAggression,AverageRiskTolerance,AverageGroupingChance,AverageThreatRange\n");
        }
    }

    public void RecordGeneration(int generation, List<EvolutionCandidate> evaluatedCandidates, int population, int offspringCount)
    {
        CurrentGeneration = generation;
        CurrentPopulation = population;
        OffspringPoolCount = offspringCount;
        ResetGroupedCounts();

        if (evaluatedCandidates == null || evaluatedCandidates.Count == 0)
        {
            ClearAverages();
            return;
        }

        float totalFitness = 0f;
        float totalSpeed = 0f;
        float totalVision = 0f;
        float totalSize = 0f;
        float totalMutation = 0f;
        float totalFood = 0f;
        float totalSurvival = 0f;
        float totalHunger = 0f;
        float totalAggression = 0f;
        float totalRisk = 0f;
        float totalGrouping = 0f;
        float totalThreat = 0f;

        List<Vector2> descriptors = new List<Vector2>();
        List<EvolutionGenome> genomes = new List<EvolutionGenome>();

        for (int i = 0; i < evaluatedCandidates.Count; i++)
        {
            EvolutionCandidate candidate = evaluatedCandidates[i];

            if (candidate == null || candidate.Genome == null)
            {
                continue;
            }

            EvolutionGenome genome = candidate.Genome;
            CreatureBehaviourType type = CreatureDebugTypeUtility.GetBehaviourType(genome);
            AddGroupedCount(type);

            totalFitness += candidate.GetFitness();
            totalSpeed += genome.Speed;
            totalVision += genome.VisionRange;
            totalSize += genome.BodySize;
            totalMutation += genome.MutationRate;
            totalFood += candidate.FoodEaten;
            totalSurvival += candidate.SurvivalTime;
            totalHunger += genome.HungerDrive;
            totalAggression += genome.Aggression;
            totalRisk += genome.RiskTolerance;
            totalGrouping += genome.GroupingChance;
            totalThreat += genome.ThreatRange;

            descriptors.Add(candidate.GetBehaviourDescriptor());
            genomes.Add(genome);
        }

        int count = Mathf.Max(1, genomes.Count);

        AverageFitness = totalFitness / count;
        AverageSpeed = totalSpeed / count;
        AverageVisionRange = totalVision / count;
        AverageBodySize = totalSize / count;
        AverageMutationRate = totalMutation / count;
        AverageFoodEaten = totalFood / count;
        AverageSurvivalTime = totalSurvival / count;
        AverageHungerDrive = totalHunger / count;
        AverageAggression = totalAggression / count;
        AverageRiskTolerance = totalRisk / count;
        AverageGroupingChance = totalGrouping / count;
        AverageThreatRange = totalThreat / count;

        BehaviourDiversity = CalculateDescriptorSpread(descriptors);
        MovementDiversity = CalculateSingleAxisSpread(descriptors, true);
        FeedingDiversity = CalculateSingleAxisSpread(descriptors, false);
        TraitDiversity = CalculateTraitDiversity(genomes);
        BehaviourTypeDiversity = CalculateBehaviourTypeDiversity(count);
        DominantBehaviourGroup = GetDominantGroupName();
        BehaviourGroupSummary = BuildGroupSummary();

        Debug.Log(
            "Generation " + generation +
            " | Pop: " + population +
            " | Offspring: " + offspringCount +
            " | Avg Fit: " + AverageFitness.ToString("F1") +
            " | Dominant: " + DominantBehaviourGroup +
            " | Diversity: " + BehaviourDiversity.ToString("F2") +
            " | Groups: " + BehaviourGroupSummary
        );

        if (WriteCsvLog)
        {
            AppendCsvLine();
        }
    }

    private void ResetGroupedCounts()
    {
        BalancedCount = 0;
        GrazerCount = 0;
        SprinterCount = 0;
        ScoutCount = 0;
        SchoolingCount = 0;
        SkittishCount = 0;
        AggressiveCount = 0;
        HeavyCount = 0;
        DominantBehaviourGroup = "None";
        BehaviourGroupSummary = "None";
    }

    private void ClearAverages()
    {
        AverageFitness = 0f;
        AverageSpeed = 0f;
        AverageVisionRange = 0f;
        AverageBodySize = 0f;
        AverageMutationRate = 0f;
        AverageFoodEaten = 0f;
        AverageSurvivalTime = 0f;
        BehaviourDiversity = 0f;
        AverageHungerDrive = 0f;
        AverageAggression = 0f;
        AverageRiskTolerance = 0f;
        AverageGroupingChance = 0f;
        AverageThreatRange = 0f;
        MovementDiversity = 0f;
        FeedingDiversity = 0f;
        TraitDiversity = 0f;
        BehaviourTypeDiversity = 0f;
    }

    private void AddGroupedCount(CreatureBehaviourType type)
    {
        switch (type)
        {
            case CreatureBehaviourType.Grazer:
                GrazerCount++;
                break;
            case CreatureBehaviourType.Sprinter:
                SprinterCount++;
                break;
            case CreatureBehaviourType.Scout:
                ScoutCount++;
                break;
            case CreatureBehaviourType.Schooling:
                SchoolingCount++;
                break;
            case CreatureBehaviourType.Skittish:
                SkittishCount++;
                break;
            case CreatureBehaviourType.Aggressive:
                AggressiveCount++;
                break;
            case CreatureBehaviourType.Heavy:
                HeavyCount++;
                break;
            default:
                BalancedCount++;
                break;
        }
    }

    private string GetDominantGroupName()
    {
        int highest = BalancedCount;
        string name = "Balanced";

        CheckDominant(GrazerCount, "Grazer", ref highest, ref name);
        CheckDominant(SprinterCount, "Sprinter", ref highest, ref name);
        CheckDominant(ScoutCount, "Scout", ref highest, ref name);
        CheckDominant(SchoolingCount, "Schooling", ref highest, ref name);
        CheckDominant(SkittishCount, "Skittish", ref highest, ref name);
        CheckDominant(AggressiveCount, "Aggressive", ref highest, ref name);
        CheckDominant(HeavyCount, "Heavy", ref highest, ref name);

        return name;
    }

    private void CheckDominant(int count, string name, ref int highest, ref string currentName)
    {
        if (count > highest)
        {
            highest = count;
            currentName = name;
        }
    }

    private string BuildGroupSummary()
    {
        return "Bal " + BalancedCount +
               " | Grazer " + GrazerCount +
               " | Sprint " + SprinterCount +
               " | Scout " + ScoutCount +
               " | School " + SchoolingCount +
               " | Skit " + SkittishCount +
               " | Agg " + AggressiveCount +
               " | Heavy " + HeavyCount;
    }

    private float CalculateDescriptorSpread(List<Vector2> descriptors)
    {
        if (descriptors == null || descriptors.Count <= 1)
        {
            return 0f;
        }

        Vector2 average = Vector2.zero;

        for (int i = 0; i < descriptors.Count; i++)
        {
            average += descriptors[i];
        }

        average /= descriptors.Count;

        float spread = 0f;

        for (int i = 0; i < descriptors.Count; i++)
        {
            spread += Vector2.Distance(average, descriptors[i]);
        }

        return spread / descriptors.Count;
    }

    private float CalculateSingleAxisSpread(List<Vector2> descriptors, bool useX)
    {
        if (descriptors == null || descriptors.Count <= 1)
        {
            return 0f;
        }

        float average = 0f;

        for (int i = 0; i < descriptors.Count; i++)
        {
            average += useX ? descriptors[i].x : descriptors[i].y;
        }

        average /= descriptors.Count;

        float spread = 0f;

        for (int i = 0; i < descriptors.Count; i++)
        {
            float value = useX ? descriptors[i].x : descriptors[i].y;
            spread += Mathf.Abs(value - average);
        }

        return spread / descriptors.Count;
    }

    private float CalculateTraitDiversity(List<EvolutionGenome> genomes)
    {
        if (genomes == null || genomes.Count <= 1)
        {
            return 0f;
        }

        float speed = AverageAbsoluteDeviation(genomes, g => g.Speed / 12f);
        float vision = AverageAbsoluteDeviation(genomes, g => g.VisionRange / 45f);
        float size = AverageAbsoluteDeviation(genomes, g => g.BodySize / 2.5f);
        float hunger = AverageAbsoluteDeviation(genomes, g => g.HungerDrive);
        float aggression = AverageAbsoluteDeviation(genomes, g => g.Aggression);
        float risk = AverageAbsoluteDeviation(genomes, g => g.RiskTolerance);
        float grouping = AverageAbsoluteDeviation(genomes, g => g.GroupingChance);
        float mutation = AverageAbsoluteDeviation(genomes, g => g.MutationRate / 0.35f);

        return (speed + vision + size + hunger + aggression + risk + grouping + mutation) / 8f;
    }

    private float AverageAbsoluteDeviation(List<EvolutionGenome> genomes, System.Func<EvolutionGenome, float> getter)
    {
        float average = 0f;

        for (int i = 0; i < genomes.Count; i++)
        {
            average += getter(genomes[i]);
        }

        average /= genomes.Count;

        float spread = 0f;

        for (int i = 0; i < genomes.Count; i++)
        {
            spread += Mathf.Abs(getter(genomes[i]) - average);
        }

        return spread / genomes.Count;
    }

    private float CalculateBehaviourTypeDiversity(int totalCount)
    {
        if (totalCount <= 0)
        {
            return 0f;
        }

        int activeGroups = 0;
        if (BalancedCount > 0) activeGroups++;
        if (GrazerCount > 0) activeGroups++;
        if (SprinterCount > 0) activeGroups++;
        if (ScoutCount > 0) activeGroups++;
        if (SchoolingCount > 0) activeGroups++;
        if (SkittishCount > 0) activeGroups++;
        if (AggressiveCount > 0) activeGroups++;
        if (HeavyCount > 0) activeGroups++;

        return activeGroups / 8f;
    }

    private void AppendCsvLine()
    {
        StringBuilder line = new StringBuilder();

        line.Append(CurrentGeneration).Append(",");
        line.Append(CurrentPopulation).Append(",");
        line.Append(OffspringPoolCount).Append(",");
        line.Append(AverageFitness.ToString("F3")).Append(",");
        line.Append(AverageSpeed.ToString("F3")).Append(",");
        line.Append(AverageVisionRange.ToString("F3")).Append(",");
        line.Append(AverageBodySize.ToString("F3")).Append(",");
        line.Append(AverageMutationRate.ToString("F4")).Append(",");
        line.Append(AverageFoodEaten.ToString("F3")).Append(",");
        line.Append(AverageSurvivalTime.ToString("F3")).Append(",");
        line.Append(BehaviourDiversity.ToString("F4")).Append(",");
        line.Append(MovementDiversity.ToString("F4")).Append(",");
        line.Append(FeedingDiversity.ToString("F4")).Append(",");
        line.Append(TraitDiversity.ToString("F4")).Append(",");
        line.Append(BehaviourTypeDiversity.ToString("F4")).Append(",");
        line.Append(DominantBehaviourGroup).Append(",");
        line.Append(BalancedCount).Append(",");
        line.Append(GrazerCount).Append(",");
        line.Append(SprinterCount).Append(",");
        line.Append(ScoutCount).Append(",");
        line.Append(SchoolingCount).Append(",");
        line.Append(SkittishCount).Append(",");
        line.Append(AggressiveCount).Append(",");
        line.Append(HeavyCount).Append(",");
        line.Append(AverageHungerDrive.ToString("F3")).Append(",");
        line.Append(AverageAggression.ToString("F3")).Append(",");
        line.Append(AverageRiskTolerance.ToString("F3")).Append(",");
        line.Append(AverageGroupingChance.ToString("F3")).Append(",");
        line.Append(AverageThreatRange.ToString("F3")).Append("\n");

        File.AppendAllText(csvPath, line.ToString());
    }

    public string GetCsvPath()
    {
        return csvPath;
    }
}
