using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Simple MAP-Elites style behaviour archive for evidence.
/// It records the best observed creature per behaviour cell, without controlling selection directly.
/// This gives a clear CSV showing which niches were discovered across the run.
/// </summary>
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
    public bool ExportEveryGeneration = false;

    private readonly Dictionary<string, ArchiveEntry> archive = new Dictionary<string, ArchiveEntry>();
    private string csvPath;
    private int lastExportedGeneration = -1;

    private void Awake()
    {
        if (Manager == null)
        {
            Manager = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance : FindFirstObjectByType<EvolutionEcosystemManager>();
        }

        csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);
        EnsureHeader();
    }

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

    [ContextMenu("IRP/Record Current Population Into Archive")]
    public void RecordCurrentPopulationContext()
    {
        RecordCurrentPopulation("Manual");
        ExportArchiveSnapshot("Manual");
    }

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
        archive[key] = entry;
    }

    [ContextMenu("IRP/Export Archive Snapshot")]
    public void ExportArchiveSnapshotContext()
    {
        ExportArchiveSnapshot("Manual");
    }

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
            line.Append(e.BrainConnections.ToString("F0"));
            File.AppendAllText(csvPath, line.ToString() + "\n");
        }
    }

    [ContextMenu("IRP/Clear Archive")]
    public void ClearArchive()
    {
        archive.Clear();
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
            File.WriteAllText(csvPath, "RunId,Generation,Reason,CellKey,CoreNiche,DisplayName,FirstSeenGeneration,LastImprovedGeneration,BestFitness,PlantDiet,MeatDiet,CarrionDiet,Speed,BodySize,Aggression,Grouping,BrainHidden,BrainConnections\n");
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
