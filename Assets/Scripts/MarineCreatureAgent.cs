using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MarineCreatureAgent : MonoBehaviour
{
    [Header("Runtime")]
    public EvolutionCandidate Candidate;
    public float CurrentEnergy;

    [Header("Energy")]
    public float BaseEnergyDrainPerSecond = 3.5f;
    public float ReproductionCooldown = 5f;

    [Header("Movement")]
    public float FoodEatDistance = 1.25f;
    public float SeparationDistance = 2f;
    public float SwimNoiseStrength = 0.15f;

    private Rigidbody rb;
    private FoodSource nearestFood;
    private MarineCreatureAgent nearestCreature;

    private float reproductionTimer;
    private Vector3 lastPosition;
    private Vector3 wantedDirection;
    private float aliveTimer;

    public void Initialise(EvolutionCandidate candidate)
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
        CurrentEnergy = Candidate.Genome.EnergyCapacity * 0.65f;

        transform.localScale = Vector3.one * Candidate.Genome.BodySize;
        lastPosition = transform.position;
        aliveTimer = 0f;
        reproductionTimer = Random.Range(1f, ReproductionCooldown);

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        aliveTimer += Time.fixedDeltaTime;
        reproductionTimer -= Time.fixedDeltaTime;

        SenseEnvironment();
        RunBrainMovement();
        DrainEnergy();
        TryEatFood();
        TryReproduce();
        UpdateMetrics();

        if (CurrentEnergy <= 0f)
        {
            Die(false);
        }
    }

    private void SenseEnvironment()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        nearestFood = EvolutionEcosystemManager.Instance.GetNearestFood(transform.position, Candidate.Genome.VisionRange);
        nearestCreature = EvolutionEcosystemManager.Instance.GetNearestCreature(this, transform.position, Candidate.Genome.VisionRange);
    }

    private void RunBrainMovement()
    {
        Vector3 toFood = Vector3.zero;
        float foodDistanceNormalised = 1f;

        if (nearestFood != null)
        {
            toFood = nearestFood.transform.position - transform.position;
            foodDistanceNormalised = Mathf.Clamp01(toFood.magnitude / Candidate.Genome.VisionRange);
            toFood = toFood.normalized;
        }

        Vector3 toCreature = Vector3.zero;
        float creatureDistanceNormalised = 1f;

        if (nearestCreature != null)
        {
            toCreature = nearestCreature.transform.position - transform.position;
            creatureDistanceNormalised = Mathf.Clamp01(toCreature.magnitude / Candidate.Genome.VisionRange);
            toCreature = toCreature.normalized;
        }

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Candidate.Genome.EnergyCapacity);

        float[] inputs =
        {
            energyRatio,
            toFood.x,
            toFood.z,
            1f - foodDistanceNormalised,
            toCreature.x,
            toCreature.z,
            1f - creatureDistanceNormalised,
            Random.Range(-1f, 1f)
        };

        float[] outputs = Candidate.Genome.Brain.Evaluate(inputs);

        Vector3 brainDirection = new Vector3(outputs[0], 0f, outputs[1]);

        Vector3 foodPull = toFood * Candidate.Genome.HungerDrive * (1f - energyRatio);
        Vector3 groupingPull = toCreature * Candidate.Genome.GroupingChance * Candidate.Genome.AttractionRange;

        Vector3 separationPush = Vector3.zero;
        if (nearestCreature != null)
        {
            float creatureDistance = Vector3.Distance(transform.position, nearestCreature.transform.position);

            if (creatureDistance < SeparationDistance * Candidate.Genome.BodySize)
            {
                separationPush = -toCreature * (1f - Candidate.Genome.RiskTolerance);
            }
        }

        Vector3 noise = Random.insideUnitSphere * SwimNoiseStrength;
        noise.y = 0f;

        wantedDirection = brainDirection + foodPull + groupingPull + separationPush + noise;

        if (wantedDirection.sqrMagnitude < 0.05f)
        {
            wantedDirection = transform.forward;
        }

        wantedDirection.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(wantedDirection, Vector3.up);
        Quaternion newRotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            Candidate.Genome.TurnRate * Time.fixedDeltaTime
        );

        rb.MoveRotation(newRotation);

        Vector3 wantedVelocity = transform.forward * Candidate.Genome.Speed;
        Vector3 newVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            wantedVelocity,
            Candidate.Genome.Acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = newVelocity;

        if (EvolutionEcosystemManager.Instance != null)
        {
            Vector3 clampedPosition = EvolutionEcosystemManager.Instance.ClampToSimulationArea(transform.position);
            rb.position = clampedPosition;
        }
    }

    private void DrainEnergy()
    {
        float environmentDrain = 1f;

        if (EvolutionEcosystemManager.Instance != null && EvolutionEcosystemManager.Instance.Environment != null)
        {
            environmentDrain = EvolutionEcosystemManager.Instance.Environment.EnergyDrainMultiplier;
        }

        float movementCost = rb.linearVelocity.magnitude / Mathf.Max(0.1f, Candidate.Genome.Speed);
        float traitCost = Candidate.Genome.GetEnergyDrainMultiplier();

        float drain = BaseEnergyDrainPerSecond * traitCost * environmentDrain;
        drain += movementCost * 0.75f;

        CurrentEnergy -= drain * Time.fixedDeltaTime;
    }

    private void TryEatFood()
    {
        if (nearestFood == null || nearestFood.IsConsumed)
        {
            return;
        }

        float eatDistance = FoodEatDistance * Candidate.Genome.BodySize;

        if (Vector3.Distance(transform.position, nearestFood.transform.position) > eatDistance)
        {
            return;
        }

        float energyGained = nearestFood.Consume();

        CurrentEnergy = Mathf.Min(
            CurrentEnergy + energyGained,
            Candidate.Genome.EnergyCapacity
        );

        Candidate.EnergyGained += energyGained;
        Candidate.FoodEaten++;
    }

    private void TryReproduce()
    {
        if (reproductionTimer > 0f)
        {
            return;
        }

        if (CurrentEnergy < Candidate.Genome.ReproductionEnergyThreshold)
        {
            return;
        }

        if (EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        float mutationMultiplier = 1f;

        if (EvolutionEcosystemManager.Instance.Environment != null)
        {
            mutationMultiplier = EvolutionEcosystemManager.Instance.Environment.MutationMultiplier;
        }

        EvolutionCandidate offspring = Candidate.CreateChild(mutationMultiplier);
        EvolutionEcosystemManager.Instance.RegisterOffspring(offspring);

        Candidate.ReproductionCount++;
        CurrentEnergy *= 0.5f;
        reproductionTimer = ReproductionCooldown;
    }

    private void UpdateMetrics()
    {
        float distance = Vector3.Distance(transform.position, lastPosition);
        Candidate.DistanceTravelled += distance;
        Candidate.SurvivalTime = aliveTimer;
        Candidate.AverageSpeedUsed = Mathf.Lerp(Candidate.AverageSpeedUsed, rb.linearVelocity.magnitude, 0.05f);

        if (nearestFood != null)
        {
            float foodDistance = Vector3.Distance(transform.position, nearestFood.transform.position);
            Candidate.AverageFoodDistance = Mathf.Lerp(Candidate.AverageFoodDistance, foodDistance, 0.05f);
        }

        lastPosition = transform.position;
    }

    public void Die(bool causedByExtinctionEvent)
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterCreature(this);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Candidate.Genome.VisionRange);
    }
}
