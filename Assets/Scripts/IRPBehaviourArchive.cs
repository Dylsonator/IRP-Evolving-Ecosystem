using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// MAP-Elites style archive for showing which behaviour niches were found.
// MAP-Elites style archive storing good fish from different behaviour cells.
public class IRPBehaviourArchive : MonoBehaviour
{
    private class ArchiveEntry
    {
        public string Key;
        public string CoreNiche;
        public string DisplayName;
        public int FirstSeenGeneration;
        public int LastImprovedGeneration;
        public float BestFitness;
        public float PlantDiet;
        public float MeatDiet;
        public float CarrionDiet;
        public float Speed;
        public float BodySize;
        public float Aggression;
        public float Grouping;
        public float BrainHidden;
        public float BrainConnections;
        public float BrainMemoryDecay;
        public float SurvivalTime;
        public float PreyKills;
        public float ReproductionCount;
        public string MorphologySummary;
        public string PrimaryBehaviourSummary;
    }

    [Header("References")]
    public EvolutionEcosystemManager Manager;

    [Header("Archive")]
    public bool RecordArchive = true;
    public int BehaviourBins = 5;
    public int ArchivedCellCount { get { return archive.Count; } }
    public string RunId = "UnlabelledRun";

    [Header("Logging")]
    public bool WriteCsv = true;
    public string CsvFileName = "IRP_BehaviourArchive.csv";
    public bool ExportEveryGeneration = true;

    private readonly Dictionary<string, ArchiveEntry> archive = new Dictionary<string, ArchiveEntry>();
    private string csvPath;
    private int lastExportedGeneration = -1;

    // Sets up cached references and safe starting values before the sim runs
    private void Awake()
    {
        if (Manager == null)
        {
            Manager = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance : FindFirstObjectByType<EvolutionEcosystemManager>();
        }

        csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);
        EnsureHeader();
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        if (!RecordArchive || Manager == null || !ExportEveryGeneration)
        {
            return;
        }

