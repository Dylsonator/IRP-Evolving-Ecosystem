using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Small graph-focused CSV exporter.
/// This intentionally writes a narrow, tidy row once per completed generation so it is easy
/// to import into Excel/Sheets for dissertation graphs.
/// </summary>
public class IRPGraphReadyMetricsExporter : MonoBehaviour
{
    public EvolutionEcosystemManager Manager;
    public string RunId = "UnlabelledRun";
    public string ExperimentPhase = "Uncontrolled";
    public bool WriteCsv = true;
    public string CsvFileName = "IRP_GraphReadyGenerationSummary.csv";

    private string csvPath;

    private void Awake()
    {
        ResolveReferences();
        csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);
        EnsureHeader();
    }

    private void ResolveReferences()
    {
        if (Manager == null)
        {
            Manager = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance : FindFirstObjectByType<EvolutionEcosystemManager>();
        }

        IRPExperimentController experiment = GetComponent<IRPExperimentController>();
        if (experiment != null)
        {
            RunId = experiment.CurrentRunId;
            ExperimentPhase = experiment.CurrentPhase.ToString();
        }
    }

    public void RecordCompletedGenerationSnapshot(string reason, int generation, List<EvolutionCandidate> evaluatedCandidates, int populationAtEnd, int offspringAtEnd)
    {
        if (!WriteCsv || evaluatedCandidates == null || evaluatedCandidates.Count == 0)
        {
            return;
        }

        ResolveReferences();
        EnsureHeader();

        int count = 0;
        int predators = 0;
        int grazers = 0;
        int scavengers = 0;
        int omnivores = 0;
        int defensive = 0;
        int eggGuardians = 0;
        int survivors = 0;
        int reproduced = 0;
        float totalFitness = 0f;
        float totalSurvival = 0f;
        float totalPlant = 0f;
        float totalMeat = 0f;
        float totalCarrion = 0f;
        float totalPreyBites = 0f;
        float totalPreyKills = 0f;
        float totalEggsLaid = 0f;
        float totalEggsHatched = 0f;
        float totalBrainHidden = 0f;
        float totalBrainConnections = 0f;
        float totalBrainHunt = 0f;
        float totalBrainFlee = 0f;
        float totalBrainSprint = 0f;
        float totalSurvivalEmergency = 0f;
        Dictionary<string, int> coreNiches = new Dictionary<string, int>();
        List<EvolutionCandidate> valid = new List<EvolutionCandidate>();

        for (int i = 0; i < evaluatedCandidates.Count; i++)
        {
            EvolutionCandidate c = evaluatedCandidates[i];
            if (c == null || c.Genome == null)
            {
                continue;
            }

            count++;
            valid.Add(c);
            EvolutionGenome g = c.Genome;
            totalFitness += c.GetFitness();
            totalSurvival += c.SurvivalTime;
            totalPlant += g.PlantDiet;
            totalMeat += g.MeatDiet;
            totalCarrion += g.CarrionDiet;
            totalPreyBites += c.PreyBites;
            totalPreyKills += c.PreyKills;
            totalEggsLaid += c.EggsLaid;
            totalEggsHatched += c.EggsHatched;
            totalBrainHunt += c.AverageBrainHuntBias;
            totalBrainFlee += c.AverageBrainFleeBias;
            totalBrainSprint += c.AverageBrainSprintBias;
            totalSurvivalEmergency += c.SurvivalEmergencyTime;

            if (g.Brain != null)
            {
                totalBrainHidden += g.Brain.HiddenCount;
                totalBrainConnections += g.Brain.GetConnectionCount();
            }

            if (c.FinalHealth > 0f)
            {
                survivors++;
            }

            if (c.ReproductionCount > 0 || c.EggsLaid > 0 || c.EggsHatched > 0)
            {
                reproduced++;
            }

            string core = EvolutionNicheUtility.BuildCoreNicheKey(c);
            if (!coreNiches.ContainsKey(core))
            {
                coreNiches[core] = 0;
            }
            coreNiches[core]++;

            CreatureBehaviourType type = CreatureDebugTypeUtility.GetBehaviourType(g);
            if (type == CreatureBehaviourType.Predator || type == CreatureBehaviourType.StreamlinedHunter) predators++;
            if (type == CreatureBehaviourType.Grazer || type == CreatureBehaviourType.ArmouredGrazer || type == CreatureBehaviourType.DefensiveHerbivore) grazers++;
            if (type == CreatureBehaviourType.Scavenger || type == CreatureBehaviourType.SensorScavenger) scavengers++;
            if (type == CreatureBehaviourType.Omnivore) omnivores++;
            if (g.Armour >= 0.55f || g.SpikeSize >= 0.8f || g.DangerFactor >= 0.75f) defensive++;
            if (g.EggProtection >= 0.55f && g.NestingDrive >= 0.45f) eggGuardians++;
        }

        if (count <= 0)
        {
            return;
        }

        int dominantCount = 0;
        string dominantNiche = "None";
        foreach (KeyValuePair<string, int> pair in coreNiches)
        {
            if (pair.Value > dominantCount)
            {
                dominantCount = pair.Value;
                dominantNiche = pair.Key;
            }
        }

        float inv = 1f / count;
        float diversity = CalculateShannon(coreNiches, count);
        float featureSpread = EvolutionBehaviourDescriptorUtility.CalculateFeatureSpread(valid);
        float stability = Manager != null && Manager.FixedPopulationSize > 0 ? Mathf.Clamp01(populationAtEnd / (float)Manager.FixedPopulationSize) : 0f;
        float dominantFraction = dominantCount / (float)count;
        float reproductionRate = reproduced / (float)count;
        float survivorFraction = survivors / (float)count;

        StringBuilder line = new StringBuilder();
        line.Append(Safe(RunId)).Append(',');
        line.Append(Safe(ExperimentPhase)).Append(',');
        line.Append(generation).Append(',');
        line.Append(populationAtEnd).Append(',');
        line.Append(offspringAtEnd).Append(',');
        line.Append(coreNiches.Count).Append(',');
        line.Append(Safe(dominantNiche)).Append(',');
        line.Append(dominantFraction.ToString("F3")).Append(',');
        line.Append(diversity.ToString("F3")).Append(',');
        line.Append(featureSpread.ToString("F3")).Append(',');
        line.Append(stability.ToString("F3")).Append(',');
        line.Append(survivorFraction.ToString("F3")).Append(',');
        line.Append(reproductionRate.ToString("F3")).Append(',');
        line.Append((totalFitness * inv).ToString("F3")).Append(',');
        line.Append((totalSurvival * inv).ToString("F3")).Append(',');
        line.Append((totalPlant * inv).ToString("F3")).Append(',');
        line.Append((totalMeat * inv).ToString("F3")).Append(',');
        line.Append((totalCarrion * inv).ToString("F3")).Append(',');
        line.Append((totalPreyBites * inv).ToString("F3")).Append(',');
        line.Append((totalPreyKills * inv).ToString("F3")).Append(',');
        line.Append((totalEggsLaid * inv).ToString("F3")).Append(',');
        line.Append((totalEggsHatched * inv).ToString("F3")).Append(',');
        line.Append((totalBrainHidden * inv).ToString("F3")).Append(',');
        line.Append((totalBrainConnections * inv).ToString("F3")).Append(',');
        line.Append((totalBrainHunt * inv).ToString("F3")).Append(',');
        line.Append((totalBrainFlee * inv).ToString("F3")).Append(',');
        line.Append((totalBrainSprint * inv).ToString("F3")).Append(',');
        line.Append((totalSurvivalEmergency * inv).ToString("F3")).Append(',');
        line.Append(grazers).Append(',');
        line.Append(predators).Append(',');
        line.Append(scavengers).Append(',');
        line.Append(omnivores).Append(',');
        line.Append(defensive).Append(',');
        line.Append(eggGuardians).Append(',');
        line.Append(Safe(reason));
        File.AppendAllText(csvPath, line.ToString() + "\n");
    }

    private float CalculateShannon(Dictionary<string, int> counts, int population)
    {
        if (counts == null || counts.Count == 0 || population <= 0)
        {
            return 0f;
        }

        float shannon = 0f;
        foreach (KeyValuePair<string, int> pair in counts)
        {
            float p = pair.Value / (float)population;
            if (p > 0f)
            {
                shannon -= p * Mathf.Log(p);
            }
        }
        return shannon;
    }

    private void EnsureHeader()
    {
        if (!WriteCsv)
        {
            return;
        }

        if (string.IsNullOrEmpty(csvPath))
        {
            csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);
        }

        if (!File.Exists(csvPath))
        {
            File.WriteAllText(csvPath,
                "RunId,ExperimentPhase,Generation,Population,OffspringPool,CoreNicheCount,DominantCoreNiche,DominantCoreFraction,CoreNicheShannon,FeatureSpread,PopulationStability,SurvivorFraction,ReproductionFraction,AverageFitness,AverageSurvival,AveragePlantDiet,AverageMeatDiet,AverageCarrionDiet,AveragePreyBites,AveragePreyKills,AverageEggsLaid,AverageEggsHatched,AverageBrainHidden,AverageBrainConnections,AverageBrainHuntBias,AverageBrainFleeBias,AverageBrainSprintBias,AverageSurvivalEmergencyTime,Grazers,Predators,Scavengers,Omnivores,Defensive,EggGuardians,Reason\n");
        }
    }

    private string Safe(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');
    }
}
