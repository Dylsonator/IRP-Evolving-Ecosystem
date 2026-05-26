using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SelectionMode
{
    Tournament,
    Elite,
    QualityDiversityLite
}

public class EvolutionEcosystemManager : MonoBehaviour
{
    public static EvolutionEcosystemManager Instance { get; private set; }

    [Header("Prefabs")]
    public MarineCreatureAgent CreaturePrefab;
    public FoodSource FoodPrefab;
    public CarrionSource CarrionPrefab;
    public FishEggCluster EggClusterPrefab;

    [Header("Libraries")]
    public CreatureMorphLibrary MorphLibrary;

    [Header("References")]
    public SeasonalEnvironment Environment;
    public EvolutionStatsTracker StatsTracker;
    public EcosystemDebugSettings DebugSettings;

    [Header("Real Ecosystem Objects")]
    public bool UsePlacedPlantsForFood = true;
    public bool EnableRandomFoodFallback = false;
    public int MaxActiveCreatures = 90;

    [Header("Terrain / Water Height")]
    public Terrain TerrainSource;
    public LayerMask TerrainRaycastMask = ~0;
    public bool KeepSpawnsAboveTerrain = true;
    public bool KeepSimulationAboveTerrain = true;
    public float TerrainClearance = 0.35f;
    public float FishTerrainClearance = 1.0f;
    public float FoodTerrainClearance = 0.12f;
    public float CarrionTerrainClearance = 0.12f;
    public float PlantTerrainOffset = 0.04f;
    public float TerrainProbeHeight = 160f;
    public float TerrainProbeDistance = 320f;
    public bool UseWaterSurfaceLimit = true;
    public float WaterSurfacePadding = 1.0f;

    [Header("Simulation Area")]
    public Vector3 SimulationAreaSize = new Vector3(100f, 35f, 100f);
    public float SpawnPaddingFromBounds = 4f;

    [Header("Generation Settings")]
    public int StartingPopulation = 35;
    public int FixedPopulationSize = 35;
    public float GenerationDuration = 150f;
    public SelectionMode Selection = SelectionMode.QualityDiversityLite;
    public int TournamentSize = 4;

    [Header("Quality Diversity / Brain Selection")]
    [Tooltip("Uses diet, movement and feeding feature bins instead of only readable species names. This better matches MAP-Elites style diversity preservation.")]
    public bool UseBehaviourFeatureBuckets = true;
    public int QualityDiversityBins = 4;
    public bool UseNoveltySelectionBias = true;
    public float NoveltySelectionBonus = 35f;
    [Tooltip("Small pressure against uncontrolled neural bloat. Keeps NEAT-lite complexification useful instead of endlessly growing.")]
    public float BrainComplexityPenalty = 0.08f;

    [Header("Anti-Convergence / Diversity Rescue")]
    [Tooltip("Prevents one successful ecological role from taking over the whole population too early.")]
    public bool UseAntiConvergence = true;
    [Range(0.12f, 0.75f)] public float MaxSameCoreNicheFraction = 0.30f;
    [Tooltip("If fewer broad ecological roles survive selection, the manager injects rescue mutants into weak/missing roles.")]
    public int MinimumSurvivingCoreNiches = 6;
    public int MaximumDiversityRescueMutants = 6;
    public float RareNicheSelectionBonus = 55f;
    public float CollapseMutationMultiplier = 1.65f;
    public bool InjectMissingNichesWhenCollapsed = true;

    public bool UseFixedRandomSeed = false;
    public int RandomSeed = 12345;

    [Header("Ecology Balance Safeguards")]
    public bool UseEcologyBalanceSafeguards = true;
    public bool SeedStartingDietNiches = true;
    [Range(0f, 0.45f)] public float StartingPredatorFraction = 0.10f;
    [Range(0f, 0.35f)] public float StartingScavengerFraction = 0.10f;
    public bool ReplaceMissingDietNichesEachGeneration = false;
    public int MinimumPredatorSeedsPerGeneration = 0;
    public int MinimumScavengerSeedsPerGeneration = 0;

    [Header("Initial Genome")]
    [Tooltip("If true, generation 1 starts from the same base genome so roles emerge through mutation/selection instead of being predefined.")]
    public bool StartFromUniformBaseline = true;
    [Range(0f, 0.15f)] public float InitialGenomeVariation = 0f;

    [Header("Food Settings")]
    public int StartingFood = 240;
    public int TargetFoodAmount = 280;
    public float FoodSpawnInterval = 0.12f;

    [Header("Predation / Carrion")]
    public bool EnablePredation = true;
    [Range(0f, 1f)] public float MinimumMeatDietToHunt = 0.34f;
    [Range(0f, 1f)] public float MinimumAggressionToHunt = 0.20f;
    public float MaxPreySizeRatio = 1.15f;
    public float BiteEnergyGainMultiplier = 0.72f;
    public bool SpawnCarrionFromDeaths = true;
    public bool SpawnCarrionFromExtinctionEvents = true;
    public float CarrionEnergyFromBodySize = 58f;
    public int MaxCarrionSources = 130;

    [Header("Defensive Morphology")]
    [Tooltip("Higher values make spiked/armoured herbivores more able to discourage predators.")]
    public float PredatorFearOfDangerFactor = 0.85f;

    [Header("Extinction Pressure")]
    public bool UseExtinctionEvents = true;
    public float ExtinctionEventInterval = 190f;
    [Range(0f, 1f)] public float ExtinctionKillPercentage = 0.22f;
    public bool ExtinctionChangesEnvironment = true;
    public bool ExtinctionCreatesCarrion = true;

    [Header("Performance / Startup")]
    [Tooltip("Spreads initial spawning across several frames to avoid a large first-frame spike.")]
    public bool StaggerInitialSpawn = true;
    public int CreatureSpawnsPerFrame = 6;
    public int FoodSpawnsPerFrame = 25;

    [Header("Debug")]
    public int CurrentGeneration = 1;
    public float GenerationTimer;
    public int OffspringCount;
    public int ActiveCarrionCount;

    private readonly List<MarineCreatureAgent> activeCreatures = new List<MarineCreatureAgent>();
    private readonly List<FoodSource> activeFood = new List<FoodSource>();
    private readonly List<CarrionSource> activeCarrion = new List<CarrionSource>();
    private readonly List<PlantResource> activePlants = new List<PlantResource>();
    private readonly List<FishEggCluster> activeEggClusters = new List<FishEggCluster>();
    private readonly List<EcosystemWaterCurrentZone> activeCurrentZones = new List<EcosystemWaterCurrentZone>();
    private readonly List<EcosystemPressureZone> activePressureZones = new List<EcosystemPressureZone>();
    private readonly List<EvolutionCandidate> offspringPool = new List<EvolutionCandidate>();

    [Header("Spatial Lookup")]
    public bool UseSpatialGrid = true;
    public EcosystemSpatialGrid SpatialGrid;
    public float SpatialGridCellSize = 12f;
    public float SpatialGridRefreshInterval = 0.25f;

    [Header("Research Tools")]
    public bool AutoAttachResearchTools = true;

    [Header("Environmental Pressure Events")]
    public bool SpawnPressureZoneOnExtinction = true;
    public EcosystemPressureZone PressureZonePrefab;
    public float ExtinctionPressureZoneRadius = 18f;
    public float ExtinctionPressureZoneLifetime = 80f;

    [Header("Performance")]
    [Tooltip("How often manager lists are cleaned for destroyed/null entries. Registration still removes objects immediately on disable.")]
    public float ListCleanInterval = 0.5f;

    private float foodSpawnTimer;
    private float extinctionTimer;
    private float listCleanTimer;
    private float spatialGridTimer;
    private int nextCreatureId = 1;
    private EvolutionGenome baselineGenome;
    private Coroutine bootstrapRoutine;

    private void Awake()
    {
        Instance = this;

        if (UseFixedRandomSeed)
        {
            Random.InitState(RandomSeed);
        }

        if (DebugSettings == null)
        {
            DebugSettings = FindFirstObjectByType<EcosystemDebugSettings>();
        }

        ApplyEcologyBalanceSafeguards();
        EnsureSpatialGrid();
        EnsureResearchTools();
        CreatureMorphLibrary.SetActiveLibrary(MorphLibrary);
    }