        if (Manager.CurrentGeneration != lastExportedGeneration)
        {
            RecordCurrentPopulation("GenerationChanged");
            ExportArchiveSnapshot("GenerationChanged");
            lastExportedGeneration = Manager.CurrentGeneration;
        }
    }

    // Records current population context.
    [ContextMenu("IRP/Record Current Population Into Archive")]
    // Records current population context so it can be checked later
    public void RecordCurrentPopulationContext()
    {
        RecordCurrentPopulation("Manual");
        ExportArchiveSnapshot("Manual");
    }

    // Records current population so it can be checked later
    public void RecordCurrentPopulation(string reason)
    {
        if (!RecordArchive || Manager == null)
        {
            return;
        }

        List<MarineCreatureAgent> creatures = Manager.GetActiveCreatures();
        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent creature = creatures[i];
            if (creature == null || creature.Candidate == null || creature.Candidate.Genome == null)
            {
                continue;
            }

            RecordCandidate(creature.Candidate);
        }
    }

    // Records candidate so it can be checked later
    public void RecordCandidate(EvolutionCandidate candidate)
    {
        if (candidate == null || candidate.Genome == null)
        {
            return;
        }

        string key = EvolutionNicheUtility.BuildSelectionKey(candidate, BehaviourBins);
        float fitness = candidate.GetFitness();

        if (archive.TryGetValue(key, out ArchiveEntry existing) && existing.BestFitness >= fitness)
        {
            return;
        }

        EvolutionGenome g = candidate.Genome;
        ArchiveEntry entry = existing ?? new ArchiveEntry();
        entry.Key = key;
        entry.CoreNiche = EvolutionNicheUtility.BuildCoreNicheKey(candidate);
        entry.DisplayName = candidate.DisplayName;
        if (entry.FirstSeenGeneration <= 0)
        {
            entry.FirstSeenGeneration = Mathf.Max(1, candidate.GenerationBorn);
        }
        entry.LastImprovedGeneration = Manager != null ? Manager.CurrentGeneration : candidate.GenerationBorn;
        entry.BestFitness = fitness;
        entry.PlantDiet = g.PlantDiet;
        entry.MeatDiet = g.MeatDiet;
        entry.CarrionDiet = g.CarrionDiet;
        entry.Speed = g.Speed;
        entry.BodySize = g.BodySize;
        entry.Aggression = g.Aggression;
        entry.Grouping = g.GroupingChance;
        entry.BrainHidden = g.Brain != null ? g.Brain.HiddenCount : 0;
        entry.BrainConnections = g.Brain != null ? g.Brain.GetConnectionCount() : 0;
        entry.BrainMemoryDecay = g.BrainMemoryDecay;
        entry.SurvivalTime = candidate.SurvivalTime;
        entry.PreyKills = candidate.PreyKills;
        entry.ReproductionCount = candidate.ReproductionCount + candidate.EggsLaid + candidate.EggsHatched;
        entry.MorphologySummary = CreatureDebugTypeUtility.GetMorphologyName(g);
        entry.PrimaryBehaviourSummary = BuildPrimaryBehaviourSummary(candidate);
        archive[key] = entry;
    }

    [ContextMenu("IRP/Export Archive Snapshot")]
    // Handles export archive snapshot context
    public void ExportArchiveSnapshotContext()
    {
        ExportArchiveSnapshot("Manual");
    }

    // Handles export archive snapshot
    public void ExportArchiveSnapshot(string reason)
    {
        if (!WriteCsv)
        {
            return;
        }

        EnsureHeader();
        foreach (KeyValuePair<string, ArchiveEntry> pair in archive)
        {
            ArchiveEntry e = pair.Value;
            StringBuilder line = new StringBuilder();
            line.Append(Safe(RunId)).Append(',');
            line.Append(Manager != null ? Manager.CurrentGeneration : 0).Append(',');
            line.Append(Safe(reason)).Append(',');
            line.Append(Safe(e.Key)).Append(',');
            line.Append(Safe(e.CoreNiche)).Append(',');
            line.Append(Safe(e.DisplayName)).Append(',');
            line.Append(e.FirstSeenGeneration).Append(',');
            line.Append(e.LastImprovedGeneration).Append(',');
            line.Append(e.BestFitness.ToString("F3")).Append(',');
            line.Append(e.PlantDiet.ToString("F3")).Append(',');
            line.Append(e.MeatDiet.ToString("F3")).Append(',');
            line.Append(e.CarrionDiet.ToString("F3")).Append(',');
            line.Append(e.Speed.ToString("F3")).Append(',');
            line.Append(e.BodySize.ToString("F3")).Append(',');
            line.Append(e.Aggression.ToString("F3")).Append(',');
            line.Append(e.Grouping.ToString("F3")).Append(',');
            line.Append(e.BrainHidden.ToString("F0")).Append(',');
            line.Append(e.BrainConnections.ToString("F0")).Append(',');
            line.Append(e.BrainMemoryDecay.ToString("F3")).Append(',');
            line.Append(e.SurvivalTime.ToString("F2")).Append(',');
            line.Append(e.PreyKills.ToString("F0")).Append(',');
            line.Append(e.ReproductionCount.ToString("F0")).Append(',');
            line.Append(Safe(e.MorphologySummary)).Append(',');
            line.Append(Safe(e.PrimaryBehaviourSummary));
            File.AppendAllText(csvPath, line.ToString() + "\n");
        }
    }

    // Clears archive.
    [ContextMenu("IRP/Clear Archive")]
    // Clears archive ready for fresh data
    public void ClearArchive()
    {
        archive.Clear();
    }

    // Builds the primary behaviour summary data from the current values
    private string BuildPrimaryBehaviourSummary(EvolutionCandidate candidate)
    {
        if (candidate == null)
        {
            return "Unknown";
        }

        string best = "Resting";
        float value = candidate.RestingTime;
        CheckBehaviour(candidate.ExploringTime, "Exploring", ref best, ref value);
        CheckBehaviour(candidate.SchoolingTime, "Schooling", ref best, ref value);
        CheckBehaviour(candidate.ForagingTime, "Foraging", ref best, ref value);
        CheckBehaviour(candidate.FeedingTime, "Feeding", ref best, ref value);
        CheckBehaviour(candidate.MateSeekingTime, "Mating/Nesting", ref best, ref value);
        CheckBehaviour(candidate.HuntingTime, "Hunting/Ambush", ref best, ref value);
        CheckBehaviour(candidate.FleeingTime, "Fleeing/Mobbing", ref best, ref value);
        CheckBehaviour(candidate.RecoveryTime, "Recovering", ref best, ref value);
        return best;
    }

    // Handles check behaviour
    private void CheckBehaviour(float candidateTime, string label, ref string bestLabel, ref float bestTime)
    {
        if (candidateTime > bestTime)
        {
            bestTime = candidateTime;
            bestLabel = label;
        }
    }

    // Creates the CSV header if the file is new
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
            File.WriteAllText(csvPath, "RunId,Generation,Reason,CellKey,CoreNiche,DisplayName,FirstSeenGeneration,LastImprovedGeneration,BestFitness,PlantDiet,MeatDiet,CarrionDiet,Speed,BodySize,Aggression,Grouping,BrainHidden,BrainConnections,BrainMemoryDecay,SurvivalTime,PreyKills,ReproductionCount,MorphologySummary,PrimaryBehaviourSummary\n");
        }
    }

    // Cleans text so commas and nulls do not break CSV output
    private string Safe(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');
    }
}
