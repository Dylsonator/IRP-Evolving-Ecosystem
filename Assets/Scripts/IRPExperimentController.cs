using System;
using System.IO;
using System.Text;
using UnityEngine;

public enum IRPExperimentPhase
{
    Baseline,
    ResourceBloom,
    Scarcity,
    ColdCurrent,
    MutationPulse,
    ExtinctionRecovery
}

/// <summary>
/// Lightweight controller for repeatable IRP-style trials.
/// It does not replace the ecosystem manager. It applies labelled pressure phases,
/// records an event log and gives clear evidence for controlled runs.
/// </summary>
public class IRPExperimentController : MonoBehaviour
{
    [Header("References")]
    public EvolutionEcosystemManager Manager;
    public SeasonalEnvironment Environment;
    public EvolutionResearchMetricsRecorder MetricsRecorder;
    public IRPBehaviourArchive BehaviourArchive;

    [Header("Run Identity")]
    public string RunLabel = "IRP_Run";
    public bool AutoGenerateRunId = true;
    public string CurrentRunId;
    public int TrialIndex = 1;

    [Header("Repeatability")]
    public bool ApplyFixedSeedOnStart = false;
    public int FixedSeed = 12345;
    public bool ResetCsvOnNewTrial = false;

    [Header("Experiment Schedule")]
    public bool UsePhaseSchedule = true;
    public int BaselineGenerations = 3;
    public int BloomGenerations = 2;
    public int ScarcityGenerations = 2;
    public int ColdGenerations = 2;
    public int MutationPulseGenerations = 1;
    public int RecoveryGenerations = 3;
    public bool RepeatSchedule = true;

    [Header("Auto Stop / Save")]
    public bool StopAfterGenerationLimit = false;
    public int GenerationLimit = 30;
    public bool SavePopulationAtLimit = true;

    [Header("Phase Pressure Values")]
    public float BloomFoodMultiplier = 1.55f;
    public float BloomDrainMultiplier = 0.85f;
    public float BloomMutationMultiplier = 0.95f;

    public float ScarcityFoodMultiplier = 0.58f;
    public float ScarcityDrainMultiplier = 1.28f;
    public float ScarcityMutationMultiplier = 1.12f;

    public float ColdFoodMultiplier = 0.70f;
    public float ColdDrainMultiplier = 1.22f;
    public float ColdMutationMultiplier = 1.18f;

    public float MutationPulseFoodMultiplier = 0.95f;
    public float MutationPulseDrainMultiplier = 1.05f;
    public float MutationPulseMutationMultiplier = 1.75f;

    public float RecoveryFoodMultiplier = 1.22f;
    public float RecoveryDrainMultiplier = 0.95f;
    public float RecoveryMutationMultiplier = 1.05f;

    [Header("Optional Disruptions")]
    public bool TriggerExtinctionAtStartOfScarcity = false;
    [Range(0f, 1f)] public float ScheduledExtinctionKillPercent = 0.18f;

    [Header("Logging")]
    public bool WriteEventCsv = true;
    public string EventCsvFileName = "IRP_ExperimentEvents.csv";
    public bool LogPhaseChanges = true;

    public IRPExperimentPhase CurrentPhase { get; private set; }

    private int lastGeneration = -1;
    private IRPExperimentPhase lastPhase;
    private string eventCsvPath;
    private bool hasStarted;
    private bool stopHandled;