    private void ApplyEcologyBalanceSafeguards()
    {
        if (!UseEcologyBalanceSafeguards)
        {
            return;
        }

        // Respect the population the scene is actually tuned for. The previous safeguard
        // forced the demo back up to a much heavier population, which caused start-up spikes.
        StartingPopulation = Mathf.Max(1, StartingPopulation);
        FixedPopulationSize = Mathf.Max(1, FixedPopulationSize);
        MaxActiveCreatures = Mathf.Max(MaxActiveCreatures, FixedPopulationSize + 35);

        // 150s generations need enough time for feeding -> socialising -> mating -> hatching.
        GenerationDuration = Mathf.Max(GenerationDuration, 150f);

        MinimumMeatDietToHunt = Mathf.Min(MinimumMeatDietToHunt, 0.34f);
        MinimumAggressionToHunt = Mathf.Min(MinimumAggressionToHunt, 0.20f);
        BiteEnergyGainMultiplier = Mathf.Max(BiteEnergyGainMultiplier, 0.72f);
        CarrionEnergyFromBodySize = Mathf.Max(CarrionEnergyFromBodySize, 58f);
        MaxCarrionSources = Mathf.Max(MaxCarrionSources, 90);

        // Predators/scavengers should be possible, not forcibly guaranteed every generation.
        StartingPredatorFraction = Mathf.Clamp(StartingPredatorFraction, 0.04f, 0.16f);
        StartingScavengerFraction = Mathf.Clamp(StartingScavengerFraction, 0.04f, 0.16f);
        ReplaceMissingDietNichesEachGeneration = false;
        MinimumPredatorSeedsPerGeneration = 0;
        MinimumScavengerSeedsPerGeneration = 0;

        UseAntiConvergence = true;
        MaxSameCoreNicheFraction = Mathf.Clamp(MaxSameCoreNicheFraction, 0.22f, 0.42f);
        MinimumSurvivingCoreNiches = Mathf.Clamp(MinimumSurvivingCoreNiches, 4, Mathf.Max(4, FixedPopulationSize / 3));
        MaximumDiversityRescueMutants = Mathf.Clamp(MaximumDiversityRescueMutants, 2, Mathf.Max(2, FixedPopulationSize / 4));
        RareNicheSelectionBonus = Mathf.Max(RareNicheSelectionBonus, 45f);
        CollapseMutationMultiplier = Mathf.Clamp(CollapseMutationMultiplier, 1.15f, 2.5f);

        SpawnCarrionFromDeaths = true;
        SpawnCarrionFromExtinctionEvents = ExtinctionCreatesCarrion;
        ExtinctionKillPercentage = Mathf.Clamp(ExtinctionKillPercentage, 0.18f, 0.34f);
        ExtinctionEventInterval = Mathf.Max(ExtinctionEventInterval, GenerationDuration * 1.1f);

        if (!UsePlacedPlantsForFood || EnableRandomFoodFallback)
        {
            StartingFood = Mathf.Max(StartingFood, FixedPopulationSize * 5);
            TargetFoodAmount = Mathf.Max(TargetFoodAmount, FixedPopulationSize * 6);
            FoodSpawnInterval = Mathf.Min(FoodSpawnInterval, 0.10f);
        }
    }

    private void Start()
    {
        StartBootstrap();
    }

    private void StartBootstrap()
    {
        if (bootstrapRoutine != null)
        {
            StopCoroutine(bootstrapRoutine);
        }

        bootstrapRoutine = StartCoroutine(BootstrapSimulationRoutine());
    }

    private IEnumerator BootstrapSimulationRoutine()
    {
        ClearSimulation();
        CreatureMorphLibrary.SetActiveLibrary(MorphLibrary);
        baselineGenome = EvolutionGenome.CreateBaseline();

        int creatureAmount = Mathf.Max(1, StartingPopulation);
        int creatureBudget = Mathf.Max(1, CreatureSpawnsPerFrame);
        for (int i = 0; i < creatureAmount; i++)
        {
            EvolutionGenome genome = StartFromUniformBaseline
                ? baselineGenome.CreateInitialVariant(InitialGenomeVariation)
                : EvolutionGenome.CreateRandom();
            ApplyStartingDietNicheSeed(genome, i, creatureAmount);

            SpawnCreature(new EvolutionCandidate(genome), GetSpawnPointForCreature());

            if (StaggerInitialSpawn && i % creatureBudget == creatureBudget - 1)
            {
                yield return null;
            }
        }

        if (!UsePlacedPlantsForFood || EnableRandomFoodFallback)
        {
            int foodBudget = Mathf.Max(1, FoodSpawnsPerFrame);
            for (int i = 0; i < StartingFood; i++)
            {
                SpawnFood();

                if (StaggerInitialSpawn && i % foodBudget == foodBudget - 1)
                {
                    yield return null;
                }
            }
        }

        EnsureSpatialGrid();
        bootstrapRoutine = null;
    }

    private void Update()
    {
        if (bootstrapRoutine != null)
        {
            return;
        }

        GenerationTimer += Time.deltaTime;
        foodSpawnTimer += Time.deltaTime;
        extinctionTimer += Time.deltaTime;

        listCleanTimer -= Time.deltaTime;
        if (listCleanTimer <= 0f)
        {
            listCleanTimer = Mathf.Max(0.05f, ListCleanInterval);
            CleanLists();
        }

        UpdateSpatialGridIfNeeded();

        HandleFoodSpawning();
        HandleExtinctionEvents();

        OffspringCount = offspringPool.Count;
        ActiveCarrionCount = activeCarrion.Count;

        if (GenerationTimer >= GenerationDuration)
        {
            EndGenerationAndSpawnNext();
        }

        if (activeCreatures.Count == 0)
        {
            EndGenerationAndSpawnNext();
        }
    }

    private void SpawnInitialGeneration()
    {
        ClearSimulation();
        CreatureMorphLibrary.SetActiveLibrary(MorphLibrary);
        baselineGenome = EvolutionGenome.CreateBaseline();

        int amount = Mathf.Max(1, StartingPopulation);

        for (int i = 0; i < amount; i++)
        {
            EvolutionGenome genome = StartFromUniformBaseline
                ? baselineGenome.CreateInitialVariant(InitialGenomeVariation)
                : EvolutionGenome.CreateRandom();
            ApplyStartingDietNicheSeed(genome, i, amount);

            EvolutionCandidate candidate = new EvolutionCandidate(genome);
            SpawnCreature(candidate, GetSpawnPointForCreature());
        }
    }

    private void SpawnStartingFood()
    {
        for (int i = 0; i < StartingFood; i++)
        {
            SpawnFood();
        }
    }

    private void HandleFoodSpawning()
    {
        if (UsePlacedPlantsForFood && !EnableRandomFoodFallback)
        {
            return;
        }

        if (foodSpawnTimer < FoodSpawnInterval)
        {
            return;
        }

        foodSpawnTimer = 0f;
        float multiplier = Environment != null ? Environment.FoodSpawnMultiplier : 1f;
        int targetFood = Mathf.RoundToInt(TargetFoodAmount * multiplier);

        if (activeFood.Count < targetFood)
        {
            SpawnFood();
        }
    }

    private void HandleExtinctionEvents()
    {
        if (!UseExtinctionEvents)
        {
            return;
        }

        if (extinctionTimer < ExtinctionEventInterval)
        {
            return;
        }

        extinctionTimer = 0f;
        TriggerExtinctionEvent();
    }

    private void EndGenerationAndSpawnNext()
    {
        List<EvolutionCandidate> evaluated = GatherEvaluationCandidates();

        if (StatsTracker != null)
        {
            StatsTracker.RecordGeneration(CurrentGeneration, evaluated, activeCreatures.Count, offspringPool.Count);
        }

        List<EvolutionCandidate> nextGeneration = SelectNextGeneration(evaluated);

        if (bootstrapRoutine != null)
        {
            StopCoroutine(bootstrapRoutine);
        }

        bootstrapRoutine = StartCoroutine(SpawnNextGenerationRoutine(nextGeneration));
    }

    private IEnumerator SpawnNextGenerationRoutine(List<EvolutionCandidate> nextGeneration)
    {
        ClearCreaturesOnly();
        CurrentGeneration++;
        GenerationTimer = 0f;
        offspringPool.Clear();
        CreatureMorphLibrary.SetActiveLibrary(MorphLibrary);

        int budget = Mathf.Max(1, CreatureSpawnsPerFrame);
        for (int i = 0; i < nextGeneration.Count; i++)
        {
            SpawnCreature(nextGeneration[i], GetSpawnPointForCreature());
            if (StaggerInitialSpawn && i % budget == budget - 1)
            {
                yield return null;
            }
        }

        EnsureSpatialGrid();
        bootstrapRoutine = null;
        Debug.Log("Started generation " + CurrentGeneration + " with " + nextGeneration.Count + " creatures.");
    }

    private List<EvolutionCandidate> GatherEvaluationCandidates()
    {
        List<EvolutionCandidate> evaluated = new List<EvolutionCandidate>();

        for (int i = 0; i < activeCreatures.Count; i++)
        {
            MarineCreatureAgent creature = activeCreatures[i];

            if (creature != null && creature.Candidate != null)
            {
                evaluated.Add(creature.Candidate);
            }
        }

        for (int i = 0; i < offspringPool.Count; i++)
        {
            if (offspringPool[i] != null)
            {
                evaluated.Add(offspringPool[i]);
            }
        }

        if (evaluated.Count == 0)
        {
            for (int i = 0; i < FixedPopulationSize; i++)
            {
                EvolutionGenome fallback = StartFromUniformBaseline ? EvolutionGenome.CreateBaseline() : EvolutionGenome.CreateRandom();
                ApplyStartingDietNicheSeed(fallback, i, FixedPopulationSize);
                evaluated.Add(new EvolutionCandidate(fallback));
            }
        }

        return evaluated;
    }

    private List<EvolutionCandidate> SelectNextGeneration(List<EvolutionCandidate> evaluated)
    {
        List<EvolutionCandidate> selected = new List<EvolutionCandidate>();
        int target = Mathf.Max(1, FixedPopulationSize);

        evaluated.Sort((a, b) => b.GetFitness().CompareTo(a.GetFitness()));

        if (Selection == SelectionMode.Elite)
        {
            for (int i = 0; i < target; i++)
            {
                EvolutionCandidate parent = evaluated[i % evaluated.Count];
                selected.Add(parent.CreateChild(GetEnvironmentMutationMultiplier()));
            }

            return FinaliseSelectedGeneration(selected, target, evaluated);
        }

        if (Selection == SelectionMode.QualityDiversityLite)
        {
            return FinaliseSelectedGeneration(SelectQualityDiversityLite(evaluated, target), target, evaluated);
        }

        for (int i = 0; i < target; i++)
        {
            EvolutionCandidate parent = RunTournament(evaluated);
            selected.Add(parent.CreateChild(GetEnvironmentMutationMultiplier()));
        }

        return FinaliseSelectedGeneration(selected, target, evaluated);
    }

