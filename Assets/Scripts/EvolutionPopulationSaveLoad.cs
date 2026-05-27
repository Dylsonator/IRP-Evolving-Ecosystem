using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class EvolutionPopulationSaveData
{
    public int Generation;
    public float GenerationTimer;
    public string SavedAt;
    public List<EvolutionGenome> Genomes = new List<EvolutionGenome>();
}

/// <summary>
/// Save long-run evolved populations and reload them into a clean demo scene.
/// Attach this next to EvolutionEcosystemManager or call the context menu commands.
/// </summary>
public class EvolutionPopulationSaveLoad : MonoBehaviour
{
    public EvolutionEcosystemManager Manager;
    public string FolderName = "IRP_EvolvedPopulations";
    public string FileName = "evolved_population.json";
    public string BaselineFileName = "baseline_population.json";
    public bool ResetGenerationTimerOnLoad = true;

    private string FullPath
    {
        get
        {
            return GetFullPath(FileName);
        }
    }

    private string GetFullPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, FolderName, string.IsNullOrEmpty(fileName) ? FileName : fileName);
    }

    private void Awake()
    {
        if (Manager == null)
        {
            Manager = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance : FindFirstObjectByType<EvolutionEcosystemManager>();
        }
    }

    [ContextMenu("Save Current Evolved Population")]
    public void SaveCurrentPopulation()
    {
        SaveCurrentPopulationToFile(FileName);
    }

    [ContextMenu("Save Timestamped Population Snapshot")]
    public void SaveTimestampedPopulationSnapshot()
    {
        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        int generation = Manager != null ? Manager.CurrentGeneration : 0;
        SaveCurrentPopulationToFile("evolved_population_gen" + generation.ToString("0000") + "_" + stamp + ".json");
    }

    [ContextMenu("Save Current Population As Baseline")]
    public void SaveCurrentPopulationAsBaseline()
    {
        SaveCurrentPopulationToFile(BaselineFileName);
    }

    public void SaveCurrentPopulationToFile(string fileName)
    {
        if (Manager == null)
        {
            Debug.LogWarning("No EvolutionEcosystemManager found for population save.", this);
            return;
        }

        EvolutionPopulationSaveData data = new EvolutionPopulationSaveData();
        data.Generation = Manager.CurrentGeneration;
        data.GenerationTimer = Manager.GenerationTimer;
        data.SavedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        List<MarineCreatureAgent> creatures = Manager.GetActiveCreatures();
        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent creature = creatures[i];
            if (creature == null || creature.Candidate == null || creature.Candidate.Genome == null)
            {
                continue;
            }

            data.Genomes.Add(creature.Candidate.Genome.Clone());
        }

        string path = GetFullPath(fileName);
        string folder = Path.GetDirectoryName(path);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log("Saved evolved population with " + data.Genomes.Count + " genomes to: " + path, this);
    }

    [ContextMenu("Load Saved Evolved Population")]
    public void LoadSavedPopulation()
    {
        LoadPopulationFromFile(FileName);
    }

    [ContextMenu("Load Baseline Population")]
    public void LoadBaselinePopulation()
    {
        LoadPopulationFromFile(BaselineFileName);
    }

    public void LoadPopulationFromFile(string fileName)
    {
        if (Manager == null)
        {
            Debug.LogWarning("No EvolutionEcosystemManager found for population load.", this);
            return;
        }

        string path = GetFullPath(fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning("No evolved population save found at: " + path, this);
            return;
        }

        EvolutionPopulationSaveData data = JsonUtility.FromJson<EvolutionPopulationSaveData>(File.ReadAllText(path));
        if (data == null || data.Genomes == null || data.Genomes.Count == 0)
        {
            Debug.LogWarning("Population save was empty or invalid: " + path, this);
            return;
        }

        Manager.ReplacePopulationWithGenomes(data.Genomes, data.Generation, ResetGenerationTimerOnLoad ? 0f : data.GenerationTimer);
        Debug.Log("Loaded evolved population with " + data.Genomes.Count + " genomes from: " + path, this);
    }

    public string GetSavePath()
    {
        return FullPath;
    }
}
