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

    [Header("Libraries")]
    public CreatureMorphLibrary MorphLibrary;

    [Header("References")]
    public SeasonalEnvironment Environment;
    public EvolutionStatsTracker StatsTracker;
    public EcosystemDebugSettings DebugSettings;

    [Header("Simulation Area")]
    public Vector3 SimulationAreaSize = new Vector3(100f, 35f, 100f);
    public float SpawnPaddingFromBounds = 4f;

    [Header("Terrain / Seabed")]
    [Tooltip("Raises creature, food and carrion spawn points if they would appear inside or under the seabed.")]
    public bool KeepSpawnsAboveTerrain = true;
    [Tooltip("Also prevents swimming positions from being clamped below the seabed during movement.")]
    public bool KeepSimulationAboveTerrain = true;
    [Tooltip("Optional Unity Terrain. If left empty, the active terrain is used when available.")]
    public Terrain TerrainSource;
    [Tooltip("Optional collider for a mesh/probuilder seabed. Assign this if your map is not a Unity Terrain object.")]
    public Collider TerrainCollider;
    [Tooltip("Used for mesh/probuilder seabeds when no explicit terrain collider is assigned. Put the seabed on its own layer and use that layer here.")]
    public LayerMask TerrainRaycastMask = ~0;
    [Tooltip("How far above the seabed spawned objects are lifted.")]
    public float MinimumHeightAboveTerrain = 1.25f;
    [Tooltip("How high above the simulation area the terrain ray starts.")]
    public float TerrainProbeHeight = 120f;
    [Tooltip("How far down the terrain ray checks.")]
    public float TerrainProbeDistance = 260f;

    [Header("Environmental Pressure Zones")]
    public bool AutoFindPressureZones = true;
    public List<EcosystemPressureZone> PressureZones = new List<EcosystemPressureZone>();
    [Range(0f, 1f)] public float FoodZoneSpawnChance = 0.78f;
    [Range(0f, 1f)] public float CreatureZoneSpawnChance = 0.62f;
    public float ZoneRefreshInterval = 3f;

    [Header("Generation Settings")]
    public int StartingPopulation = 60;
    public int FixedPopulationSize = 60;
    public float GenerationDuration = 100f;
    public SelectionMode Selection = SelectionMode.QualityDiversityLite;
    public int TournamentSize = 4;
    public bool UseFixedRandomSeed = false;
    public int RandomSeed = 12345;

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
    [Range(0f, 1f)] public float MinimumMeatDietToHunt = 0.46f;
    [Range(0f, 1f)] public float MinimumAggressionToHunt = 0.28f;
    public float MaxPreySizeRatio = 1.15f;
    public float BiteEnergyGainMultiplier = 0.32f;
    public bool SpawnCarrionFromDeaths = true;
    public bool SpawnCarrionFromExtinctionEvents = false;
    public float CarrionEnergyFromBodySize = 34f;
    public int MaxCarrionSources = 130;

    [Header("Defensive Morphology")]
    [Tooltip("Higher values make spiked/armoured herbivores more able to discourage predators.")]
    public float PredatorFearOfDangerFactor = 0.85f;

    [Header("Extinction Pressure")]
    public bool UseExtinctionEvents = true;
    public float ExtinctionEventInterval = 160f;
    [Range(0f, 1f)] public float ExtinctionKillPercentage = 0.16f;

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
    private readonly List<EvolutionCandidate> offspringPool = new List<EvolutionCandidate>();

    private float foodSpawnTimer;
    private float extinctionTimer;
    private float zoneRefreshTimer;
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

        CreatureMorphLibrary.SetActiveLibrary(MorphLibrary);
        RefreshPressureZones();
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

            EvolutionCandidate candidate = new EvolutionCandidate(genome);
            SpawnCreature(candidate, GetSpawnPointForCandidate(candidate));

            if (StaggerInitialSpawn && i % creatureBudget == creatureBudget - 1)
            {
                yield return null;
            }
        }

        int foodBudget = Mathf.Max(1, FoodSpawnsPerFrame);
        for (int i = 0; i < StartingFood; i++)
        {
            SpawnFood();

            if (StaggerInitialSpawn && i % foodBudget == foodBudget - 1)
            {
                yield return null;
            }
        }

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
        zoneRefreshTimer += Time.deltaTime;

        if (AutoFindPressureZones && zoneRefreshTimer >= ZoneRefreshInterval)
        {
            zoneRefreshTimer = 0f;
            RefreshPressureZones();
        }

        CleanLists();
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

            EvolutionCandidate candidate = new EvolutionCandidate(genome);
            SpawnCreature(candidate, GetSpawnPointForCandidate(candidate));
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
            SpawnCreature(nextGeneration[i], GetSpawnPointForCandidate(nextGeneration[i]));
            if (StaggerInitialSpawn && i % budget == budget - 1)
            {
                yield return null;
            }
        }

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

            return selected;
        }

        if (Selection == SelectionMode.QualityDiversityLite)
        {
            return SelectQualityDiversityLite(evaluated, target);
        }

        for (int i = 0; i < target; i++)
        {
            EvolutionCandidate parent = RunTournament(evaluated);
            selected.Add(parent.CreateChild(GetEnvironmentMutationMultiplier()));
        }

        return selected;
    }

    private List<EvolutionCandidate> SelectQualityDiversityLite(List<EvolutionCandidate> evaluated, int target)
    {
        List<EvolutionCandidate> selected = new List<EvolutionCandidate>();
        Dictionary<string, List<EvolutionCandidate>> buckets = new Dictionary<string, List<EvolutionCandidate>>();

        for (int i = 0; i < evaluated.Count; i++)
        {
            EvolutionCandidate candidate = evaluated[i];
            if (candidate == null || candidate.Genome == null)
            {
                continue;
            }

            string key = CreatureDebugTypeUtility.GetSpeciesGroupName(candidate.Genome);
            if (!buckets.ContainsKey(key))
            {
                buckets[key] = new List<EvolutionCandidate>();
            }

            buckets[key].Add(candidate);
        }

        foreach (KeyValuePair<string, List<EvolutionCandidate>> pair in buckets)
        {
            pair.Value.Sort((a, b) => b.GetFitness().CompareTo(a.GetFitness()));
        }

        int roundIndex = 0;
        int safety = 0;
        while (selected.Count < target && safety < target * 20)
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
                selected.Add(parent.CreateChild(GetEnvironmentMutationMultiplier()));
                addedThisRound = true;
            }

            if (!addedThisRound)
            {
                break;
            }

            roundIndex++;
        }

        while (selected.Count < target)
        {
            EvolutionCandidate parent = RunTournament(evaluated);
            selected.Add(parent.CreateChild(GetEnvironmentMutationMultiplier()));
        }

        return selected;
    }

    private EvolutionCandidate RunTournament(List<EvolutionCandidate> evaluated)
    {
        EvolutionCandidate best = null;
        int size = Mathf.Max(1, TournamentSize);

        for (int i = 0; i < size; i++)
        {
            EvolutionCandidate candidate = evaluated[Random.Range(0, evaluated.Count)];

            if (best == null || candidate.GetFitness() > best.GetFitness())
            {
                best = candidate;
            }
        }

        return best;
    }

    private float GetEnvironmentMutationMultiplier()
    {
        return Environment != null ? Environment.MutationMultiplier : 1f;
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

        position = GetSafeSpawnPoint(position);
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

        FoodSource food = Instantiate(FoodPrefab, GetSafeSpawnPoint(GetFoodSpawnPoint()), Quaternion.identity);
        if (food.MaxMass <= 0f)
        {
            food.MaxMass = Mathf.Max(1f, food.EnergyValue);
        }
        if (food.RemainingMass < 0f)
        {
            food.RemainingMass = food.MaxMass;
        }
        activeFood.Add(food);
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
            carrion = Instantiate(CarrionPrefab, GetSafeSpawnPoint(creature.transform.position), Quaternion.identity);
        }
        else
        {
            GameObject carrionObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            carrionObject.name = "Carrion_Source";
            carrionObject.transform.position = GetSafeSpawnPoint(creature.transform.position);
            carrionObject.transform.localScale = Vector3.one * Mathf.Clamp(size * 0.45f, 0.25f, 1.4f);
            carrion = carrionObject.AddComponent<CarrionSource>();
        }

        carrion.EnergyValue = energyValue;
        carrion.MaxMass = Mathf.Max(1f, energyValue);
        carrion.RemainingMass = carrion.MaxMass;
        activeCarrion.Add(carrion);
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

    public void UnregisterCarrion(CarrionSource carrion)
    {
        activeCarrion.Remove(carrion);
    }


    public FoodSource GetBestFoodForCreature(MarineCreatureAgent requester, Vector3 position, float searchRadius, float crowdRadius, int comfortableCrowdLimit, float crowdPenalty)
    {
        FoodSource best = null;
        float bestScore = float.MaxValue;
        float searchRadiusSqr = searchRadius * searchRadius;
        float crowdRadiusSafe = Mathf.Max(0.25f, crowdRadius);
        int comfort = Mathf.Max(0, comfortableCrowdLimit);

        for (int i = 0; i < activeFood.Count; i++)
        {
            FoodSource food = activeFood[i];
            if (food == null || food.IsConsumed)
            {
                continue;
            }

            Vector3 foodPosition = food.transform.position;
            float distanceSqr = (foodPosition - position).sqrMagnitude;
            if (distanceSqr > searchRadiusSqr)
            {
                continue;
            }

            int crowd = CountCreaturesNearPoint(foodPosition, crowdRadiusSafe, requester);
            int recentFeeders = food.GetRecentFeederCount();
            int excessCrowd = Mathf.Max(0, crowd + recentFeeders - comfort);
            float distance = Mathf.Sqrt(distanceSqr);
            float massBonus = Mathf.Lerp(6f, 0f, food.GetMassRatio());
            float score = distance + excessCrowd * Mathf.Max(0f, crowdPenalty) + massBonus;
            score += crowd * 0.35f + recentFeeders * 1.75f;

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
        CarrionSource best = null;
        float bestScore = float.MaxValue;
        float searchRadiusSqr = searchRadius * searchRadius;
        float crowdRadiusSafe = Mathf.Max(0.25f, crowdRadius);
        int comfort = Mathf.Max(0, comfortableCrowdLimit);

        for (int i = 0; i < activeCarrion.Count; i++)
        {
            CarrionSource carrion = activeCarrion[i];
            if (carrion == null || carrion.IsConsumed)
            {
                continue;
            }

            Vector3 carrionPosition = carrion.transform.position;
            float distanceSqr = (carrionPosition - position).sqrMagnitude;
            if (distanceSqr > searchRadiusSqr)
            {
                continue;
            }

            int crowd = CountCreaturesNearPoint(carrionPosition, crowdRadiusSafe, requester);
            int recentFeeders = carrion.GetRecentFeederCount();
            int excessCrowd = Mathf.Max(0, crowd + recentFeeders - comfort);
            float distance = Mathf.Sqrt(distanceSqr);
            float massBonus = Mathf.Lerp(6f, 0f, carrion.GetMassRatio());
            float score = distance + excessCrowd * Mathf.Max(0f, crowdPenalty) + massBonus;
            score += crowd * 0.35f + recentFeeders * 1.75f;

            if (score < bestScore)
            {
                bestScore = score;
                best = carrion;
            }
        }

        return best;
    }

    public int CountCreaturesNearPoint(Vector3 point, float radius, MarineCreatureAgent ignoredCreature = null)
    {
        float radiusSqr = radius * radius;
        int count = 0;

        for (int i = 0; i < activeCreatures.Count; i++)
        {
            MarineCreatureAgent creature = activeCreatures[i];
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

        for (int i = 0; i < activeFood.Count; i++)
        {
            FoodSource food = activeFood[i];

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

        for (int i = 0; i < activeCarrion.Count; i++)
        {
            CarrionSource carrion = activeCarrion[i];

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

        for (int i = 0; i < activeCreatures.Count; i++)
        {
            MarineCreatureAgent creature = activeCreatures[i];

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

        for (int i = 0; i < activeCreatures.Count; i++)
        {
            MarineCreatureAgent creature = activeCreatures[i];

            if (creature == null || creature == requester)
            {
                continue;
            }

            if (!requester.CanAttackPrey(creature))
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

        Debug.Log("Extinction event killed " + killCount + " creatures.");
    }


    public void RefreshPressureZones()
    {
        if (!AutoFindPressureZones)
        {
            return;
        }

        PressureZones.Clear();
        EcosystemPressureZone[] zones = FindObjectsByType<EcosystemPressureZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null && zones[i].isActiveAndEnabled)
            {
                PressureZones.Add(zones[i]);
            }
        }
    }

    private Vector3 GetFoodSpawnPoint()
    {
        if (PressureZones == null || PressureZones.Count == 0 || Random.value > FoodZoneSpawnChance)
        {
            return GetRandomPointInSimulationArea();
        }

        EcosystemPressureZone zone = GetWeightedFoodZone();
        return zone != null ? zone.GetRandomPointInside(this) : GetRandomPointInSimulationArea();
    }

    private Vector3 GetSpawnPointForCandidate(EvolutionCandidate candidate)
    {
        if (candidate == null || candidate.Genome == null || PressureZones == null || PressureZones.Count == 0 || Random.value > CreatureZoneSpawnChance)
        {
            return GetRandomPointInSimulationArea();
        }

        EcosystemPressureZone zone = GetWeightedHabitatZone(candidate.Genome);
        return zone != null ? zone.GetRandomPointInside(this) : GetRandomPointInSimulationArea();
    }

    private EcosystemPressureZone GetWeightedFoodZone()
    {
        float totalWeight = 0f;
        for (int i = 0; i < PressureZones.Count; i++)
        {
            if (PressureZones[i] != null)
            {
                totalWeight += PressureZones[i].GetFoodSpawnWeight();
            }
        }

        if (totalWeight <= 0.001f)
        {
            return null;
        }

        float pick = Random.value * totalWeight;
        float running = 0f;
        for (int i = 0; i < PressureZones.Count; i++)
        {
            EcosystemPressureZone zone = PressureZones[i];
            if (zone == null)
            {
                continue;
            }

            running += zone.GetFoodSpawnWeight();
            if (pick <= running)
            {
                return zone;
            }
        }

        return null;
    }

    private EcosystemPressureZone GetWeightedHabitatZone(EvolutionGenome genome)
    {
        float totalWeight = 0f;
        for (int i = 0; i < PressureZones.Count; i++)
        {
            if (PressureZones[i] != null)
            {
                totalWeight += PressureZones[i].GetHabitatSuitability(genome);
            }
        }

        if (totalWeight <= 0.001f)
        {
            return null;
        }

        float pick = Random.value * totalWeight;
        float running = 0f;
        for (int i = 0; i < PressureZones.Count; i++)
        {
            EcosystemPressureZone zone = PressureZones[i];
            if (zone == null)
            {
                continue;
            }

            running += zone.GetHabitatSuitability(genome);
            if (pick <= running)
            {
                return zone;
            }
        }

        return null;
    }

    public EcosystemPressureZone GetPressureZoneAt(Vector3 position)
    {
        if (PressureZones == null || PressureZones.Count == 0)
        {
            return null;
        }

        EcosystemPressureZone best = null;
        float bestInfluence = 0f;
        for (int i = 0; i < PressureZones.Count; i++)
        {
            EcosystemPressureZone zone = PressureZones[i];
            if (zone == null)
            {
                continue;
            }

            float influence = zone.GetInfluence01(position);
            if (influence > bestInfluence)
            {
                bestInfluence = influence;
                best = zone;
            }
        }

        return best;
    }

    public EcosystemPressureZone GetBestHabitatZoneForGenome(EvolutionGenome genome, Vector3 position)
    {
        if (PressureZones == null || PressureZones.Count == 0 || genome == null)
        {
            return null;
        }

        EcosystemPressureZone best = null;
        float bestScore = float.MinValue;
        for (int i = 0; i < PressureZones.Count; i++)
        {
            EcosystemPressureZone zone = PressureZones[i];
            if (zone == null)
            {
                continue;
            }

            float distance = Vector3.Distance(position, zone.transform.position);
            float distanceScore = 1f - Mathf.Clamp01(distance / Mathf.Max(1f, SimulationAreaSize.magnitude * 0.35f));
            float score = zone.GetHabitatSuitability(genome) * 1.15f + distanceScore * Mathf.Lerp(0.1f, 0.7f, genome.HabitatLoyalty);
            score -= zone.DangerPressure * Mathf.Lerp(0.75f, 0.15f, genome.Bravery);

            if (score > bestScore)
            {
                bestScore = score;
                best = zone;
            }
        }

        return best;
    }

    public float GetZoneStressAt(Vector3 position)
    {
        EcosystemPressureZone zone = GetPressureZoneAt(position);
        return zone != null ? zone.GetStressAt(position) : 0f;
    }

    public float GetZoneEnergyDrainMultiplierAt(Vector3 position)
    {
        EcosystemPressureZone zone = GetPressureZoneAt(position);
        if (zone == null)
        {
            return 1f;
        }

        float influence = zone.GetInfluence01(position);
        return Mathf.Lerp(1f, Mathf.Max(0.1f, zone.EnergyDrainMultiplier), influence);
    }

    public float GetZoneMutationMultiplierAt(Vector3 position)
    {
        EcosystemPressureZone zone = GetPressureZoneAt(position);
        if (zone == null)
        {
            return 1f;
        }

        return Mathf.Lerp(1f, Mathf.Max(0.1f, zone.MutationPressure), zone.GetInfluence01(position));
    }

    public float GetZonePreferredDepth01At(Vector3 position, float fallback)
    {
        EcosystemPressureZone zone = GetPressureZoneAt(position);
        if (zone == null)
        {
            return fallback;
        }

        return Mathf.Lerp(fallback, zone.PreferredDepth01, zone.DepthInfluence * zone.GetInfluence01(position));
    }

    public string GetZoneNameAt(Vector3 position)
    {
        EcosystemPressureZone zone = GetPressureZoneAt(position);
        return zone != null ? zone.ZoneName : "Open water";
    }

    public Vector3 GetRandomPointInSimulationArea()
    {
        Vector3 half = SimulationAreaSize * 0.5f;
        Vector3 centre = transform.position;
        float padding = Mathf.Max(0f, SpawnPaddingFromBounds);
        float xPadding = Mathf.Min(padding, Mathf.Max(0f, half.x - 0.1f));
        float yPadding = Mathf.Min(padding, Mathf.Max(0f, half.y - 0.1f));
        float zPadding = Mathf.Min(padding, Mathf.Max(0f, half.z - 0.1f));

        Vector3 point = new Vector3(
            Random.Range(centre.x - half.x + xPadding, centre.x + half.x - xPadding),
            Random.Range(centre.y - half.y + yPadding, centre.y + half.y - yPadding),
            Random.Range(centre.z - half.z + zPadding, centre.z + half.z - zPadding)
        );

        return GetSafeSpawnPoint(point);
    }

    public Vector3 ClampToSimulationArea(Vector3 position)
    {
        Vector3 half = SimulationAreaSize * 0.5f;
        Vector3 centre = transform.position;

        position.x = Mathf.Clamp(position.x, centre.x - half.x, centre.x + half.x);
        position.y = Mathf.Clamp(position.y, centre.y - half.y, centre.y + half.y);
        position.z = Mathf.Clamp(position.z, centre.z - half.z, centre.z + half.z);

        if (KeepSimulationAboveTerrain)
        {
            position = LiftPointAboveTerrain(position);
            position.y = Mathf.Clamp(position.y, centre.y - half.y, centre.y + half.y);
        }

        return position;
    }

    public Vector3 GetSafeSpawnPoint(Vector3 position)
    {
        position = ClampToSimulationArea(position);

        if (KeepSpawnsAboveTerrain)
        {
            position = LiftPointAboveTerrain(position);
            position = ClampToSimulationArea(position);
        }

        return position;
    }

    public Vector3 LiftPointAboveTerrain(Vector3 position)
    {
        float terrainY;
        if (!TryGetTerrainHeight(position, out terrainY))
        {
            return position;
        }

        float minimumY = terrainY + Mathf.Max(0f, MinimumHeightAboveTerrain);
        if (position.y < minimumY)
        {
            position.y = minimumY;
        }

        return position;
    }

    public bool TryGetTerrainHeight(Vector3 position, out float terrainY)
    {
        Terrain terrain = TerrainSource;
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideTerrain = position.x >= terrainPosition.x && position.x <= terrainPosition.x + terrainSize.x &&
                                 position.z >= terrainPosition.z && position.z <= terrainPosition.z + terrainSize.z;

            if (insideTerrain)
            {
                terrainY = terrain.SampleHeight(position) + terrainPosition.y;
                return true;
            }
        }

        if (TerrainCollider != null)
        {
            Ray ray = new Ray(new Vector3(position.x, position.y + Mathf.Max(1f, TerrainProbeHeight), position.z), Vector3.down);
            RaycastHit colliderHit;
            if (TerrainCollider.Raycast(ray, out colliderHit, Mathf.Max(1f, TerrainProbeDistance + TerrainProbeHeight)))
            {
                terrainY = colliderHit.point.y;
                return true;
            }
        }

        RaycastHit hit;
        Vector3 origin = new Vector3(position.x, transform.position.y + SimulationAreaSize.y * 0.5f + Mathf.Max(1f, TerrainProbeHeight), position.z);
        float distance = Mathf.Max(1f, TerrainProbeDistance + SimulationAreaSize.y + TerrainProbeHeight);
        if (Physics.Raycast(origin, Vector3.down, out hit, distance, TerrainRaycastMask, QueryTriggerInteraction.Ignore))
        {
            terrainY = hit.point.y;
            return true;
        }

        terrainY = 0f;
        return false;
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

        activeFood.Clear();
        activeCarrion.Clear();
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
        zoneRefreshTimer = 0f;
        nextCreatureId = 1;
        RefreshPressureZones();
        StartBootstrap();
    }

    public void ForceEndGeneration()
    {
        EndGenerationAndSpawnNext();
    }

    private void CleanLists()
    {
        activeCreatures.RemoveAll(creature => creature == null);
        activeFood.RemoveAll(food => food == null);
        activeCarrion.RemoveAll(carrion => carrion == null);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, SimulationAreaSize);
    }
}