    private List<EvolutionCandidate> SelectQualityDiversityLite(List<EvolutionCandidate> evaluated, int target)
    {
        List<EvolutionCandidate> selected = new List<EvolutionCandidate>();
        Dictionary<string, List<EvolutionCandidate>> buckets = new Dictionary<string, List<EvolutionCandidate>>();
        Dictionary<string, int> evaluatedCoreCounts = BuildCoreNicheCounts(evaluated);
        Dictionary<string, int> selectedCoreCounts = new Dictionary<string, int>();
        int maxPerCore = Mathf.Max(1, Mathf.CeilToInt(target * Mathf.Clamp01(MaxSameCoreNicheFraction)));

        for (int i = 0; i < evaluated.Count; i++)
        {
            EvolutionCandidate candidate = evaluated[i];
            if (candidate == null || candidate.Genome == null)
            {
                continue;
            }

            string key = BuildQualityDiversityKey(candidate);
            if (!buckets.ContainsKey(key))
            {
                buckets[key] = new List<EvolutionCandidate>();
            }

            buckets[key].Add(candidate);
        }

        foreach (KeyValuePair<string, List<EvolutionCandidate>> pair in buckets)
        {
            pair.Value.Sort((a, b) => GetSelectionScore(b, evaluatedCoreCounts).CompareTo(GetSelectionScore(a, evaluatedCoreCounts)));
        }

        int roundIndex = 0;
        int safety = 0;
        while (selected.Count < target && safety < target * 30)
        {
            safety++;
            bool addedThisRound = false;

            foreach (KeyValuePair<string, List<EvolutionCandidate>> pair in buckets)
            {
                if (selected.Count >= target)
                {
                    break;
                }

                if (roundIndex >= pair.Value.Count)
                {
                    continue;
                }

                EvolutionCandidate parent = pair.Value[roundIndex];
                string coreKey = BuildCoreNicheKey(parent);

                if (UseAntiConvergence && GetDictionaryCount(selectedCoreCounts, coreKey) >= maxPerCore)
                {
                    continue;
                }

                selected.Add(parent.CreateChild(GetAdaptiveMutationMultiplier(evaluatedCoreCounts)));
                IncrementDictionaryCount(selectedCoreCounts, coreKey);
                addedThisRound = true;
            }

            if (!addedThisRound)
            {
                break;
            }

            roundIndex++;
        }

        // Fill remaining slots. Still respect the cap when possible, then relax it only if the
        // population would otherwise fail to fill. This keeps the sim stable without letting one
        // niche instantly dominate after a lucky generation.
        int fallbackSafety = 0;
        while (selected.Count < target && fallbackSafety < target * 20)
        {
            fallbackSafety++;
            EvolutionCandidate parent = RunTournament(evaluated, evaluatedCoreCounts);
            if (parent == null)
            {
                break;
            }

            string coreKey = BuildCoreNicheKey(parent);
            if (UseAntiConvergence && GetDictionaryCount(selectedCoreCounts, coreKey) >= maxPerCore && selectedCoreCounts.Count >= MinimumSurvivingCoreNiches)
            {
                continue;
            }

            selected.Add(parent.CreateChild(GetAdaptiveMutationMultiplier(evaluatedCoreCounts)));
            IncrementDictionaryCount(selectedCoreCounts, coreKey);
        }

        while (selected.Count < target)
        {
            EvolutionCandidate parent = RunTournament(evaluated, evaluatedCoreCounts);
            if (parent == null)
            {
                break;
            }

            selected.Add(parent.CreateChild(GetAdaptiveMutationMultiplier(evaluatedCoreCounts)));
        }

        return selected;
    }


    private List<EvolutionCandidate> FinaliseSelectedGeneration(List<EvolutionCandidate> selected, int target, List<EvolutionCandidate> evaluated)
    {
        if (selected == null)
        {
            selected = new List<EvolutionCandidate>();
        }

        while (selected.Count < target)
        {
            EvolutionGenome genome = StartFromUniformBaseline && baselineGenome != null ? baselineGenome.Clone() : EvolutionGenome.CreateRandom();
            ApplyStartingDietNicheSeed(genome, selected.Count, target);
            selected.Add(new EvolutionCandidate(genome));
        }

        if (!ReplaceMissingDietNichesEachGeneration)
        {
            return UseAntiConvergence ? RescueCollapsedDiversity(selected, target, evaluated) : selected;
        }

        int predatorCount = 0;
        int scavengerCount = 0;
        for (int i = 0; i < selected.Count; i++)
        {
            EvolutionCandidate candidate = selected[i];
            if (candidate == null || candidate.Genome == null)
            {
                continue;
            }

            if (IsPredatorGenome(candidate.Genome))
            {
                predatorCount++;
            }
            else if (IsScavengerGenome(candidate.Genome))
            {
                scavengerCount++;
            }
        }

        int replaceIndex = selected.Count - 1;
        int predatorTarget = Mathf.Clamp(MinimumPredatorSeedsPerGeneration, 0, Mathf.Max(1, target / 4));
        while (predatorCount < predatorTarget && replaceIndex >= 0)
        {
            EvolutionGenome genome = StartFromUniformBaseline && baselineGenome != null ? baselineGenome.Clone() : EvolutionGenome.CreateRandom();
            ForcePredatorGenome(genome);
            selected[replaceIndex] = new EvolutionCandidate(genome);
            predatorCount++;
            replaceIndex--;
        }

        int scavengerTarget = Mathf.Clamp(MinimumScavengerSeedsPerGeneration, 0, Mathf.Max(1, target / 5));
        while (scavengerCount < scavengerTarget && replaceIndex >= 0)
        {
            EvolutionGenome genome = StartFromUniformBaseline && baselineGenome != null ? baselineGenome.Clone() : EvolutionGenome.CreateRandom();
            ForceScavengerGenome(genome);
            selected[replaceIndex] = new EvolutionCandidate(genome);
            scavengerCount++;
            replaceIndex--;
        }

        return UseAntiConvergence ? RescueCollapsedDiversity(selected, target, evaluated) : selected;
    }

    private List<EvolutionCandidate> RescueCollapsedDiversity(List<EvolutionCandidate> selected, int target, List<EvolutionCandidate> evaluated)
    {
        if (!UseAntiConvergence || !InjectMissingNichesWhenCollapsed || selected == null || selected.Count == 0)
        {
            return selected;
        }

        Dictionary<string, int> selectedCounts = BuildCoreNicheCounts(selected);
        int dominantCount = 0;
        foreach (KeyValuePair<string, int> pair in selectedCounts)
        {
            dominantCount = Mathf.Max(dominantCount, pair.Value);
        }

        float dominantFraction = selected.Count > 0 ? dominantCount / (float)selected.Count : 0f;
        bool collapsed = selectedCounts.Count < MinimumSurvivingCoreNiches || dominantFraction > MaxSameCoreNicheFraction;
        if (!collapsed)
        {
            return selected;
        }

        string[] desiredNiches =
        {
            "grazer",
            "schooling_grazer",
            "defensive_grazer",
            "omnivore",
            "scavenger",
            "active_predator",
            "ambush_predator",
            "egg_guardian"
        };

        int injected = 0;
        for (int i = 0; i < desiredNiches.Length && injected < MaximumDiversityRescueMutants; i++)
        {
            string desired = desiredNiches[i];
            if (selectedCounts.ContainsKey(desired))
            {
                continue;
            }

            int replaceIndex = FindReplaceableDominantIndex(selected, selectedCounts);
            if (replaceIndex < 0)
            {
                break;
            }

            EvolutionCandidate rescue = CreateDiversityRescueCandidate(desired, evaluated);
            if (rescue == null)
            {
                continue;
            }

            string oldKey = BuildCoreNicheKey(selected[replaceIndex]);
            if (selectedCounts.ContainsKey(oldKey))
            {
                selectedCounts[oldKey] = Mathf.Max(0, selectedCounts[oldKey] - 1);
            }

            selected[replaceIndex] = rescue;
            IncrementDictionaryCount(selectedCounts, desired);
            injected++;
        }

        if (injected > 0)
        {
            Debug.Log("[IRP Anti-Convergence] Injected " + injected + " rescue mutants. Core niches now: " + selectedCounts.Count + ".");
        }

        return selected;
    }

    private int FindReplaceableDominantIndex(List<EvolutionCandidate> selected, Dictionary<string, int> selectedCounts)
    {
        if (selected == null || selectedCounts == null || selected.Count == 0)
        {
            return -1;
        }

        string dominantKey = "";
        int dominantCount = 0;
        foreach (KeyValuePair<string, int> pair in selectedCounts)
        {
            if (pair.Value > dominantCount)
            {
                dominantKey = pair.Key;
                dominantCount = pair.Value;
            }
        }

        if (dominantCount <= 1)
        {
            return -1;
        }

        for (int i = selected.Count - 1; i >= 0; i--)
        {
            EvolutionCandidate candidate = selected[i];
            if (candidate == null || candidate.Genome == null)
            {
                return i;
            }

            if (BuildCoreNicheKey(candidate) == dominantKey)
            {
                return i;
            }
        }

        return -1;
    }

    private EvolutionCandidate CreateDiversityRescueCandidate(string desiredNiche, List<EvolutionCandidate> evaluated)
    {
        EvolutionGenome genome = null;

        if (evaluated != null && evaluated.Count > 0 && Random.value < 0.65f)
        {
            EvolutionCandidate parent = evaluated[Random.Range(0, evaluated.Count)];
            if (parent != null && parent.Genome != null)
            {
                genome = parent.Genome.CreateMutatedCopy(Mathf.Max(1f, CollapseMutationMultiplier));
            }
        }

        if (genome == null)
        {
            genome = StartFromUniformBaseline && baselineGenome != null ? baselineGenome.CreateMutatedCopy(Mathf.Max(1f, CollapseMutationMultiplier)) : EvolutionGenome.CreateRandom();
        }

        ForceGenomeTowardCoreNiche(genome, desiredNiche);
        return new EvolutionCandidate(genome);
    }

