using System;
using System.IO;
using System.Text;
using UnityEngine;

// Runs repeatable fixed-seed trials and logs final trial summaries for comparison.
public enum IRPTrialCondition
{
    Baseline,
    ResourceBloom,
    Scarcity,
    ColdCurrent,
    MutationPulse,
    ExtinctionRecovery
}

// Optional fixed-seed trial runner so tests can be repeated fairly.
public class IRPTrialBatchRunner : MonoBehaviour
{
    public EvolutionEcosystemManager Manager;
    public SeasonalEnvironment Environment;
    public IRPExperimentController ExperimentController;
    public EvolutionResearchMetricsRecorder MetricsRecorder;
    public IRPGraphReadyMetricsExporter GraphExporter;

    [Header("Trial Identity")]
    public string BaseRunLabel = "IRP_ControlledTrial";
    public int SeedStart = 12345;
    public int CurrentSeed;
    public int CurrentTrialIndex = 0;
    public IRPTrialCondition CurrentCondition = IRPTrialCondition.Baseline;

    [Header("Batch")]
    public bool AutoRunBatch;
    public int GenerationsPerTrial = 12;
    public int SeedsPerCondition = 3;
    public bool SavePopulationAtEndOfEachTrial;
    public IRPTrialCondition[] ConditionOrder =
    {
        IRPTrialCondition.Baseline,
        IRPTrialCondition.Scarcity,
        IRPTrialCondition.ColdCurrent,
        IRPTrialCondition.ExtinctionRecovery,
        IRPTrialCondition.MutationPulse
    };

    [Header("Condition Values")]
    public float BaselineFood = 1f;
    public float BaselineDrain = 1f;
    public float BaselineMutation = 1f;
    public float BloomFood = 1.55f;
    public float BloomDrain = 0.85f;
    public float BloomMutation = 0.95f;
    public float ScarcityFood = 0.58f;
    public float ScarcityDrain = 1.28f;
    public float ScarcityMutation = 1.12f;
    public float ColdFood = 0.70f;
    public float ColdDrain = 1.22f;
    public float ColdMutation = 1.18f;
    public float MutationPulseFood = 0.95f;
    public float MutationPulseDrain = 1.05f;
    public float MutationPulseMutation = 1.75f;
    public float RecoveryFood = 1.22f;
    public float RecoveryDrain = 0.95f;
    public float RecoveryMutation = 1.05f;

    [Header("Logging")]
    public bool WriteTrialCsv = true;
    public string TrialCsvFileName = "IRP_TrialRuns.csv";

    private int currentConditionIndex;
    private int currentSeedOffset;
    private int startGeneration;
    private string currentRunId;
    private string trialCsvPath;
    private bool trialRunning;

    // Sets up cached references and safe starting values before the sim runs
    private void Awake()
    {
        ResolveReferences();
        CurrentSeed = SeedStart;
        trialCsvPath = Path.Combine(Application.persistentDataPath, TrialCsvFileName);
        EnsureHeader();
    }

    // Starts the setup that needs other scene objects to already exist
    private void Start()
    {
        ResolveReferences();
        if (AutoRunBatch && !trialRunning)
        {
            StartBatchFromBeginning();
        }
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        if (!AutoRunBatch || !trialRunning || Manager == null)
        {
            return;
        }

        if (Manager.CurrentGeneration >= startGeneration + Mathf.Max(1, GenerationsPerTrial))
        {
            CompleteCurrentTrial("GenerationLimitReached");
            StartNextBatchTrial();
        }
    }

    [ContextMenu("IRP Trials/Start Batch From Beginning")]
    // Starts the full batch trial list from the first condition
    public void StartBatchFromBeginning()
    {
        currentConditionIndex = 0;
        currentSeedOffset = 0;
        CurrentTrialIndex = 0;
        StartNextBatchTrial();
    }

    // Handles the run baseline trial step.
    [ContextMenu("IRP Trials/Run Baseline Trial")]
    // Starts a single baseline trial
    public void RunBaselineTrial()
    {
        StartSingleTrial(IRPTrialCondition.Baseline, SeedStart);
    }

