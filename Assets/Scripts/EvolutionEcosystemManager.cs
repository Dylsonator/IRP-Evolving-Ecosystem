using System.Collections.Generic;
using UnityEngine;

public enum SelectionMode
{
    Tournament,
    Elite
}

public class EvolutionEcosystemManager : MonoBehaviour
{
    public static EvolutionEcosystemManager Instance { get; private set; }

    [Header("Prefabs")]
    public MarineCreatureAgent CreaturePrefab;
    public FoodSource FoodPrefab;

    [Header("References")]
    public SeasonalEnvironment Environment;
    public EvolutionStatsTracker StatsTracker;

    [Header("Simulation Area")]
    public Vector3 SimulationAreaSize = new Vector3(80f, 25f, 80f);

    [Header("Generation Settings")]
    public int StartingPopulation = 35;
    public int FixedPopulationSize = 35;
    public float GenerationDuration = 90f;
    public SelectionMode Selection = SelectionMode.Tournament;
    public int TournamentSize = 4;
    public bool UseFixedRandomSeed = false;
    public int RandomSeed = 12345;

    [Header("Food Settings")]
    public int StartingFood = 90;
    public int TargetFoodAmount = 120;
    public float FoodSpawnInterval = 0.25f;

    [Header("Extinction Pressure")]
    public bool UseExtinctionEvents = true;
    public float ExtinctionEventInterval = 120f;
    [Range(0f, 1f)] public float ExtinctionKillPercentage = 0.25f;

    [Header("Debug")]
    public int CurrentGeneration = 1;
    public float GenerationTimer;
    public int OffspringCount;

    private readonly List<MarineCreatureAgent> activeCreatures = new List<MarineCreatureAgent>();
    private readonly List<FoodSource> activeFood = new List<FoodSource>();
    private readonly List<EvolutionCandidate> offspringPool = new List<EvolutionCandidate>();
    private readonly List<EvolutionCandidate> lastGenerationCandidates = new List<EvolutionCandidate>();

    private float foodSpawnTimer;
    private float extinctionTimer;

    private void Awake()
    {
        Instance = this;

        if (UseFixedRandomSeed)
        {
            Random.InitState(RandomSeed);
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

        for (int i = 0; i < target; i++)
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

        return new Vector3(
            Random.Range(centre.x - half.x, centre.x + half.x),
            Random.Range(centre.y - half.y, centre.y + half.y),
            Random.Range(centre.z - half.z, centre.z + half.z)
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

        activeFood.Clear();
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
    }

    public List<MarineCreatureAgent> GetActiveCreatures()
    {
        return activeCreatures;
    }

    public List<EvolutionCandidate> GetOffspringPool()
    {
        return offspringPool;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, SimulationAreaSize);
    }
}