    private void ForceGenomeTowardCoreNiche(EvolutionGenome genome, string desiredNiche)
    {
        if (genome == null)
        {
            return;
        }

        switch (desiredNiche)
        {
            case "active_predator":
                ForcePredatorGenome(genome);
                genome.Stealth = Mathf.Min(genome.Stealth, Random.Range(0.12f, 0.38f));
                genome.Speed = Mathf.Max(genome.Speed, Random.Range(5.4f, 7.0f));
                break;

            case "ambush_predator":
                ForcePredatorGenome(genome);
                genome.Stealth = Mathf.Max(genome.Stealth, Random.Range(0.58f, 0.88f));
                genome.Territoriality = Mathf.Max(genome.Territoriality, Random.Range(0.42f, 0.78f));
                genome.Speed = Mathf.Clamp(genome.Speed, 3.4f, 5.5f);
                break;

            case "scavenger":
                ForceScavengerGenome(genome);
                break;

            case "omnivore":
                ForceOmnivoreGenome(genome);
                break;

            case "defensive_grazer":
                ForceGrazerGenome(genome);
                genome.Armour = Mathf.Max(genome.Armour, Random.Range(0.55f, 0.95f));
                genome.SpikeSize = Mathf.Max(genome.SpikeSize, Random.Range(0.65f, 1.15f));
                genome.DangerFactor = Mathf.Max(genome.DangerFactor, Random.Range(0.75f, 1.35f));
                genome.RiskTolerance = Mathf.Clamp(genome.RiskTolerance, 0.22f, 0.58f);
                break;

            case "schooling_grazer":
                ForceGrazerGenome(genome);
                genome.GroupingChance = Mathf.Max(genome.GroupingChance, Random.Range(0.62f, 0.92f));
                genome.SchoolTightness = Mathf.Max(genome.SchoolTightness, Random.Range(0.52f, 0.88f));
                genome.FoodSharing = Mathf.Max(genome.FoodSharing, Random.Range(0.62f, 0.92f));
                genome.Selfishness = Mathf.Min(genome.Selfishness, Random.Range(0.02f, 0.22f));
                break;

            case "egg_guardian":
                ForceGrazerGenome(genome);
                genome.EggProtection = Mathf.Max(genome.EggProtection, Random.Range(0.68f, 0.95f));
                genome.NestingDrive = Mathf.Max(genome.NestingDrive, Random.Range(0.58f, 0.90f));
                genome.Territoriality = Mathf.Max(genome.Territoriality, Random.Range(0.38f, 0.72f));
                genome.MateDrive = Mathf.Max(genome.MateDrive, Random.Range(0.62f, 0.92f));
                break;

            default:
                ForceGrazerGenome(genome);
                break;
        }

        genome.MutationRate = Mathf.Max(genome.MutationRate, 0.075f);
        genome.MutationStrength = Mathf.Max(genome.MutationStrength, 1.0f);
        genome.MorphPartMutationRate = Mathf.Max(genome.MorphPartMutationRate, 0.035f);
        genome.NormaliseDietTraits();
        genome.ClampValues();
    }

    private void ForceGrazerGenome(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return;
        }

