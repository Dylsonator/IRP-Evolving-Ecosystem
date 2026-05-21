using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MarineCreatureAgent : MonoBehaviour
{
    private static int labelFrame = -1;
    private static int labelsDrawnThisFrame;

    [Header("Runtime")]
    public EvolutionCandidate Candidate;
    public CreatureEffectiveStats EffectiveStats;
    public CreatureMorphLibrary MorphLibrary;
    public float CurrentEnergy;
    public string DebugName;
    public CreatureBehaviourType DebugBehaviourType;

    [Header("Energy")]
    public float BaseEnergyDrainPerSecond = 1.45f;
    public float ReproductionCooldown = 7f;

    [Header("3D Swimming")]
    public float FoodEatDistance = 1.0f;
    public float SeparationDistance = 1.8f;
    public float SwimNoiseStrength = 0.10f;
    public bool EnableFull3DRoll = true;
    public float MaxRollAngle = 42f;
    public float RollResponsiveness = 5f;
    public float VerticalSwimMultiplier = 1.25f;
    public float MinimumApproachSpeedScale = 0.28f;
    public float CloseTargetSlowdownDistance = 2.4f;
    public float StuckNudgeSpeed = 0.08f;
    public float StuckNudgeTime = 1.2f;

    [Header("Mouth / Eating Area")]
    public bool UseMouthBasedEating = true;
    public float MouthForwardOffset = 0.58f;
    public float MouthRadius = 0.36f;
    [Range(5f, 180f)] public float MouthAngle = 92f;
    public float CloseContactEatForgiveness = 0.2f;

    [Header("Predation")]
    public float BiteCooldown = 1.85f;
    public float BaseBiteDamage = 6.5f;
    public float ArmourDamageReductionPerPoint = 0.13f;

    [Header("Decision Stability")]
    public float SurvivalFoodPullMultiplier = 2.65f;
    public float BrainWeightWhenHungry = 0.16f;
    public float BrainWeightWhenFull = 0.9f;
    public float SocialWeightWhenHungry = 0.06f;
    public float SocialWeightWhenFull = 0.65f;
    public float PreyPullMultiplier = 0.72f;
    public float WanderTargetRefreshTime = 4.5f;
    public float WanderStrength = 0.16f;

    [Header("Boundary Safety")]
    public float BoundaryAvoidanceDistance = 7f;
    public float BoundaryAvoidanceStrength = 5.5f;
    public float BoundaryHardStopMargin = 0.4f;
    public float BoundaryVelocityDamping = 0.15f;
    public bool DebugBoundaryAvoidance;

    [Header("Debug Visuals")]
    public bool ApplyTypeColour = true;
    public bool LocalDebugRays;
    public bool LocalDebugLabels;

    private Rigidbody rb;
    private FoodSource nearestFood;
    private CarrionSource nearestCarrion;
    private MarineCreatureAgent nearestCreature;
    private MarineCreatureAgent nearestPrey;
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock materialBlock;

    private float reproductionTimer;
    private float biteTimer;
    private Vector3 lastPosition;
    private Vector3 wantedDirection;
    private Vector3 lastFoodDirection;
    private Vector3 lastCarrionDirection;
    private Vector3 lastCreatureDirection;
    private Vector3 lastPreyDirection;
    private Vector3 lastBoundaryPush;
    private Vector3 wanderDirection;
    private float wanderTimer;
    private float aliveTimer;
    private float lowSpeedNearTargetTimer;

    public void Initialise(EvolutionCandidate candidate)
    {
        Candidate = candidate;

        if (Candidate == null)
        {
            Candidate = new EvolutionCandidate(EvolutionGenome.CreateBaseline());
        }

        if (Candidate.Genome == null)
        {
            Candidate.Genome = EvolutionGenome.CreateBaseline();
        }

        if (MorphLibrary == null)
        {
            MorphLibrary = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance.MorphLibrary : CreatureMorphLibrary.ActiveLibrary;
        }

        Candidate.Genome.ClampValues();
        EffectiveStats = CreatureEffectiveStats.Build(Candidate.Genome, MorphLibrary);
        Candidate.RefreshDebugIdentity();

        DebugName = Candidate.DisplayName;
        DebugBehaviourType = Candidate.BehaviourType;
        gameObject.name = "Creature_" + DebugName;

        CurrentEnergy = EffectiveStats.EnergyCapacity * 0.9f;
        transform.localScale = Vector3.one * EffectiveStats.BodySize;
        lastPosition = transform.position;
        aliveTimer = 0f;
        lowSpeedNearTargetTimer = 0f;
        reproductionTimer = Random.Range(1f, ReproductionCooldown);
        biteTimer = Random.Range(0f, BiteCooldown);
        PickNewWanderDirection();

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.linearDamping = 1.8f;
        rb.angularDamping = 4.5f;

        ApplyMorphVisuals();
        CacheRenderers(true);
        ApplyDebugColour();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        CacheRenderers();
    }

    private void Start()
    {
        if (Candidate == null)
        {
            Initialise(new EvolutionCandidate(EvolutionGenome.CreateBaseline()));
        }
    }

    private void FixedUpdate()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (EffectiveStats == null)
        {
            EffectiveStats = CreatureEffectiveStats.Build(Candidate.Genome, MorphLibrary);
        }

        aliveTimer += Time.fixedDeltaTime;
        reproductionTimer -= Time.fixedDeltaTime;
        biteTimer -= Time.fixedDeltaTime;

        SenseEnvironment();
        RunBrainMovement();
        DrainEnergy();
        TryEatFood();
        TryEatCarrion();
        TryBitePrey();
        TryReproduce();
        UpdateMetrics();
        DrawRuntimeDebugRays();

        if (CurrentEnergy <= 0f)
        {
            Die(false);
        }
    }

    private void CacheRenderers(bool forceRefresh = false)
    {
        if (forceRefresh || cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>();
        }

        if (materialBlock == null)
        {
            materialBlock = new MaterialPropertyBlock();
        }
    }

    private void ApplyMorphVisuals()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        CreatureMorphBuilder builder = GetComponent<CreatureMorphBuilder>();
        if (builder == null)
        {
            builder = gameObject.AddComponent<CreatureMorphBuilder>();
        }

        builder.MorphLibrary = MorphLibrary;
        builder.UseTypeColour = ApplyTypeColour;
        Color colour = CreatureDebugTypeUtility.GetTypeColour(Candidate.BehaviourType);
        builder.Build(Candidate.Genome, EffectiveStats, colour);
    }

    private void ApplyDebugColour()
    {
        if (!ApplyTypeColour || cachedRenderers == null || Candidate == null)
        {
            return;
        }

        Color colour = CreatureDebugTypeUtility.GetTypeColour(Candidate.BehaviourType);

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer rendererToColour = cachedRenderers[i];

            if (rendererToColour == null)
            {
                continue;
            }

            rendererToColour.GetPropertyBlock(materialBlock);
            materialBlock.SetColor("_BaseColor", colour);
            materialBlock.SetColor("_Color", colour);
            rendererToColour.SetPropertyBlock(materialBlock);
        }
    }

    private void SenseEnvironment()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        float senseRange = EffectiveStats != null ? EffectiveStats.VisionRange : Candidate.Genome.VisionRange;
        nearestFood = EvolutionEcosystemManager.Instance.GetNearestFood(GetMouthWorldPosition(), senseRange);
        nearestCarrion = EvolutionEcosystemManager.Instance.GetNearestCarrion(GetMouthWorldPosition(), senseRange);
        nearestCreature = EvolutionEcosystemManager.Instance.GetNearestCreature(this, transform.position, senseRange);
        nearestPrey = EvolutionEcosystemManager.Instance.GetNearestPrey(this, transform.position, senseRange);
    }

    private void RunBrainMovement()
    {
        Vector3 toFood = GetDirectionAndNormalisedDistance(nearestFood != null ? nearestFood.transform.position : (Vector3?)null, out float foodDistanceNormalised);
        Vector3 toCarrion = GetDirectionAndNormalisedDistance(nearestCarrion != null ? nearestCarrion.transform.position : (Vector3?)null, out float carrionDistanceNormalised);
        Vector3 toCreature = GetDirectionAndNormalisedDistance(nearestCreature != null ? nearestCreature.transform.position : (Vector3?)null, out float creatureDistanceNormalised);
        Vector3 toPrey = GetDirectionAndNormalisedDistance(nearestPrey != null ? nearestPrey.GetBiteTargetPosition() : (Vector3?)null, out float preyDistanceNormalised);

        lastFoodDirection = toFood;
        lastCarrionDirection = toCarrion;
        lastCreatureDirection = toCreature;
        lastPreyDirection = toPrey;

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats.EnergyCapacity));
        float hungerPressure = 1f - energyRatio;

        float[] inputs =
        {
            energyRatio,
            toFood.x,
            toFood.y,
            toFood.z,
            1f - foodDistanceNormalised,
            toCreature.x,
            toCreature.y,
            toCreature.z,
            1f - creatureDistanceNormalised,
            1f - carrionDistanceNormalised,
            1f - preyDistanceNormalised,
            Random.Range(-1f, 1f)
        };

        float[] outputs = Candidate.Genome.Brain.Evaluate(inputs);
        float outputX = outputs.Length > 0 ? outputs[0] : 0f;
        float outputY = outputs.Length > 1 ? outputs[1] : 0f;
        float outputZ = outputs.Length > 2 ? outputs[2] : 0f;
        Vector3 brainDirection = new Vector3(outputX, outputY, outputZ);
        brainDirection.y *= VerticalSwimMultiplier * EffectiveStats.VerticalControl;

        UpdateWanderDirection();

        float brainWeight = Mathf.Lerp(BrainWeightWhenHungry, BrainWeightWhenFull, energyRatio);
        float survivalPull = Mathf.Lerp(SurvivalFoodPullMultiplier, 0.8f, energyRatio);
        float plantNeed = Candidate.Genome.PlantDiet * Candidate.Genome.HungerDrive * hungerPressure * survivalPull;
        float carrionNeed = Candidate.Genome.CarrionDiet * Candidate.Genome.HungerDrive * hungerPressure * survivalPull;
        float preyNeed = Candidate.Genome.MeatDiet * Candidate.Genome.Aggression * hungerPressure * PreyPullMultiplier;

        Vector3 foodPull = toFood * plantNeed;
        Vector3 carrionPull = toCarrion * carrionNeed;
        Vector3 preyPull = toPrey * preyNeed;

        float socialWeight = Mathf.Lerp(SocialWeightWhenHungry, SocialWeightWhenFull, energyRatio);
        Vector3 groupingPull = toCreature * Candidate.Genome.GroupingChance * Candidate.Genome.AttractionRange * socialWeight;
        Vector3 wanderPull = wanderDirection * WanderStrength * Mathf.Lerp(0.25f, 1f, energyRatio);
        brainDirection *= brainWeight;

        Vector3 separationPush = GetSocialSeparationPush(toCreature, socialWeight);
        Vector3 noise = Random.insideUnitSphere * SwimNoiseStrength;
        noise.y *= 0.7f;

        Vector3 boundaryPush = GetBoundaryAvoidanceDirection();
        lastBoundaryPush = boundaryPush;

        wantedDirection = brainDirection + foodPull + carrionPull + preyPull + groupingPull + separationPush + boundaryPush + wanderPull + noise;
        wantedDirection += GetCloseTargetCorrection(hungerPressure);
        PreventOutwardDirectionAtBounds(ref wantedDirection);

        if (wantedDirection.sqrMagnitude < 0.05f)
        {
            wantedDirection = GetDirectionToSimulationCentre();
        }

        wantedDirection.Normalize();
        MoveTowardsWantedDirection(GetApproachSpeedScale());
    }

    private Vector3 GetSocialSeparationPush(Vector3 toCreature, float socialWeight)
    {
        Vector3 separationPush = Vector3.zero;

        if (nearestCreature == null)
        {
            return separationPush;
        }

        float creatureDistance = Vector3.Distance(transform.position, nearestCreature.transform.position);
        float personalSpace = SeparationDistance * EffectiveStats.BodySize;
        float threatDistance = GetThreatRange();
        bool actualPredatorThreat = nearestCreature.CanAttackPrey(this);

        if (creatureDistance < personalSpace)
        {
            separationPush += -toCreature * (1f - Candidate.Genome.RiskTolerance) * socialWeight;
        }

        bool shouldFlee = actualPredatorThreat || (Candidate.Genome.ThreatRange > 0.65f && creatureDistance < personalSpace * 1.25f);
        if (shouldFlee && threatDistance > 0.01f)
        {
            float cappedThreatDistance = actualPredatorThreat ? threatDistance : Mathf.Min(threatDistance, personalSpace * 1.5f);
            float threatStrength = 1f - Mathf.Clamp01(creatureDistance / Mathf.Max(0.01f, cappedThreatDistance));
            float predatorBonus = actualPredatorThreat ? 1.4f : 0.55f;
            separationPush += -toCreature * threatStrength * Candidate.Genome.ThreatRange * (1f - Candidate.Genome.RiskTolerance) * socialWeight * predatorBonus;
        }

        return separationPush;
    }

    private Vector3 GetCloseTargetCorrection(float hungerPressure)
    {
        Vector3? target = GetPrimaryFoodTargetPosition();
        if (!target.HasValue)
        {
            lowSpeedNearTargetTimer = 0f;
            return Vector3.zero;
        }

        Vector3 mouthPosition = GetMouthWorldPosition();
        Vector3 toTargetFromMouth = target.Value - mouthPosition;
        float distanceFromMouth = toTargetFromMouth.magnitude;

        if (distanceFromMouth > CloseTargetSlowdownDistance)
        {
            lowSpeedNearTargetTimer = 0f;
            return Vector3.zero;
        }

        if (rb.linearVelocity.magnitude <= StuckNudgeSpeed)
        {
            lowSpeedNearTargetTimer += Time.fixedDeltaTime;
        }
        else
        {
            lowSpeedNearTargetTimer = 0f;
        }

        if (distanceFromMouth <= 0.001f)
        {
            return transform.forward * 0.5f;
        }

        Vector3 correction = toTargetFromMouth.normalized * (1.35f + hungerPressure);

        if (lowSpeedNearTargetTimer >= StuckNudgeTime)
        {
            correction += transform.forward * 0.9f;
            correction += Random.insideUnitSphere * 0.18f;
        }

        return correction;
    }

    private float GetApproachSpeedScale()
    {
        Vector3? target = GetPrimaryFoodTargetPosition();
        if (!target.HasValue)
        {
            return 1f;
        }

        float distance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
        float t = Mathf.Clamp01(distance / Mathf.Max(0.01f, CloseTargetSlowdownDistance));
        return Mathf.Lerp(MinimumApproachSpeedScale, 1f, t);
    }

    private Vector3? GetPrimaryFoodTargetPosition()
    {
        if (nearestFood != null && !nearestFood.IsConsumed && Candidate.Genome.PlantDiet >= Candidate.Genome.CarrionDiet && Candidate.Genome.PlantDiet >= Candidate.Genome.MeatDiet * 0.75f)
        {
            return nearestFood.transform.position;
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed && Candidate.Genome.CarrionDiet >= Candidate.Genome.PlantDiet * 0.75f)
        {
            return nearestCarrion.transform.position;
        }

        if (nearestPrey != null && Candidate.Genome.MeatDiet >= Candidate.Genome.PlantDiet * 0.75f)
        {
            return nearestPrey.GetBiteTargetPosition();
        }

        if (nearestFood != null && !nearestFood.IsConsumed)
        {
            return nearestFood.transform.position;
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            return nearestCarrion.transform.position;
        }

        return null;
    }

    private void MoveTowardsWantedDirection(float speedScale)
    {
        Quaternion targetRotation = Quaternion.LookRotation(wantedDirection, Vector3.up);

        if (EnableFull3DRoll)
        {
            Vector3 localWanted = transform.InverseTransformDirection(wantedDirection);
            float roll = Mathf.Clamp(-localWanted.x * MaxRollAngle, -MaxRollAngle, MaxRollAngle);
            Quaternion bank = Quaternion.AngleAxis(roll, Vector3.forward);
            targetRotation *= bank;
        }

        Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, EffectiveStats.TurnRate * Time.fixedDeltaTime * RollResponsiveness);
        rb.MoveRotation(newRotation);

        Vector3 wantedVelocity = transform.forward * EffectiveStats.Speed * Mathf.Clamp01(speedScale);
        Vector3 newVelocity = Vector3.MoveTowards(rb.linearVelocity, wantedVelocity, EffectiveStats.Acceleration * Time.fixedDeltaTime);
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

    private void UpdateWanderDirection()
    {
        wanderTimer -= Time.fixedDeltaTime;

        if (wanderTimer <= 0f || wanderDirection.sqrMagnitude < 0.01f)
        {
            PickNewWanderDirection();
        }
    }

    private void PickNewWanderDirection()
    {
        wanderTimer = Random.Range(WanderTargetRefreshTime * 0.65f, WanderTargetRefreshTime * 1.35f);
        wanderDirection = Random.insideUnitSphere;

        if (wanderDirection.sqrMagnitude < 0.01f)
        {
            wanderDirection = transform.forward;
        }

        wanderDirection.Normalize();
    }

    private Vector3 GetDirectionAndNormalisedDistance(Vector3? targetPosition, out float normalisedDistance)
    {
        normalisedDistance = 1f;

        if (!targetPosition.HasValue || Candidate == null || Candidate.Genome == null)
        {
            return Vector3.zero;
        }

        Vector3 origin = GetMouthWorldPosition();
        Vector3 direction = targetPosition.Value - origin;
        float distance = direction.magnitude;
        float vision = EffectiveStats != null ? EffectiveStats.VisionRange : Candidate.Genome.VisionRange;
        normalisedDistance = Mathf.Clamp01(distance / Mathf.Max(0.01f, vision));

        if (distance <= 0.0001f)
        {
            return Vector3.zero;
        }

        return direction / distance;
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

        if (DebugBoundaryAvoidance || ShouldDrawBoundaryDebug())
        {
            Debug.DrawRay(position, result, Color.yellow, Time.fixedDeltaTime);
        }

        return result;
    }

    private void AddBoundaryPush(float distanceToEdge, Vector3 inwardDirection, ref Vector3 push)
    {
        if (BoundaryAvoidanceDistance <= 0f || distanceToEdge > BoundaryAvoidanceDistance)
        {
            return;
        }

        float t = 1f - Mathf.Clamp01(distanceToEdge / BoundaryAvoidanceDistance);
        push += inwardDirection * t * t;
    }

    private void PreventOutwardDirectionAtBounds(ref Vector3 direction)
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        Vector3 centre = EvolutionEcosystemManager.Instance.transform.position;
        Vector3 half = EvolutionEcosystemManager.Instance.SimulationAreaSize * 0.5f;
        Vector3 position = rb != null ? rb.position : transform.position;
        Vector3 min = centre - half;
        Vector3 max = centre + half;
        float margin = Mathf.Max(0.01f, BoundaryHardStopMargin);

        if (position.x <= min.x + margin && direction.x < 0f) direction.x = 0f;
        if (position.x >= max.x - margin && direction.x > 0f) direction.x = 0f;
        if (position.y <= min.y + margin && direction.y < 0f) direction.y = 0f;
        if (position.y >= max.y - margin && direction.y > 0f) direction.y = 0f;
        if (position.z <= min.z + margin && direction.z < 0f) direction.z = 0f;
        if (position.z >= max.z - margin && direction.z > 0f) direction.z = 0f;
    }

    private Vector3 PreventOutwardVelocityAtBounds(Vector3 velocity)
    {
        PreventOutwardDirectionAtBounds(ref velocity);
        return velocity;
    }

    private Vector3 GetDirectionToSimulationCentre()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return transform.forward;
        }

        Vector3 direction = EvolutionEcosystemManager.Instance.transform.position - transform.position;

        if (direction.sqrMagnitude < 0.05f)
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    private void DrainEnergy()
    {
        float environmentDrain = 1f;

        if (EvolutionEcosystemManager.Instance != null && EvolutionEcosystemManager.Instance.Environment != null)
        {
            environmentDrain = EvolutionEcosystemManager.Instance.Environment.EnergyDrainMultiplier;
        }

        float movementCost = rb.linearVelocity.magnitude / Mathf.Max(0.1f, EffectiveStats.Speed);
        float drain = BaseEnergyDrainPerSecond * EffectiveStats.EnergyDrainMultiplier * environmentDrain;
        drain += movementCost * 0.28f;
        CurrentEnergy -= drain * Time.fixedDeltaTime;
    }

    private void TryEatFood()
    {
        if (nearestFood == null || nearestFood.IsConsumed)
        {
            return;
        }

        if (UseMouthBasedEating && !IsPositionInsideMouthArea(nearestFood.transform.position))
        {
            return;
        }

        float dietEfficiency = Mathf.Lerp(0.3f, 1.25f, Candidate.Genome.PlantDiet);
        float energyGained = nearestFood.Consume() * dietEfficiency;
        CurrentEnergy = Mathf.Min(CurrentEnergy + energyGained, EffectiveStats.EnergyCapacity);
        Candidate.EnergyGained += energyGained;
        Candidate.FoodEaten++;
        lowSpeedNearTargetTimer = 0f;
    }

    private void TryEatCarrion()
    {
        if (nearestCarrion == null || nearestCarrion.IsConsumed)
        {
            return;
        }

        if (UseMouthBasedEating && !IsPositionInsideMouthArea(nearestCarrion.transform.position))
        {
            return;
        }

        float dietEfficiency = Mathf.Lerp(0.25f, 1.25f, Candidate.Genome.CarrionDiet);
        float energyGained = nearestCarrion.Consume() * dietEfficiency;
        CurrentEnergy = Mathf.Min(CurrentEnergy + energyGained, EffectiveStats.EnergyCapacity);
        Candidate.EnergyGained += energyGained;
        Candidate.CarrionEaten++;
        lowSpeedNearTargetTimer = 0f;
    }

    private void TryBitePrey()
    {
        if (biteTimer > 0f || nearestPrey == null)
        {
            return;
        }

        if (!CanAttackPrey(nearestPrey))
        {
            return;
        }

        if (!IsPositionInsideMouthArea(nearestPrey.GetBiteTargetPosition()))
        {
            return;
        }

        float damage = GetBiteDamage();
        bool killed = nearestPrey.ReceiveBite(this, damage, out float energyGained);
        CurrentEnergy = Mathf.Min(CurrentEnergy + energyGained, EffectiveStats.EnergyCapacity);
        Candidate.EnergyGained += energyGained;
        Candidate.PreyBites++;
        Candidate.BiteDamageDealt += damage;

        if (killed)
        {
            Candidate.PreyKills++;
        }

        biteTimer = BiteCooldown;
    }

    private bool IsPositionInsideMouthArea(Vector3 targetPosition)
    {
        Vector3 mouthPosition = GetMouthWorldPosition();
        Vector3 toTarget = targetPosition - mouthPosition;
        float scaledMouthRadius = GetScaledMouthRadius();
        float distanceSqr = toTarget.sqrMagnitude;

        if (distanceSqr > scaledMouthRadius * scaledMouthRadius)
        {
            float forgivingDistance = scaledMouthRadius + CloseContactEatForgiveness * EffectiveStats.BodySize;
            if (Vector3.Distance(transform.position, targetPosition) > forgivingDistance)
            {
                return false;
            }
        }

        if (distanceSqr <= 0.0001f)
        {
            return true;
        }

        float dot = Vector3.Dot(transform.forward, toTarget.normalized);
        float requiredDot = Mathf.Cos((MouthAngle * 0.5f) * Mathf.Deg2Rad);
        return dot >= requiredDot || Vector3.Distance(transform.position, targetPosition) <= scaledMouthRadius * 0.65f;
    }

    public Vector3 GetMouthWorldPosition()
    {
        float bodyScale = EffectiveStats != null ? EffectiveStats.BodySize : transform.localScale.x;
        float offsetMultiplier = EffectiveStats != null ? EffectiveStats.MouthForwardOffsetMultiplier : 1f;
        return transform.position + transform.forward * MouthForwardOffset * bodyScale * offsetMultiplier;
    }

    public float GetScaledMouthRadius()
    {
        float bodyScale = EffectiveStats != null ? EffectiveStats.BodySize : transform.localScale.x;
        float mouthMultiplier = EffectiveStats != null ? EffectiveStats.MouthRadiusMultiplier : 1f;
        return Mathf.Max(0.05f, MouthRadius * bodyScale * mouthMultiplier);
    }

    public Vector3 GetBiteTargetPosition()
    {
        float bodyScale = EffectiveStats != null ? EffectiveStats.BodySize : transform.localScale.x;
        return transform.position + transform.up * bodyScale * 0.12f;
    }

    public float GetThreatRange()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return 0f;
        }

        return EffectiveStats.ThreatRange * EffectiveStats.VisionRange;
    }

    public float GetBiteDamage()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return BaseBiteDamage;
        }

        float damage = BaseBiteDamage + EffectiveStats.BiteDamage;
        damage += Candidate.Genome.Aggression * 4f;
        damage *= Mathf.Lerp(0.45f, 1.15f, Candidate.Genome.MeatDiet);
        return Mathf.Max(1f, damage);
    }

    public bool CanAttackPrey(MarineCreatureAgent prey)
    {
        if (prey == null || prey == this || Candidate == null || Candidate.Genome == null || prey.Candidate == null || prey.Candidate.Genome == null)
        {
            return false;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null || !manager.EnablePredation)
        {
            return false;
        }

        if (Candidate.Genome.MeatDiet < manager.MinimumMeatDietToHunt)
        {
            return false;
        }

        if (Candidate.Genome.Aggression < manager.MinimumAggressionToHunt && CurrentEnergy > EffectiveStats.EnergyCapacity * 0.35f)
        {
            return false;
        }

        float preySize = Mathf.Max(0.1f, prey.EffectiveStats != null ? prey.EffectiveStats.BodySize : prey.Candidate.Genome.BodySize);
        float ownSize = Mathf.Max(0.1f, EffectiveStats.BodySize);
        float allowedSizeRatio = manager.MaxPreySizeRatio + Candidate.Genome.RiskTolerance * 0.45f + Candidate.Genome.Aggression * 0.25f;

        if (preySize / ownSize > allowedSizeRatio)
        {
            return false;
        }

        float attackerConfidence = EffectiveStats.DangerFactor + Candidate.Genome.Aggression + Candidate.Genome.RiskTolerance + Candidate.Genome.MeatDiet;
        float preyDanger = prey.EffectiveStats != null ? prey.EffectiveStats.DangerFactor : prey.Candidate.Genome.DangerFactor;
        float preyEnergyRatio = Mathf.Clamp01(prey.CurrentEnergy / Mathf.Max(0.01f, prey.EffectiveStats != null ? prey.EffectiveStats.EnergyCapacity : prey.Candidate.Genome.EnergyCapacity));
        float scareStrength = preyDanger * manager.PredatorFearOfDangerFactor * Mathf.Lerp(0.65f, 1.15f, preyEnergyRatio);

        if (prey.Candidate.Genome.PlantDiet >= prey.Candidate.Genome.MeatDiet && scareStrength > attackerConfidence + 0.25f)
        {
            return false;
        }

        return true;
    }

    public bool ReceiveBite(MarineCreatureAgent attacker, float incomingDamage, out float energyGainedByAttacker)
    {
        energyGainedByAttacker = 0f;

        if (CurrentEnergy <= 0f || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        float defence = EffectiveStats != null ? EffectiveStats.Defence : Candidate.Genome.Armour;
        float armourReduction = Mathf.Clamp01(defence * ArmourDamageReductionPerPoint);
        float finalDamage = Mathf.Max(1f, incomingDamage * (1f - armourReduction));
        CurrentEnergy -= finalDamage;

        float meatEfficiency = attacker != null && attacker.Candidate != null && attacker.Candidate.Genome != null
            ? attacker.Candidate.Genome.MeatDiet
            : 0.5f;

        float gainMultiplier = EvolutionEcosystemManager.Instance != null
            ? EvolutionEcosystemManager.Instance.BiteEnergyGainMultiplier
            : 0.35f;

        energyGainedByAttacker = finalDamage * gainMultiplier * Mathf.Lerp(0.35f, 1.15f, meatEfficiency);

        if (CurrentEnergy <= 0f)
        {
            Die(false);
            return true;
        }

        return false;
    }

    private void TryReproduce()
    {
        if (reproductionTimer > 0f || CurrentEnergy < Candidate.Genome.ReproductionEnergyThreshold || EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        float mutationMultiplier = EvolutionEcosystemManager.Instance.Environment != null
            ? EvolutionEcosystemManager.Instance.Environment.MutationMultiplier
            : 1f;

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
            float foodDistance = Vector3.Distance(GetMouthWorldPosition(), nearestFood.transform.position);
            Candidate.AverageFoodDistance = Mathf.Lerp(Candidate.AverageFoodDistance, foodDistance, 0.05f);
        }

        if (nearestPrey != null)
        {
            float preyDistance = Vector3.Distance(GetMouthWorldPosition(), nearestPrey.GetBiteTargetPosition());
            Candidate.AveragePreyDistance = Mathf.Lerp(Candidate.AveragePreyDistance, preyDistance, 0.05f);
        }

        if (nearestCarrion != null)
        {
            float carrionDistance = Vector3.Distance(GetMouthWorldPosition(), nearestCarrion.transform.position);
            Candidate.AverageCarrionDistance = Mathf.Lerp(Candidate.AverageCarrionDistance, carrionDistance, 0.05f);
        }

        lastPosition = transform.position;
    }

    private void DrawRuntimeDebugRays()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        bool drawRays = LocalDebugRays || (settings != null && settings.DrawCreatureMovementRays);

        if (!drawRays)
        {
            return;
        }

        float duration = settings != null ? settings.FoodRayDuration : 0f;
        float wantedLength = settings != null ? settings.WantedDirectionRayLength : 4f;
        float velocityScale = settings != null ? settings.VelocityRayScale : 0.4f;
        Vector3 mouth = GetMouthWorldPosition();

        if (settings == null || settings.DrawWantedDirectionRays)
        {
            Debug.DrawRay(transform.position, wantedDirection * wantedLength, Color.blue, duration);
        }

        if (settings == null || settings.DrawVelocityRays)
        {
            Debug.DrawRay(transform.position, rb.linearVelocity * velocityScale, Color.white, duration);
        }

        if (nearestFood != null && (settings == null || settings.DrawFoodTargetRays))
        {
            Debug.DrawLine(mouth, nearestFood.transform.position, Color.green, duration);
            Debug.DrawRay(mouth, lastFoodDirection * 3f, Color.green, duration);
        }

        if (nearestCarrion != null && (settings == null || settings.DrawCarrionTargetRays))
        {
            Debug.DrawLine(mouth, nearestCarrion.transform.position, new Color(0.55f, 0.3f, 0.1f), duration);
            Debug.DrawRay(mouth, lastCarrionDirection * 3f, new Color(0.55f, 0.3f, 0.1f), duration);
        }

        if (nearestPrey != null && (settings == null || settings.DrawPreyTargetRays))
        {
            Debug.DrawLine(mouth, nearestPrey.GetBiteTargetPosition(), Color.red, duration);
            Debug.DrawRay(mouth, lastPreyDirection * 3f, Color.red, duration);
        }

        if (nearestCreature != null && (settings == null || settings.DrawSocialTargetRays))
        {
            Color socialColour = Candidate.Genome.ThreatRange > Candidate.Genome.GroupingChance ? Color.red : Color.magenta;
            Debug.DrawLine(transform.position, nearestCreature.transform.position, socialColour, duration);
            Debug.DrawRay(transform.position, lastCreatureDirection * 3f, socialColour, duration);
        }

        if ((settings == null || settings.DrawBoundaryPush) && lastBoundaryPush.sqrMagnitude > 0.001f)
        {
            Debug.DrawRay(transform.position, lastBoundaryPush, Color.yellow, duration);
        }
    }

    private bool ShouldDrawBoundaryDebug()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        return settings != null && settings.DrawBoundaryPush;
    }

    public string GetDebugSummary()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return "Uninitialised creature";
        }

        return DebugName +
               " | Energy " + CurrentEnergy.ToString("F0") + "/" + EffectiveStats.EnergyCapacity.ToString("F0") +
               " | Speed " + EffectiveStats.Speed.ToString("F1") +
               " | Vision " + EffectiveStats.VisionRange.ToString("F1") +
               " | Mouth " + GetScaledMouthRadius().ToString("F2") +
               " | Bite " + GetBiteDamage().ToString("F1") +
               " | Def " + EffectiveStats.Defence.ToString("F1") +
               " | Danger " + EffectiveStats.DangerFactor.ToString("F1") +
               " | " + EffectiveStats.MorphSummary +
               " | Diet P/M/C " + Candidate.Genome.PlantDiet.ToString("F2") + "/" + Candidate.Genome.MeatDiet.ToString("F2") + "/" + Candidate.Genome.CarrionDiet.ToString("F2");
    }

    public void Die(bool causedByExtinctionEvent)
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.SpawnCarrionFromDeath(this, causedByExtinctionEvent);
            EvolutionEcosystemManager.Instance.UnregisterCreature(this);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        DrawGizmosInternal(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmosInternal(true);
    }

    private void DrawGizmosInternal(bool selectedStyle)
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        bool drawVision = selectedStyle || (settings != null && settings.DrawVisionRange);
        bool drawMouth = selectedStyle || (settings != null && settings.DrawMouthRange);
        bool drawBite = selectedStyle || (settings != null && settings.DrawBiteRange);

        if (drawVision)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, selectedStyle ? 0.8f : 0.25f);
            Gizmos.DrawWireSphere(transform.position, EffectiveStats != null ? EffectiveStats.VisionRange : Candidate.Genome.VisionRange);

            float threatRange = GetThreatRange();
            if (threatRange > 0.1f)
            {
                Gizmos.color = new Color(1f, 0.1f, 0.1f, selectedStyle ? 0.7f : 0.2f);
                Gizmos.DrawWireSphere(transform.position, threatRange);
            }
        }

        if (drawMouth)
        {
            Gizmos.color = new Color(1f, 0.2f, 1f, selectedStyle ? 0.9f : 0.45f);
            Gizmos.DrawWireSphere(GetMouthWorldPosition(), GetScaledMouthRadius());
            Gizmos.DrawLine(transform.position, GetMouthWorldPosition());
        }

        if (drawBite)
        {
            Gizmos.color = new Color(1f, 0f, 0f, selectedStyle ? 0.9f : 0.35f);
            Gizmos.DrawWireSphere(GetMouthWorldPosition(), GetScaledMouthRadius() * 1.05f);
        }
    }

    private void OnGUI()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;

        if (!(LocalDebugLabels || (settings != null && settings.ShowCreatureLabels)))
        {
            return;
        }

        if (Candidate == null || Candidate.Genome == null || Camera.main == null)
        {
            return;
        }

        if (labelFrame != Time.frameCount)
        {
            labelFrame = Time.frameCount;
            labelsDrawnThisFrame = 0;
        }

        int maxLabels = settings != null ? settings.MaxLabelCount : 80;
        if (labelsDrawnThisFrame >= maxLabels)
        {
            return;
        }

        float maxDistance = settings != null ? settings.LabelMaxDistance : 80f;
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);

        if (distance > maxDistance)
        {
            return;
        }

        float bodySize = EffectiveStats != null ? EffectiveStats.BodySize : Candidate.Genome.BodySize;
        Vector3 worldPoint = transform.position + Vector3.up * bodySize * 1.4f;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);

        if (screenPoint.z <= 0f)
        {
            return;
        }

        Vector2 offset = settings != null ? settings.LabelOffset : new Vector2(0f, -14f);
        Rect rect = new Rect(screenPoint.x - 110f + offset.x, Screen.height - screenPoint.y + offset.y, 220f, 72f);
        Color oldColour = GUI.color;
        GUI.color = CreatureDebugTypeUtility.GetTypeColour(DebugBehaviourType);

        string label = DebugName + "\nE " + CurrentEnergy.ToString("F0") + " | " + CreatureDebugTypeUtility.GetMorphologyName(Candidate.Genome);
        if (settings != null && settings.ShowDietInLabels)
        {
            label += "\nP" + Candidate.Genome.PlantDiet.ToString("F1") + " M" + Candidate.Genome.MeatDiet.ToString("F1") + " C" + Candidate.Genome.CarrionDiet.ToString("F1");
        }

        GUI.Label(rect, label);
        GUI.color = oldColour;
        labelsDrawnThisFrame++;
    }
}
