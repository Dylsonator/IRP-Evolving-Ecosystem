using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Lightweight novelty/behaviour archive used as the natural anti-convergence mechanism.
/// Instead of injecting hard-coded missing roles, candidates receive selection pressure from:
/// 1) novelty compared with previous successful behaviours,
/// 2) fitness sharing against behaviourally similar candidates.
/// </summary>
public class EvolutionDiversityArchive : MonoBehaviour
{
    public EvolutionEcosystemManager Manager;

    [Header("Archive")]
    public bool Enabled = true;
    public int MaxArchiveEntries = 450;
    public int MaxEntriesAddedPerGeneration = 12;
    public int NoveltyK = 8;
    public float MinimumArchiveDistance = 0.18f;

    [Header("Logging")]
    public bool WriteCsv = true;
    public string CsvFileName = "IRP_NoveltyArchive.csv";

    private readonly List<EvolutionDiversityArchiveEntry> entries = new List<EvolutionDiversityArchiveEntry>();
    private string csvPath;

    public int Count
    {
        get { return entries.Count; }
    }

    private void Awake()
    {
        if (Manager == null)
        {
            Manager = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance : FindFirstObjectByType<EvolutionEcosystemManager>();
        }

        csvPath = Path.Combine(Application.persistentDataPath, CsvFileName);
        EnsureHeader();
    }

    public void RecordGeneration(int generation, List<EvolutionCandidate> evaluated)
    {
        if (!Enabled || evaluated == null || evaluated.Count == 0)
        {
            return;
        }

        List<EvolutionCandidate> ranked = new List<EvolutionCandidate>();
        for (int i = 0; i < evaluated.Count; i++)
        {
            if (evaluated[i] != null && evaluated[i].Genome != null)
            {
                ranked.Add(evaluated[i]);
            }
        }

        ranked.Sort((a, b) => BuildArchiveScore(b, evaluated).CompareTo(BuildArchiveScore(a, evaluated)));

        int added = 0;
        for (int i = 0; i < ranked.Count && added < MaxEntriesAddedPerGeneration; i++)
        {
            EvolutionCandidate candidate = ranked[i];
            float[] descriptor = EvolutionBehaviourDescriptorUtility.BuildDescriptor(candidate);
            if (GetNearestArchiveDistance(descriptor) < MinimumArchiveDistance && entries.Count > 0)
            {
                continue;
            }

            EvolutionDiversityArchiveEntry entry = new EvolutionDiversityArchiveEntry();
            entry.Generation = generation;
            entry.Fitness = candidate.GetFitness();
            entry.CoreNiche = EvolutionNicheUtility.BuildCoreNicheKey(candidate);
            entry.Niche = EvolutionNicheUtility.BuildNicheKey(candidate);
            entry.Descriptor = descriptor;
            entry.BrainHidden = candidate.Genome.Brain != null ? candidate.Genome.Brain.HiddenCount : 0;
            entry.BrainConnections = candidate.Genome.Brain != null ? candidate.Genome.Brain.GetConnectionCount() : 0;
            entries.Add(entry);
            added++;
        }

        TrimArchive();
        if (added > 0)
        {
            AppendCsv(generation, added, evaluated);
        }
    }

    public float GetNoveltyScore(EvolutionCandidate candidate, List<EvolutionCandidate> currentCandidates)
    {
        if (!Enabled || candidate == null || candidate.Genome == null)
        {
            return 0f;
        }

        float[] descriptor = EvolutionBehaviourDescriptorUtility.BuildDescriptor(candidate);
        List<float> distances = new List<float>();

        for (int i = 0; i < entries.Count; i++)
        {
            distances.Add(EvolutionBehaviourDescriptorUtility.Distance(descriptor, entries[i].Descriptor));
        }

        if (currentCandidates != null)
        {
            for (int i = 0; i < currentCandidates.Count; i++)
            {
                EvolutionCandidate other = currentCandidates[i];
                if (other == null || other == candidate || other.Genome == null)
                {
                    continue;
                }
                distances.Add(EvolutionBehaviourDescriptorUtility.Distance(descriptor, EvolutionBehaviourDescriptorUtility.BuildDescriptor(other)));
            }
        }

        if (distances.Count == 0)
        {
            return 0.5f;
        }

        distances.Sort();
        int k = Mathf.Clamp(NoveltyK, 1, distances.Count);
        float total = 0f;
        for (int i = 0; i < k; i++)
        {
            total += distances[i];
        }

        return Mathf.Clamp01(total / k * 2.5f);
    }