        genome.PlantDiet = Random.Range(0.58f, 0.78f);
        genome.MeatDiet = Random.Range(0.04f, 0.18f);
        genome.CarrionDiet = Random.Range(0.08f, 0.22f);
        genome.Aggression = Mathf.Min(genome.Aggression, Random.Range(0.05f, 0.24f));
        genome.RiskTolerance = Mathf.Clamp(genome.RiskTolerance, 0.25f, 0.72f);
        genome.HungerDrive = Mathf.Clamp(genome.HungerDrive, 0.48f, 0.82f);
        genome.MateDrive = Mathf.Max(genome.MateDrive, 0.42f);
        genome.NormaliseDietTraits();
        genome.ClampValues();
    }

    private void ApplyStartingDietNicheSeed(EvolutionGenome genome, int index, int total)
    {
        if (!SeedStartingDietNiches || genome == null || total <= 0)
        {
            return;
        }

        // Random niche bias. Predators/scavengers can appear in generation 1,
        // but the ecosystem is not guaranteed to start with a fixed ratio.
        float roll = Random.value;
        float predatorChance = Mathf.Clamp01(StartingPredatorFraction);
        float scavengerChance = Mathf.Clamp01(StartingScavengerFraction);
        float omnivoreChance = 0.18f;

        if (roll < predatorChance)
        {
            ForcePredatorGenome(genome);
        }
        else if (roll < predatorChance + scavengerChance)
        {
            ForceScavengerGenome(genome);
        }
        else if (roll < predatorChance + scavengerChance + omnivoreChance)
        {
            ForceOmnivoreGenome(genome);
        }
    }

    private bool IsPredatorGenome(EvolutionGenome genome)
    {
        return genome != null && genome.MeatDiet >= MinimumMeatDietToHunt && genome.Aggression >= MinimumAggressionToHunt;
    }

    private bool IsScavengerGenome(EvolutionGenome genome)
    {
        return genome != null && genome.CarrionDiet >= 0.38f;
    }

    private void ForcePredatorGenome(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return;
        }

        genome.PlantDiet = Random.Range(0.12f, 0.24f);
        genome.MeatDiet = Random.Range(0.56f, 0.76f);
        genome.CarrionDiet = Random.Range(0.10f, 0.24f);
        genome.Aggression = Mathf.Max(genome.Aggression, Random.Range(0.42f, 0.72f));
        genome.RiskTolerance = Mathf.Max(genome.RiskTolerance, Random.Range(0.55f, 0.86f));
        genome.JawSize = Mathf.Max(genome.JawSize, Random.Range(1.12f, 1.55f));
        genome.JawLength = Mathf.Max(genome.JawLength, Random.Range(1.05f, 1.45f));
        genome.Speed = Mathf.Max(genome.Speed, Random.Range(4.8f, 6.8f));
        genome.HungerThreshold = Mathf.Min(genome.HungerThreshold, 0.42f);
        genome.MateDrive = Mathf.Max(genome.MateDrive, 0.42f);
        genome.NormaliseDietTraits();
        genome.ClampValues();
    }

    private void ForceScavengerGenome(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return;
        }

        genome.PlantDiet = Random.Range(0.18f, 0.34f);
        genome.MeatDiet = Random.Range(0.12f, 0.28f);
        genome.CarrionDiet = Random.Range(0.48f, 0.68f);
        genome.Aggression = Mathf.Max(genome.Aggression, Random.Range(0.18f, 0.42f));
        genome.SensorSize = Mathf.Max(genome.SensorSize, Random.Range(1.15f, 1.65f));
        genome.VisionRange = Mathf.Max(genome.VisionRange, Random.Range(20f, 32f));
        genome.FoodMemoryStrength = Mathf.Max(genome.FoodMemoryStrength, 0.55f);
        genome.HungerThreshold = Mathf.Min(genome.HungerThreshold, 0.46f);
        genome.MateDrive = Mathf.Max(genome.MateDrive, 0.42f);
        genome.NormaliseDietTraits();
        genome.ClampValues();
    }

    private void ForceOmnivoreGenome(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return;
        }

        genome.PlantDiet = Random.Range(0.34f, 0.46f);
        genome.MeatDiet = Random.Range(0.24f, 0.36f);
        genome.CarrionDiet = Random.Range(0.18f, 0.30f);
        genome.Aggression = Mathf.Max(genome.Aggression, Random.Range(0.18f, 0.36f));
        genome.RiskTolerance = Mathf.Max(genome.RiskTolerance, Random.Range(0.42f, 0.70f));
        genome.MateDrive = Mathf.Max(genome.MateDrive, 0.45f);
        genome.NormaliseDietTraits();
        genome.ClampValues();
    }


    private string BuildQualityDiversityKey(EvolutionCandidate candidate)
    {
        if (candidate == null || candidate.Genome == null)
        {
            return "unknown";
        }

        if (!UseBehaviourFeatureBuckets)
        {
            return CreatureDebugTypeUtility.GetSpeciesGroupName(candidate.Genome);
        }

        int bins = Mathf.Clamp(QualityDiversityBins, 2, 8);
        return EvolutionNicheUtility.BuildSelectionKey(candidate, bins);
    }

    private string BuildCoreNicheKey(EvolutionCandidate candidate)
    {
        return EvolutionNicheUtility.BuildCoreNicheKey(candidate);
    }

    private Dictionary<string, int> BuildCoreNicheCounts(List<EvolutionCandidate> candidates)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        if (candidates == null)
        {
            return counts;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            EvolutionCandidate candidate = candidates[i];
            if (candidate == null || candidate.Genome == null)
            {
                continue;
            }

            IncrementDictionaryCount(counts, BuildCoreNicheKey(candidate));
        }

        return counts;
    }

    private int GetDictionaryCount(Dictionary<string, int> counts, string key)
    {
        if (counts == null || string.IsNullOrEmpty(key) || !counts.ContainsKey(key))
        {
            return 0;
        }

        return counts[key];
    }

    private void IncrementDictionaryCount(Dictionary<string, int> counts, string key)
    {
        if (counts == null || string.IsNullOrEmpty(key))
        {
            return;
        }

        if (!counts.ContainsKey(key))
        {
            counts[key] = 0;
        }

        counts[key]++;
    }

    private float GetSelectionScore(EvolutionCandidate candidate, Dictionary<string, int> coreCounts = null)
    {
        if (candidate == null)
        {
            return 0f;
        }

        float score = candidate.GetFitness();

        if (UseNoveltySelectionBias)
        {
            score += GetBehaviourNoveltyScore(candidate) * Mathf.Max(0f, NoveltySelectionBonus);
        }

        if (UseAntiConvergence && coreCounts != null && RareNicheSelectionBonus > 0f)
        {
            int count = Mathf.Max(1, GetDictionaryCount(coreCounts, BuildCoreNicheKey(candidate)));
            score += RareNicheSelectionBonus / count;
        }

        if (candidate.Genome != null && candidate.Genome.Brain != null && BrainComplexityPenalty > 0f)
        {
            int excessHidden = Mathf.Max(0, candidate.Genome.Brain.HiddenCount - EvolutionGenome.BrainHiddenCount);
            score -= excessHidden * BrainComplexityPenalty;
        }

        return Mathf.Max(0f, score);
    }

    private float GetBehaviourNoveltyScore(EvolutionCandidate candidate)
    {
        if (candidate == null || candidate.Genome == null)
        {
            return 0f;
        }

        float activeModeCount = 0f;
        if (candidate.RestingTime > 1f) activeModeCount += 1f;
        if (candidate.ExploringTime > 1f) activeModeCount += 1f;
        if (candidate.SchoolingTime > 1f) activeModeCount += 1f;
        if (candidate.ForagingTime > 1f) activeModeCount += 1f;
        if (candidate.FeedingTime > 1f) activeModeCount += 1f;
        if (candidate.MateSeekingTime > 1f) activeModeCount += 1f;
        if (candidate.HuntingTime > 1f) activeModeCount += 1f;
        if (candidate.FleeingTime > 1f) activeModeCount += 1f;
        if (candidate.RecoveryTime > 1f) activeModeCount += 0.5f;

        float modeDiversity = Mathf.Clamp01(activeModeCount / 8f);
        float dietSpecialisation = Mathf.Max(candidate.Genome.PlantDiet, Mathf.Max(candidate.Genome.MeatDiet, candidate.Genome.CarrionDiet));
        float dietNovelty = 1f - Mathf.Clamp01(dietSpecialisation);
        float reproductionNovelty = candidate.EggsLaid > 0 || candidate.ReproductionCount > 0 ? 0.15f : 0f;
        float predatorNovelty = candidate.PreyBites > 0 || candidate.PreyKills > 0 ? 0.15f : 0f;
        float mobilityNovelty = Mathf.Clamp01(candidate.DistanceTravelled / 450f) * 0.2f;

        return Mathf.Clamp01(modeDiversity * 0.42f + dietNovelty * 0.28f + reproductionNovelty + predatorNovelty + mobilityNovelty);
    }

    private EvolutionCandidate RunTournament(List<EvolutionCandidate> evaluated, Dictionary<string, int> coreCounts = null)
    {
        if (evaluated == null || evaluated.Count == 0)
        {
            return null;
        }

        EvolutionCandidate best = null;
        int size = Mathf.Max(1, TournamentSize);

        for (int i = 0; i < size; i++)
        {
            EvolutionCandidate candidate = evaluated[Random.Range(0, evaluated.Count)];

            if (best == null || GetSelectionScore(candidate, coreCounts) > GetSelectionScore(best, coreCounts))
            {
                best = candidate;
            }
        }

        return best;
    }

    private float GetAdaptiveMutationMultiplier(Dictionary<string, int> coreCounts)
    {
        float multiplier = GetEnvironmentMutationMultiplier();

        if (!UseAntiConvergence || coreCounts == null)
        {
            return multiplier;
        }

        if (coreCounts.Count < MinimumSurvivingCoreNiches)
        {
            multiplier *= CollapseMutationMultiplier;
        }

        return Mathf.Clamp(multiplier, 0.5f, 3.0f);
    }

    private float GetEnvironmentMutationMultiplier()
    {
        float baseMultiplier = Environment != null ? Environment.MutationMultiplier : 1f;

        if (activePressureZones.Count == 0)
        {
            return baseMultiplier;
        }

        // End-of-generation selection uses a population-wide average pressure, while
        // egg reproduction uses local pressure in MarineCreatureAgent.
        float total = 0f;
        int count = 0;
        for (int i = 0; i < activePressureZones.Count; i++)
        {
            EcosystemPressureZone zone = activePressureZones[i];
            if (zone == null)
            {
                continue;
            }

            total += Mathf.Max(0.1f, zone.MutationMultiplier);
            count++;
        }

        return count > 0 ? baseMultiplier * Mathf.Clamp(total / count, 0.75f, 1.8f) : baseMultiplier;
    }

    public float GetEnvironmentMutationMultiplierAt(Vector3 position)
    {
        float multiplier = Environment != null ? Environment.MutationMultiplier : 1f;

        for (int i = activePressureZones.Count - 1; i >= 0; i--)
        {
            EcosystemPressureZone zone = activePressureZones[i];
            if (zone == null)
            {
                activePressureZones.RemoveAt(i);
                continue;
            }

            float influence = zone.GetInfluence01(position);
            if (influence > 0f)
            {
                multiplier *= Mathf.Lerp(1f, Mathf.Max(0.1f, zone.MutationMultiplier), influence);
            }
        }

        return Mathf.Clamp(multiplier, 0.5f, 2.2f);
    }


    private void EnsureResearchTools()
    {
        if (!AutoAttachResearchTools)
        {
            return;
        }

        EvolutionPopulationSaveLoad saveLoad = GetComponent<EvolutionPopulationSaveLoad>();
        if (saveLoad == null)
        {
            saveLoad = gameObject.AddComponent<EvolutionPopulationSaveLoad>();
        }
        saveLoad.Manager = this;

        EvolutionResearchMetricsRecorder recorder = GetComponent<EvolutionResearchMetricsRecorder>();
        if (recorder == null)
        {
            recorder = gameObject.AddComponent<EvolutionResearchMetricsRecorder>();
        }
        recorder.Manager = this;

        IRPBehaviourArchive archive = GetComponent<IRPBehaviourArchive>();
        if (archive == null)
        {
            archive = gameObject.AddComponent<IRPBehaviourArchive>();
        }
        archive.Manager = this;

        IRPExperimentController experiment = GetComponent<IRPExperimentController>();
        if (experiment == null)
        {
            experiment = gameObject.AddComponent<IRPExperimentController>();
        }
        experiment.Manager = this;
        experiment.Environment = Environment;
        experiment.MetricsRecorder = recorder;
        experiment.BehaviourArchive = archive;
    }

    private void EnsureSpatialGrid()
    {
        if (!UseSpatialGrid)
        {
            return;
        }

        if (SpatialGrid == null)
        {
            SpatialGrid = GetComponent<EcosystemSpatialGrid>();
        }

        if (SpatialGrid == null)
        {
            SpatialGrid = gameObject.AddComponent<EcosystemSpatialGrid>();
        }

        SpatialGrid.CellSize = Mathf.Max(1f, SpatialGridCellSize);
        SpatialGrid.Rebuild(activeCreatures, activeFood, activeCarrion, activeEggClusters);
    }

    private void UpdateSpatialGridIfNeeded()
    {
        if (!UseSpatialGrid)
        {
            return;
        }

        spatialGridTimer -= Time.deltaTime;
        if (SpatialGrid == null || spatialGridTimer <= 0f)
        {
            spatialGridTimer = Mathf.Max(0.05f, SpatialGridRefreshInterval);
            EnsureSpatialGrid();
        }
    }

    private bool HasUsableSpatialGrid()
    {
        return UseSpatialGrid && SpatialGrid != null;
    }

    public List<MarineCreatureAgent> GetNearbyCreatures(Vector3 position, float radius)
    {
        if (HasUsableSpatialGrid())
        {
            return SpatialGrid.QueryCreatures(position, radius);
        }

        return activeCreatures;
    }

    public List<FoodSource> GetNearbyFood(Vector3 position, float radius)
    {
        if (HasUsableSpatialGrid())
        {
            return SpatialGrid.QueryFood(position, radius);
        }

        return activeFood;
    }

    public List<CarrionSource> GetNearbyCarrion(Vector3 position, float radius)
    {
        if (HasUsableSpatialGrid())
        {
            return SpatialGrid.QueryCarrion(position, radius);
        }

        return activeCarrion;
    }

    public List<FishEggCluster> GetNearbyEggClusters(Vector3 position, float radius)
    {
        if (HasUsableSpatialGrid())
        {
            return SpatialGrid.QueryEggClusters(position, radius);
        }

        return activeEggClusters;
    }

    public MarineCreatureAgent SpawnCreature(EvolutionCandidate candidate, Vector3 position)
    {
        if (CreaturePrefab == null)
        {
            Debug.LogError("CreaturePrefab is missing on EvolutionEcosystemManager.");
            return null;
        }

        if (candidate == null)
        {
            candidate = new EvolutionCandidate(StartFromUniformBaseline ? EvolutionGenome.CreateBaseline() : EvolutionGenome.CreateRandom());
        }

        int parentId = candidate.ParentId;
        candidate.AssignRuntimeIdentity(nextCreatureId, CurrentGeneration, parentId);
        nextCreatureId++;

        MarineCreatureAgent creature = Instantiate(CreaturePrefab, position, Quaternion.identity);
        creature.MorphLibrary = MorphLibrary;
        creature.Initialise(candidate);

        activeCreatures.Add(creature);
        return creature;
    }

    private void SpawnFood()
    {
        if (FoodPrefab == null)
        {
            Debug.LogError("FoodPrefab is missing on EvolutionEcosystemManager.");
            return;
        }

        FoodSource food = Instantiate(FoodPrefab, GetSpawnPointForFood(), Quaternion.identity);
        if (food.MaxMass <= 0f)
        {
            food.MaxMass = Mathf.Max(1f, food.EnergyValue);
        }
        if (food.RemainingMass < 0f)
        {
            food.RemainingMass = food.MaxMass;
        }
        RegisterFood(food);
    }

    public void SpawnCarrionFromDeath(MarineCreatureAgent creature, bool causedByExtinctionEvent)
    {
        if (creature == null || creature.Candidate == null || creature.Candidate.Genome == null)
        {
            return;
        }

        if (!SpawnCarrionFromDeaths)
        {
            return;
        }

        if (causedByExtinctionEvent && !SpawnCarrionFromExtinctionEvents)
        {
            return;
        }

        CleanLists();

        if (activeCarrion.Count >= MaxCarrionSources)
        {
            RemoveOldestCarrion();
        }

        float size = creature.EffectiveStats != null ? creature.EffectiveStats.BodySize : creature.Candidate.Genome.BodySize;
        float energyValue = 10f + size * CarrionEnergyFromBodySize;
        energyValue += Mathf.Max(0f, creature.CurrentEnergy) * 0.15f;

        CarrionSource carrion;

        if (CarrionPrefab != null)
        {
            carrion = Instantiate(CarrionPrefab, ProjectPointAboveTerrain(creature.transform.position, CarrionTerrainClearance), Quaternion.identity);
        }
        else
        {
            GameObject carrionObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            carrionObject.name = "Carrion_Source";
            carrionObject.transform.position = ProjectPointAboveTerrain(creature.transform.position, CarrionTerrainClearance);
            carrionObject.transform.localScale = Vector3.one * Mathf.Clamp(size * 0.45f, 0.25f, 1.4f);
            carrion = carrionObject.AddComponent<CarrionSource>();
        }

        carrion.EnergyValue = energyValue;
        carrion.MaxMass = Mathf.Max(1f, energyValue);
        carrion.RemainingMass = carrion.MaxMass;
        RegisterCarrion(carrion);
    }

    private void RemoveOldestCarrion()
    {
        CleanLists();
        CarrionSource oldest = null;
        float oldestAge = float.MinValue;

        for (int i = 0; i < activeCarrion.Count; i++)
        {
            CarrionSource carrion = activeCarrion[i];
            if (carrion == null)
            {
                continue;
            }

            if (carrion.Age > oldestAge)
            {
                oldestAge = carrion.Age;
                oldest = carrion;
            }
        }

        if (oldest != null)
        {
            UnregisterCarrion(oldest);
            Destroy(oldest.gameObject);
        }
    }

    public void RegisterOffspring(EvolutionCandidate offspring)
    {
        if (offspring == null)
        {
            return;
        }

        offspringPool.Add(offspring);
    }

    public void UnregisterCreature(MarineCreatureAgent creature)
    {
        activeCreatures.Remove(creature);
    }

    public void UnregisterFood(FoodSource food)
    {
        activeFood.Remove(food);
    }

    public void RegisterCarrion(CarrionSource carrion)
    {
        if (carrion != null && !activeCarrion.Contains(carrion))
        {
            activeCarrion.Add(carrion);
        }
    }

    public void UnregisterCarrion(CarrionSource carrion)
    {
        activeCarrion.Remove(carrion);
    }

    public void RegisterFood(FoodSource food)
    {
        if (food != null && !activeFood.Contains(food))
        {
            activeFood.Add(food);
        }
    }

    public void RegisterPlant(PlantResource plant)
    {
        if (plant != null && !activePlants.Contains(plant))
        {
            activePlants.Add(plant);
        }
    }

    public void UnregisterPlant(PlantResource plant)
    {
        activePlants.Remove(plant);
    }

    public void RegisterEggCluster(FishEggCluster eggCluster)
    {
        if (eggCluster != null && !activeEggClusters.Contains(eggCluster))
        {
            activeEggClusters.Add(eggCluster);
        }
    }

    public void UnregisterEggCluster(FishEggCluster eggCluster)
    {
        activeEggClusters.Remove(eggCluster);
    }

    public void RegisterCurrentZone(EcosystemWaterCurrentZone zone)
    {
        if (zone != null && !activeCurrentZones.Contains(zone))
        {
            activeCurrentZones.Add(zone);
        }
    }

    public void UnregisterCurrentZone(EcosystemWaterCurrentZone zone)
    {
        activeCurrentZones.Remove(zone);
    }

    public void RegisterPressureZone(EcosystemPressureZone zone)
    {
        if (zone != null && !activePressureZones.Contains(zone))
        {
            activePressureZones.Add(zone);
        }
    }

    public void UnregisterPressureZone(EcosystemPressureZone zone)
    {
        activePressureZones.Remove(zone);
    }


    public FoodSource GetBestFoodForCreature(MarineCreatureAgent requester, Vector3 position, float searchRadius, float crowdRadius, int comfortableCrowdLimit, float crowdPenalty)
    {
        // Emergency optimisation:
        // Closest useful food wins. The previous crowd-aware scoring checked every food
        // against every creature, which is brutal with lots of plant buds.
        FoodSource best = null;
        float bestScore = float.MaxValue;
        float searchRadiusSqr = searchRadius * searchRadius;
        int requesterId = requester != null && requester.Candidate != null ? requester.Candidate.Id : 0;

        List<FoodSource> foodCandidates = GetNearbyFood(position, searchRadius);
        for (int i = 0; i < foodCandidates.Count; i++)
        {
            FoodSource food = foodCandidates[i];
            if (food == null || food.IsConsumed)
            {
                continue;
            }

            float distanceSqr = (food.transform.position - position).sqrMagnitude;
            if (distanceSqr > searchRadiusSqr)
            {
                continue;
            }

            float score = distanceSqr;

            if (requesterId != 0 && food.WasRecentlyFedBy(requesterId))
            {
                score *= 0.20f;
            }

            if (food.IsDetachedPlantBud())
            {
                score *= 0.75f;
            }

            if (food.GetMassRatio() < 0.08f)
            {
                score *= 1.35f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = food;
            }
        }

        return best;
    }

    public CarrionSource GetBestCarrionForCreature(MarineCreatureAgent requester, Vector3 position, float searchRadius, float crowdRadius, int comfortableCrowdLimit, float crowdPenalty)
    {
        // Emergency optimisation: closest valid carrion wins.
        CarrionSource best = null;
        float bestScore = searchRadius * searchRadius;

        List<CarrionSource> carrionCandidates = GetNearbyCarrion(position, searchRadius);
        for (int i = 0; i < carrionCandidates.Count; i++)
        {
            CarrionSource carrion = carrionCandidates[i];
            if (carrion == null || carrion.IsConsumed)
            {
                continue;
            }

            float distanceSqr = (carrion.transform.position - position).sqrMagnitude;
            if (distanceSqr < bestScore)
            {
                bestScore = distanceSqr;
                best = carrion;
            }
        }

        return best;
    }

    public int CountCreaturesNearPoint(Vector3 point, float radius, MarineCreatureAgent ignoredCreature = null)
    {
        float radiusSqr = radius * radius;
        int count = 0;

        List<MarineCreatureAgent> creatureCandidates = GetNearbyCreatures(point, radius);
        for (int i = 0; i < creatureCandidates.Count; i++)
        {
            MarineCreatureAgent creature = creatureCandidates[i];
            if (creature == null || creature == ignoredCreature)
            {
                continue;
            }

            if ((creature.transform.position - point).sqrMagnitude <= radiusSqr)
            {
                count++;
            }
        }

        return count;
    }

    public FoodSource GetNearestFood(Vector3 position, float searchRadius)
    {
        FoodSource nearest = null;
        float bestDistanceSqr = searchRadius * searchRadius;

        List<FoodSource> foodCandidates = GetNearbyFood(position, searchRadius);
        for (int i = 0; i < foodCandidates.Count; i++)
        {
            FoodSource food = foodCandidates[i];

            if (food == null || food.IsConsumed)
            {
                continue;
            }

            float distanceSqr = (food.transform.position - position).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                nearest = food;
            }
        }

        return nearest;
    }

    public CarrionSource GetNearestCarrion(Vector3 position, float searchRadius)
    {
        CarrionSource nearest = null;
        float bestDistanceSqr = searchRadius * searchRadius;

        List<CarrionSource> carrionCandidates = GetNearbyCarrion(position, searchRadius);
        for (int i = 0; i < carrionCandidates.Count; i++)
        {
            CarrionSource carrion = carrionCandidates[i];

            if (carrion == null || carrion.IsConsumed)
            {
                continue;
            }

            float distanceSqr = (carrion.transform.position - position).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                nearest = carrion;
            }
        }

        return nearest;
    }

    public MarineCreatureAgent GetNearestCreature(MarineCreatureAgent requester, Vector3 position, float searchRadius)
    {
        MarineCreatureAgent nearest = null;
        float bestDistanceSqr = searchRadius * searchRadius;

        List<MarineCreatureAgent> creatureCandidates = GetNearbyCreatures(position, searchRadius);
        for (int i = 0; i < creatureCandidates.Count; i++)
        {
            MarineCreatureAgent creature = creatureCandidates[i];

            if (creature == null || creature == requester)
            {
                continue;
            }

            float distanceSqr = (creature.transform.position - position).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                nearest = creature;
            }
        }

        return nearest;
    }

    public MarineCreatureAgent GetNearestPrey(MarineCreatureAgent requester, Vector3 position, float searchRadius)
    {
        if (!EnablePredation || requester == null)
        {
            return null;
        }

        MarineCreatureAgent nearest = null;
        float bestDistanceSqr = searchRadius * searchRadius;

        List<MarineCreatureAgent> preyCandidates = GetNearbyCreatures(position, searchRadius);
        for (int i = 0; i < preyCandidates.Count; i++)
        {
            MarineCreatureAgent creature = preyCandidates[i];
            if (creature == null || creature == requester)
            {
                continue;
            }

            float distanceSqr = (creature.transform.position - position).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            if (!requester.CanAttackPrey(creature))
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            nearest = creature;
        }

        return nearest;
    }

    public bool CanSpawnMoreActiveCreatures()
    {
        CleanLists();
        return activeCreatures.Count < Mathf.Max(1, MaxActiveCreatures);
    }

    public MarineCreatureAgent GetBestMateFor(MarineCreatureAgent seeker, float searchRadius, float requiredMorphSimilarity)
    {
        if (seeker == null || seeker.Candidate == null || seeker.Candidate.Genome == null)
        {
            return null;
        }

        bool seekerFemale = seeker.Candidate.Genome.SexGene >= 0.5f;
        MarineCreatureAgent best = null;
        float bestScore = float.MinValue;
        float radiusSqr = searchRadius * searchRadius;

        List<MarineCreatureAgent> mateCandidates = GetNearbyCreatures(seeker.transform.position, searchRadius);
        for (int i = 0; i < mateCandidates.Count; i++)
        {
            MarineCreatureAgent other = mateCandidates[i];
            if (other == null || other == seeker || other.Candidate == null || other.Candidate.Genome == null)
            {
                continue;
            }

            bool otherFemale = other.Candidate.Genome.SexGene >= 0.5f;
            if (otherFemale == seekerFemale)
            {
                continue;
            }

            if (!other.IsMatureForMating() || !other.HasMatingEnergy())
            {
                continue;
            }

            float similarity = seeker.Candidate.Genome.GetMorphSimilarity(other.Candidate.Genome);
            if (similarity < requiredMorphSimilarity)
            {
                continue;
            }

            float distSqr = (other.transform.position - seeker.transform.position).sqrMagnitude;
            if (distSqr > radiusSqr)
            {
                continue;
            }

            float distance = Mathf.Sqrt(distSqr);
            float score = similarity * 2f + other.GetHealthRatio() + other.GetEffectiveEnergyRatio() - distance * 0.03f;
            if (score > bestScore)
            {
                bestScore = score;
                best = other;
            }
        }

        return best;
    }

    public Vector3 FindSafeEggPositionNear(Vector3 origin, EvolutionGenome genome, float radius, int samples)
    {
        Vector3 best = ClampToSimulationArea(origin);
        float bestScore = float.MinValue;
        int count = Mathf.Max(4, samples);

        for (int i = 0; i < count; i++)
        {
            Vector3 candidate = origin + Random.insideUnitSphere * Mathf.Max(1f, radius);
            candidate = ClampToSimulationArea(candidate);
            if (KeepSpawnsAboveTerrain)
            {
                candidate = ProjectPointAboveTerrain(candidate, TerrainClearance);
            }

            float creatureCrowd = CountCreaturesNearPoint(candidate, 7f, null);
            float currentStress = GetCurrentStressAt(candidate);
            float foodScore = GetNearestFood(candidate, 16f) != null ? 0.35f : 0f;
            float score = foodScore - creatureCrowd * 0.15f - currentStress * Mathf.Lerp(0.25f, 1.2f, genome != null ? 1f - genome.Bravery : 0.5f);

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    public FishEggCluster SpawnEggCluster(MarineCreatureAgent mother, MarineCreatureAgent father, Vector3 position, List<EvolutionCandidate> children, float hatchTime, float health, float mass)
    {
        FishEggCluster cluster;
        if (EggClusterPrefab != null)
        {
            cluster = Instantiate(EggClusterPrefab, position, Quaternion.identity);
        }
        else
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "Fish_Egg_Cluster";
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * 0.55f;
            Collider c = obj.GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
            cluster = obj.AddComponent<FishEggCluster>();
        }

        cluster.Initialise(children, hatchTime, health, mass, mother, father);
        RegisterEggCluster(cluster);
        return cluster;
    }

    public void TriggerExtinctionEvent()
    {
        CleanLists();

        if (activeCreatures.Count == 0)
        {
            return;
        }

        int killCount = Mathf.RoundToInt(activeCreatures.Count * ExtinctionKillPercentage);
        killCount = Mathf.Clamp(killCount, 1, activeCreatures.Count);
        List<MarineCreatureAgent> copy = new List<MarineCreatureAgent>(activeCreatures);

        for (int i = 0; i < killCount; i++)
        {
            if (copy.Count == 0)
            {
                break;
            }

            int index = Random.Range(0, copy.Count);
            MarineCreatureAgent creature = copy[index];
            copy.RemoveAt(index);

            if (creature != null)
            {
                creature.Die(true);
            }
        }

        ApplyExtinctionEnvironmentShock();
        SpawnExtinctionPressureZone();
        Debug.Log("Extinction event killed " + killCount + " creatures and shifted the ecosystem pressure.");
    }

    private void SpawnExtinctionPressureZone()
    {
        if (!SpawnPressureZoneOnExtinction)
        {
            return;
        }

        Vector3 position = GetRandomPointInSimulationArea();
        EcosystemPressureZone zone;
        if (PressureZonePrefab != null)
        {
            zone = Instantiate(PressureZonePrefab, position, Quaternion.identity);
        }
        else
        {
            GameObject obj = new GameObject("Extinction_Pressure_Zone");
            obj.transform.position = position;
            zone = obj.AddComponent<EcosystemPressureZone>();
        }

        zone.Radius = Mathf.Max(4f, ExtinctionPressureZoneRadius);
        zone.Lifetime = Mathf.Max(5f, ExtinctionPressureZoneLifetime);
        zone.Temporary = true;
        zone.DriftDirection = Random.insideUnitSphere;
        zone.DriftDirection.y *= 0.15f;
        if (zone.DriftDirection.sqrMagnitude <= 0.001f) zone.DriftDirection = Vector3.forward;

        int roll = Random.Range(0, 4);
        EcosystemPressureZoneType type = roll == 0 ? EcosystemPressureZoneType.ColdCurrent : roll == 1 ? EcosystemPressureZoneType.DeadZone : roll == 2 ? EcosystemPressureZoneType.MutationHotspot : EcosystemPressureZoneType.WarmBloom;
        zone.ConfigureForType(type);
        RegisterPressureZone(zone);
    }

    private void ApplyExtinctionEnvironmentShock()
    {
        if (!ExtinctionChangesEnvironment)
        {
            return;
        }

        if (Environment != null)
        {
            Environment.MoveToNextSeason();
        }

        // A disaster should change opportunity as well as kill count. Extra carrion gives
        // scavengers/predators a temporary boom instead of pure population collapse.
        SpawnCarrionFromExtinctionEvents = ExtinctionCreatesCarrion;
    }

    public Vector3 GetSpawnPointForCreature()
    {
        Vector3 point = GetRandomPointInSimulationArea();
        return KeepSpawnsAboveTerrain ? ProjectPointAboveTerrain(point, FishTerrainClearance) : point;
    }

    public Vector3 GetSpawnPointForFood()
    {
        Vector3 point = GetRandomPointInSimulationArea();
        return KeepSpawnsAboveTerrain ? ProjectPointAboveTerrain(point, FoodTerrainClearance) : point;
    }

    public Vector3 ProjectPointToTerrain(Vector3 point, float offset)
    {
        if (TryGetTerrainHeight(point, out float y))
        {
            point.y = y + Mathf.Max(0f, offset);
        }
        return point;
    }

    public Vector3 ProjectPointAboveTerrain(Vector3 point, float clearance)
    {
        if (TryGetTerrainHeight(point, out float y))
        {
            point.y = Mathf.Max(point.y, y + Mathf.Max(0f, clearance));
        }
        return point;
    }

    public bool TryGetTerrainHeight(Vector3 point, out float terrainY)
    {
        if (TerrainSource != null)
        {
            Vector3 terrainPos = TerrainSource.transform.position;
            TerrainData data = TerrainSource.terrainData;
            if (data != null)
            {
                Vector3 local = point - terrainPos;
                if (local.x >= 0f && local.z >= 0f && local.x <= data.size.x && local.z <= data.size.z)
                {
                    terrainY = terrainPos.y + TerrainSource.SampleHeight(point);
                    return true;
                }
            }
        }

        Vector3 origin = point + Vector3.up * Mathf.Max(1f, TerrainProbeHeight);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Mathf.Max(1f, TerrainProbeDistance), TerrainRaycastMask, QueryTriggerInteraction.Ignore))
        {
            terrainY = hit.point.y;
            return true;
        }

        terrainY = 0f;
        return false;
    }

    public Vector3 GetCurrentVelocityAt(Vector3 position)
    {
        Vector3 total = Vector3.zero;
        for (int i = activeCurrentZones.Count - 1; i >= 0; i--)
        {
            EcosystemWaterCurrentZone zone = activeCurrentZones[i];
            if (zone == null)
            {
                activeCurrentZones.RemoveAt(i);
                continue;
            }

            total += zone.GetCurrentVelocity(position);
        }
        return total;
    }

    public float GetCurrentStressAt(Vector3 position)
    {
        float stress = 0f;
        for (int i = activeCurrentZones.Count - 1; i >= 0; i--)
        {
            EcosystemWaterCurrentZone zone = activeCurrentZones[i];
            if (zone == null)
            {
                activeCurrentZones.RemoveAt(i);
                continue;
            }

            stress += zone.GetInfluence01(position) * (zone.EnergyDrainPressure + zone.HealthPressure + Mathf.Max(0f, zone.FlowStrength) * 0.04f);
        }
        for (int i = activePressureZones.Count - 1; i >= 0; i--)
        {
            EcosystemPressureZone zone = activePressureZones[i];
            if (zone == null)
            {
                activePressureZones.RemoveAt(i);
                continue;
            }

            stress += zone.GetInfluence01(position) * Mathf.Max(0f, zone.StressScore);
        }

        return stress;
    }

    public float GetLocalEnergyDrainMultiplier(Vector3 position)
    {
        float multiplier = Environment != null ? Environment.EnergyDrainMultiplier : 1f;

        for (int i = activePressureZones.Count - 1; i >= 0; i--)
        {
            EcosystemPressureZone zone = activePressureZones[i];
            if (zone == null)
            {
                activePressureZones.RemoveAt(i);
                continue;
            }

            float influence = zone.GetInfluence01(position);
            if (influence > 0f)
            {
                multiplier *= Mathf.Lerp(1f, Mathf.Max(0.05f, zone.EnergyDrainMultiplier), influence);
            }
        }

        return Mathf.Clamp(multiplier, 0.35f, 2.5f);
    }

    public float GetLocalHealthPressure(Vector3 position)
    {
        float damage = 0f;

        for (int i = activePressureZones.Count - 1; i >= 0; i--)
        {
            EcosystemPressureZone zone = activePressureZones[i];
            if (zone == null)
            {
                activePressureZones.RemoveAt(i);
                continue;
            }

            damage += zone.HealthDamagePerSecond * zone.GetInfluence01(position);
        }

        return Mathf.Max(0f, damage);
    }

    public float GetLocalFoodOpportunityMultiplier(Vector3 position)
    {
        float multiplier = 1f;

        for (int i = activePressureZones.Count - 1; i >= 0; i--)
        {
            EcosystemPressureZone zone = activePressureZones[i];
            if (zone == null)
            {
                activePressureZones.RemoveAt(i);
                continue;
            }

            float influence = zone.GetInfluence01(position);
            if (influence > 0f)
            {
                multiplier *= Mathf.Lerp(1f, Mathf.Max(0.05f, zone.FoodOpportunityMultiplier), influence);
            }
        }

        return Mathf.Clamp(multiplier, 0.25f, 2.5f);
    }

    public Vector3 GetRandomPointInSimulationArea()
    {
        Vector3 half = SimulationAreaSize * 0.5f;
        Vector3 centre = transform.position;
        float padding = Mathf.Max(0f, SpawnPaddingFromBounds);
        float xPadding = Mathf.Min(padding, Mathf.Max(0f, half.x - 0.1f));
        float yPadding = Mathf.Min(padding, Mathf.Max(0f, half.y - 0.1f));
        float zPadding = Mathf.Min(padding, Mathf.Max(0f, half.z - 0.1f));

        return new Vector3(
            Random.Range(centre.x - half.x + xPadding, centre.x + half.x - xPadding),
            Random.Range(centre.y - half.y + yPadding, centre.y + half.y - yPadding),
            Random.Range(centre.z - half.z + zPadding, centre.z + half.z - zPadding)
        );
    }

    public Vector3 ClampToSimulationArea(Vector3 position)
    {
        Vector3 half = SimulationAreaSize * 0.5f;
        Vector3 centre = transform.position;

        position.x = Mathf.Clamp(position.x, centre.x - half.x, centre.x + half.x);
        position.y = Mathf.Clamp(position.y, centre.y - half.y, centre.y + half.y);
        position.z = Mathf.Clamp(position.z, centre.z - half.z, centre.z + half.z);

        if (KeepSimulationAboveTerrain && TryGetTerrainHeight(position, out float terrainY))
        {
            position.y = Mathf.Max(position.y, terrainY + Mathf.Max(0f, FishTerrainClearance));
        }

        if (UseWaterSurfaceLimit)
        {
            position.y = Mathf.Min(position.y, centre.y + half.y - Mathf.Max(0f, WaterSurfacePadding));
        }

        return position;
    }

    private void ClearSimulation()
    {
        ClearCreaturesOnly();

        for (int i = activeFood.Count - 1; i >= 0; i--)
        {
            if (activeFood[i] != null)
            {
                Destroy(activeFood[i].gameObject);
            }
        }

        for (int i = activeCarrion.Count - 1; i >= 0; i--)
        {
            if (activeCarrion[i] != null)
            {
                Destroy(activeCarrion[i].gameObject);
            }
        }

        for (int i = activeEggClusters.Count - 1; i >= 0; i--)
        {
            if (activeEggClusters[i] != null)
            {
                Destroy(activeEggClusters[i].gameObject);
            }
        }

        for (int i = activePressureZones.Count - 1; i >= 0; i--)
        {
            if (activePressureZones[i] != null)
            {
                Destroy(activePressureZones[i].gameObject);
            }
        }

        activeFood.Clear();
        activeCarrion.Clear();
        activeEggClusters.Clear();
        activePressureZones.Clear();
        offspringPool.Clear();
    }

    private void ClearCreaturesOnly()
    {
        for (int i = activeCreatures.Count - 1; i >= 0; i--)
        {
            if (activeCreatures[i] != null)
            {
                Destroy(activeCreatures[i].gameObject);
            }
        }

        activeCreatures.Clear();
    }

    [ContextMenu("Reset Simulation")]
    public void ResetSimulation()
    {
        CurrentGeneration = 1;
        GenerationTimer = 0f;
        extinctionTimer = 0f;
        foodSpawnTimer = 0f;
        nextCreatureId = 1;
        StartBootstrap();
    }

    public void ForceEndGeneration()
    {
        EndGenerationAndSpawnNext();
    }

    private void CleanLists()
    {
        RemoveNullCreatures();
        RemoveNullFood();
        RemoveNullCarrion();
        RemoveNullPlants();
        RemoveNullEggClusters();
        RemoveNullCurrentZones();
        RemoveNullPressureZones();
    }

    private void RemoveNullCreatures()
    {
        for (int i = activeCreatures.Count - 1; i >= 0; i--)
        {
            if (activeCreatures[i] == null) activeCreatures.RemoveAt(i);
        }
    }

    private void RemoveNullFood()
    {
        for (int i = activeFood.Count - 1; i >= 0; i--)
        {
            if (activeFood[i] == null) activeFood.RemoveAt(i);
        }
    }

    private void RemoveNullCarrion()
    {
        for (int i = activeCarrion.Count - 1; i >= 0; i--)
        {
            if (activeCarrion[i] == null) activeCarrion.RemoveAt(i);
        }
    }

    private void RemoveNullPlants()
    {
        for (int i = activePlants.Count - 1; i >= 0; i--)
        {
            if (activePlants[i] == null) activePlants.RemoveAt(i);
        }
    }

    private void RemoveNullEggClusters()
    {
        for (int i = activeEggClusters.Count - 1; i >= 0; i--)
        {
            if (activeEggClusters[i] == null) activeEggClusters.RemoveAt(i);
        }
    }

    private void RemoveNullCurrentZones()
    {
        for (int i = activeCurrentZones.Count - 1; i >= 0; i--)
        {
            if (activeCurrentZones[i] == null) activeCurrentZones.RemoveAt(i);
        }
    }

    private void RemoveNullPressureZones()
    {
        for (int i = activePressureZones.Count - 1; i >= 0; i--)
        {
            if (activePressureZones[i] == null) activePressureZones.RemoveAt(i);
        }
    }

    public List<MarineCreatureAgent> GetActiveCreatures()
    {
        return activeCreatures;
    }

    public List<EvolutionCandidate> GetOffspringPool()
    {
        return offspringPool;
    }

    public List<FoodSource> GetActiveFood()
    {
        return activeFood;
    }

    public List<CarrionSource> GetActiveCarrion()
    {
        return activeCarrion;
    }

    public List<PlantResource> GetActivePlants()
    {
        return activePlants;
    }

    public List<FishEggCluster> GetActiveEggClusters()
    {
        return activeEggClusters;
    }

    public List<EcosystemWaterCurrentZone> GetActiveCurrentZones()
    {
        return activeCurrentZones;
    }

    public List<EcosystemPressureZone> GetActivePressureZones()
    {
        return activePressureZones;
    }

    public void ReplacePopulationWithGenomes(List<EvolutionGenome> genomes, int generation, float generationTimerValue)
    {
        if (genomes == null || genomes.Count == 0)
        {
            Debug.LogWarning("Cannot load evolved population because no genomes were supplied.", this);
            return;
        }

        if (bootstrapRoutine != null)
        {
            StopCoroutine(bootstrapRoutine);
            bootstrapRoutine = null;
        }

        ClearCreaturesOnly();
        offspringPool.Clear();
        CurrentGeneration = Mathf.Max(1, generation);
        GenerationTimer = Mathf.Max(0f, generationTimerValue);
        nextCreatureId = 1;

        int count = Mathf.Min(genomes.Count, Mathf.Max(1, MaxActiveCreatures));
        for (int i = 0; i < count; i++)
        {
            EvolutionGenome genome = genomes[i] != null ? genomes[i].Clone() : EvolutionGenome.CreateBaseline();
            SpawnCreature(new EvolutionCandidate(genome), GetSpawnPointForCreature());
        }

        EnsureSpatialGrid();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, SimulationAreaSize);
    }
}
