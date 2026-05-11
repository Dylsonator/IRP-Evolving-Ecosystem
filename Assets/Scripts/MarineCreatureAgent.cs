using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MarineCreatureAgent : MonoBehaviour
{
    [Header("Runtime")]
    public EvolutionCandidate Candidate;
    public float CurrentEnergy;
    public float CurrentHealth;
    public bool IsAlive = true;

    [Header("Energy")]
    public float BaseEnergyDrainPerSecond = 3.2f;
    public float ReproductionCooldown = 6f;
    public float MinimumAgeBeforeReproduction = 8f;

    [Header("Movement")]
    public float FoodEatDistance = 1.2f;
    public float CreatureSeparationDistance = 2f;
    public float SwimNoiseStrength = 0.18f;

    [Header("Combat")]
    public float BaseAttackCooldown = 1.35f;
    public float MinimumHunterScoreToChase = 0.34f;
    public float SameSpeciesAttackPenalty = 0.35f;

    [Header("Boundary Safety")]
    public float BoundaryAvoidanceDistance = 7f;
    public float BoundaryAvoidanceStrength = 6f;
    public float BoundaryHardStopMargin = 0.4f;
    public float BoundaryVelocityDamping = 0.15f;
    public bool DebugBoundaryAvoidance;

    private Rigidbody rb;
    private CreatureBodyMorph bodyMorph;

    private FoodSource nearestFood;
    private MarineCreatureAgent nearestCreature;
    private MarineCreatureAgent nearestGroupMate;
    private MarineCreatureAgent nearestPrey;
    private MarineCreatureAgent nearestThreat;

    private float reproductionTimer;
    private float attackTimer;
    private float aliveTimer;
    private Vector3 lastPosition;
    private Vector3 wantedDirection;

    public EvolutionGenome Genome
    {
        get
        {
            return Candidate != null ? Candidate.Genome : null;
        }
    }

    public void Initialise(EvolutionCandidate candidate, float startingEnergy = -1f)
    {
        Candidate = candidate;

        if (Candidate == null)
        {
            Candidate = new EvolutionCandidate(EvolutionGenome.CreateRandom());
        }

        if (Candidate.Genome == null)
        {
            Candidate.Genome = EvolutionGenome.CreateRandom();
        }

        Candidate.Genome.ClampValues();

        CurrentEnergy = startingEnergy > 0f
            ? Mathf.Min(startingEnergy, Candidate.Genome.EnergyCapacity)
            : Candidate.Genome.EnergyCapacity * 0.65f;

        CurrentHealth = Candidate.Genome.GetMaxHealth();
        IsAlive = true;

        transform.localScale = Vector3.one * Candidate.Genome.BodySize;
        lastPosition = transform.position;
        aliveTimer = 0f;
        reproductionTimer = Random.Range(1f, ReproductionCooldown);
        attackTimer = Random.Range(0f, BaseAttackCooldown);

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.linearDamping = 2.2f;
        rb.angularDamping = 5f;

        if (bodyMorph == null)
        {
            bodyMorph = GetComponent<CreatureBodyMorph>();
        }

        if (bodyMorph != null)
        {
            bodyMorph.ApplyGenome(Candidate.Genome);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bodyMorph = GetComponent<CreatureBodyMorph>();
    }

    private void Start()
    {
        if (Candidate == null)
        {
            Initialise(new EvolutionCandidate(EvolutionGenome.CreateRandom()));
        }
    }

    private void FixedUpdate()
    {
        if (!IsAlive || Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        aliveTimer += Time.fixedDeltaTime;
        reproductionTimer -= Time.fixedDeltaTime;
        attackTimer -= Time.fixedDeltaTime;

        SenseEnvironment();
        RunBrainMovement();
        DrainEnergy();
        TryEatFood();
        TryAttackPrey();
        TryReproduce();
        UpdateMetrics();

        if (CurrentEnergy <= 0f)
        {
            Die(null, true, "Starved");
        }
    }

    private void SenseEnvironment()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        float visionRange = Candidate.Genome.GetVisionRange();

        nearestFood = EvolutionEcosystemManager.Instance.GetNearestEdibleFood(transform.position, visionRange, Candidate.Genome);
        nearestCreature = EvolutionEcosystemManager.Instance.GetNearestCreature(this, transform.position, visionRange);
        nearestGroupMate = EvolutionEcosystemManager.Instance.GetNearestSimilarCreature(this, transform.position, visionRange);
        nearestPrey = EvolutionEcosystemManager.Instance.GetNearestPreyCandidate(this, transform.position, visionRange);
        nearestThreat = EvolutionEcosystemManager.Instance.GetNearestThreat(this, transform.position, visionRange);
    }

    private void RunBrainMovement()
    {
        EvolutionGenome genome = Candidate.Genome;
        genome.EnsureBrainIsValid();

        float visionRange = Mathf.Max(0.1f, genome.GetVisionRange());
        float energyRatio = Mathf.Clamp01(CurrentEnergy / genome.EnergyCapacity);

        Vector3 toFood = GetDirectionTo(nearestFood != null ? nearestFood.transform.position : transform.position);
        float foodCloseness = GetCloseness(nearestFood != null ? nearestFood.transform.position : transform.position, visionRange, nearestFood != null);

        Vector3 toCreature = GetDirectionTo(nearestCreature != null ? nearestCreature.transform.position : transform.position);
        float creatureCloseness = GetCloseness(nearestCreature != null ? nearestCreature.transform.position : transform.position, visionRange, nearestCreature != null);

        Vector3 toThreat = GetDirectionTo(nearestThreat != null ? nearestThreat.transform.position : transform.position);

        float[] inputs =
        {
            energyRatio,
            toFood.x,
            toFood.y,
            toFood.z,
            foodCloseness,
            toCreature.x,
            toCreature.y,
            toCreature.z,
            creatureCloseness,
            toThreat.x,
            toThreat.y,
            toThreat.z,
            genome.HungerDrive,
            genome.Aggression,
            Random.Range(-1f, 1f)
        };

        float[] outputs = genome.Brain.Evaluate(inputs);
        Vector3 brainDirection = new Vector3(outputs[0], outputs[1], outputs[2]);

        float hunger = 1f - energyRatio;
        float foodDietValue = nearestFood != null ? genome.GetDietPreference(nearestFood.FoodType) : 0f;
        Vector3 foodPull = toFood * genome.HungerDrive * hunger * (0.3f + foodDietValue);

        Vector3 preyPull = Vector3.zero;
        if (nearestPrey != null)
        {
            float hunterScore = genome.GetHunterScore();
            float sameSpeciesMultiplier = IsSimilarSpecies(nearestPrey) ? SameSpeciesAttackPenalty : 1f;
            preyPull = GetDirectionTo(nearestPrey.transform.position) * hunterScore * genome.Aggression * sameSpeciesMultiplier;
        }

        Vector3 groupingPull = Vector3.zero;
        if (nearestGroupMate != null)
        {
            float socialWeight = Mathf.Lerp(0.2f, 1f, energyRatio);
            groupingPull = GetDirectionTo(nearestGroupMate.transform.position) * genome.GroupingChance * socialWeight;
        }

        Vector3 separationPush = Vector3.zero;
        if (nearestCreature != null)
        {
            float distance = Vector3.Distance(transform.position, nearestCreature.transform.position);
            float personalSpace = CreatureSeparationDistance * genome.BodySize;

            if (distance < personalSpace)
            {
                float strength = 1f - Mathf.Clamp01(distance / personalSpace);
                separationPush = -GetDirectionTo(nearestCreature.transform.position) * strength * genome.SeparationDrive;
            }
        }

        Vector3 threatPush = Vector3.zero;
        if (nearestThreat != null)
        {
            float threatDistance = Vector3.Distance(transform.position, nearestThreat.transform.position);
            float threatStrength = 1f - Mathf.Clamp01(threatDistance / visionRange);
            threatPush = -GetDirectionTo(nearestThreat.transform.position) * threatStrength * (1f - genome.RiskTolerance) * 1.8f;
        }

        Vector3 noise = Random.insideUnitSphere * SwimNoiseStrength;
        Vector3 boundaryPush = GetBoundaryAvoidanceDirection();

        wantedDirection = brainDirection + foodPull + preyPull + groupingPull + separationPush + threatPush + boundaryPush + noise;
        PreventOutwardDirectionAtBounds(ref wantedDirection);

        if (wantedDirection.sqrMagnitude < 0.05f)
        {
            wantedDirection = GetDirectionToSimulationCentre();
        }

        wantedDirection.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(wantedDirection, Vector3.up);
        Quaternion newRotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            genome.GetEffectiveTurnRate() * Time.fixedDeltaTime
        );

        rb.MoveRotation(newRotation);

        Vector3 wantedVelocity = transform.forward * genome.GetEffectiveSpeed();
        Vector3 newVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            wantedVelocity,
            genome.GetEffectiveAcceleration() * Time.fixedDeltaTime
        );

        rb.linearVelocity = PreventOutwardVelocityAtBounds(newVelocity);

        if (EvolutionEcosystemManager.Instance != null)
        {
            Vector3 clampedPosition = EvolutionEcosystemManager.Instance.ClampToSimulationArea(rb.position);

            if ((clampedPosition - rb.position).sqrMagnitude > 0.0001f)
            {
                rb.position = clampedPosition;
                rb.linearVelocity = PreventOutwardVelocityAtBounds(rb.linearVelocity) * BoundaryVelocityDamping;
            }
        }
    }

    private void DrainEnergy()
    {
        float environmentDrainMultiplier = 1f;

        if (EvolutionEcosystemManager.Instance != null && EvolutionEcosystemManager.Instance.Environment != null)
        {
            environmentDrainMultiplier = EvolutionEcosystemManager.Instance.Environment.EnergyDrainMultiplier;
        }

        float drain = BaseEnergyDrainPerSecond * Candidate.Genome.GetEnergyDrainMultiplier() * environmentDrainMultiplier;
        CurrentEnergy -= drain * Time.fixedDeltaTime;
    }

    private void TryEatFood()
    {
        if (nearestFood == null || nearestFood.IsConsumed)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, nearestFood.transform.position);
        float eatDistance = FoodEatDistance * Mathf.Max(0.5f, Candidate.Genome.BodySize);

        if (distance > eatDistance)
        {
            return;
        }

        float dietPreference = Candidate.Genome.GetDietPreference(nearestFood.FoodType);
        if (dietPreference <= 0.05f)
        {
            return;
        }

        EcosystemFoodType foodType = nearestFood.FoodType;
        float baseEnergy = nearestFood.Consume();
        float gained = baseEnergy * Mathf.Lerp(0.35f, 1.25f, dietPreference) * Candidate.Genome.DigestiveEfficiency;

        CurrentEnergy = Mathf.Min(CurrentEnergy + gained, Candidate.Genome.EnergyCapacity);

        Candidate.EnergyGained += gained;
        Candidate.FoodEaten++;

        if (foodType == EcosystemFoodType.Plant)
        {
            Candidate.PlantMeals++;
        }
        else if (foodType == EcosystemFoodType.FreshMeat)
        {
            Candidate.MeatMeals++;
        }
        else
        {
            Candidate.CarrionMeals++;
        }

        if (EvolutionEcosystemManager.Instance != null && EvolutionEcosystemManager.Instance.StatsTracker != null)
        {
            EvolutionEcosystemManager.Instance.StatsTracker.RegisterFoodEaten(foodType);
        }
    }

    private void TryAttackPrey()
    {
        EvolutionGenome genome = Candidate.Genome;

        if (nearestPrey == null || !nearestPrey.IsAlive)
        {
            return;
        }

        if (attackTimer > 0f)
        {
            return;
        }

        if (genome.GetHunterScore() < MinimumHunterScoreToChase)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, nearestPrey.transform.position);
        if (distance > genome.GetAttackRange())
        {
            return;
        }

        attackTimer = Mathf.Lerp(BaseAttackCooldown * 1.25f, BaseAttackCooldown * 0.55f, genome.Aggression);

        float damage = genome.GetAttackDamage();
        if (IsSimilarSpecies(nearestPrey))
        {
            damage *= SameSpeciesAttackPenalty;
        }

        Candidate.DamageDealt += damage;
        nearestPrey.TakeDamage(damage, this);
    }

    private void TryReproduce()
    {
        if (reproductionTimer > 0f || aliveTimer < MinimumAgeBeforeReproduction)
        {
            return;
        }

        if (CurrentEnergy < Candidate.Genome.ReproductionEnergyThreshold)
        {
            return;
        }

        if (EvolutionEcosystemManager.Instance == null || !EvolutionEcosystemManager.Instance.CanSpawnMoreCreatures())
        {
            return;
        }

        float mutationMultiplier = EvolutionEcosystemManager.Instance.GetEnvironmentMutationMultiplier();
        EvolutionCandidate childCandidate = Candidate.CreateChild(mutationMultiplier);

        float childEnergy = childCandidate.Genome.EnergyCapacity * 0.45f;
        CurrentEnergy *= 0.55f;

        Vector3 childPosition = transform.position + Random.insideUnitSphere * Mathf.Max(1.5f, Candidate.Genome.BodySize * 2f);
        childPosition = EvolutionEcosystemManager.Instance.ClampToSimulationArea(childPosition);

        EvolutionEcosystemManager.Instance.SpawnCreature(childCandidate, childPosition, childEnergy);

        Candidate.ReproductionCount++;
        reproductionTimer = ReproductionCooldown;
    }

    public void TakeDamage(float amount, MarineCreatureAgent attacker)
    {
        if (!IsAlive)
        {
            return;
        }

        float finalDamage = amount / (1f + Candidate.Genome.Armour * 0.55f);
        CurrentHealth -= finalDamage;
        Candidate.DamageTaken += finalDamage;

        if (CurrentHealth <= 0f)
        {
            Die(attacker, true, "Killed");
        }
    }

    private void Die(MarineCreatureAgent killer, bool createMeat, string reason)
    {
        if (!IsAlive)
        {
            return;
        }

        IsAlive = false;

        if (killer != null && killer.Candidate != null)
        {
            killer.Candidate.Kills++;
        }

        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterCreature(this, createMeat, killer != null);
        }

        Destroy(gameObject);
    }

    private void UpdateMetrics()
    {
        Candidate.SurvivalTime = aliveTimer;
        Candidate.DistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
    }

    private bool IsSimilarSpecies(MarineCreatureAgent other)
    {
        return other != null && SpeciesUtility.AreSimilarEnoughForGrouping(Candidate.Genome, other.Genome);
    }

    private Vector3 GetDirectionTo(Vector3 target)
    {
        Vector3 direction = target - transform.position;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    private float GetCloseness(Vector3 target, float range, bool valid)
    {
        if (!valid)
        {
            return 0f;
        }

        return 1f - Mathf.Clamp01(Vector3.Distance(transform.position, target) / range);
    }

    private Vector3 GetBoundaryAvoidanceDirection()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return Vector3.zero;
        }

        Vector3 centre = EvolutionEcosystemManager.Instance.transform.position;
        Vector3 half = EvolutionEcosystemManager.Instance.SimulationAreaSize * 0.5f;
        Vector3 position = rb != null ? rb.position : transform.position;

        Vector3 min = centre - half;
        Vector3 max = centre + half;
        Vector3 push = Vector3.zero;

        AddBoundaryPush(position.x - min.x, Vector3.right, ref push);
        AddBoundaryPush(max.x - position.x, Vector3.left, ref push);
        AddBoundaryPush(position.y - min.y, Vector3.up, ref push);
        AddBoundaryPush(max.y - position.y, Vector3.down, ref push);
        AddBoundaryPush(position.z - min.z, Vector3.forward, ref push);
        AddBoundaryPush(max.z - position.z, Vector3.back, ref push);

        if (push.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 result = push.normalized * BoundaryAvoidanceStrength;

        if (DebugBoundaryAvoidance)
        {
            Debug.DrawRay(position, result, Color.yellow, Time.fixedDeltaTime);
        }

        return result;
    }

    private void AddBoundaryPush(float distanceToBoundary, Vector3 inwardDirection, ref Vector3 push)
    {
        if (distanceToBoundary >= BoundaryAvoidanceDistance)
        {
            return;
        }

        float strength = 1f - Mathf.Clamp01(distanceToBoundary / Mathf.Max(0.01f, BoundaryAvoidanceDistance));
        push += inwardDirection * strength;
    }

    private void PreventOutwardDirectionAtBounds(ref Vector3 direction)
    {
        if (EvolutionEcosystemManager.Instance == null || direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 blocked = PreventOutwardVelocityAtBounds(direction);
        direction = blocked;
    }

    private Vector3 PreventOutwardVelocityAtBounds(Vector3 velocity)
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return velocity;
        }

        Vector3 centre = EvolutionEcosystemManager.Instance.transform.position;
        Vector3 half = EvolutionEcosystemManager.Instance.SimulationAreaSize * 0.5f;
        Vector3 position = rb != null ? rb.position : transform.position;

        Vector3 min = centre - half;
        Vector3 max = centre + half;

        if (position.x <= min.x + BoundaryHardStopMargin && velocity.x < 0f) velocity.x = 0f;
        if (position.x >= max.x - BoundaryHardStopMargin && velocity.x > 0f) velocity.x = 0f;
        if (position.y <= min.y + BoundaryHardStopMargin && velocity.y < 0f) velocity.y = 0f;
        if (position.y >= max.y - BoundaryHardStopMargin && velocity.y > 0f) velocity.y = 0f;
        if (position.z <= min.z + BoundaryHardStopMargin && velocity.z < 0f) velocity.z = 0f;
        if (position.z >= max.z - BoundaryHardStopMargin && velocity.z > 0f) velocity.z = 0f;

        return velocity;
    }

    private Vector3 GetDirectionToSimulationCentre()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return Random.insideUnitSphere.normalized;
        }

        Vector3 direction = EvolutionEcosystemManager.Instance.transform.position - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return Random.insideUnitSphere.normalized;
        }

        return direction.normalized;
    }
}
