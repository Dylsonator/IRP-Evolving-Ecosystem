using System.Collections.Generic;
using UnityEngine;

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
    public Vector3 SimulationAreaSize = new Vector3(90f, 35f, 90f);
    public float SpawnPaddingFromBounds = 4f;

    [Header("Continuous Population")]
    public int StartingPopulation = 40;
    public int MaxPopulation = 180;
    public bool AutoReseedIfExtinct = true;
    public int ReseedPopulation = 20;
    public bool UseFixedRandomSeed = false;
    public int RandomSeed = 12345;

    [Header("Plant Food")]
    public int StartingPlantFood = 120;
    public int TargetPlantFood = 160;
    public float PlantFoodSpawnInterval = 0.2f;
    public float PlantEnergyValue = 28f;

    [Header("Meat Food")]
    public bool CreateMeatOnDeath = true;
    public float MeatEnergyPerBodySize = 35f;
    public float MinimumMeatEnergy = 20f;

    [Header("Extinction Pressure")]
    public bool UseExtinctionEvents = true;
    public float ExtinctionEventInterval = 150f;
    [Range(0f, 1f)] public float ExtinctionKillPercentage = 0.25f;

    [Header("Debug")]
    public float Runtime;
    public int CurrentPopulation;
    public int CurrentPlantFood;
    public int CurrentFreshMeat;
    public int CurrentRottenMeat;

    private readonly List<MarineCreatureAgent> activeCreatures = new List<MarineCreatureAgent>();
    private readonly List<FoodSource> activeFood = new List<FoodSource>();

    private float foodSpawnTimer;
    private float extinctionTimer;

    public IReadOnlyList<MarineCreatureAgent> ActiveCreatures
    {
        get { return activeCreatures; }
    }

    public IReadOnlyList<FoodSource> ActiveFood
    {
        get { return activeFood; }
    }

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
        SpawnInitialPopulation();
        SpawnStartingFood();
    }

    private void Update()
    {
        Runtime += Time.deltaTime;
        foodSpawnTimer += Time.deltaTime;
        extinctionTimer += Time.deltaTime;

        CleanLists();
        UpdateCounts();
        HandlePlantFoodSpawning();
        HandleExtinctionEvents();
        HandleExtinctionRecovery();
    }

    private void SpawnInitialPopulation()
    {
        for (int i = 0; i < StartingPopulation; i++)
        {
            EvolutionCandidate candidate = new EvolutionCandidate(EvolutionGenome.CreateRandom());
            SpawnCreature(candidate, GetRandomPointInSimulationArea());
        }
    }

    private void SpawnStartingFood()
    {
        for (int i = 0; i < StartingPlantFood; i++)
        {
            SpawnFood(EcosystemFoodType.Plant, GetRandomPointInSimulationArea(), PlantEnergyValue);
        }
    }

    private void HandlePlantFoodSpawning()
    {
        if (foodSpawnTimer < PlantFoodSpawnInterval)
        {
            return;
        }

        foodSpawnTimer = 0f;

        float multiplier = Environment != null ? Environment.PlantFoodSpawnMultiplier : 1f;
        int target = Mathf.RoundToInt(TargetPlantFood * multiplier);

        if (CurrentPlantFood < target)
        {
            SpawnFood(EcosystemFoodType.Plant, GetRandomPointInSimulationArea(), PlantEnergyValue);
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

    private void HandleExtinctionRecovery()
    {
        if (!AutoReseedIfExtinct)
        {
            return;
        }

        if (activeCreatures.Count > 0)
        {
            return;
        }

        for (int i = 0; i < ReseedPopulation; i++)
        {
            SpawnCreature(new EvolutionCandidate(EvolutionGenome.CreateRandom()), GetRandomPointInSimulationArea());
        }

        Debug.Log("Population went extinct, reseeded for testing.");
    }

    public MarineCreatureAgent SpawnCreature(EvolutionCandidate candidate, Vector3 position, float startingEnergy = -1f)
    {
        if (CreaturePrefab == null)
        {
            Debug.LogError("CreaturePrefab is missing on EvolutionEcosystemManager.");
            return null;
        }

        MarineCreatureAgent creature = Instantiate(CreaturePrefab, position, Random.rotation);
        creature.Initialise(candidate, startingEnergy);
        activeCreatures.Add(creature);

        if (StatsTracker != null)
        {
            StatsTracker.RegisterBirth();
        }

        return creature;
    }

    public FoodSource SpawnFood(EcosystemFoodType foodType, Vector3 position, float energyValue)
    {
        if (FoodPrefab == null)
        {
            Debug.LogError("FoodPrefab is missing on EvolutionEcosystemManager.");
            return null;
        }

        FoodSource food = Instantiate(FoodPrefab, position, Quaternion.identity);
        food.Initialise(foodType, energyValue);

        if (Environment != null && foodType != EcosystemFoodType.Plant)
        {
            food.FreshMeatRotTime /= Mathf.Max(0.05f, Environment.MeatRotSpeedMultiplier);
        }

        activeFood.Add(food);
        return food;
    }

    public void UnregisterCreature(MarineCreatureAgent creature, bool createMeat, bool killedByCreature)
    {
        if (creature == null)
        {
            return;
        }

        activeCreatures.Remove(creature);

        if (StatsTracker != null)
        {
            StatsTracker.RegisterDeath(killedByCreature);
        }

        if (createMeat && CreateMeatOnDeath && creature.Candidate != null && creature.Candidate.Genome != null)
        {
            float energy = Mathf.Max(MinimumMeatEnergy, creature.Candidate.Genome.BodySize * MeatEnergyPerBodySize + Mathf.Max(0f, creature.CurrentEnergy * 0.35f));
            SpawnFood(EcosystemFoodType.FreshMeat, ClampToSimulationArea(creature.transform.position), energy);
        }
    }

    public void UnregisterFood(FoodSource food)
    {
        if (food != null)
        {
            activeFood.Remove(food);
        }
    }

    public void RegisterFoodRotted()
    {
        if (StatsTracker != null)
        {
            StatsTracker.RegisterFoodRotted();
        }
    }

    public void TriggerExtinctionEvent()
    {
        CleanLists();

        if (activeCreatures.Count <= 0)
        {
            return;
        }

        int amountToKill = Mathf.RoundToInt(activeCreatures.Count * ExtinctionKillPercentage);
        amountToKill = Mathf.Clamp(amountToKill, 1, activeCreatures.Count);

        List<MarineCreatureAgent> copy = new List<MarineCreatureAgent>(activeCreatures);

        for (int i = 0; i < amountToKill; i++)
        {
            if (copy.Count <= 0)
            {
                break;
            }

            int index = Random.Range(0, copy.Count);
            MarineCreatureAgent creature = copy[index];
            copy.RemoveAt(index);

            if (creature != null)
            {
                creature.TakeDamage(999999f, null);
            }
        }

        if (StatsTracker != null)
        {
            StatsTracker.RegisterExtinctionEvent();
        }

        Debug.Log("Extinction pressure triggered. Removed about " + amountToKill + " creatures.");
    }

    public bool CanSpawnMoreCreatures()
    {
        return activeCreatures.Count < MaxPopulation;
    }

    public float GetEnvironmentMutationMultiplier()
    {
        return Environment != null ? Environment.MutationMultiplier : 1f;
    }

    public FoodSource GetNearestEdibleFood(Vector3 position, float searchRange, EvolutionGenome genome)
    {
        FoodSource best = null;
        float bestScore = 0f;
        float searchRangeSqr = searchRange * searchRange;

        for (int i = 0; i < activeFood.Count; i++)
        {
            FoodSource food = activeFood[i];

            if (food == null || food.IsConsumed)
            {
                continue;
            }

            float dietPreference = genome != null ? genome.GetDietPreference(food.FoodType) : 1f;
            if (dietPreference <= 0.05f)
            {
                continue;
            }

            float distanceSqr = (food.transform.position - position).sqrMagnitude;
            if (distanceSqr > searchRangeSqr)
            {
                continue;
            }

            float distance = Mathf.Sqrt(Mathf.Max(0.0001f, distanceSqr));
            float score = dietPreference * food.EnergyValue / distance;

            if (score > bestScore)
            {
                bestScore = score;
                best = food;
            }
        }

        return best;
    }

    public MarineCreatureAgent GetNearestCreature(MarineCreatureAgent requester, Vector3 position, float searchRange)
    {
        MarineCreatureAgent best = null;
        float bestDistanceSqr = searchRange * searchRange;

        for (int i = 0; i < activeCreatures.Count; i++)
        {
            MarineCreatureAgent creature = activeCreatures[i];

            if (creature == null || creature == requester || !creature.IsAlive)
            {
                continue;
            }

            float distanceSqr = (creature.transform.position - position).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                best = creature;
            }
        }

        return best;
    }

    public MarineCreatureAgent GetNearestSimilarCreature(MarineCreatureAgent requester, Vector3 position, float searchRange)
    {
        if (requester == null || requester.Genome == null)
        {
            return null;
        }

        MarineCreatureAgent best = null;
        float bestDistanceSqr = searchRange * searchRange;

        for (int i = 0; i < activeCreatures.Count; i++)
        {
            MarineCreatureAgent creature = activeCreatures[i];

            if (creature == null || creature == requester || !creature.IsAlive || creature.Genome == null)
            {
                continue;
            }

            if (!SpeciesUtility.AreSimilarEnoughForGrouping(requester.Genome, creature.Genome))
            {
                continue;
            }

            float distanceSqr = (creature.transform.position - position).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                best = creature;
            }
        }

        return best;
    }

    public MarineCreatureAgent GetNearestPreyCandidate(MarineCreatureAgent hunter, Vector3 position, float searchRange)
    {
        if (hunter == null || hunter.Genome == null || hunter.Genome.GetHunterScore() < 0.25f)
        {
            return null;
        }

        MarineCreatureAgent best = null;
        float bestScore = 0f;
        float searchRangeSqr = searchRange * searchRange;

        for (int i = 0; i < activeCreatures.Count; i++)
        {
            MarineCreatureAgent target = activeCreatures[i];

            if (target == null || target == hunter || !target.IsAlive || target.Genome == null)
            {
                continue;
            }

            float distanceSqr = (target.transform.position - position).sqrMagnitude;
            if (distanceSqr > searchRangeSqr)
            {
                continue;
            }

            float sizeLimit = hunter.Genome.BodySize * Mathf.Lerp(0.75f, 1.45f, hunter.Genome.RiskTolerance);
            if (target.Genome.BodySize > sizeLimit)
            {
                continue;
            }

            float distance = Mathf.Sqrt(Mathf.Max(0.0001f, distanceSqr));
            float speciesPenalty = SpeciesUtility.AreSimilarEnoughForGrouping(hunter.Genome, target.Genome) ? 0.35f : 1f;
            float targetValue = target.Genome.BodySize + Mathf.Clamp01(target.CurrentEnergy / Mathf.Max(1f, target.Genome.EnergyCapacity));
            float score = hunter.Genome.GetHunterScore() * hunter.Genome.MeatDiet * speciesPenalty * targetValue / distance;

            if (score > bestScore)
            {
                bestScore = score;
                best = target;
            }
        }

        return best;
    }

    public MarineCreatureAgent GetNearestThreat(MarineCreatureAgent requester, Vector3 position, float searchRange)
    {
        if (requester == null || requester.Genome == null)
        {
            return null;
        }

        MarineCreatureAgent best = null;
        float bestScore = 0f;
        float searchRangeSqr = searchRange * searchRange;

        for (int i = 0; i < activeCreatures.Count; i++)
        {
            MarineCreatureAgent other = activeCreatures[i];

            if (other == null || other == requester || !other.IsAlive || other.Genome == null)
            {
                continue;
            }

            if (other.Genome.GetHunterScore() < 0.35f)
            {
                continue;
            }

            float distanceSqr = (other.transform.position - position).sqrMagnitude;
            if (distanceSqr > searchRangeSqr)
            {
                continue;
            }

            float sizeThreat = other.Genome.BodySize / Mathf.Max(0.1f, requester.Genome.BodySize);
            float aggressionThreat = other.Genome.Aggression;
            float distance = Mathf.Sqrt(Mathf.Max(0.0001f, distanceSqr));
            float score = (sizeThreat * 0.55f + aggressionThreat * 0.45f) / distance;

            if (score > bestScore)
            {
                bestScore = score;
                best = other;
            }
        }

        return best;
    }

    public Vector3 GetRandomPointInSimulationArea()
    {
        Vector3 centre = transform.position;
        Vector3 half = SimulationAreaSize * 0.5f;
        float padding = Mathf.Max(0f, SpawnPaddingFromBounds);

        float minX = centre.x - half.x + padding;
        float maxX = centre.x + half.x - padding;
        float minY = centre.y - half.y + padding;
        float maxY = centre.y + half.y - padding;
        float minZ = centre.z - half.z + padding;
        float maxZ = centre.z + half.z - padding;

        if (minX > maxX) { minX = centre.x; maxX = centre.x; }
        if (minY > maxY) { minY = centre.y; maxY = centre.y; }
        if (minZ > maxZ) { minZ = centre.z; maxZ = centre.z; }

        return new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            Random.Range(minZ, maxZ)
        );
    }

    public Vector3 ClampToSimulationArea(Vector3 position)
    {
        Vector3 centre = transform.position;
        Vector3 half = SimulationAreaSize * 0.5f;

        position.x = Mathf.Clamp(position.x, centre.x - half.x, centre.x + half.x);
        position.y = Mathf.Clamp(position.y, centre.y - half.y, centre.y + half.y);
        position.z = Mathf.Clamp(position.z, centre.z - half.z, centre.z + half.z);

        return position;
    }

    private void CleanLists()
    {
        activeCreatures.RemoveAll(creature => creature == null || !creature.IsAlive);
        activeFood.RemoveAll(food => food == null || food.IsConsumed);
    }

    private void UpdateCounts()
    {
        CurrentPopulation = activeCreatures.Count;
        CurrentPlantFood = 0;
        CurrentFreshMeat = 0;
        CurrentRottenMeat = 0;

        for (int i = 0; i < activeFood.Count; i++)
        {
            FoodSource food = activeFood[i];
            if (food == null)
            {
                continue;
            }

            if (food.FoodType == EcosystemFoodType.Plant) CurrentPlantFood++;
            else if (food.FoodType == EcosystemFoodType.FreshMeat) CurrentFreshMeat++;
            else CurrentRottenMeat++;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, SimulationAreaSize);
    }
}
