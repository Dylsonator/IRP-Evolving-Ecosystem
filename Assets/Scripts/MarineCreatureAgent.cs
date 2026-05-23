using System.Collections.Generic;
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

    [Header("Energy / Survival")]
    public float BaseEnergyDrainPerSecond = 1.0f;
    public float ReproductionCooldown = 9f;
    public float LowEnergyRatio = 0.35f;

    [Header("Evolved Movement Model")]
    [Tooltip("Minimum swim speed as a ratio of evolved speed. Fish should rarely hard-stop.")]
    public float MinimumCruiseSpeedScale = 0.28f;
    public float NormalCruiseSpeedScale = 0.78f;
    public float HungryCruiseSpeedScale = 1.08f;
    public float SharpTurnSpeedLoss = 0.55f;
    public float SteeringAcceleration = 7.5f;
    public float RotationResponsiveness = 4.5f;
    public float MaxBankAngle = 34f;
    public float WanderStrength = 0.16f;
    public float WanderRefreshMin = 2.5f;
    public float WanderRefreshMax = 6.5f;
    public float BrainInfluence = 0.08f;

    [Header("Schooling From Genome")]
    public float SchoolPerceptionRadius = 10f;
    public float FriendlySeparationRadius = 1.55f;
    public int MaxSchoolNeighbours = 34;
    public float SeparationWeight = 1.35f;
    public float AlignmentWeight = 1.10f;
    public float CohesionWeight = 1.05f;
    public float DifferentMorphSpacingWeight = 0.45f;
    public float FoodPrioritySchoolReduction = 0.18f;
    public float MorphSimilarityForSchool = 0.72f;
    public float HorizontalSeparationBias = 0.12f;
    public float GroupDangerRadius = 8f;
    public float GroupDangerWeight = 0.18f;

    [Header("Feeding")]
    public float FoodEatDistance = 1.0f;
    public float CloseTargetSlowdownDistance = 2.8f;
    public float CloseTargetPull = 2.6f;
    public float FoodQueueRadius = 2.05f;
    public float FoodQueueStrength = 0.25f;
    public float MouthForwardOffset = 0.58f;
    public float MouthRadius = 0.38f;
    [Range(5f, 180f)] public float MouthAngle = 120f;
    public float CloseContactEatForgiveness = 0.45f;
    [Tooltip("When a fish is this close to an edible target, schooling/wander/depth steering are suppressed so the fish commits to feeding instead of stalling near the target.")]
    public float FeedingCommitDistance = 4.0f;
    [Tooltip("Extra body-centre forgiveness for eating static food/carrion. This prevents fish from starving while visually touching food but failing a strict mouth angle check.")]
    public float StaticFoodContactConsumeDistance = 1.45f;
    [Tooltip("Close feeding uses direct velocity towards the target. Rotation still follows movement, but the fish no longer has to complete a wide turn before making progress.")]
    public float CloseFeedingDirectMotionDistance = 4.5f;
    [Range(0f, 1f)] public float FeedingSocialSuppression = 0.08f;
    public float TargetRetainTime = 2f;

    [Header("Predation / Danger")]
    public float BiteCooldown = 1.9f;
    public float BaseBiteDamage = 6f;
    public float ArmourDamageReductionPerPoint = 0.13f;
    public float MissedAttackEnergyCost = 1.4f;
    [Range(0f, 1f)] public float SameMorphAttackAggressionRequired = 0.70f;
    [Range(0f, 1f)] public float SameMorphAttackMeatRequired = 0.72f;
    [Range(0f, 1f)] public float StarvingAttackEnergyRatio = 0.26f;
    [Range(0f, 1f)] public float StarvingAttackAggressionRequired = 0.38f;
    [Range(0f, 1f)] public float StarvingAttackMeatRequired = 0.64f;

    [Header("Habitat / Depth")]
    public float DepthPreferenceStrength = 0.95f;
    public float DepthPadding = 3f;
    public float BoundaryAvoidanceDistance = 8f;
    public float BoundaryAvoidanceStrength = 5.5f;
    public float BoundaryHardStopMargin = 0.55f;
    public float BoundaryVelocityDamping = 0.65f;

    [Header("Anti-Stuck / Collision Avoidance")]
    public bool UseRootLogicCollider = true;
    public bool RootColliderIsTrigger = true;
    public float RootColliderRadius = 0.95f;
    public bool DisableVisualColliders = true;
    public float PersonalSpaceMultiplier = 1.25f;
    public float EmergencyUnstickRadiusMultiplier = 0.85f;
    public float EmergencyUnstickWeight = 2.75f;
    public float StuckSpeedThreshold = 0.12f;
    public float StuckCheckTime = 1.2f;
    public float StuckEscapeDuration = 1.8f;
    public float StuckEscapeWeight = 2.4f;
    public float StuckPositionDeltaThreshold = 0.12f;

    [Header("Evolution Feedback")]
    public float DietLearningRate = 0.028f;
    public float BehaviourDecayRate = 0.0015f;

    [Header("Visuals / Debug")]
    public bool ApplyTypeColour = true;
    public bool LocalDebugRays;
    public bool LocalDebugLabels;
    public bool ScaleCreatureRootByEffectiveBodySize = true;
    public bool DisableLegacyPhenotypeVisuals = true;

    private Rigidbody rb;
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock materialBlock;

    private FoodSource nearestFood;
    private CarrionSource nearestCarrion;
    private MarineCreatureAgent nearestCreature;
    private MarineCreatureAgent nearestPrey;

    private FoodSource retainedFood;
    private CarrionSource retainedCarrion;
    private MarineCreatureAgent retainedPrey;
    private float retainedTargetTimer;

    private float reproductionTimer;
    private float biteTimer;
    private float aliveTimer;
    private float senseTimer;
    private float senseInterval;
    private float lowProgressTimer;
    private float stuckEscapeTimer;

    private Vector3 lastPosition;
    private Vector3 wanderDirection;
    private float wanderTimer;
    private Vector3 stuckEscapeDirection;
    private Vector3 currentVelocity;

    private Vector3 wantedDirection;
    private Vector3 lastFoodDirection;
    private Vector3 lastCarrionDirection;
    private Vector3 lastCreatureDirection;
    private Vector3 lastPreyDirection;
    private Vector3 lastBoundaryPush;
    private Vector3 lastSchoolCohesion;
    private Vector3 lastSchoolAlignment;
    private Vector3 lastSchoolSeparation;
    private Vector3 lastThreatAvoidance;
    private Vector3 lastEmergencyUnstick;
    private int lastFriendlyCount;
    private int lastThreatCount;
    private int lastDifferentCount;
    private string debugMoveState = "Initialising";
    private string debugVerticalReason = "Initialising";

    public void Initialise(EvolutionCandidate candidate)
    {
        Candidate = candidate ?? new EvolutionCandidate(EvolutionGenome.CreateBaseline());
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

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 1.85f;
        rb.angularDamping = 5.0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        transform.localScale = ScaleCreatureRootByEffectiveBodySize ? Vector3.one * EffectiveStats.BodySize : Vector3.one;
        CurrentEnergy = EffectiveStats.EnergyCapacity * 0.92f;

        aliveTimer = 0f;
        lowProgressTimer = 0f;
        retainedTargetTimer = 0f;
        reproductionTimer = Random.Range(1f, ReproductionCooldown);
        biteTimer = Random.Range(0f, BiteCooldown);
        senseInterval = Random.Range(0.08f, 0.18f);
        senseTimer = Random.Range(0f, senseInterval);
        lastPosition = transform.position;
        currentVelocity = Vector3.zero;
        PickNewWanderDirection();

        ApplyMorphVisuals();

        if (DisableVisualColliders)
        {
            DisableBlockingCollidersOnVisuals();
        }

        if (UseRootLogicCollider)
        {
            EnsureRootLogicCollider();
        }

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
        senseTimer -= Time.fixedDeltaTime;
        retainedTargetTimer -= Time.fixedDeltaTime;

        if (senseTimer <= 0f)
        {
            senseTimer = senseInterval;
            SenseEnvironment();
        }

        RunEvolvedMovement();
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

    private void ApplyMorphVisuals()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (DisableLegacyPhenotypeVisuals)
        {
            RemoveLegacyPhenotypeVisuals();
        }

        CreatureMorphBuilder builder = GetComponent<CreatureMorphBuilder>();
        if (builder == null)
        {
            builder = gameObject.AddComponent<CreatureMorphBuilder>();
        }

        builder.MorphLibrary = MorphLibrary;
        builder.UseTypeColour = ApplyTypeColour;
        builder.Build(Candidate.Genome, EffectiveStats, CreatureDebugTypeUtility.GetTypeColour(Candidate.BehaviourType));
    }

    private void CacheRenderers(bool forceRefresh = false)
    {
        if (forceRefresh || cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (materialBlock == null)
        {
            materialBlock = new MaterialPropertyBlock();
        }
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

    private void DisableBlockingCollidersOnVisuals()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || colliders[i].gameObject == gameObject)
            {
                continue;
            }

            colliders[i].enabled = false;
        }
    }

    private void EnsureRootLogicCollider()
    {
        SphereCollider rootCollider = GetComponent<SphereCollider>();
        if (rootCollider == null)
        {
            rootCollider = gameObject.AddComponent<SphereCollider>();
        }

        float size = EffectiveStats != null ? Mathf.Max(0.1f, EffectiveStats.BodySize) : 1f;
        rootCollider.enabled = true;
        rootCollider.isTrigger = RootColliderIsTrigger;
        rootCollider.center = Vector3.zero;
        rootCollider.radius = Mathf.Max(0.1f, RootColliderRadius * size);

        if (rb != null)
        {
            rb.detectCollisions = true;
        }
    }

    private void RemoveLegacyPhenotypeVisuals()
    {
        CreaturePhenotypeVisuals legacyVisuals = GetComponent<CreaturePhenotypeVisuals>();
        if (legacyVisuals != null)
        {
            legacyVisuals.AutoCreateParts = false;
            legacyVisuals.enabled = false;
        }

        string[] legacyNames =
        {
            "Phenotype_Body",
            "Phenotype_Tail",
            "Phenotype_LeftFin",
            "Phenotype_RightFin",
            "Phenotype_Mouth",
            "Phenotype_LeftSensor",
            "Phenotype_RightSensor"
        };

        for (int i = 0; i < legacyNames.Length; i++)
        {
            Transform child = transform.Find(legacyNames[i]);
            if (child == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void SenseEnvironment()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        float senseRange = EffectiveStats != null ? EffectiveStats.VisionRange : Candidate.Genome.VisionRange;
        Vector3 mouth = GetMouthWorldPosition();
        nearestFood = manager.GetNearestFood(mouth, senseRange);
        nearestCarrion = manager.GetNearestCarrion(mouth, senseRange);
        nearestCreature = manager.GetNearestCreature(this, transform.position, senseRange);
        nearestPrey = manager.GetNearestPrey(this, transform.position, senseRange);

        if (nearestFood != null && !nearestFood.IsConsumed)
        {
            retainedFood = nearestFood;
            retainedTargetTimer = TargetRetainTime;
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            retainedCarrion = nearestCarrion;
            retainedTargetTimer = TargetRetainTime;
        }

        if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            retainedPrey = nearestPrey;
            retainedTargetTimer = TargetRetainTime;
        }

        if (retainedTargetTimer <= 0f)
        {
            retainedFood = null;
            retainedCarrion = null;
            retainedPrey = null;
        }
    }

    private void RunEvolvedMovement()
    {
        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats.EnergyCapacity));
        float hungerPressure = 1f - energyRatio;

        Vector3 targetPull = GetFeedingTargetPull(hungerPressure);
        Vector3 schoolPull = GetEvolvedSchoolingPull(hungerPressure);
        Vector3 dangerPull = GetDangerAvoidancePull(hungerPressure);
        Vector3 depthPull = GetDepthPreferencePull(targetPull, dangerPull, hungerPressure);
        Vector3 boundaryPull = GetBoundaryAvoidanceDirection();
        Vector3 closeTargetPull = GetCloseTargetPull(hungerPressure);
        Vector3 queuePull = GetFoodQueuePull(hungerPressure);
        Vector3 brainPull = GetBrainPull(energyRatio);
        Vector3 wanderPull = GetWanderPull(energyRatio);
        Vector3 emergencyPull = GetEmergencyUnstickPull();

        bool feedingCommit = ShouldCommitToStaticFeedingTarget(hungerPressure);
        if (feedingCommit)
        {
            // Once food/carrion is close, the fish must commit to feeding.
            // This prevents the old loop where schooling/queue/depth forces cancelled the food pull
            // and the fish starved while sitting beside a resource.
            float suppression = Mathf.Clamp01(FeedingSocialSuppression);
            schoolPull *= suppression;
            depthPull = Vector3.zero;
            queuePull = Vector3.zero;
            brainPull *= 0.15f;
            wanderPull = Vector3.zero;
            closeTargetPull *= 1.65f;
        }

        lastBoundaryPush = boundaryPull;

        Vector3 combined = targetPull + schoolPull + dangerPull + depthPull + boundaryPull + closeTargetPull + queuePull + brainPull + wanderPull + emergencyPull;
        if (stuckEscapeTimer > 0f)
        {
            stuckEscapeTimer -= Time.fixedDeltaTime;
            combined += stuckEscapeDirection * StuckEscapeWeight;
        }

        if (combined.sqrMagnitude < 0.025f)
        {
            combined = transform.forward.sqrMagnitude > 0.01f ? transform.forward : Vector3.forward;
        }

        PreventOutwardDirectionAtBounds(ref combined);
        if (combined.sqrMagnitude < 0.025f)
        {
            combined = GetDirectionToSimulationCentre();
        }

        wantedDirection = combined.normalized;
        UpdateDebugMovementState(hungerPressure, targetPull, dangerPull, schoolPull, boundaryPull, emergencyPull);
        MoveFish(hungerPressure);
        UpdateStuckDetection(combined);
    }

    private Vector3 GetFeedingTargetPull(float hungerPressure)
    {
        Vector3 pull = Vector3.zero;
        Vector3? targetPosition = null;
        string targetKind = GetPreferredFoodKind();

        if (targetKind == "Meat" && nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            targetPosition = nearestPrey.GetBiteTargetPosition();
        }
        else if (targetKind == "Carrion" && nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            targetPosition = nearestCarrion.transform.position;
        }
        else if (targetKind == "Plant" && nearestFood != null && !nearestFood.IsConsumed)
        {
            targetPosition = nearestFood.transform.position;
        }

        if (!targetPosition.HasValue)
        {
            if (nearestFood != null && !nearestFood.IsConsumed && Candidate.Genome.PlantDiet >= 0.12f)
            {
                targetPosition = nearestFood.transform.position;
            }
            else if (nearestCarrion != null && !nearestCarrion.IsConsumed && Candidate.Genome.CarrionDiet >= 0.12f)
            {
                targetPosition = nearestCarrion.transform.position;
            }
            else if (nearestPrey != null && CanAttackPrey(nearestPrey))
            {
                targetPosition = nearestPrey.GetBiteTargetPosition();
            }
        }

        if (targetPosition.HasValue)
        {
            Vector3 toTarget = targetPosition.Value - GetMouthWorldPosition();
            float distance = toTarget.magnitude;
            if (distance > 0.001f)
            {
                float hunger = Mathf.Lerp(0.55f, 3.1f, Mathf.Clamp01(hungerPressure));
                pull += toTarget.normalized * hunger * Mathf.Lerp(0.75f, 1.3f, Candidate.Genome.HungerDrive);
            }
        }

        lastFoodDirection = DirectionTo(nearestFood != null && !nearestFood.IsConsumed ? nearestFood.transform.position : (Vector3?)null);
        lastCarrionDirection = DirectionTo(nearestCarrion != null && !nearestCarrion.IsConsumed ? nearestCarrion.transform.position : (Vector3?)null);
        lastPreyDirection = DirectionTo(nearestPrey != null && CanAttackPrey(nearestPrey) ? nearestPrey.GetBiteTargetPosition() : (Vector3?)null);
        return pull;
    }

    private string GetPreferredFoodKind()
    {
        float plant = Candidate.Genome.PlantDiet;
        float meat = Candidate.Genome.MeatDiet;
        float carrion = Candidate.Genome.CarrionDiet;

        if (meat >= plant && meat >= carrion)
        {
            return "Meat";
        }

        if (carrion >= plant && carrion >= meat)
        {
            return "Carrion";
        }

        return "Plant";
    }

    private Vector3 GetEvolvedSchoolingPull(float hungerPressure)
    {
        lastSchoolCohesion = Vector3.zero;
        lastSchoolAlignment = Vector3.zero;
        lastSchoolSeparation = Vector3.zero;
        lastEmergencyUnstick = Vector3.zero;
        lastFriendlyCount = 0;
        lastDifferentCount = 0;

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null || Candidate == null || Candidate.Genome == null)
        {
            return Vector3.zero;
        }

        List<MarineCreatureAgent> creatures = manager.GetActiveCreatures();
        if (creatures == null || creatures.Count <= 1)
        {
            return Vector3.zero;
        }

        Vector3 ownPosition = rb != null ? rb.position : transform.position;
        float ownRadius = GetPersonalRadius();
        float schoolDrive = Mathf.Clamp01(Candidate.Genome.GroupingChance * 0.65f + Candidate.Genome.SchoolTightness * 0.45f + Candidate.Genome.FoodSharing * 0.15f);
        float schoolReductionNearFood = HasCloseFoodTarget() ? Mathf.Lerp(1f, FoodPrioritySchoolReduction, hungerPressure) : 1f;
        float cohesionWeight = CohesionWeight * schoolDrive * schoolReductionNearFood;
        float alignmentWeight = AlignmentWeight * schoolDrive * schoolReductionNearFood;
        float separationWeight = SeparationWeight * Mathf.Lerp(1.25f, 0.65f, Candidate.Genome.SchoolTightness);

        Vector3 friendlyCentre = Vector3.zero;
        Vector3 friendlyDirection = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 differentSpacing = Vector3.zero;
        int friendlyCount = 0;
        int separationCount = 0;
        int differentCount = 0;
        int used = 0;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || other.Candidate == null || other.Candidate.Genome == null || other.EffectiveStats == null)
            {
                continue;
            }

            Vector3 otherPosition = other.transform.position;
            Vector3 awayVector = ownPosition - otherPosition;
            float distance = awayVector.magnitude;
            if (distance <= 0.0001f)
            {
                awayVector = GetFallbackSideDirection();
                distance = 0.0001f;
            }

            if (distance > SchoolPerceptionRadius)
            {
                continue;
            }

            bool friendly = IsFriendlyByMorph(other);
            bool realThreat = IsActualThreat(other);
            if (realThreat)
            {
                continue;
            }

            float otherRadius = other.GetPersonalRadius();
            float personalSpace = (ownRadius + otherRadius) * PersonalSpaceMultiplier;
            Vector3 away = awayVector / distance;
            Vector3 mostlySidewaysAway = away;
            mostlySidewaysAway.y *= HorizontalSeparationBias;
            if (mostlySidewaysAway.sqrMagnitude <= 0.0001f)
            {
                mostlySidewaysAway = GetHorizontalAwayFrom(otherPosition);
            }
            mostlySidewaysAway.Normalize();

            if (friendly)
            {
                float leaderWeight = Mathf.Lerp(0.65f, 1.45f, other.GetLeadershipWeight());
                friendlyCentre += otherPosition * leaderWeight;
                Vector3 otherVelocity = other.GetCurrentVelocityOrForward();
                friendlyDirection += otherVelocity.normalized * leaderWeight;
                friendlyCount++;

                if (distance < personalSpace)
                {
                    float t = 1f - Mathf.Clamp01(distance / personalSpace);
                    float emergency = distance < personalSpace * EmergencyUnstickRadiusMultiplier ? EmergencyUnstickWeight : 1f;
                    separation += mostlySidewaysAway * t * t * emergency;
                    separationCount++;
                }
            }
            else
            {
                float differentSpace = personalSpace * Mathf.Lerp(1.0f, 1.45f, Candidate.Genome.Territoriality);
                if (distance < differentSpace)
                {
                    float t = 1f - Mathf.Clamp01(distance / differentSpace);
                    differentSpacing += mostlySidewaysAway * t * t;
                    differentCount++;
                }
            }

            used++;
            if (used >= MaxSchoolNeighbours)
            {
                break;
            }
        }

        if (friendlyCount > 0)
        {
            friendlyCentre /= friendlyCount;
            Vector3 toCentre = friendlyCentre - ownPosition;
            if (toCentre.sqrMagnitude > 0.001f)
            {
                lastSchoolCohesion = toCentre.normalized * cohesionWeight;
            }

            friendlyDirection /= friendlyCount;
            if (friendlyDirection.sqrMagnitude > 0.001f)
            {
                lastSchoolAlignment = friendlyDirection.normalized * alignmentWeight;
            }
        }

        if (separationCount > 0)
        {
            lastSchoolSeparation = separation.normalized * separationWeight;
        }

        if (differentCount > 0)
        {
            lastSchoolSeparation += differentSpacing.normalized * DifferentMorphSpacingWeight * Mathf.Lerp(0.55f, 1.25f, Candidate.Genome.Territoriality + Candidate.Genome.Aggression * 0.35f);
        }

        lastFriendlyCount = friendlyCount;
        lastDifferentCount = differentCount;
        return lastSchoolCohesion + lastSchoolAlignment + lastSchoolSeparation;
    }

    private Vector3 GetDangerAvoidancePull(float hungerPressure)
    {
        lastThreatAvoidance = Vector3.zero;
        lastThreatCount = 0;

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return Vector3.zero;
        }

        List<MarineCreatureAgent> creatures = manager.GetActiveCreatures();
        if (creatures == null || creatures.Count <= 1)
        {
            return Vector3.zero;
        }

        Vector3 ownPosition = rb != null ? rb.position : transform.position;
        float fear = Mathf.Lerp(0.25f, 1.35f, 1f - Candidate.Genome.RiskTolerance);
        Vector3 avoid = Vector3.zero;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || !IsActualThreat(other))
            {
                continue;
            }

            Vector3 away = ownPosition - other.transform.position;
            float distance = away.magnitude;
            if (distance <= 0.001f)
            {
                away = GetFallbackSideDirection();
                distance = 0.001f;
            }

            float dangerRange = Mathf.Max(4f, GetThreatRange() + other.GetThreatRange() * 0.5f);
            if (distance > dangerRange)
            {
                continue;
            }

            float t = 1f - Mathf.Clamp01(distance / dangerRange);
            Vector3 direction = away.normalized;
            if (Mathf.Abs(direction.y) < 0.3f)
            {
                direction.y *= 0.35f;
            }
            avoid += direction.normalized * t * t * fear;
            lastThreatCount++;
        }

        if (lastThreatCount > 0)
        {
            lastThreatAvoidance = avoid.normalized * Mathf.Lerp(2.2f, 5.5f, fear);
        }

        return lastThreatAvoidance;
    }

    private Vector3 GetDepthPreferencePull(Vector3 targetPull, Vector3 dangerPull, float hungerPressure)
    {
        if (EvolutionEcosystemManager.Instance == null || Candidate == null || Candidate.Genome == null)
        {
            return Vector3.zero;
        }

        Vector3? activeTarget = GetPrimaryTargetPosition();
        if (activeTarget.HasValue && Mathf.Abs(activeTarget.Value.y - transform.position.y) > 0.75f)
        {
            return Vector3.zero;
        }

        Vector3 centre = EvolutionEcosystemManager.Instance.transform.position;
        Vector3 half = EvolutionEcosystemManager.Instance.SimulationAreaSize * 0.5f;
        float low = centre.y - half.y + DepthPadding;
        float high = centre.y + half.y - DepthPadding;
        float preferredY = Mathf.Lerp(low, high, Candidate.Genome.PreferredDepth01);
        float yError = preferredY - transform.position.y;
        float normalised = Mathf.Clamp(yError / Mathf.Max(0.1f, half.y), -1f, 1f);
        float flexibility = Candidate.Genome.DepthFlexibility;
        float strength = DepthPreferenceStrength * Mathf.Lerp(1.15f, 0.15f, flexibility);

        if (targetPull.sqrMagnitude > 0.01f || dangerPull.sqrMagnitude > 0.01f)
        {
            strength *= Mathf.Lerp(0.25f, 0.65f, flexibility);
        }

        strength *= Mathf.Lerp(0.85f, 0.45f, hungerPressure);
        Vector3 depth = Vector3.up * normalised * strength;
        return depth;
    }

    private Vector3 GetCloseTargetPull(float hungerPressure)
    {
        Vector3? target = GetPrimaryTargetPosition();
        if (!target.HasValue)
        {
            return Vector3.zero;
        }

        Vector3 mouth = GetMouthWorldPosition();
        Vector3 toTarget = target.Value - mouth;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f || distance > CloseTargetSlowdownDistance)
        {
            return Vector3.zero;
        }

        float closeness = 1f - Mathf.Clamp01(distance / CloseTargetSlowdownDistance);
        return toTarget.normalized * CloseTargetPull * closeness * Mathf.Lerp(0.65f, 1.35f, hungerPressure);
    }

    private Vector3 GetFoodQueuePull(float hungerPressure)
    {
        Vector3? target = GetPrimaryTargetPosition();
        if (!target.HasValue || Candidate == null || Candidate.Genome == null || Candidate.Genome.FoodSharing <= 0.05f)
        {
            return Vector3.zero;
        }

        if (ShouldCommitToStaticFeedingTarget(hungerPressure))
        {
            return Vector3.zero;
        }

        float distance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
        if (distance > CloseTargetSlowdownDistance * 1.4f)
        {
            return Vector3.zero;
        }

        float angle = (Candidate.Id * 137.508f) * Mathf.Deg2Rad;
        Vector3 orbit = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        float radius = FoodQueueRadius * Mathf.Lerp(0.7f, 1.45f, Candidate.Genome.FoodSharing);
        Vector3 queuePoint = target.Value + orbit * radius;
        Vector3 toQueue = queuePoint - GetMouthWorldPosition();
        if (toQueue.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        return toQueue.normalized * FoodQueueStrength * Candidate.Genome.FoodSharing * Mathf.Clamp01(hungerPressure + 0.25f);
    }

    private Vector3 GetBrainPull(float energyRatio)
    {
        if (Candidate == null || Candidate.Genome == null || Candidate.Genome.Brain == null || BrainInfluence <= 0f)
        {
            return Vector3.zero;
        }

        float[] inputs =
        {
            energyRatio,
            lastFoodDirection.x,
            lastFoodDirection.y,
            lastFoodDirection.z,
            lastCarrionDirection.x,
            lastCarrionDirection.y,
            lastCarrionDirection.z,
            lastPreyDirection.x,
            lastPreyDirection.y,
            lastPreyDirection.z,
            lastThreatAvoidance.sqrMagnitude > 0.01f ? 1f : 0f,
            Random.Range(-1f, 1f)
        };

        float[] outputs = Candidate.Genome.Brain.Evaluate(inputs);
        Vector3 brain = new Vector3(outputs.Length > 0 ? outputs[0] : 0f, outputs.Length > 1 ? outputs[1] : 0f, outputs.Length > 2 ? outputs[2] : 0f);
        if (brain.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        brain.y *= Mathf.Clamp(EffectiveStats.VerticalControl, 0.25f, 2.5f);
        return brain.normalized * BrainInfluence * Mathf.Lerp(0.35f, 1f, energyRatio);
    }

    private Vector3 GetWanderPull(float energyRatio)
    {
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f || wanderDirection.sqrMagnitude < 0.01f)
        {
            PickNewWanderDirection();
        }

        return wanderDirection * WanderStrength * Mathf.Lerp(0.25f, 1f, energyRatio) * Mathf.Lerp(0.75f, 1.2f, Candidate.Genome.ActivityCycle);
    }

    private Vector3 GetEmergencyUnstickPull()
    {
        return lastEmergencyUnstick;
    }

    private void MoveFish(float hungerPressure)
    {
        if (rb == null)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(wantedDirection, Vector3.up);
        Vector3 localWanted = transform.InverseTransformDirection(wantedDirection);
        float bank = Mathf.Clamp(-localWanted.x * MaxBankAngle, -MaxBankAngle, MaxBankAngle);
        targetRotation *= Quaternion.AngleAxis(bank, Vector3.forward);

        float turn = Mathf.Max(35f, EffectiveStats.TurnRate) * RotationResponsiveness;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, turn * Time.fixedDeltaTime));

        float alignment = Mathf.Clamp01((Vector3.Dot(transform.forward, wantedDirection) + 1f) * 0.5f);
        float sharpTurnScale = Mathf.Lerp(Mathf.Clamp01(1f - SharpTurnSpeedLoss), 1f, alignment);
        float targetScale = Mathf.Lerp(NormalCruiseSpeedScale, HungryCruiseSpeedScale, Mathf.Clamp01(hungerPressure));

        Vector3? target = GetPrimaryTargetPosition();
        if (target.HasValue)
        {
            float distance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
            float closeT = Mathf.Clamp01(distance / Mathf.Max(0.01f, CloseTargetSlowdownDistance));
            targetScale *= Mathf.Lerp(MinimumCruiseSpeedScale, 1f, closeT);
        }

        bool directFeedingMotion = IsCloseStaticFeedingTarget();
        targetScale = Mathf.Max(MinimumCruiseSpeedScale, targetScale * sharpTurnScale);

        Vector3 movementDirection = directFeedingMotion
            ? wantedDirection
            : Vector3.Slerp(transform.forward, wantedDirection, Mathf.Lerp(0.35f, 0.85f, Mathf.Clamp01(EffectiveStats.VerticalControl * 0.5f)));

        if (movementDirection.sqrMagnitude <= 0.001f)
        {
            movementDirection = transform.forward;
        }

        Vector3 desiredVelocity = movementDirection.normalized * EffectiveStats.Speed * targetScale;
        desiredVelocity = PreventOutwardVelocityAtBounds(desiredVelocity);

        float accel = Mathf.Max(1f, EffectiveStats.Acceleration) * SteeringAcceleration;
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, accel * Time.fixedDeltaTime);
        currentVelocity = PreventOutwardVelocityAtBounds(currentVelocity);
        rb.linearVelocity = currentVelocity;

        if (EvolutionEcosystemManager.Instance != null)
        {
            Vector3 clamped = EvolutionEcosystemManager.Instance.ClampToSimulationArea(rb.position);
            if ((clamped - rb.position).sqrMagnitude > 0.0001f)
            {
                rb.position = clamped;
                currentVelocity = PreventOutwardVelocityAtBounds(currentVelocity) * BoundaryVelocityDamping;
                rb.linearVelocity = currentVelocity;
            }
        }
    }

    private void UpdateStuckDetection(Vector3 combinedSteering)
    {
        float moved = Vector3.Distance(rb.position, lastPosition);
        bool shouldBeMoving = combinedSteering.sqrMagnitude > 0.2f;
        bool barelyMoved = moved < StuckPositionDeltaThreshold * Time.fixedDeltaTime * 4f || rb.linearVelocity.magnitude < StuckSpeedThreshold;

        if (shouldBeMoving && barelyMoved)
        {
            lowProgressTimer += Time.fixedDeltaTime;
        }
        else
        {
            lowProgressTimer = Mathf.Max(0f, lowProgressTimer - Time.fixedDeltaTime * 0.75f);
        }

        if (lowProgressTimer >= StuckCheckTime)
        {
            lowProgressTimer = 0f;
            stuckEscapeTimer = StuckEscapeDuration;
            stuckEscapeDirection = BuildStuckEscapeDirection();
            currentVelocity *= 0.35f;
        }

        lastPosition = rb.position;
    }

    private Vector3 BuildStuckEscapeDirection()
    {
        Vector3 dir = lastSchoolSeparation + lastBoundaryPush + GetFallbackSideDirection() * 0.6f;
        Vector3? target = GetPrimaryTargetPosition();
        if (target.HasValue)
        {
            Vector3 toTarget = (target.Value - transform.position);
            if (toTarget.sqrMagnitude > 0.001f)
            {
                dir += Vector3.Cross(toTarget.normalized, Vector3.up).normalized * 0.55f;
            }
        }

        if (dir.sqrMagnitude <= 0.001f)
        {
            dir = Random.insideUnitSphere;
        }

        dir.y *= 0.35f;
        if (dir.sqrMagnitude <= 0.001f)
        {
            dir = Vector3.right;
        }

        return dir.normalized;
    }

    private void PickNewWanderDirection()
    {
        wanderTimer = Random.Range(WanderRefreshMin, WanderRefreshMax);
        wanderDirection = Random.insideUnitSphere;
        wanderDirection.y *= Mathf.Lerp(0.25f, 1.0f, Candidate != null && Candidate.Genome != null ? Candidate.Genome.DepthFlexibility : 0.5f);
        if (wanderDirection.sqrMagnitude < 0.01f)
        {
            wanderDirection = transform.forward;
        }
        wanderDirection.Normalize();
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

        return push.normalized * BoundaryAvoidanceStrength;
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

    private Vector3 DirectionTo(Vector3? targetPosition)
    {
        if (!targetPosition.HasValue)
        {
            return Vector3.zero;
        }

        Vector3 direction = targetPosition.Value - GetMouthWorldPosition();
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    private Vector3 GetDirectionToSimulationCentre()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return transform.forward;
        }

        Vector3 direction = EvolutionEcosystemManager.Instance.transform.position - transform.position;
        return direction.sqrMagnitude < 0.05f ? transform.forward : direction.normalized;
    }

    private Vector3 GetHorizontalAwayFrom(Vector3 otherPosition)
    {
        Vector3 away = transform.position - otherPosition;
        away.y = 0f;
        if (away.sqrMagnitude <= 0.0001f)
        {
            away = transform.right;
            away.y = 0f;
        }
        if (away.sqrMagnitude <= 0.0001f)
        {
            away = Vector3.right;
        }
        return away.normalized;
    }

    private Vector3 GetFallbackSideDirection()
    {
        Vector3 side = transform.right;
        side.y = 0f;
        if (side.sqrMagnitude <= 0.0001f)
        {
            Vector2 random = Random.insideUnitCircle.normalized;
            if (random.sqrMagnitude <= 0.0001f)
            {
                random = Vector2.right;
            }
            side = new Vector3(random.x, 0f, random.y);
        }
        return side.normalized;
    }

    private Vector3 GetCurrentVelocityOrForward()
    {
        Vector3 velocity = rb != null ? rb.linearVelocity : currentVelocity;
        if (velocity.sqrMagnitude <= 0.001f)
        {
            velocity = transform.forward;
        }
        return velocity;
    }

    private bool HasCloseFoodTarget()
    {
        Vector3? target = GetPrimaryTargetPosition();
        if (!target.HasValue)
        {
            return false;
        }

        return Vector3.Distance(GetMouthWorldPosition(), target.Value) <= CloseTargetSlowdownDistance * 1.4f;
    }

    private bool ShouldCommitToStaticFeedingTarget(float hungerPressure)
    {
        if (hungerPressure < 0.08f)
        {
            return false;
        }

        Vector3? target = GetPrimaryStaticFoodTargetPosition();
        if (!target.HasValue)
        {
            return false;
        }

        float mouthDistance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
        float bodyDistance = Vector3.Distance(transform.position, target.Value);
        float commitDistance = Mathf.Max(FeedingCommitDistance, GetPersonalRadius() + GetScaledMouthRadius() + 1.1f);

        return mouthDistance <= commitDistance || bodyDistance <= commitDistance;
    }

    private bool IsCloseStaticFeedingTarget()
    {
        Vector3? target = GetPrimaryStaticFoodTargetPosition();
        if (!target.HasValue)
        {
            return false;
        }

        float mouthDistance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
        float bodyDistance = Vector3.Distance(transform.position, target.Value);
        float directDistance = Mathf.Max(CloseFeedingDirectMotionDistance, GetPersonalRadius() + GetScaledMouthRadius() + 1.2f);

        return mouthDistance <= directDistance || bodyDistance <= directDistance;
    }

    private Vector3? GetPrimaryStaticFoodTargetPosition()
    {
        string preferred = GetPreferredFoodKind();

        if (preferred == "Carrion" && nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            return nearestCarrion.transform.position;
        }

        if (preferred == "Plant" && nearestFood != null && !nearestFood.IsConsumed)
        {
            return nearestFood.transform.position;
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

    private Vector3? GetPrimaryTargetPosition()
    {
        if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            return nearestPrey.GetBiteTargetPosition();
        }

        string preferred = GetPreferredFoodKind();
        if (preferred == "Carrion" && nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            return nearestCarrion.transform.position;
        }

        if (preferred == "Plant" && nearestFood != null && !nearestFood.IsConsumed)
        {
            return nearestFood.transform.position;
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

    private bool IsFriendlyByMorph(MarineCreatureAgent other)
    {
        if (other == null || other.Candidate == null || other.Candidate.Genome == null || Candidate == null || Candidate.Genome == null)
        {
            return false;
        }

        if (CanAttackPrey(other) || other.CanAttackPrey(this))
        {
            return false;
        }

        float similarity = Candidate.Genome.GetMorphSimilarity(other.Candidate.Genome);
        if (similarity >= MorphSimilarityForSchool)
        {
            return true;
        }

        string ownMorph = CreatureDebugTypeUtility.GetMorphologyName(Candidate.Genome);
        string otherMorph = CreatureDebugTypeUtility.GetMorphologyName(other.Candidate.Genome);
        return ownMorph == otherMorph;
    }

    private bool IsActualThreat(MarineCreatureAgent other)
    {
        if (other == null || other == this)
        {
            return false;
        }

        return other.CanAttackPrey(this);
    }

    private float GetLeadershipWeight()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return 0.25f;
        }

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats != null ? EffectiveStats.EnergyCapacity : Candidate.Genome.EnergyCapacity));
        return Mathf.Clamp01(Candidate.Genome.Leadership * 0.55f + energyRatio * 0.25f + Candidate.Genome.RiskTolerance * 0.2f);
    }

    private float GetPersonalRadius()
    {
        float size = EffectiveStats != null ? EffectiveStats.BodySize : Candidate != null && Candidate.Genome != null ? Candidate.Genome.BodySize : 1f;
        return Mathf.Max(0.2f, RootColliderRadius * size);
    }

    private float GetGroupDangerSupport()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return 0f;
        }

        float support = 0f;
        List<MarineCreatureAgent> creatures = manager.GetActiveCreatures();
        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || !IsFriendlyByMorph(other))
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance > GroupDangerRadius)
            {
                continue;
            }

            float t = 1f - Mathf.Clamp01(distance / GroupDangerRadius);
            support += t;
        }

        return support * GroupDangerWeight;
    }

    private void DrainEnergy()
    {
        float environmentDrain = 1f;
        if (EvolutionEcosystemManager.Instance != null && EvolutionEcosystemManager.Instance.Environment != null)
        {
            environmentDrain = EvolutionEcosystemManager.Instance.Environment.EnergyDrainMultiplier;
        }

        float movementCost = rb != null ? rb.linearVelocity.magnitude / Mathf.Max(0.1f, EffectiveStats.Speed) : 0f;
        float drain = BaseEnergyDrainPerSecond * EffectiveStats.EnergyDrainMultiplier * environmentDrain;
        drain += movementCost * 0.18f;
        CurrentEnergy -= drain * Time.fixedDeltaTime;
    }

    private void TryEatFood()
    {
        if (nearestFood == null || nearestFood.IsConsumed)
        {
            return;
        }

        if (!CanConsumeStaticTarget(nearestFood.transform.position))
        {
            return;
        }

        float dietEfficiency = Mathf.Lerp(0.35f, 1.25f, Candidate.Genome.PlantDiet);
        float energyGained = nearestFood.Consume() * dietEfficiency;
        CurrentEnergy = Mathf.Min(CurrentEnergy + energyGained, EffectiveStats.EnergyCapacity);
        Candidate.EnergyGained += energyGained;
        Candidate.PlantEnergyConsumed += energyGained;
        Candidate.FoodEaten++;
        Candidate.Genome.ReinforceDietUsage(Candidate.PlantEnergyConsumed, Candidate.MeatEnergyConsumed, Candidate.CarrionEnergyConsumed, DietLearningRate);
        nearestFood = null;
    }

    private void TryEatCarrion()
    {
        if (nearestCarrion == null || nearestCarrion.IsConsumed)
        {
            return;
        }

        if (!CanConsumeStaticTarget(nearestCarrion.transform.position))
        {
            return;
        }

        float dietEfficiency = Mathf.Lerp(0.25f, 1.25f, Candidate.Genome.CarrionDiet);
        float energyGained = nearestCarrion.Consume() * dietEfficiency;
        CurrentEnergy = Mathf.Min(CurrentEnergy + energyGained, EffectiveStats.EnergyCapacity);
        Candidate.EnergyGained += energyGained;
        Candidate.CarrionEnergyConsumed += energyGained;
        Candidate.CarrionEaten++;
        Candidate.Genome.ReinforceDietUsage(Candidate.PlantEnergyConsumed, Candidate.MeatEnergyConsumed, Candidate.CarrionEnergyConsumed, DietLearningRate);
        nearestCarrion = null;
    }

    private void TryBitePrey()
    {
        if (biteTimer > 0f || nearestPrey == null || !CanAttackPrey(nearestPrey))
        {
            return;
        }

        if (!IsPositionInsideMouthArea(nearestPrey.GetBiteTargetPosition()))
        {
            if (Vector3.Distance(GetMouthWorldPosition(), nearestPrey.GetBiteTargetPosition()) <= GetScaledMouthRadius() * 1.45f)
            {
                CurrentEnergy -= MissedAttackEnergyCost;
                biteTimer = BiteCooldown * 0.5f;
            }
            return;
        }

        float damage = GetBiteDamage();
        bool killed = nearestPrey.ReceiveBite(this, damage, out float energyGained);
        CurrentEnergy = Mathf.Min(CurrentEnergy + energyGained, EffectiveStats.EnergyCapacity);
        Candidate.EnergyGained += energyGained;
        Candidate.MeatEnergyConsumed += energyGained;
        Candidate.Genome.ReinforceDietUsage(Candidate.PlantEnergyConsumed, Candidate.MeatEnergyConsumed, Candidate.CarrionEnergyConsumed, DietLearningRate);
        Candidate.PreyBites++;
        Candidate.BiteDamageDealt += damage;
        if (killed)
        {
            Candidate.PreyKills++;
        }
        biteTimer = BiteCooldown;
    }

    private bool CanConsumeStaticTarget(Vector3 targetPosition)
    {
        if (IsPositionInsideMouthArea(targetPosition))
        {
            return true;
        }

        // Static resources are not enemies and should not require perfect nose alignment.
        // If the body is visibly touching/covering the food, consume it instead of letting
        // the fish starve because its mouth socket is a little off.
        float bodyDistance = Vector3.Distance(transform.position, targetPosition);
        float bodyContact = Mathf.Max(StaticFoodContactConsumeDistance, GetPersonalRadius() + GetScaledMouthRadius() * 0.55f);
        if (bodyDistance <= bodyContact)
        {
            return true;
        }

        float mouthDistance = Vector3.Distance(GetMouthWorldPosition(), targetPosition);
        float mouthContact = Mathf.Max(GetScaledMouthRadius() * 1.65f, StaticFoodContactConsumeDistance * 0.75f);
        return mouthDistance <= mouthContact;
    }

    private bool IsPositionInsideMouthArea(Vector3 targetPosition)
    {
        Vector3 mouthPosition = GetMouthWorldPosition();
        Vector3 toTarget = targetPosition - mouthPosition;
        float radius = GetScaledMouthRadius();
        float distance = toTarget.magnitude;

        if (distance <= radius)
        {
            if (distance <= 0.0001f)
            {
                return true;
            }

            float dot = Vector3.Dot(transform.forward, toTarget.normalized);
            float requiredDot = Mathf.Cos((MouthAngle * 0.5f) * Mathf.Deg2Rad);
            return dot >= requiredDot;
        }

        float forgivingDistance = radius + CloseContactEatForgiveness * Mathf.Max(0.25f, EffectiveStats.BodySize);
        return Vector3.Distance(transform.position, targetPosition) <= forgivingDistance;
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
        if (prey == null || prey == this || Candidate == null || Candidate.Genome == null || prey.Candidate == null || prey.Candidate.Genome == null || EffectiveStats == null)
        {
            return false;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null || !manager.EnablePredation)
        {
            return false;
        }

        float morphSimilarity = Candidate.Genome.GetMorphSimilarity(prey.Candidate.Genome);
        if (morphSimilarity >= MorphSimilarityForSchool)
        {
            if (Candidate.Genome.Aggression < SameMorphAttackAggressionRequired || Candidate.Genome.MeatDiet < SameMorphAttackMeatRequired)
            {
                return false;
            }
        }

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats.EnergyCapacity));
        bool normalPredator = Candidate.Genome.MeatDiet >= manager.MinimumMeatDietToHunt && Candidate.Genome.Aggression >= manager.MinimumAggressionToHunt;
        bool starvingPredator = energyRatio <= StarvingAttackEnergyRatio && Candidate.Genome.MeatDiet >= StarvingAttackMeatRequired && Candidate.Genome.Aggression >= StarvingAttackAggressionRequired;
        if (!normalPredator && !starvingPredator)
        {
            return false;
        }

        float preySize = Mathf.Max(0.1f, prey.EffectiveStats != null ? prey.EffectiveStats.BodySize : prey.Candidate.Genome.BodySize);
        float ownSize = Mathf.Max(0.1f, EffectiveStats.BodySize);
        float allowedRatio = manager.MaxPreySizeRatio + Candidate.Genome.RiskTolerance * 0.45f + Candidate.Genome.Aggression * 0.25f;
        if (preySize / ownSize > allowedRatio)
        {
            return false;
        }

        float attackerConfidence = EffectiveStats.DangerFactor + Candidate.Genome.Aggression + Candidate.Genome.RiskTolerance + Candidate.Genome.MeatDiet;
        float preyDanger = prey.EffectiveStats != null ? prey.EffectiveStats.DangerFactor : prey.Candidate.Genome.DangerFactor;
        preyDanger += Mathf.Max(0f, preySize / ownSize - 1f) * 0.75f;
        preyDanger += prey.GetGroupDangerSupport();
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
        if (CurrentEnergy <= 0f)
        {
            return true;
        }

        float defence = EffectiveStats != null ? EffectiveStats.Defence : Candidate.Genome.Armour;
        float reduction = Mathf.Clamp01(defence * ArmourDamageReductionPerPoint);
        float damage = Mathf.Max(0.25f, incomingDamage * (1f - reduction));
        CurrentEnergy -= damage;

        if (CurrentEnergy <= 0f)
        {
            float size = EffectiveStats != null ? EffectiveStats.BodySize : Candidate.Genome.BodySize;
            float gainMultiplier = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance.BiteEnergyGainMultiplier : 0.32f;
            energyGainedByAttacker = Mathf.Max(5f, size * 22f) * gainMultiplier;
            Die(false);
            return true;
        }

        return false;
    }

    private void TryReproduce()
    {
        if (reproductionTimer > 0f || EvolutionEcosystemManager.Instance == null || Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (CurrentEnergy < Candidate.Genome.ReproductionEnergyThreshold)
        {
            return;
        }

        float mutationMultiplier = EvolutionEcosystemManager.Instance.Environment != null ? EvolutionEcosystemManager.Instance.Environment.MutationMultiplier : 1f;
        EvolutionGenome childGenome = Candidate.Genome.CreateMutatedCopy(mutationMultiplier);
        EvolutionCandidate child = new EvolutionCandidate(childGenome);
        child.ParentId = Candidate.Id;
        EvolutionEcosystemManager.Instance.RegisterOffspring(child);

        CurrentEnergy *= 0.68f;
        reproductionTimer = ReproductionCooldown;
        Candidate.ReproductionCount++;
    }

    private void UpdateMetrics()
    {
        if (Candidate == null)
        {
            return;
        }

        Candidate.SurvivalTime = aliveTimer;
        Candidate.FinalEnergy = CurrentEnergy;
        float currentSpeed = rb != null ? rb.linearVelocity.magnitude : 0f;
        Candidate.DistanceTravelled += currentSpeed * Time.fixedDeltaTime;
        Candidate.AverageSpeedUsed = Mathf.Lerp(Candidate.AverageSpeedUsed, currentSpeed, 0.02f);

        if (Candidate.Genome != null)
        {
            Candidate.Genome.DecayUnusedBehaviourTraits(BehaviourDecayRate * Time.fixedDeltaTime);
        }
    }

    private void UpdateDebugMovementState(float hungerPressure, Vector3 targetPull, Vector3 dangerPull, Vector3 schoolPull, Vector3 boundaryPull, Vector3 emergencyPull)
    {
        if (stuckEscapeTimer > 0f || emergencyPull.sqrMagnitude > 0.01f)
        {
            debugMoveState = "Unsticking";
        }
        else if (dangerPull.sqrMagnitude > 0.05f)
        {
            debugMoveState = "Avoiding predator";
        }
        else if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            debugMoveState = "Hunting";
        }
        else if (targetPull.sqrMagnitude > 0.05f && hungerPressure > 0.25f)
        {
            debugMoveState = "Feeding";
        }
        else if (schoolPull.sqrMagnitude > 0.05f && lastFriendlyCount > 0)
        {
            debugMoveState = "Schooling";
        }
        else if (boundaryPull.sqrMagnitude > 0.05f)
        {
            debugMoveState = "Bounds";
        }
        else
        {
            debugMoveState = "Cruising";
        }

        if (Mathf.Abs(wantedDirection.y) > 0.35f)
        {
            if (targetPull.sqrMagnitude > 0.05f && Mathf.Abs(targetPull.y) > 0.15f) debugVerticalReason = "Food depth";
            else if (dangerPull.sqrMagnitude > 0.05f && Mathf.Abs(dangerPull.y) > 0.15f) debugVerticalReason = "Threat depth";
            else if (boundaryPull.sqrMagnitude > 0.05f && Mathf.Abs(boundaryPull.y) > 0.15f) debugVerticalReason = "Bounds";
            else debugVerticalReason = "Preferred depth";
        }
        else
        {
            debugVerticalReason = "Cruise";
        }
    }

    private void DrawRuntimeDebugRays()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        bool drawRays = LocalDebugRays || (settings != null && settings.DrawCreatureMovementRays);
        if (!drawRays || rb == null)
        {
            return;
        }

        float duration = settings != null ? settings.FoodRayDuration : 0f;
        float wantedLength = settings != null ? settings.WantedDirectionRayLength : 4f;
        float velocityScale = settings != null ? settings.VelocityRayScale : 0.4f;
        Vector3 mouth = GetMouthWorldPosition();

        if (settings == null || settings.DrawWantedDirectionRays) Debug.DrawRay(transform.position, wantedDirection * wantedLength, Color.blue, duration);
        if (settings == null || settings.DrawVelocityRays) Debug.DrawRay(transform.position, rb.linearVelocity * velocityScale, Color.white, duration);
        if (nearestFood != null && (settings == null || settings.DrawFoodTargetRays)) Debug.DrawLine(mouth, nearestFood.transform.position, Color.green, duration);
        if (nearestCarrion != null && (settings == null || settings.DrawCarrionTargetRays)) Debug.DrawLine(mouth, nearestCarrion.transform.position, new Color(0.55f, 0.3f, 0.1f), duration);
        if (nearestPrey != null && (settings == null || settings.DrawPreyTargetRays)) Debug.DrawLine(mouth, nearestPrey.GetBiteTargetPosition(), Color.red, duration);
        if (nearestCreature != null && (settings == null || settings.DrawSocialTargetRays)) Debug.DrawLine(transform.position, nearestCreature.transform.position, IsActualThreat(nearestCreature) ? Color.red : new Color(0.4f, 0.7f, 1f), duration);
        if (settings == null || settings.DrawBoidRays)
        {
            if (lastSchoolCohesion.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastSchoolCohesion, new Color(0.1f, 0.9f, 1f), duration);
            if (lastSchoolAlignment.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastSchoolAlignment, new Color(0.55f, 0.75f, 1f), duration);
            if (lastSchoolSeparation.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastSchoolSeparation, Color.cyan, duration);
            if (lastThreatAvoidance.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastThreatAvoidance, new Color(1f, 0.35f, 0.1f), duration);
            if (lastEmergencyUnstick.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastEmergencyUnstick, Color.magenta, duration);
        }
        if ((settings == null || settings.DrawBoundaryPush) && lastBoundaryPush.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastBoundaryPush, Color.yellow, duration);
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
               " | State " + debugMoveState +
               " | School F/D/T " + lastFriendlyCount + "/" + lastDifferentCount + "/" + lastThreatCount +
               " | Vertical " + debugVerticalReason +
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

    public string GetDebugMoveState()
    {
        return debugMoveState;
    }

    public string GetDebugVerticalReason()
    {
        return debugVerticalReason;
    }

    public int GetFriendlySchoolmateCount()
    {
        return lastFriendlyCount;
    }

    public int GetThreatCount()
    {
        return lastThreatCount;
    }

    public float GetPreferredDepth01()
    {
        return Candidate != null && Candidate.Genome != null ? Candidate.Genome.PreferredDepth01 : 0.5f;
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
        Rect rect = new Rect(screenPoint.x - 125f + offset.x, Screen.height - screenPoint.y + offset.y, 250f, 96f);
        Color oldColour = GUI.color;
        GUI.color = CreatureDebugTypeUtility.GetTypeColour(DebugBehaviourType);

        string label = DebugName + "\nE " + CurrentEnergy.ToString("F0") + " | " + CreatureDebugTypeUtility.GetMorphologyName(Candidate.Genome);
        if (settings != null && settings.ShowDietInLabels)
        {
            label += "\nP" + Candidate.Genome.PlantDiet.ToString("F1") + " M" + Candidate.Genome.MeatDiet.ToString("F1") + " C" + Candidate.Genome.CarrionDiet.ToString("F1");
        }

        if (settings == null || settings.ShowMovementStateInLabels)
        {
            label += "\n" + debugMoveState + " F" + lastFriendlyCount + " T" + lastThreatCount;
        }

        if (settings != null && settings.ShowVerticalReasonInLabels)
        {
            label += " | Y: " + debugVerticalReason;
        }

        GUI.Label(rect, label);
        GUI.color = oldColour;
        labelsDrawnThisFrame++;
    }
}