    private void Awake()
    {
        ResolveReferences();
        if (AutoGenerateRunId || string.IsNullOrEmpty(CurrentRunId))
        {
            CurrentRunId = RunLabel + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        eventCsvPath = Path.Combine(Application.persistentDataPath, EventCsvFileName);
        EnsureEventHeader();
    }

    private void Start()
    {
        ResolveReferences();
        if (ApplyFixedSeedOnStart)
        {
            UnityEngine.Random.InitState(FixedSeed);
            if (Manager != null)
            {
                Manager.UseFixedRandomSeed = true;
                Manager.RandomSeed = FixedSeed;
            }
        }

        hasStarted = true;
        ApplyPhaseForCurrentGeneration(true);
        LogEvent("RunStarted", "Trial started.");
    }

    private void Update()
    {
        if (!hasStarted)
        {
            return;
        }

        ResolveReferences();
        if (Manager == null)
        {
            return;
        }

        if (Manager.CurrentGeneration != lastGeneration)
        {
            ApplyPhaseForCurrentGeneration(false);

            if (MetricsRecorder != null)
            {
                MetricsRecorder.CurrentRunId = CurrentRunId;
                MetricsRecorder.CurrentExperimentPhase = CurrentPhase.ToString();
            }

            if (BehaviourArchive != null)
            {
                BehaviourArchive.RunId = CurrentRunId;
                BehaviourArchive.RecordCurrentPopulation("GenerationChanged");
                BehaviourArchive.ExportArchiveSnapshot("GenerationChanged");
            }

            LogEvent("GenerationChanged", "Generation changed to " + Manager.CurrentGeneration + ".");
            lastGeneration = Manager.CurrentGeneration;
        }

        if (StopAfterGenerationLimit && !stopHandled && Manager.CurrentGeneration >= GenerationLimit)
        {
            stopHandled = true;
            LogEvent("GenerationLimitReached", "Reached generation limit " + GenerationLimit + ".");
            if (SavePopulationAtLimit)
            {
                EvolutionPopulationSaveLoad saveLoad = Manager.GetComponent<EvolutionPopulationSaveLoad>();
                if (saveLoad != null)
                {
                    saveLoad.SaveCurrentPopulation();
                }
            }
        }
    }

    [ContextMenu("IRP/Start New Trial")]
    public void StartNewTrial()
    {
        TrialIndex++;
        CurrentRunId = RunLabel + "_trial" + TrialIndex + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        stopHandled = false;
        lastGeneration = -1;
        lastPhase = IRPExperimentPhase.Baseline;

        if (ResetCsvOnNewTrial)
        {
            DeleteFileIfExists(EventCsvFileName);
            DeleteFileIfExists("IRP_EcosystemResearchMetrics_V2.csv");
            DeleteFileIfExists("IRP_EcosystemResearchMetrics_V3.csv");
            DeleteFileIfExists("IRP_GraphReadyGenerationSummary.csv");
            DeleteFileIfExists("IRP_NoveltyArchive.csv");
            DeleteFileIfExists("IRP_BehaviourArchive.csv");
            eventCsvPath = Path.Combine(Application.persistentDataPath, EventCsvFileName);
            EnsureEventHeader();
        }

        ResolveReferences();
        if (ApplyFixedSeedOnStart)
        {
            UnityEngine.Random.InitState(FixedSeed + TrialIndex);
        }

        if (Manager != null)
        {
            Manager.ResetSimulation();
        }

        LogEvent("NewTrial", "Started new trial.");
    }

    [ContextMenu("IRP/Log Manual Observation")]
    public void LogManualObservation()
    {
        LogEvent("ManualObservation", "Manual observation marker.");
        if (MetricsRecorder != null)
        {
            MetricsRecorder.RecordSnapshot("ManualObservation");
        }
        if (BehaviourArchive != null)
        {
            BehaviourArchive.RecordCurrentPopulation("ManualObservation");
            BehaviourArchive.ExportArchiveSnapshot("ManualObservation");
        }
    }

    [ContextMenu("IRP/Trigger Controlled Extinction")]
    public void TriggerControlledExtinction()
    {
        if (Manager == null)
        {
            return;
        }

        float oldKill = Manager.ExtinctionKillPercentage;
        Manager.ExtinctionKillPercentage = Mathf.Clamp01(ScheduledExtinctionKillPercent);
        Manager.TriggerExtinctionEvent();
        Manager.ExtinctionKillPercentage = oldKill;
        LogEvent("ControlledExtinction", "Triggered controlled extinction at " + ScheduledExtinctionKillPercent.ToString("P0") + ".");
    }

    private void ApplyPhaseForCurrentGeneration(bool force)
    {
        if (Manager == null)
        {
            return;
        }

        IRPExperimentPhase phase = UsePhaseSchedule ? GetScheduledPhase(Manager.CurrentGeneration) : IRPExperimentPhase.Baseline;
        CurrentPhase = phase;

        if (!force && phase == lastPhase)
        {
            return;
        }

        lastPhase = phase;
        ApplyPhaseValues(phase);

        if (LogPhaseChanges)
        {
            LogEvent("PhaseChanged", "Applied phase " + phase + ".");
        }

        if (TriggerExtinctionAtStartOfScarcity && phase == IRPExperimentPhase.Scarcity)
        {
            TriggerControlledExtinction();
        }
    }

    private IRPExperimentPhase GetScheduledPhase(int generation)
    {
        int b = Mathf.Max(0, BaselineGenerations);
        int bloom = Mathf.Max(0, BloomGenerations);
        int scarcity = Mathf.Max(0, ScarcityGenerations);
        int cold = Mathf.Max(0, ColdGenerations);
        int mutation = Mathf.Max(0, MutationPulseGenerations);
        int recovery = Mathf.Max(0, RecoveryGenerations);
        int length = Mathf.Max(1, b + bloom + scarcity + cold + mutation + recovery);

        int index = Mathf.Max(0, generation - 1);
        if (RepeatSchedule)
        {
            index %= length;
        }
        else
        {
            index = Mathf.Min(index, length - 1);
        }

        if (index < b) return IRPExperimentPhase.Baseline;
        index -= b;
        if (index < bloom) return IRPExperimentPhase.ResourceBloom;
        index -= bloom;
        if (index < scarcity) return IRPExperimentPhase.Scarcity;
        index -= scarcity;
        if (index < cold) return IRPExperimentPhase.ColdCurrent;
        index -= cold;
        if (index < mutation) return IRPExperimentPhase.MutationPulse;
        return IRPExperimentPhase.ExtinctionRecovery;
    }

    private void ApplyPhaseValues(IRPExperimentPhase phase)
    {
        if (Environment == null)
        {
            return;
        }

        switch (phase)
        {
            case IRPExperimentPhase.ResourceBloom:
                Environment.FoodSpawnMultiplier = BloomFoodMultiplier;
                Environment.EnergyDrainMultiplier = BloomDrainMultiplier;
                Environment.MutationMultiplier = BloomMutationMultiplier;
                break;
            case IRPExperimentPhase.Scarcity:
                Environment.FoodSpawnMultiplier = ScarcityFoodMultiplier;
                Environment.EnergyDrainMultiplier = ScarcityDrainMultiplier;
                Environment.MutationMultiplier = ScarcityMutationMultiplier;
                break;
            case IRPExperimentPhase.ColdCurrent:
                Environment.FoodSpawnMultiplier = ColdFoodMultiplier;
                Environment.EnergyDrainMultiplier = ColdDrainMultiplier;
                Environment.MutationMultiplier = ColdMutationMultiplier;
                break;
            case IRPExperimentPhase.MutationPulse:
                Environment.FoodSpawnMultiplier = MutationPulseFoodMultiplier;
                Environment.EnergyDrainMultiplier = MutationPulseDrainMultiplier;
                Environment.MutationMultiplier = MutationPulseMutationMultiplier;
                break;
            case IRPExperimentPhase.ExtinctionRecovery:
                Environment.FoodSpawnMultiplier = RecoveryFoodMultiplier;
                Environment.EnergyDrainMultiplier = RecoveryDrainMultiplier;
                Environment.MutationMultiplier = RecoveryMutationMultiplier;
                break;
            default:
                Environment.ApplySeasonSettings();
                break;
        }
    }

    private void ResolveReferences()
    {
        if (Manager == null)
        {
            Manager = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance : FindFirstObjectByType<EvolutionEcosystemManager>();
        }

        if (Manager != null && Environment == null)
        {
            Environment = Manager.Environment;
        }

        if (MetricsRecorder == null)
        {
            MetricsRecorder = GetComponent<EvolutionResearchMetricsRecorder>();
        }

        if (BehaviourArchive == null)
        {
            BehaviourArchive = GetComponent<IRPBehaviourArchive>();
        }

        if (MetricsRecorder != null)
        {
            MetricsRecorder.Manager = Manager;
            MetricsRecorder.CurrentRunId = CurrentRunId;
            MetricsRecorder.CurrentExperimentPhase = CurrentPhase.ToString();
        }

        if (BehaviourArchive != null)
        {
            BehaviourArchive.Manager = Manager;
            BehaviourArchive.RunId = CurrentRunId;
        }
    }

    private void LogEvent(string eventType, string note)
    {
        if (!WriteEventCsv)
        {
            return;
        }

        EnsureEventHeader();
        int generation = Manager != null ? Manager.CurrentGeneration : 0;
        float timer = Manager != null ? Manager.GenerationTimer : 0f;
        int population = Manager != null ? Manager.GetActiveCreatures().Count : 0;
        int offspring = Manager != null ? Manager.GetOffspringPool().Count : 0;
        int nicheCount = BehaviourArchive != null ? BehaviourArchive.ArchivedCellCount : 0;

        StringBuilder line = new StringBuilder();
        line.Append(Escape(CurrentRunId)).Append(',');
        line.Append(TrialIndex).Append(',');
        line.Append(Time.time.ToString("F2")).Append(',');
        line.Append(generation).Append(',');
        line.Append(timer.ToString("F2")).Append(',');
        line.Append(CurrentPhase).Append(',');
        line.Append(eventType).Append(',');
        line.Append(population).Append(',');
        line.Append(offspring).Append(',');
        line.Append(nicheCount).Append(',');
        line.Append(Escape(note));
        File.AppendAllText(eventCsvPath, line.ToString() + "\n");
    }

    private void EnsureEventHeader()
    {
        if (!WriteEventCsv)
        {
            return;
        }

        if (string.IsNullOrEmpty(eventCsvPath))
        {
            eventCsvPath = Path.Combine(Application.persistentDataPath, EventCsvFileName);
        }

        if (!File.Exists(eventCsvPath))
        {
            File.WriteAllText(eventCsvPath, "RunId,TrialIndex,Realtime,Generation,GenerationTimer,Phase,EventType,Population,OffspringPool,ArchiveCells,Note\n");
        }
    }

    private void DeleteFileIfExists(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string Escape(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');
    }
}
