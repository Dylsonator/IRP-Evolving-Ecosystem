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
                "Generation,Population,Offspring,AverageFitness,AverageSpeed,AverageVisionRange,AverageBodySize,AverageMutationRate,AverageFoodEaten,AverageSurvivalTime,BehaviourDiversity\n");
        }
    }

    public void RecordGeneration(int generation, List<EvolutionCandidate> evaluatedCandidates, int population, int offspringCount)
    {
        CurrentGeneration = generation;
        CurrentPopulation = population;
        OffspringPoolCount = offspringCount;

        if (evaluatedCandidates == null || evaluatedCandidates.Count == 0)
        {
            AverageFitness = 0f;
            AverageSpeed = 0f;
            AverageVisionRange = 0f;
            AverageBodySize = 0f;
            AverageMutationRate = 0f;
            AverageFoodEaten = 0f;
            AverageSurvivalTime = 0f;
            BehaviourDiversity = 0f;
            return;
        }

        float totalFitness = 0f;
        float totalSpeed = 0f;
        float totalVision = 0f;
        float totalSize = 0f;
        float totalMutation = 0f;
        float totalFood = 0f;
        float totalSurvival = 0f;

        List<Vector2> descriptors = new List<Vector2>();

        for (int i = 0; i < evaluatedCandidates.Count; i++)
        {
            EvolutionCandidate candidate = evaluatedCandidates[i];

            if (candidate == null || candidate.Genome == null)
            {
                continue;
            }

            totalFitness += candidate.GetFitness();
            totalSpeed += candidate.Genome.Speed;
            totalVision += candidate.Genome.VisionRange;
            totalSize += candidate.Genome.BodySize;
            totalMutation += candidate.Genome.MutationRate;
            totalFood += candidate.FoodEaten;
            totalSurvival += candidate.SurvivalTime;
            descriptors.Add(candidate.GetBehaviourDescriptor());
        }

        int count = Mathf.Max(1, evaluatedCandidates.Count);

        AverageFitness = totalFitness / count;
        AverageSpeed = totalSpeed / count;
        AverageVisionRange = totalVision / count;
        AverageBodySize = totalSize / count;
        AverageMutationRate = totalMutation / count;
        AverageFoodEaten = totalFood / count;
        AverageSurvivalTime = totalSurvival / count;
        BehaviourDiversity = CalculateDescriptorSpread(descriptors);

        Debug.Log(
            "Generation " + generation +
            " | Pop: " + population +
            " | Offspring: " + offspringCount +
            " | Avg Fit: " + AverageFitness.ToString("F1") +
            " | Avg Speed: " + AverageSpeed.ToString("F2") +
            " | Diversity: " + BehaviourDiversity.ToString("F2")
        );

        if (WriteCsvLog)
        {
            AppendCsvLine();
        }
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
        line.Append(BehaviourDiversity.ToString("F4")).Append("\n");

        File.AppendAllText(csvPath, line.ToString());
    }

    public string GetCsvPath()
    {
        return csvPath;
    }
}