    // Handles the run scarcity trial step.
    [ContextMenu("IRP Trials/Run Scarcity Trial")]
    // Starts a single scarcity trial
    public void RunScarcityTrial()
    {
        StartSingleTrial(IRPTrialCondition.Scarcity, SeedStart);
    }

    // Handles the run cold current trial step.
    [ContextMenu("IRP Trials/Run Cold Current Trial")]
    // Starts a single cold current trial
    public void RunColdCurrentTrial()
    {
        StartSingleTrial(IRPTrialCondition.ColdCurrent, SeedStart);
    }

    // Handles the run mutation pulse trial step.
    [ContextMenu("IRP Trials/Run Mutation Pulse Trial")]
    // Starts a single mutation pulse trial
    public void RunMutationPulseTrial()
    {
        StartSingleTrial(IRPTrialCondition.MutationPulse, SeedStart);
    }

    // Handles the complete trial manually step.
    [ContextMenu("IRP Trials/Complete Current Trial")]
    // Ends the current trial when the user chooses to stop it
    public void CompleteTrialManually()
    {
        CompleteCurrentTrial("ManualComplete");
    }

    // Starts a one-off trial with the selected condition
    public void StartSingleTrial(IRPTrialCondition condition, int seed)
    {
        AutoRunBatch = false;
        CurrentCondition = condition;
        CurrentSeed = seed;
        CurrentTrialIndex++;
        StartTrial(condition, seed);
    }

    // Moves the batch runner onto the next condition
    private void StartNextBatchTrial()
    {
        if (ConditionOrder == null || ConditionOrder.Length == 0)
        {
            AutoRunBatch = false;
            trialRunning = false;
            return;
        }

        if (currentConditionIndex >= ConditionOrder.Length)
        {
            currentConditionIndex = 0;
            currentSeedOffset++;
        }

        if (currentSeedOffset >= Mathf.Max(1, SeedsPerCondition))
        {
            AutoRunBatch = false;
            trialRunning = false;
            LogTrialEvent("BatchComplete", CurrentCondition, CurrentSeed, "All scheduled trials completed.");
            return;
        }

        CurrentCondition = ConditionOrder[currentConditionIndex];
        CurrentSeed = SeedStart + currentSeedOffset;
        currentConditionIndex++;
        CurrentTrialIndex++;
        StartTrial(CurrentCondition, CurrentSeed);
    }

    // Sets the seed, condition and timers for a trial
    private void StartTrial(IRPTrialCondition condition, int seed)
    {
        ResolveReferences();
        if (Manager == null)
        {
            return;
        }

        currentRunId = BaseRunLabel + "_" + condition + "_seed" + seed + "_trial" + CurrentTrialIndex + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        UnityEngine.Random.InitState(seed);
        Manager.UseFixedRandomSeed = true;
        Manager.RandomSeed = seed;
        ApplyCondition(condition);
        PushRunContext();
        startGeneration = 1;
        trialRunning = true;
        LogTrialEvent("TrialStarted", condition, seed, currentRunId);
        Manager.ResetSimulation();
    }

    // Logs the result and optionally moves to the next trial
    private void CompleteCurrentTrial(string reason)
    {
        if (!trialRunning)
        {
            return;
        }

        LogTrialEvent(reason, CurrentCondition, CurrentSeed, currentRunId);
        if (SavePopulationAtEndOfEachTrial && Manager != null)
        {
            EvolutionPopulationSaveLoad saveLoad = Manager.GetComponent<EvolutionPopulationSaveLoad>();
            if (saveLoad != null)
            {
                saveLoad.SaveCurrentPopulation();
            }
        }

        trialRunning = false;
    }