    public float GetSharingDensity(EvolutionCandidate candidate, List<EvolutionCandidate> currentCandidates, float sigma)
    {
        if (!Enabled || candidate == null || candidate.Genome == null || currentCandidates == null || currentCandidates.Count <= 1)
        {
            return 0f;
        }

        sigma = Mathf.Max(0.05f, sigma);
        float[] descriptor = EvolutionBehaviourDescriptorUtility.BuildDescriptor(candidate);
        float density = 0f;

        for (int i = 0; i < currentCandidates.Count; i++)
        {
            EvolutionCandidate other = currentCandidates[i];
            if (other == null || other == candidate || other.Genome == null)
            {
                continue;
            }

            float distance = EvolutionBehaviourDescriptorUtility.Distance(descriptor, EvolutionBehaviourDescriptorUtility.BuildDescriptor(other));
            if (distance < sigma)
            {
                density += 1f - distance / sigma;
            }
        }

        return density;
    }

    public float GetArchiveSpread()
    {
        if (entries.Count <= 1)
        {
            return 0f;
        }

        float total = 0f;
        int comparisons = 0;
        int step = Mathf.Max(1, entries.Count / 80);
        for (int i = 0; i < entries.Count; i += step)
        {
            for (int j = i + step; j < entries.Count; j += step)
            {
                total += EvolutionBehaviourDescriptorUtility.Distance(entries[i].Descriptor, entries[j].Descriptor);
                comparisons++;
            }
        }

        return comparisons > 0 ? total / comparisons : 0f;
    }

    private float BuildArchiveScore(EvolutionCandidate candidate, List<EvolutionCandidate> evaluated)
    {
        if (candidate == null)
        {
            return 0f;
        }

        float fitness = Mathf.Max(0f, candidate.GetFitness());
        float novelty = GetNoveltyScore(candidate, evaluated);
        float reproduction = candidate.ReproductionCount + candidate.EggsLaid + candidate.EggsHatched > 0 ? 30f : 0f;
        float predator = candidate.PreyBites > 0 || candidate.PreyKills > 0 ? 25f : 0f;
        return fitness + novelty * 120f + reproduction + predator;
    }

    private float GetNearestArchiveDistance(float[] descriptor)
    {
        if (entries.Count == 0)
        {
            return 1f;
        }

        float best = float.MaxValue;
        for (int i = 0; i < entries.Count; i++)
        {
            best = Mathf.Min(best, EvolutionBehaviourDescriptorUtility.Distance(descriptor, entries[i].Descriptor));
        }
        return best;
    }

    private void TrimArchive()
    {
        int max = Mathf.Max(20, MaxArchiveEntries);
        while (entries.Count > max)
        {
            int weakestIndex = 0;
            float weakestScore = float.MaxValue;
            for (int i = 0; i < entries.Count; i++)
            {
                float score = entries[i].Fitness + GetEntryIsolation(i) * 120f;
                if (score < weakestScore)
                {
                    weakestScore = score;
                    weakestIndex = i;
                }
            }
            entries.RemoveAt(weakestIndex);
        }
    }

    private float GetEntryIsolation(int index)
    {
        if (index < 0 || index >= entries.Count || entries.Count <= 1)
        {
            return 0f;
        }

        float best = float.MaxValue;
        for (int i = 0; i < entries.Count; i++)
        {
            if (i == index)
            {
                continue;
            }
            best = Mathf.Min(best, EvolutionBehaviourDescriptorUtility.Distance(entries[index].Descriptor, entries[i].Descriptor));
        }
        return best == float.MaxValue ? 0f : best;
    }

    private void EnsureHeader()
    {
        if (!WriteCsv || string.IsNullOrEmpty(csvPath) || File.Exists(csvPath))
        {
            return;
        }

        File.WriteAllText(csvPath, "Generation,Added,ArchiveSize,ArchiveSpread,PopulationFeatureSpread\n");
    }

    private void AppendCsv(int generation, int added, List<EvolutionCandidate> evaluated)
    {
        if (!WriteCsv)
        {
            return;
        }

        EnsureHeader();
        StringBuilder line = new StringBuilder();
        line.Append(generation).Append(',');
        line.Append(added).Append(',');
        line.Append(entries.Count).Append(',');
        line.Append(GetArchiveSpread().ToString("F4")).Append(',');
        line.Append(EvolutionBehaviourDescriptorUtility.CalculateFeatureSpread(evaluated).ToString("F4"));
        File.AppendAllText(csvPath, line.ToString() + "\n");
    }
}

[System.Serializable]
public class EvolutionDiversityArchiveEntry
{
    public int Generation;
    public float Fitness;
    public string CoreNiche;
    public string Niche;
    public float[] Descriptor;
    public int BrainHidden;
    public int BrainConnections;
}
