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

    [Header("References")]
    public SeasonalEnvironment Environment;
    public EvolutionStatsTracker StatsTracker;
    public EcosystemDebugSettings DebugSettings;

    [Header("Simulation Area")]
    public Vector3 SimulationAreaSize = new Vector3(80f, 25f, 80f);
    public float SpawnPaddingFromBounds = 3f;

    [Header("Generation Settings")]
    public int StartingPopulation = 35;
    public int FixedPopulationSize = 35;
    public float GenerationDuration = 90f;
    public SelectionMode Selection = SelectionMode.QualityDiversityLite;
    public int TournamentSize = 4;
    public bool UseFixedRandomSeed = false;
    public int RandomSeed = 12345;

    [Header("Food Settings")]
    public int StartingFood = 130;
    public int TargetFoodAmount = 165;
    public float FoodSpawnInterval = 0.18f;

    [Header("Predation / Carrion")]
    public bool EnablePredation = true;
    [Range(0f, 1f)] public float MinimumMeatDietToHunt = 0.48f;
    [Range(0f, 1f)] public float MinimumAggressionToHunt = 0.35f;
    public float MaxPreySizeRatio = 1.15f;
    public float BiteEnergyGainMultiplier = 0.35f;
    public bool SpawnCarrionFromDeaths = true;
    public bool SpawnCarrionFromExtinctionEvents = false;
    public float CarrionEnergyFromBodySize = 32f;
    public int MaxCarrionSources = 80;

    [Header("Extinction Pressure")]
    public bool UseExtinctionEvents = true;
    public float ExtinctionEventInterval = 150f;
    [Range(0f, 1f)] public float ExtinctionKillPercentage = 0.18f;

    [Header("Debug")]
    public int CurrentGeneration = 1;
    public float GenerationTimer;
    public int OffspringCount;
    public int ActiveCarrionCount;

    private readonly List<MarineCreatureAgent> activeCreatures = new List<MarineCreatureAgent>();
    private readonly List<FoodSource> activeFood = new List<FoodSource>();
    private readonly List<CarrionSource> activeCarrion = new List<CarrionSource>();
    private readonly List<EvolutionCandidate> offspringPool = new List<EvolutionCandidate>();
    private readonly List<EvolutionCandidate> lastGenerationCandidates = new List<EvolutionCandidate>();

    private float foodSpawnTimer;
    private float extinctionTimer;
    private int nextCreatureId = 1;

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
    }

    private void Start()
    {
        SpawnInitialGeneration();
        SpawnStartingFood();
    }

    private void Update()
    {
        GenerationTimer += Time.deltaTime;
        foodSpawnTimer += Time.deltaTime;
        extinctionTimer += Time.deltaTime;

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

        int amount = Mathf.Max(1, StartingPopulation);

        for (int i = 0; i < amount; i++)
        {
            EvolutionCandidate candidate = new EvolutionCandidate(EvolutionGenome.CreateRandom());
            SpawnCreature(candidate, GetRandomPointInSimulationArea());
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
            StatsTracker.RecordGeneration(
                CurrentGeneration,
                evaluated,
                activeCreatures.Count,
                offspringPool.Count
            );
        }

        List<EvolutionCandidate> nextGeneration = SelectNextGeneration(evaluated);

        ClearCreaturesOnly();

        CurrentGeneration++;
        GenerationTimer = 0f;
        offspringPool.Clear();
        lastGenerationCandidates.Clear();

        for (int i = 0; i < nextGeneration.Count; i++)
        {
            SpawnCreature(nextGeneration[i], GetRandomPointInSimulationArea());
        }

        Debug.Log("Started generation " + CurrentGeneration);
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
                evaluated.Add(new EvolutionCandidate(EvolutionGenome.CreateRandom()));
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
        Dictionary<CreatureBehaviourType, List<EvolutionCandidate>> buckets = new Dictionary<CreatureBehaviourType, List<EvolutionCandidate>>();

        for (int i = 0; i < evaluated.Count; i++)
        {
            EvolutionCandidate candidate = evaluated[i];
            if (candidate == null || candidate.Genome == null)
            {
                continue;
            }

            CreatureBehaviourType type = CreatureDebugTypeUtility.GetBehaviourType(candidate.Genome);
            if (!buckets.ContainsKey(type))
            {
                buckets[type] = new List<EvolutionCandidate>();
            }

            buckets[type].Add(candidate);
        }

        foreach (KeyValuePair<CreatureBehaviourType, List<EvolutionCandidate>> pair in buckets)
        {
            pair.Value.Sort((a, b) => b.GetFitness().CompareTo(a.GetFitness()));
        }

        int roundIndex = 0;
        int safety = 0;
        while (selected.Count < target && safety < target * 16)
        {
            safety++;
            bool addedThisRound = false;

            foreach (KeyValuePair<CreatureBehaviourType, List<EvolutionCandidate>> pair in buckets)
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
            candidate = new EvolutionCandidate(EvolutionGenome.CreateRandom());
        }

        int parentId = candidate.ParentId;
        candidate.AssignRuntimeIdentity(nextCreatureId, CurrentGeneration, parentId);
        nextCreatureId++;

        MarineCreatureAgent creature = Instantiate(CreaturePrefab, position, Quaternion.identity);
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

        FoodSource food = Instantiate(FoodPrefab, GetRandomPointInSimulationArea(), Quaternion.identity);
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

        float energyValue = 10f + creature.Candidate.Genome.BodySize * CarrionEnergyFromBodySize;
        energyValue += Mathf.Max(0f, creature.CurrentEnergy) * 0.15f;

        CarrionSource carrion;

        if (CarrionPrefab != null)
        {
            carrion = Instantiate(CarrionPrefab, creature.transform.position, Quaternion.identity);
        }
        else
        {
            GameObject carrionObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            carrionObject.name = "Carrion_Source";
            carrionObject.transform.position = creature.transform.position;
            carrionObject.transform.localScale = Vector3.one * Mathf.Clamp(creature.Candidate.Genome.BodySize * 0.45f, 0.25f, 1.4f);

            Collider collider = carrionObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            carrion = carrionObject.AddComponent<CarrionSource>();
        }

        carrion.EnergyValue = energyValue;
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