    // Applies the selected trial condition to the ecosystem
    private void ApplyCondition(IRPTrialCondition condition)
    {
        if (Environment == null)
        {
            return;
        }

        switch (condition)
        {
            case IRPTrialCondition.ResourceBloom:
                Environment.FoodSpawnMultiplier = BloomFood;
                Environment.EnergyDrainMultiplier = BloomDrain;
                Environment.MutationMultiplier = BloomMutation;
                break;
            case IRPTrialCondition.Scarcity:
                Environment.FoodSpawnMultiplier = ScarcityFood;
                Environment.EnergyDrainMultiplier = ScarcityDrain;
                Environment.MutationMultiplier = ScarcityMutation;
                break;
            case IRPTrialCondition.ColdCurrent:
                Environment.FoodSpawnMultiplier = ColdFood;
                Environment.EnergyDrainMultiplier = ColdDrain;
                Environment.MutationMultiplier = ColdMutation;
                break;
            case IRPTrialCondition.MutationPulse:
                Environment.FoodSpawnMultiplier = MutationPulseFood;
                Environment.EnergyDrainMultiplier = MutationPulseDrain;
                Environment.MutationMultiplier = MutationPulseMutation;
                break;
            case IRPTrialCondition.ExtinctionRecovery:
                Environment.FoodSpawnMultiplier = RecoveryFood;
                Environment.EnergyDrainMultiplier = RecoveryDrain;
                Environment.MutationMultiplier = RecoveryMutation;
                break;
            default:
                Environment.FoodSpawnMultiplier = BaselineFood;
                Environment.EnergyDrainMultiplier = BaselineDrain;
                Environment.MutationMultiplier = BaselineMutation;
                break;
        }
    }

    // Pushes the trial name into the loggers so CSVs line up
    private void PushRunContext()
    {
        if (ExperimentController != null)
        {
            ExperimentController.CurrentRunId = currentRunId;
            ExperimentController.RunLabel = BaseRunLabel;
            ExperimentController.FixedSeed = CurrentSeed;
            ExperimentController.TrialIndex = CurrentTrialIndex;
            ExperimentController.UsePhaseSchedule = false;
        }

        if (MetricsRecorder != null)
        {
            MetricsRecorder.CurrentRunId = currentRunId;
            MetricsRecorder.CurrentExperimentPhase = CurrentCondition.ToString();
        }

        if (GraphExporter != null)
        {
            GraphExporter.RunId = currentRunId;
            GraphExporter.ExperimentPhase = CurrentCondition.ToString();
        }
    }

    // Finds manager and helper references if they were not assigned
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

        if (ExperimentController == null)
        {
            ExperimentController = GetComponent<IRPExperimentController>();
        }

        if (MetricsRecorder == null)
        {
            MetricsRecorder = GetComponent<EvolutionResearchMetricsRecorder>();
        }

        if (GraphExporter == null)
        {
            GraphExporter = GetComponent<IRPGraphReadyMetricsExporter>();
        }
    }

    // Writes a trial event to the trial CSV
    private void LogTrialEvent(string eventType, IRPTrialCondition condition, int seed, string note)
    {
        if (!WriteTrialCsv)
        {
            return;
        }

        EnsureHeader();
        StringBuilder line = new StringBuilder();
        line.Append(Safe(currentRunId)).Append(',');
        line.Append(CurrentTrialIndex).Append(',');
        line.Append(Safe(condition.ToString())).Append(',');
        line.Append(seed).Append(',');
        line.Append(Manager != null ? Manager.CurrentGeneration : 0).Append(',');
        line.Append(Time.time.ToString("F2")).Append(',');
        line.Append(Safe(eventType)).Append(',');
        line.Append(Safe(note));
        File.AppendAllText(trialCsvPath, line.ToString() + "\n");
    }

    // Creates the CSV header if the file is new
    private void EnsureHeader()
    {
        if (!WriteTrialCsv)
        {
            return;
        }

        if (string.IsNullOrEmpty(trialCsvPath))
        {
            trialCsvPath = Path.Combine(Application.persistentDataPath, TrialCsvFileName);
        }

        if (!File.Exists(trialCsvPath))
        {
            File.WriteAllText(trialCsvPath, "RunId,TrialIndex,Condition,Seed,Generation,Realtime,EventType,Note\n");
        }
    }

    // Cleans text so commas and nulls do not break CSV output
    private string Safe(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');
    }
}
