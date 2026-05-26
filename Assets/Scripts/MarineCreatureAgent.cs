using System.Collections.Generic;
using UnityEngine;

public enum FishAutonomousBehaviourMode
{
    Resting,
    Exploring,
    Schooling,
    Foraging,
    Feeding,
    FollowingLeader,
    SeekingMate,
    Nesting,
    Hunting,
    Fleeing,
    Recovering
}

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
    public float CurrentHealth;
    public float StomachPlant;
    public float StomachMeat;
    public float StomachCarrion;
    public float AgeSeconds;
    public bool IsJuvenile;
    public string DebugName;
    public CreatureBehaviourType DebugBehaviourType;

    [Header("Energy / Survival")]
    public float BaseEnergyDrainPerSecond = 1.0f;
    public float ReproductionCooldown = 9f;
    public float LowEnergyRatio = 0.35f;
    public float BaseHealth = 100f;
    public float StarvationHealthDamagePerSecond = 4.0f;
    public float HealthRecoveryPerSecond = 1.6f;
    public float BaseStomachCapacity = 46f;
    public float BaseDigestionPerSecond = 3.2f;
    public float BaseBiteMass = 7.5f;
    public float FullStomachSlowdown = 0.18f;

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
    public bool ShouldLeaveCurrentResource;

    [Header("Autonomous Behaviour Brain")]
    [Tooltip("How often the fish re-evaluates its high-level behaviour. Higher values make behaviour calmer and less twitchy.")]
    public float BrainDecisionInterval = 0.65f;
    [Tooltip("Minimum time before the fish willingly changes between major behaviours unless survival is at risk.")]
    public float MinimumBehaviourHoldTime = 1.35f;
    [Tooltip("How much health pressure pushes food seeking and survival behaviour.")]
    public float HealthNeedWeight = 0.85f;
    [Tooltip("How full the fish wants to be before mating becomes more important than feeding.")]
    [Range(0f, 1f)] public float MateSatietyBias = 0.68f;
    [Tooltip("How strongly bravery and exploration drive casual roaming when the fish is safe and fed.")]
    public float ExplorationBehaviourWeight = 1.05f;
    [Tooltip("Extra reluctance to switch away from feeding once a fish has chosen a nearby food item.")]
    public float FeedingDecisionStickiness = 0.55f;
    [Tooltip("How quickly the high-level brain blends its chosen pull into movement instead of snapping.")]
    public float BehaviourBlendSpeed = 2.8f;
    [Tooltip("Resting fish still cruise slowly, but mostly around their home area instead of searching for food.")]
    public float RestingWanderMultiplier = 0.55f;
    [Tooltip("Exploring fish wander further and rely more on memory and terrain than schooling.")]
    public float ExploringWanderMultiplier = 1.45f;

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
    [Tooltip("How much friendly separation is allowed to move vertically. Keep this low to stop schools bobbing when crowded.")]
    [Range(0f, 1f)] public float HorizontalSeparationBias = 0.04f;
    [Tooltip("Extra soft spacing radius used when many creatures form a clump.")]
    public float DenseCrowdRadius = 2.65f;
    public float DenseCrowdHorizontalWeight = 3.25f;
    [Range(0f, 1f)] public float DenseCrowdVerticalInfluence = 0.04f;
    [Tooltip("When a static food/carrion target has several fish around it, non-contact fish spread sideways instead of stacking vertically.")]
    public float FeedingCrowdRadius = 3.25f;
    public float FeedingCrowdSideStepWeight = 2.15f;
    public int FeedingCrowdSoftLimit = 3;
    [Tooltip("Upper clamp for any vertical contribution coming from purely social steering.")]
    public float MaxSocialVerticalComponent = 0.025f;
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
    [Range(0f, 1f)] public float FeedingSocialSuppression = 0.04f;
    [Tooltip("Hard override used only when a static food/carrion item is close. It prevents slot/social/depth steering from cancelling the final bite/eat approach.")]
    public float DirectStaticFeedingPullWeight = 7.5f;
    [Tooltip("If a static food/carrion item is this close to the creature body, it is consumed. Static resources are not enemies, so they should not require perfect mouth alignment.")]
    public float StaticBodyContactConsumeDistance = 2.15f;
    [Tooltip("If the mouth is this close to static food/carrion, consume even if the fish's angle is slightly wrong.")]
    public float StaticMouthContactConsumeDistance = 1.65f;
    public float TargetRetainTime = 2f;

    [Header("Feeding Pace / Satiety")]
    public float FeedingHoldAfterBite = 0.42f;
    public float FeedingHoldDamping = 9.5f;
    public float FeedingTurnResponsiveness = 1.35f;
    [Range(0f, 1f)] public float LeaveResourceEnergyRatio = 0.78f;
    [Range(0f, 1f)] public float LeaveResourceStomachRatio = 0.62f;
    public float LeaveResourceIgnoreTime = 4.5f;
    [Tooltip("Extra space used only while multiple fish are feeding. This stops them from sitting inside each other while nibbling.")]
    public float FeedingPersonalSpaceBoost = 1.35f;

    [Header("Autonomous Resource Distribution")]
    [Tooltip("Radius used to judge whether too many creatures are already feeding around the same resource.")]
    public float ResourceCrowdRadius = 5.25f;
    [Tooltip("Low-food-sharing creatures prefer less crowded resources.")]
    public int MinComfortableFeeders = 1;
    [Tooltip("High-food-sharing creatures tolerate more feeders around the same resource.")]
    public int MaxComfortableFeeders = 5;
    [Tooltip("Crowd penalty used by solitary/territorial creatures when picking food.")]
    public float ResourceCrowdPenaltyLowSharing = 32f;
    [Tooltip("Crowd penalty used by social/food-sharing creatures when picking food.")]
    public float ResourceCrowdPenaltyHighSharing = 7f;
    [Tooltip("When a resource is crowded, fish aim for their own approach slot first rather than every fish pushing into the centre.")]
    public float ResourceApproachSlotRadius = 1.85f;
    [Tooltip("If the fish makes no progress near a resource, it temporarily abandons that resource and searches elsewhere.")]
    public float ResourceAbandonDuration = 5.0f;
    public float StuckResourceProgressTime = 1.65f;
    public float StuckResourceDistanceEpsilon = 0.08f;
    [Tooltip("Extra sideways escape force used when a fish is stuck around a target or inside a dense school.")]
    public float HardUnstickSideStepWeight = 5.5f;
    [Tooltip("If crowding around a target is high, friendly schooling is almost fully ignored so fish can split toward different food.")]
    public float CrowdedFeedingSchoolSuppression = 0.02f;

    [Header("School Food Memory / Hunger Leadership")]
    public float HungryLeaderSearchRadius = 14f;
    public float HungryLeaderPullWeight = 1.35f;
    public float FoodMemoryDuration = 55f;
    public float FoodMemoryAreaRadius = 8f;
    public float BadFoodMemoryDuration = 18f;
    public float NotHungryFoodPullSuppression = 0.06f;
    public float DesperateHealthRatio = 0.35f;

    [Header("Habitat Memory")]
    public float HomeMemoryStrength = 0.75f;
    public float HomeAttractionDistance = 12f;
    public float HomeAttractionWeight = 0.9f;
    public float SafeHomeLearningRate = 0.035f;
    public float UnsafeHomeDecayRate = 0.08f;
    public float DangerMemoryDuration = 65f;
    public float DangerMemoryAvoidanceWeight = 1.15f;


    [Header("Mating / Eggs")]
    public float MaturityAgeSeconds = 90f;
    public float JuvenileGrowTime = 70f;
    public float JuvenileStartScale = 0.35f;
    public float MateSearchRadius = 18f;
    public float RequiredMateMorphSimilarity = 0.78f;
    public float MateEnergyRatioRequired = 0.68f;
    public float EggLayCooldown = 55f;
    public float EggLayEnergyCost = 24f;
    public int MinimumEggsPerClutch = 2;
    public int MaximumEggsPerClutch = 8;
    public float EggHatchTime = 55f;
    public float EggHealthPerBodySize = 12f;
    public float EggMassPerEgg = 7f;
    public float NestSearchRadius = 18f;
    public int NestSearchSamples = 12;

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

    [Header("Upright Swimming")]
    [Tooltip("Maximum pitch used for normal steering. Vertical targets can still be reached, but the fish avoids flipping straight up/down.")]
    public float MaxNormalPitchAngle = 58f;
    [Tooltip("Additional pitch allowance when actively feeding or hunting a target above/below.")]
    public float FeedingPitchAllowance = 18f;
    [Tooltip("Roll/bank is reduced in dense groups to stop fish rolling upside down while crowded.")]
    [Range(0f, 1f)] public float CrowdedBankReduction = 0.35f;
    [Tooltip("Hard reset angular velocity each tick so physics cannot accumulate unwanted spins.")]
    public bool SuppressPhysicsSpin = true;
    [Tooltip("Use kinematic steering instead of Rigidbody collision forces. This keeps motion autonomous but prevents physics piles from bouncing fish up/down.")]
    public bool UseKinematicSwimming = true;
    [Tooltip("How strongly the fish returns to an upright world-up posture. Bank still happens while turning, but the fish will not keep rolling over.")]
    public float UprightCorrectionStrength = 5.5f;

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

    [Header("Soft Body Spacing")]
    public int KinematicOverlapIterations = 2;
    public float KinematicOverlapResolveStrength = 0.65f;
    public float KinematicOverlapVerticalInfluence = 0.03f;
    public float FeedingOverlapResolveStrength = 1.05f;
    public float OverlapSearchRadius = 3.5f;

    [Header("Terrain Navigation")]
    public bool UseTerrainAvoidance = true;
    public float TerrainLookAhead = 4.5f;
    public float TerrainSideLookAhead = 3.25f;
    public float TerrainAvoidanceWeight = 5.0f;
    public float TerrainFloorClearance = 1.15f;
    public float TerrainFloorLiftWeight = 3.2f;
    public float TerrainWallSlideWeight = 2.4f;
    public float TerrainProbeForwardHeight = 0.35f;

    [Header("Mate Seeking")]
    public float MateSeekingWeight = 2.0f;
    public float MateSeekingEnergyRatio = 0.72f;
    public float MateSeekingStomachRatio = 0.35f;
    public float MatePairDistance = 3.2f;
    public float MateTargetRefreshTime = 2.5f;
    public float MateSearchWanderSuppression = 0.35f;
    public float MateSearchSchoolSuppression = 0.55f;

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

    [Header("Performance")]
    [Tooltip("Caches expensive all-creature social/danger scans. Lower values are more accurate but slower.")]
    public float SocialScanInterval = 0.18f;
    [Tooltip("Caches terrain ray checks. Lower values are more accurate but slower.")]
    public float TerrainScanInterval = 0.12f;

    private float socialScanTimer;
    private float dangerScanTimer;
    private float crowdScanTimer;
    private float terrainScanTimer;
    private Vector3 cachedSchoolPull;
    private Vector3 cachedDangerPull;
    private Vector3 cachedCrowdPull;
    private Vector3 cachedTerrainPull;
    private float[] brainInputs;
    private float[] brainOutputs;
    private float[] brainHiddenScratch;

    private FoodSource nearestFood;
    private CarrionSource nearestCarrion;
    private MarineCreatureAgent nearestCreature;
    private MarineCreatureAgent nearestPrey;

    private FoodSource retainedFood;
    private CarrionSource retainedCarrion;
    private MarineCreatureAgent retainedPrey;
    private MarineCreatureAgent currentMateTarget;
    private float mateTargetTimer;
    private Vector3 lastTerrainAvoidance;
    private float retainedTargetTimer;
    private FoodSource temporarilyIgnoredFood;
    private CarrionSource temporarilyIgnoredCarrion;
    private float ignoredResourceTimer;
    private Vector3 lastPrimaryStaticTargetPosition;
    private float lastPrimaryStaticTargetDistance = float.MaxValue;
    private float staticTargetNoProgressTimer;
    private bool hasFoodMemory;
    private Vector3 rememberedFoodArea;
    private float foodMemoryTimer;
    private bool rememberedFoodWasBad;
    private MarineCreatureAgent currentHungryLeader;
    private Vector3 lastLeaderPull;

    private FishAutonomousBehaviourMode currentBrainMode = FishAutonomousBehaviourMode.Resting;
    private FishAutonomousBehaviourMode previousBrainMode = FishAutonomousBehaviourMode.Resting;
    private float brainDecisionTimer;
    private float behaviourHoldTimer;
    private float brainFoodDesire;
    private float brainMateDesire;
    private float brainSchoolDesire;
    private float brainExploreDesire;
    private float brainHomeDesire;
    private float brainHuntDesire;
    private float brainFleeDesire;
    private float brainRestDesire;
    private float brainNestingDesire;
    private bool brainWantsFood;
    private bool brainWantsMate;
    private bool brainWantsHunt;
    private bool brainWantsFlee;
    private Vector3 behaviourBlendVector;
    private string brainReason = "Initialising";

    private float reproductionTimer;
    private float eggLayTimer;
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
    private int lastDenseCrowdCount;
    private int lastFeedingCrowdCount;
    private string debugMoveState = "Initialising";
    private string debugVerticalReason = "Initialising";

    private float feedingHoldTimer;
    private Vector3 currentStaticFeedingPoint;
    private bool hasStaticFeedingPoint;
    private Vector3 homeArea;
    private float homeConfidence;
    private bool hasHomeArea;
    private Vector3 rememberedDangerArea;
    private float dangerMemoryTimer;

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
        rb.isKinematic = UseKinematicSwimming;
        rb.linearDamping = 1.85f;
        rb.angularDamping = 5.0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        transform.localScale = ScaleCreatureRootByEffectiveBodySize ? Vector3.one * EffectiveStats.BodySize : Vector3.one;
        CurrentEnergy = EffectiveStats.EnergyCapacity * 0.82f;
        CurrentHealth = GetMaxHealth();
        AgeSeconds = 0f;
        IsJuvenile = false;
        StomachPlant = 0f;
        StomachMeat = 0f;
        StomachCarrion = 0f;
        hasFoodMemory = false;
        foodMemoryTimer = 0f;
        rememberedFoodWasBad = false;
        currentHungryLeader = null;
        feedingHoldTimer = 0f;
        hasStaticFeedingPoint = false;
        currentStaticFeedingPoint = transform.position;
        hasHomeArea = true;
        homeArea = transform.position;
        homeConfidence = 0.55f;
        rememberedDangerArea = transform.position;
        dangerMemoryTimer = 0f;

        aliveTimer = 0f;
        lowProgressTimer = 0f;
        retainedTargetTimer = 0f;
        ignoredResourceTimer = 0f;
        temporarilyIgnoredFood = null;
        temporarilyIgnoredCarrion = null;
        lastPrimaryStaticTargetDistance = float.MaxValue;
        staticTargetNoProgressTimer = 0f;
        reproductionTimer = Random.Range(1f, ReproductionCooldown);
        eggLayTimer = Random.Range(3f, EggLayCooldown);
        biteTimer = Random.Range(0f, BiteCooldown);
        senseInterval = Random.Range(0.14f, 0.28f);
        senseTimer = Random.Range(0f, senseInterval);
        socialScanTimer = Random.Range(0f, Mathf.Max(0.02f, SocialScanInterval));
        dangerScanTimer = Random.Range(0f, Mathf.Max(0.02f, SocialScanInterval));
        crowdScanTimer = Random.Range(0f, Mathf.Max(0.02f, SocialScanInterval));
        terrainScanTimer = Random.Range(0f, Mathf.Max(0.02f, TerrainScanInterval));
        lastPosition = transform.position;
        currentVelocity = Vector3.zero;
        currentBrainMode = FishAutonomousBehaviourMode.Resting;
        previousBrainMode = currentBrainMode;
        brainDecisionTimer = Random.Range(0f, Mathf.Max(0.05f, BrainDecisionInterval));
        behaviourHoldTimer = Random.Range(0.25f, Mathf.Max(0.3f, MinimumBehaviourHoldTime));
        behaviourBlendVector = transform.forward;
        brainReason = "Fresh spawn";
        brainWantsFood = false;
        brainWantsMate = false;
        brainWantsHunt = false;
        brainWantsFlee = false;
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
        AgeSeconds += Time.fixedDeltaTime;
        reproductionTimer -= Time.fixedDeltaTime;
        eggLayTimer -= Time.fixedDeltaTime;
        UpdateJuvenileGrowth();
        biteTimer -= Time.fixedDeltaTime;
        senseTimer -= Time.fixedDeltaTime;
        retainedTargetTimer -= Time.fixedDeltaTime;
        ignoredResourceTimer -= Time.fixedDeltaTime;
        feedingHoldTimer -= Time.fixedDeltaTime;
        socialScanTimer -= Time.fixedDeltaTime;
        dangerScanTimer -= Time.fixedDeltaTime;
        crowdScanTimer -= Time.fixedDeltaTime;
        terrainScanTimer -= Time.fixedDeltaTime;
        if (feedingHoldTimer < 0f)
        {
            feedingHoldTimer = 0f;
        }
        mateTargetTimer -= Time.fixedDeltaTime;
        if (dangerMemoryTimer > 0f)
        {
            dangerMemoryTimer -= Time.fixedDeltaTime;
        }
        if (ignoredResourceTimer <= 0f)
        {
            temporarilyIgnoredFood = null;
            temporarilyIgnoredCarrion = null;
            ignoredResourceTimer = 0f;
        }

        if (senseTimer <= 0f)
        {
            senseTimer = senseInterval;
            SenseEnvironment();
        }

        UpdateFoodMemoryTimers();
        DigestStomach();
        UpdateAutonomousBrain();
        RunEvolvedMovement();
        DrainEnergy();
        TryEatFood();
        TryEatCarrion();
        TryBitePrey();
        UpdateHabitatMemory();
        TryMateAndLayEggs();
        UpdateMetrics();
        DrawRuntimeDebugRays();

        if (CurrentHealth <= 0f)
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

        int comfortableFeeders = GetComfortableFeederLimit();
        float crowdPenalty = GetResourceCrowdPenalty();

        nearestFood = manager.GetBestFoodForCreature(this, mouth, senseRange, ResourceCrowdRadius, comfortableFeeders, crowdPenalty);
        nearestCarrion = manager.GetBestCarrionForCreature(this, mouth, senseRange, ResourceCrowdRadius, comfortableFeeders, crowdPenalty);

        if (ignoredResourceTimer > 0f && nearestFood == temporarilyIgnoredFood)
        {
            nearestFood = null;
        }

        if (ignoredResourceTimer > 0f && nearestCarrion == temporarilyIgnoredCarrion)
        {
            nearestCarrion = null;
        }

        nearestCreature = manager.GetNearestCreature(this, transform.position, senseRange);
        nearestPrey = manager.GetNearestPrey(this, transform.position, senseRange);

        if (nearestFood != null && !nearestFood.IsConsumed)
        {
            retainedFood = nearestFood;
            retainedTargetTimer = TargetRetainTime;
            RememberFoodArea(nearestFood.transform.position, false);
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            retainedCarrion = nearestCarrion;
            retainedTargetTimer = TargetRetainTime;
            RememberFoodArea(nearestCarrion.transform.position, false);
        }

        if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            retainedPrey = nearestPrey;
            retainedTargetTimer = TargetRetainTime;
        }

        UpdateMateTarget();

        if (retainedTargetTimer <= 0f)
        {
            retainedFood = null;
            retainedCarrion = null;
            retainedPrey = null;
        }
    }

    private void UpdateAutonomousBrain()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        brainDecisionTimer -= Time.fixedDeltaTime;
        behaviourHoldTimer -= Time.fixedDeltaTime;

        if (brainDecisionTimer > 0f && behaviourHoldTimer > 0f)
        {
            return;
        }

        brainDecisionTimer = Mathf.Max(0.05f, BrainDecisionInterval) * Random.Range(0.75f, 1.25f);

        float energyRatio = GetEffectiveEnergyRatio();
        float healthRatio = GetHealthRatio();
        float stomachRatio = GetStomachFullness01();
        float hungerPressure = GetHungerPressure();
        float lowHealthNeed = Mathf.Clamp01(1f - healthRatio);
        float stomachNeed = Mathf.Clamp01(1f - stomachRatio);
        float mature01 = IsMatureForMating() ? 1f : 0f;
        bool closeFood = HasCloseFoodTarget();
        bool hasStaticFood = GetPrimaryStaticFoodTargetPosition().HasValue;
        bool canHunt = nearestPrey != null && CanAttackPrey(nearestPrey);
        bool hasThreat = CountCurrentThreatsForBrain() > 0 || dangerMemoryTimer > 0f;

        brainFoodDesire = Mathf.Clamp01(
            hungerPressure * 0.62f
            + stomachNeed * Candidate.Genome.HungerDrive * 0.34f
            + lowHealthNeed * HealthNeedWeight * 0.38f
            + (hasFoodMemory && !rememberedFoodWasBad ? Candidate.Genome.FoodMemoryStrength * 0.14f : 0f));

        if (ShouldLeaveCurrentResource)
        {
            brainFoodDesire *= 0.25f;
        }

        if (hasStaticFood && closeFood && hungerPressure > 0.05f)
        {
            brainFoodDesire = Mathf.Clamp01(brainFoodDesire + FeedingDecisionStickiness);
        }

        brainHuntDesire = canHunt
            ? Mathf.Clamp01(Candidate.Genome.MeatDiet * 0.42f + Candidate.Genome.Aggression * 0.35f + hungerPressure * 0.35f + lowHealthNeed * 0.18f)
            : 0f;

        brainMateDesire = ShouldSeekMate()
            ? Mathf.Clamp01(Candidate.Genome.MateDrive * 0.45f + mature01 * 0.25f + energyRatio * 0.18f + stomachRatio * 0.18f - hungerPressure * 0.55f)
            : 0f;

        bool isFemale = Candidate.Genome.SexGene >= 0.5f;
        brainNestingDesire = isFemale && IsMatureForMating() && HasMatingEnergy()
            ? Mathf.Clamp01(Candidate.Genome.NestingDrive * 0.35f + Candidate.Genome.EggProtection * 0.25f + brainMateDesire * 0.35f - hungerPressure * 0.45f)
            : 0f;

        brainFleeDesire = hasThreat
            ? Mathf.Clamp01((1f - Candidate.Genome.RiskTolerance) * 0.45f + (1f - Candidate.Genome.Bravery) * 0.28f + lowHealthNeed * 0.35f)
            : 0f;

        brainSchoolDesire = Mathf.Clamp01(
            Candidate.Genome.GroupingChance * 0.35f
            + Candidate.Genome.SchoolTightness * 0.28f
            + Candidate.Genome.FoodSharing * 0.22f
            + (1f - Candidate.Genome.Selfishness) * 0.22f
            - brainFoodDesire * 0.28f
            - brainMateDesire * 0.18f);

        brainHomeDesire = Mathf.Clamp01((hasHomeArea ? homeConfidence : 0.15f) * 0.42f + (1f - Candidate.Genome.ExplorationDrive) * 0.35f + healthRatio * 0.15f - hungerPressure * 0.55f);
        brainExploreDesire = Mathf.Clamp01(Candidate.Genome.ExplorationDrive * ExplorationBehaviourWeight * (0.35f + Candidate.Genome.Bravery * 0.65f) - hungerPressure * 0.40f - brainMateDesire * 0.20f);
        brainRestDesire = Mathf.Clamp01(healthRatio * 0.24f + stomachRatio * 0.32f + energyRatio * 0.24f + brainHomeDesire * 0.20f - Candidate.Genome.ActivityCycle * 0.18f - hungerPressure * 0.55f);

        FishAutonomousBehaviourMode chosen = PickBrainMode();
        bool emergencySwitch = chosen == FishAutonomousBehaviourMode.Fleeing || chosen == FishAutonomousBehaviourMode.Feeding || chosen == FishAutonomousBehaviourMode.Hunting || currentBrainMode == FishAutonomousBehaviourMode.Recovering;
        if (behaviourHoldTimer > 0f && !emergencySwitch)
        {
            return;
        }

        SetBrainMode(chosen);
    }

    private FishAutonomousBehaviourMode PickBrainMode()
    {
        if (stuckEscapeTimer > 0f || lastEmergencyUnstick.sqrMagnitude > 0.35f)
        {
            brainReason = "recovering from crowding or terrain";
            return FishAutonomousBehaviourMode.Recovering;
        }

        if (brainFleeDesire > 0.52f && brainFleeDesire > brainFoodDesire * 0.85f)
        {
            brainReason = "threat or unsafe memory";
            return FishAutonomousBehaviourMode.Fleeing;
        }

        if (brainHuntDesire > 0.58f && brainHuntDesire >= brainFoodDesire * 0.85f)
        {
            brainReason = "meat-biased hunting";
            return FishAutonomousBehaviourMode.Hunting;
        }

        if (brainFoodDesire > 0.50f && HasCloseFoodTarget())
        {
            brainReason = "close edible target";
            return FishAutonomousBehaviourMode.Feeding;
        }

        if (brainFoodDesire > 0.44f)
        {
            brainReason = "hunger and stomach need";
            return FishAutonomousBehaviourMode.Foraging;
        }

        if (brainMateDesire > 0.46f && brainMateDesire >= brainExploreDesire && brainMateDesire >= brainSchoolDesire * 0.75f)
        {
            brainReason = "mature and full enough";
            return FishAutonomousBehaviourMode.SeekingMate;
        }

        if (brainNestingDesire > 0.55f && brainNestingDesire >= brainExploreDesire)
        {
            brainReason = "safe nesting drive";
            return FishAutonomousBehaviourMode.Nesting;
        }

        if (currentHungryLeader != null && brainSchoolDesire > 0.35f && brainFoodDesire < 0.32f)
        {
            brainReason = "following hungry schoolmate";
            return FishAutonomousBehaviourMode.FollowingLeader;
        }

        if (brainSchoolDesire > 0.48f && lastFriendlyCount > 0 && brainSchoolDesire >= brainExploreDesire)
        {
            brainReason = "same-morph schooling";
            return FishAutonomousBehaviourMode.Schooling;
        }

        if (brainExploreDesire > brainRestDesire && brainExploreDesire > 0.28f)
        {
            brainReason = "curious roaming";
            return FishAutonomousBehaviourMode.Exploring;
        }

        brainReason = hasHomeArea ? "safe home cruising" : "low need cruising";
        return FishAutonomousBehaviourMode.Resting;
    }

    private void SetBrainMode(FishAutonomousBehaviourMode nextMode)
    {
        if (nextMode != currentBrainMode)
        {
            previousBrainMode = currentBrainMode;
            currentBrainMode = nextMode;
            if (Candidate != null)
            {
                Candidate.BrainModeSwitches++;
            }
            float hold = MinimumBehaviourHoldTime;
            if (nextMode == FishAutonomousBehaviourMode.Feeding || nextMode == FishAutonomousBehaviourMode.Fleeing || nextMode == FishAutonomousBehaviourMode.Recovering)
            {
                hold *= 0.55f;
            }
            else if (nextMode == FishAutonomousBehaviourMode.Resting || nextMode == FishAutonomousBehaviourMode.Exploring)
            {
                hold *= 1.35f;
            }
            behaviourHoldTimer = Mathf.Max(0.15f, hold * Random.Range(0.75f, 1.25f));
        }

        brainWantsFood = currentBrainMode == FishAutonomousBehaviourMode.Foraging || currentBrainMode == FishAutonomousBehaviourMode.Feeding || currentBrainMode == FishAutonomousBehaviourMode.Hunting;
        brainWantsMate = currentBrainMode == FishAutonomousBehaviourMode.SeekingMate;
        brainWantsHunt = currentBrainMode == FishAutonomousBehaviourMode.Hunting;
        brainWantsFlee = currentBrainMode == FishAutonomousBehaviourMode.Fleeing;
    }

    private int CountCurrentThreatsForBrain()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return 0;
        }

        List<MarineCreatureAgent> creatures = manager.GetActiveCreatures();
        if (creatures == null || creatures.Count <= 1)
        {
            return 0;
        }

        int count = 0;
        Vector3 position = transform.position;
        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || !IsActualThreat(other))
            {
                continue;
            }

            float range = Mathf.Max(4f, GetThreatRange() + other.GetThreatRange() * 0.5f);
            if ((other.transform.position - position).sqrMagnitude <= range * range)
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyBrainModeToPulls(
        float hungerPressure,
        ref Vector3 targetPull,
        ref Vector3 rawStaticFeedingPull,
        ref Vector3 schoolPull,
        ref Vector3 dangerPull,
        ref Vector3 depthPull,
        ref Vector3 closeTargetPull,
        ref Vector3 queuePull,
        ref Vector3 crowdPull,
        ref Vector3 brainPull,
        ref Vector3 wanderPull,
        ref Vector3 homePull,
        ref Vector3 matePull,
        ref Vector3 terrainPull,
        ref Vector3 emergencyPull)
    {
        switch (currentBrainMode)
        {
            case FishAutonomousBehaviourMode.Fleeing:
                targetPull *= 0.15f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.35f;
                dangerPull *= 2.2f;
                closeTargetPull = Vector3.zero;
                queuePull = Vector3.zero;
                matePull = Vector3.zero;
                wanderPull *= 0.25f;
                homePull *= 0.25f;
                break;

            case FishAutonomousBehaviourMode.Hunting:
                targetPull *= Mathf.Lerp(1.15f, 1.75f, Candidate.Genome.Aggression);
                schoolPull *= Mathf.Lerp(0.25f, 0.75f, Candidate.Genome.GroupingChance);
                matePull = Vector3.zero;
                homePull *= 0.2f;
                wanderPull *= 0.15f;
                break;

            case FishAutonomousBehaviourMode.Feeding:
                targetPull *= 1.75f;
                rawStaticFeedingPull *= 1.35f;
                schoolPull *= FeedingSocialSuppression;
                depthPull *= 0.15f;
                queuePull = Vector3.zero;
                crowdPull *= 0.45f;
                brainPull *= 0.05f;
                wanderPull = Vector3.zero;
                homePull = Vector3.zero;
                matePull = Vector3.zero;
                break;

            case FishAutonomousBehaviourMode.Foraging:
                targetPull *= Mathf.Lerp(0.85f, 1.45f, brainFoodDesire);
                schoolPull *= Mathf.Lerp(0.35f, 0.95f, Candidate.Genome.FoodSharing);
                wanderPull *= 0.45f;
                homePull *= 0.2f;
                matePull = Vector3.zero;
                break;

            case FishAutonomousBehaviourMode.SeekingMate:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                matePull *= Mathf.Lerp(1.2f, 2.0f, Candidate.Genome.MateDrive);
                schoolPull *= Mathf.Clamp01(MateSearchSchoolSuppression * Mathf.Lerp(0.35f, 1f, Candidate.Genome.GroupingChance));
                wanderPull *= MateSearchWanderSuppression;
                homePull *= 0.35f;
                break;

            case FishAutonomousBehaviourMode.Nesting:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.55f;
                matePull *= 0.65f;
                homePull *= 1.35f;
                wanderPull *= 0.85f;
                break;

            case FishAutonomousBehaviourMode.FollowingLeader:
                targetPull *= 0.2f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 1.45f;
                matePull = Vector3.zero;
                wanderPull *= 0.25f;
                homePull *= 0.45f;
                break;

            case FishAutonomousBehaviourMode.Schooling:
                targetPull *= 0.2f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 1.35f;
                wanderPull *= 0.55f;
                homePull *= 0.65f;
                matePull *= 0.35f;
                break;

            case FishAutonomousBehaviourMode.Exploring:
                targetPull *= 0.18f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= Mathf.Lerp(0.45f, 0.95f, Candidate.Genome.GroupingChance);
                wanderPull *= ExploringWanderMultiplier;
                homePull *= 0.35f;
                matePull *= 0.25f;
                break;

            case FishAutonomousBehaviourMode.Recovering:
                targetPull *= 0.25f;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= 0.1f;
                crowdPull *= 1.65f;
                emergencyPull *= 1.85f;
                wanderPull *= 0.35f;
                matePull = Vector3.zero;
                break;

            default:
                targetPull = Vector3.zero;
                rawStaticFeedingPull = Vector3.zero;
                schoolPull *= Mathf.Lerp(0.35f, 0.9f, Candidate.Genome.GroupingChance);
                wanderPull *= RestingWanderMultiplier;
                homePull *= 1.25f;
                matePull *= 0.25f;
                break;
        }
    }

    private void RunEvolvedMovement()
    {
        float energyRatio = GetEffectiveEnergyRatio();
        float hungerPressure = GetHungerPressure();
        bool hungryEnough = brainWantsFood || currentBrainMode == FishAutonomousBehaviourMode.Feeding || currentBrainMode == FishAutonomousBehaviourMode.Foraging || currentBrainMode == FishAutonomousBehaviourMode.Hunting;

        Vector3 targetPull = hungryEnough ? GetFeedingTargetPull(hungerPressure) : Vector3.zero;
        Vector3 rawStaticFeedingPull = hungryEnough ? GetRawStaticFeedingPull(hungerPressure) : Vector3.zero;
        Vector3 schoolPull = GetCachedSchoolingPull(hungerPressure);
        Vector3 dangerPull = GetCachedDangerAvoidancePull(hungerPressure);
        Vector3 depthPull = GetDepthPreferencePull(targetPull, dangerPull, hungerPressure);
        Vector3 boundaryPull = GetBoundaryAvoidanceDirection();
        Vector3 closeTargetPull = GetCloseTargetPull(hungerPressure);
        Vector3 queuePull = GetFoodQueuePull(hungerPressure);
        Vector3 crowdPull = GetCachedCrowdStabilisationPull(hungerPressure);
        Vector3 brainPull = GetBrainPull(energyRatio);
        Vector3 wanderPull = GetWanderPull(energyRatio);
        Vector3 homePull = GetHomeAreaPull(hungerPressure);
        Vector3 matePull = GetMateSeekingPull(hungerPressure);
        Vector3 terrainPull = GetCachedTerrainAvoidancePull();
        Vector3 emergencyPull = GetEmergencyUnstickPull();

        ApplyBrainModeToPulls(hungerPressure,
            ref targetPull,
            ref rawStaticFeedingPull,
            ref schoolPull,
            ref dangerPull,
            ref depthPull,
            ref closeTargetPull,
            ref queuePull,
            ref crowdPull,
            ref brainPull,
            ref wanderPull,
            ref homePull,
            ref matePull,
            ref terrainPull,
            ref emergencyPull);

        bool feedingCommit = hungryEnough && ShouldCommitToStaticFeedingTarget(hungerPressure);
        if (feedingCommit)
        {
            float suppression = Mathf.Clamp01(FeedingSocialSuppression);
            schoolPull *= suppression;
            depthPull = Vector3.zero;
            queuePull = Vector3.zero;
            crowdPull *= 0.18f;
            brainPull *= 0.05f;
            wanderPull = Vector3.zero;
            matePull = Vector3.zero;
            closeTargetPull *= 0.35f;

            if (rawStaticFeedingPull.sqrMagnitude > 0.0001f)
            {
                targetPull = rawStaticFeedingPull * DirectStaticFeedingPullWeight;
            }
        }

        if (feedingHoldTimer > 0f)
        {
            schoolPull *= 0.05f;
            depthPull = Vector3.zero;
            queuePull = Vector3.zero;
            crowdPull *= 0.35f;
            brainPull = Vector3.zero;
            wanderPull = Vector3.zero;
            matePull = Vector3.zero;
            closeTargetPull = Vector3.zero;

            if (hasStaticFeedingPoint)
            {
                Vector3 toFood = currentStaticFeedingPoint - GetMouthWorldPosition();
                if (toFood.sqrMagnitude > 0.0001f)
                {
                    targetPull = toFood.normalized * DirectStaticFeedingPullWeight;
                }
            }
        }

        if (matePull.sqrMagnitude > 0.001f && !hungryEnough)
        {
            schoolPull *= Mathf.Clamp01(MateSearchSchoolSuppression);
            wanderPull *= Mathf.Clamp01(MateSearchWanderSuppression);
            homePull *= 0.45f;
        }

        lastBoundaryPush = boundaryPull;

        if (lastFeedingCrowdCount >= FeedingCrowdSoftLimit)
        {
            schoolPull *= CrowdedFeedingSchoolSuppression;
            queuePull *= 0.25f;
        }

        Vector3 combined = targetPull + schoolPull + dangerPull + depthPull + boundaryPull + closeTargetPull + queuePull + crowdPull + brainPull + wanderPull + homePull + matePull + terrainPull + emergencyPull;
        StabiliseVerticalSteering(ref combined, targetPull, dangerPull, boundaryPull, hungerPressure);
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

        behaviourBlendVector = Vector3.Slerp(behaviourBlendVector.sqrMagnitude > 0.001f ? behaviourBlendVector.normalized : transform.forward, combined.normalized, Mathf.Clamp01(BehaviourBlendSpeed * Time.fixedDeltaTime));
        wantedDirection = behaviourBlendVector.sqrMagnitude > 0.001f ? behaviourBlendVector.normalized : combined.normalized;
        UpdateDebugMovementState(hungerPressure, targetPull, dangerPull, schoolPull + crowdPull, boundaryPull, emergencyPull);
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
            targetPosition = GetMovementTargetForStaticResource(nearestCarrion.transform.position);
        }
        else if (targetKind == "Plant" && nearestFood != null && !nearestFood.IsConsumed)
        {
            targetPosition = GetMovementTargetForStaticResource(nearestFood.transform.position);
        }

        if (!targetPosition.HasValue)
        {
            if (nearestFood != null && !nearestFood.IsConsumed && Candidate.Genome.PlantDiet >= 0.12f)
            {
                targetPosition = GetMovementTargetForStaticResource(nearestFood.transform.position);
            }
            else if (nearestCarrion != null && !nearestCarrion.IsConsumed && Candidate.Genome.CarrionDiet >= 0.12f)
            {
                targetPosition = GetMovementTargetForStaticResource(nearestCarrion.transform.position);
            }
            else if (nearestPrey != null && CanAttackPrey(nearestPrey))
            {
                targetPosition = nearestPrey.GetBiteTargetPosition();
            }
        }

        if (!targetPosition.HasValue && hasFoodMemory && !rememberedFoodWasBad)
        {
            targetPosition = rememberedFoodArea;
            if (Candidate != null)
            {
                Candidate.FoodMemoryUses++;
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

    private Vector3 GetRawStaticFeedingPull(float hungerPressure)
    {
        Vector3? targetPosition = GetPrimaryStaticFoodTargetPosition();
        if (!targetPosition.HasValue)
        {
            return Vector3.zero;
        }

        Vector3 toTarget = targetPosition.Value - GetMouthWorldPosition();
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
        }

        float hunger = Mathf.Lerp(0.85f, 3.6f, Mathf.Clamp01(hungerPressure));
        return toTarget.normalized * hunger * Mathf.Lerp(0.85f, 1.35f, Candidate.Genome.HungerDrive);
    }

    private int GetComfortableFeederLimit()
    {
        float sharing = Candidate != null && Candidate.Genome != null ? Candidate.Genome.FoodSharing : 0.5f;
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(MinComfortableFeeders, MaxComfortableFeeders, sharing)), 1, Mathf.Max(1, MaxComfortableFeeders));
    }

    private float GetResourceCrowdPenalty()
    {
        float sharing = Candidate != null && Candidate.Genome != null ? Candidate.Genome.FoodSharing : 0.5f;
        float territorial = Candidate != null && Candidate.Genome != null ? Candidate.Genome.Territoriality : 0.0f;
        float basePenalty = Mathf.Lerp(ResourceCrowdPenaltyLowSharing, ResourceCrowdPenaltyHighSharing, sharing);
        return basePenalty * Mathf.Lerp(0.85f, 1.35f, territorial);
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

    private Vector3 GetCachedSchoolingPull(float hungerPressure)
    {
        if (socialScanTimer <= 0f)
        {
            socialScanTimer = Mathf.Max(0.02f, SocialScanInterval) * Random.Range(0.85f, 1.15f);
            cachedSchoolPull = GetEvolvedSchoolingPull(hungerPressure);
        }

        return cachedSchoolPull;
    }

    private Vector3 GetEvolvedSchoolingPull(float hungerPressure)
    {
        lastSchoolCohesion = Vector3.zero;
        lastSchoolAlignment = Vector3.zero;
        lastSchoolSeparation = Vector3.zero;
        lastEmergencyUnstick = Vector3.zero;
        lastFriendlyCount = 0;
        lastDifferentCount = 0;
        lastDenseCrowdCount = 0;
        lastFeedingCrowdCount = 0;

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

        lastLeaderPull = GetHungryLeaderPull(creatures, ownPosition, hungerPressure);

        Vector3 social = lastSchoolCohesion + lastSchoolAlignment + lastSchoolSeparation + lastLeaderPull;
        ClampSocialVertical(ref social);
        return social;
    }

    private Vector3 GetCachedCrowdStabilisationPull(float hungerPressure)
    {
        if (crowdScanTimer <= 0f)
        {
            crowdScanTimer = Mathf.Max(0.02f, SocialScanInterval) * Random.Range(0.85f, 1.15f);
            cachedCrowdPull = GetCrowdStabilisationPull(hungerPressure);
        }

        return cachedCrowdPull;
    }

    private Vector3 GetCrowdStabilisationPull(float hungerPressure)
    {
        lastEmergencyUnstick = Vector3.zero;

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
        Vector3 horizontalPush = Vector3.zero;
        Vector3? staticTarget = GetPrimaryStaticFoodTargetPosition();
        bool aroundStaticTarget = staticTarget.HasValue && Vector3.Distance(ownPosition, staticTarget.Value) <= FeedingCrowdRadius;
        int denseCount = 0;
        int feedingCount = 0;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || other.Candidate == null || other.Candidate.Genome == null)
            {
                continue;
            }

            Vector3 otherPosition = other.transform.position;
            Vector3 away = ownPosition - otherPosition;
            float distance = away.magnitude;
            if (distance <= 0.0001f)
            {
                away = GetFallbackSideDirection();
                distance = 0.0001f;
            }

            if (distance <= DenseCrowdRadius)
            {
                Vector3 horizontalAway = away;
                horizontalAway.y *= DenseCrowdVerticalInfluence;
                if (horizontalAway.sqrMagnitude <= 0.0001f)
                {
                    horizontalAway = GetHorizontalAwayFrom(otherPosition);
                }

                float t = 1f - Mathf.Clamp01(distance / DenseCrowdRadius);
                horizontalPush += horizontalAway.normalized * t * t;
                denseCount++;
            }

            if (aroundStaticTarget && staticTarget.HasValue && Vector3.Distance(otherPosition, staticTarget.Value) <= FeedingCrowdRadius)
            {
                feedingCount++;
            }
        }

        lastDenseCrowdCount = denseCount;
        lastFeedingCrowdCount = feedingCount;

        Vector3 pull = Vector3.zero;
        if (denseCount > 0 && horizontalPush.sqrMagnitude > 0.0001f)
        {
            float weight = DenseCrowdHorizontalWeight * Mathf.Lerp(1.15f, 0.45f, Candidate.Genome.SchoolTightness);
            pull += horizontalPush.normalized * weight;
            lastEmergencyUnstick = horizontalPush.normalized * weight;
        }

        if (aroundStaticTarget && staticTarget.HasValue && feedingCount >= FeedingCrowdSoftLimit)
        {
            float angle = ((Candidate != null ? Candidate.Id : GetInstanceID()) * 137.508f) * Mathf.Deg2Rad;
            Vector3 ring = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 targetSlot = staticTarget.Value + ring * Mathf.Lerp(0.85f, 1.65f, Candidate.Genome.FoodSharing);
            Vector3 toSlot = targetSlot - ownPosition;
            toSlot.y *= 0.04f;
            if (toSlot.sqrMagnitude > 0.0001f)
            {
                pull += toSlot.normalized * FeedingCrowdSideStepWeight * Mathf.Clamp01(0.35f + hungerPressure);
            }
        }

        ClampSocialVertical(ref pull);
        return pull;
    }


    private Vector3 GetHungryLeaderPull(List<MarineCreatureAgent> creatures, Vector3 ownPosition, float hungerPressure)
    {
        currentHungryLeader = null;

        if (IsHungryEnoughToSearch())
        {
            return Vector3.zero;
        }

        if (Candidate == null || Candidate.Genome == null || creatures == null)
        {
            return Vector3.zero;
        }

        float followDrive = Mathf.Clamp01(Candidate.Genome.GroupingChance * 0.35f + Candidate.Genome.FoodSharing * 0.45f + (1f - Candidate.Genome.Selfishness) * 0.35f);
        if (followDrive <= 0.12f)
        {
            return Vector3.zero;
        }

        MarineCreatureAgent bestLeader = null;
        float bestScore = 0f;
        float radiusSqr = HungryLeaderSearchRadius * HungryLeaderSearchRadius;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || other.Candidate == null || other.Candidate.Genome == null)
            {
                continue;
            }

            if (!IsFriendlyByMorph(other) || !other.IsHungryEnoughToSearch())
            {
                continue;
            }

            Vector3 toOther = other.transform.position - ownPosition;
            float distSqr = toOther.sqrMagnitude;
            if (distSqr > radiusSqr)
            {
                continue;
            }

            float distanceScore = 1f - Mathf.Clamp01(Mathf.Sqrt(distSqr) / Mathf.Max(0.1f, HungryLeaderSearchRadius));
            float score = other.GetHungerPressure() * 0.45f + other.Candidate.Genome.Leadership * 0.35f + other.Candidate.Genome.Bravery * 0.20f;
            score *= Mathf.Lerp(0.35f, 1f, distanceScore);

            if (score > bestScore)
            {
                bestScore = score;
                bestLeader = other;
            }
        }

        if (bestLeader == null)
        {
            return Vector3.zero;
        }

        currentHungryLeader = bestLeader;
        Vector3 pull = bestLeader.transform.position - ownPosition;
        pull.y *= 0.25f;
        if (pull.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        if (Candidate != null)
        {
            Candidate.LeaderFollowEvents++;
        }

        return pull.normalized * HungryLeaderPullWeight * followDrive * bestScore;
    }

    private void ClampSocialVertical(ref Vector3 social)
    {
        float maxY = Mathf.Max(0f, MaxSocialVerticalComponent);
        social.y = Mathf.Clamp(social.y * 0.15f, -maxY, maxY);
    }

    private void StabiliseVerticalSteering(ref Vector3 combined, Vector3 targetPull, Vector3 dangerPull, Vector3 boundaryPull, float hungerPressure)
    {
        bool targetNeedsVertical = targetPull.sqrMagnitude > 0.01f && Mathf.Abs(targetPull.normalized.y) > 0.22f;
        bool dangerNeedsVertical = dangerPull.sqrMagnitude > 0.01f && Mathf.Abs(dangerPull.normalized.y) > 0.35f;
        bool boundsNeedVertical = boundaryPull.sqrMagnitude > 0.01f && Mathf.Abs(boundaryPull.normalized.y) > 0.35f;

        if (lastDenseCrowdCount > 0 && !targetNeedsVertical && !dangerNeedsVertical && !boundsNeedVertical)
        {
            combined.y *= Mathf.Lerp(0.12f, 0.45f, Candidate != null && Candidate.Genome != null ? Candidate.Genome.DepthFlexibility : 0.5f);
        }

        if (lastFeedingCrowdCount >= FeedingCrowdSoftLimit && !targetNeedsVertical)
        {
            combined.y *= 0.18f;
        }
    }

    private Vector3 GetCachedDangerAvoidancePull(float hungerPressure)
    {
        if (dangerScanTimer <= 0f)
        {
            dangerScanTimer = Mathf.Max(0.02f, SocialScanInterval) * Random.Range(0.85f, 1.15f);
            cachedDangerPull = GetDangerAvoidancePull(hungerPressure);
        }

        return cachedDangerPull;
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

        lastThreatAvoidance += GetDangerMemoryPull();
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
        Vector3? target = GetPrimaryMovementTargetPosition();
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
        return Vector3.zero;
    }

    private Vector3 GetBrainPull(float energyRatio)
    {
        if (Candidate == null || Candidate.Genome == null || Candidate.Genome.Brain == null || BrainInfluence <= 0f)
        {
            return Vector3.zero;
        }

        SimpleNeuralNetwork network = Candidate.Genome.Brain;
        if (brainInputs == null || brainInputs.Length != network.InputCount)
        {
            brainInputs = new float[network.InputCount];
            brainOutputs = new float[network.OutputCount];
            brainHiddenScratch = new float[network.HiddenCount];
        }

        if (brainInputs.Length >= 12)
        {
            brainInputs[0] = energyRatio;
            brainInputs[1] = lastFoodDirection.x;
            brainInputs[2] = lastFoodDirection.y;
            brainInputs[3] = lastFoodDirection.z;
            brainInputs[4] = lastCarrionDirection.x;
            brainInputs[5] = lastCarrionDirection.y;
            brainInputs[6] = lastCarrionDirection.z;
            brainInputs[7] = lastPreyDirection.x;
            brainInputs[8] = lastPreyDirection.y;
            brainInputs[9] = lastPreyDirection.z;
            brainInputs[10] = lastThreatAvoidance.sqrMagnitude > 0.01f ? 1f : 0f;
            brainInputs[11] = Random.Range(-1f, 1f);
        }

        if (!network.EvaluateNonAlloc(brainInputs, brainOutputs, brainHiddenScratch))
        {
            return Vector3.zero;
        }

        Vector3 brain = new Vector3(brainOutputs.Length > 0 ? brainOutputs[0] : 0f, brainOutputs.Length > 1 ? brainOutputs[1] : 0f, brainOutputs.Length > 2 ? brainOutputs[2] : 0f);
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

    private Vector3 GetHomeAreaPull(float hungerPressure)
    {
        if (!hasHomeArea || Candidate == null || Candidate.Genome == null || hungerPressure > Candidate.Genome.HungerThreshold * 0.75f)
        {
            return Vector3.zero;
        }

        Vector3 toHome = homeArea - transform.position;
        float distance = toHome.magnitude;
        if (distance <= HomeAttractionDistance || distance <= 0.001f)
        {
            return Vector3.zero;
        }

        float confidence = Mathf.Clamp01(homeConfidence * HomeMemoryStrength);
        float exploration = Mathf.Clamp01(Candidate.Genome.ExplorationDrive);
        float pullStrength = HomeAttractionWeight * confidence * Mathf.Lerp(1.25f, 0.35f, exploration);

        toHome.y *= Mathf.Lerp(0.35f, 1f, Candidate.Genome.DepthFlexibility);
        if (toHome.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        return toHome.normalized * pullStrength;
    }

    private Vector3 GetDangerMemoryPull()
    {
        if (dangerMemoryTimer <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 away = transform.position - rememberedDangerArea;
        float distance = away.magnitude;
        if (distance <= 0.001f || distance > GroupDangerRadius * 2.5f)
        {
            return Vector3.zero;
        }

        away.y *= 0.25f;
        if (away.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        float t = 1f - Mathf.Clamp01(distance / Mathf.Max(0.1f, GroupDangerRadius * 2.5f));
        return away.normalized * DangerMemoryAvoidanceWeight * t * t;
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

        bool feedingOrHunting = GetPrimaryTargetPosition().HasValue;
        bool holdingBite = feedingHoldTimer > 0f && hasStaticFeedingPoint;
        Vector3 rotationSource = wantedDirection;
        if (holdingBite)
        {
            Vector3 lookAtFood = currentStaticFeedingPoint - GetMouthWorldPosition();
            if (lookAtFood.sqrMagnitude > 0.0001f)
            {
                rotationSource = lookAtFood.normalized;
            }
        }

        Vector3 rotationDirection = ClampPitchForUprightSwimming(rotationSource, feedingOrHunting || holdingBite);
        Quaternion targetRotation = BuildUprightFishRotation(rotationDirection);
        Vector3 localWanted = transform.InverseTransformDirection(rotationDirection);
        float crowdBankScale = lastDenseCrowdCount > 0 ? CrowdedBankReduction : 1f;
        float bank = Mathf.Clamp(-localWanted.x * MaxBankAngle * crowdBankScale, -MaxBankAngle, MaxBankAngle);
        targetRotation *= Quaternion.AngleAxis(bank, Vector3.forward);

        float turnResponsiveness = RotationResponsiveness * (holdingBite ? FeedingTurnResponsiveness : 1f);
        float turn = Mathf.Max(35f, EffectiveStats.TurnRate) * turnResponsiveness;
        Quaternion nextRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, turn * Time.fixedDeltaTime);
        rb.MoveRotation(nextRotation);
        if (SuppressPhysicsSpin && !rb.isKinematic)
        {
            rb.angularVelocity = Vector3.zero;
        }

        float alignment = Mathf.Clamp01((Vector3.Dot(transform.forward, wantedDirection) + 1f) * 0.5f);
        float sharpTurnScale = Mathf.Lerp(Mathf.Clamp01(1f - SharpTurnSpeedLoss), 1f, alignment);
        float targetScale = Mathf.Lerp(NormalCruiseSpeedScale, HungryCruiseSpeedScale, Mathf.Clamp01(hungerPressure));

        Vector3? target = GetPrimaryMovementTargetPosition();
        if (!target.HasValue && currentMateTarget != null && ShouldSeekMate())
        {
            target = currentMateTarget.transform.position;
        }
        if (target.HasValue)
        {
            float distance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
            float closeT = Mathf.Clamp01(distance / Mathf.Max(0.01f, CloseTargetSlowdownDistance));
            targetScale *= Mathf.Lerp(MinimumCruiseSpeedScale, 1f, closeT);
        }

        bool directFeedingMotion = IsCloseStaticFeedingTarget() && !holdingBite;
        float stomachSlow = Mathf.Lerp(1f, Mathf.Clamp01(1f - FullStomachSlowdown), GetStomachFullness01());
        targetScale = Mathf.Max(MinimumCruiseSpeedScale, targetScale * sharpTurnScale * stomachSlow);
        if (holdingBite)
        {
            targetScale = 0f;
        }

        Vector3 steerDirection = ClampPitchForUprightSwimming(wantedDirection, directFeedingMotion || feedingOrHunting);
        if (directFeedingMotion)
        {
            Vector3? rawStaticTarget = GetPrimaryStaticFoodTargetPosition();
            if (rawStaticTarget.HasValue)
            {
                Vector3 directToFood = rawStaticTarget.Value - GetMouthWorldPosition();
                if (directToFood.sqrMagnitude > 0.0001f)
                {
                    steerDirection = ClampPitchForUprightSwimming(directToFood.normalized, true);
                }
            }
        }

        Vector3 movementDirection = directFeedingMotion
            ? steerDirection
            : Vector3.Slerp(transform.forward, steerDirection, Mathf.Lerp(0.35f, 0.85f, Mathf.Clamp01(EffectiveStats.VerticalControl * 0.5f)));

        if (movementDirection.sqrMagnitude <= 0.001f)
        {
            movementDirection = transform.forward;
        }

        Vector3 desiredVelocity = movementDirection.normalized * EffectiveStats.Speed * targetScale;
        EvolutionEcosystemManager currentManager = EvolutionEcosystemManager.Instance;
        if (currentManager != null)
        {
            Vector3 currentFlow = currentManager.GetCurrentVelocityAt(rb.position);
            if (currentFlow.sqrMagnitude > 0.001f)
            {
                float against = Vector3.Dot(desiredVelocity.normalized, -currentFlow.normalized);
                if (against > 0f)
                {
                    desiredVelocity *= Mathf.Lerp(1f, 0.58f, Mathf.Clamp01(against));
                }
                desiredVelocity += currentFlow;
            }
        }
        desiredVelocity = PreventOutwardVelocityAtBounds(desiredVelocity);

        float accel = Mathf.Max(1f, EffectiveStats.Acceleration) * SteeringAcceleration;
        if (holdingBite)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, FeedingHoldDamping * Time.fixedDeltaTime);
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, accel * Time.fixedDeltaTime);
        }
        currentVelocity = PreventOutwardVelocityAtBounds(currentVelocity);
        if (UseKinematicSwimming)
        {
            rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = currentVelocity;
        }

        ResolveKinematicOverlaps(holdingBite);

        if (EvolutionEcosystemManager.Instance != null)
        {
            Vector3 clamped = EvolutionEcosystemManager.Instance.ClampToSimulationArea(rb.position);
            if ((clamped - rb.position).sqrMagnitude > 0.0001f)
            {
                rb.position = clamped;
                currentVelocity = PreventOutwardVelocityAtBounds(currentVelocity) * BoundaryVelocityDamping;
                if (!UseKinematicSwimming)
                {
                    rb.linearVelocity = currentVelocity;
                }
            }
        }
    }

    private void ResolveKinematicOverlaps(bool feedingHold)
    {
        if (EvolutionEcosystemManager.Instance == null || rb == null || KinematicOverlapIterations <= 0)
        {
            return;
        }

        List<MarineCreatureAgent> creatures = EvolutionEcosystemManager.Instance.GetActiveCreatures();
        if (creatures == null || creatures.Count <= 1)
        {
            return;
        }

        for (int iteration = 0; iteration < KinematicOverlapIterations; iteration++)
        {
            Vector3 correction = Vector3.zero;
            int overlaps = 0;
            Vector3 ownPosition = rb.position;
            float ownRadius = GetPersonalRadius() * (feedingHold ? FeedingPersonalSpaceBoost : 1f);
            float searchRadius = Mathf.Max(OverlapSearchRadius, ownRadius * 2.5f);

            for (int i = 0; i < creatures.Count; i++)
            {
                MarineCreatureAgent other = creatures[i];
                if (other == null || other == this)
                {
                    continue;
                }

                Vector3 otherPosition = other.transform.position;
                Vector3 away = ownPosition - otherPosition;
                float distance = away.magnitude;
                if (distance > searchRadius)
                {
                    continue;
                }

                float otherRadius = other.GetPersonalRadius();
                float required = (ownRadius + otherRadius) * PersonalSpaceMultiplier;
                if (distance >= required)
                {
                    continue;
                }

                if (distance <= 0.001f)
                {
                    away = GetFallbackSideDirection();
                    distance = 0.001f;
                }

                away.y *= KinematicOverlapVerticalInfluence;
                if (away.sqrMagnitude <= 0.001f)
                {
                    away = GetHorizontalAwayFrom(otherPosition);
                }

                float penetration = required - distance;
                correction += away.normalized * penetration;
                overlaps++;
            }

            if (overlaps <= 0 || correction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float strength = feedingHold ? FeedingOverlapResolveStrength : KinematicOverlapResolveStrength;
            Vector3 step = correction / overlaps * Mathf.Clamp01(strength);
            step.y *= KinematicOverlapVerticalInfluence;
            rb.position += step;
            currentVelocity += step / Mathf.Max(Time.fixedDeltaTime, 0.001f) * 0.15f;
        }
    }

    private Vector3 ClampPitchForUprightSwimming(Vector3 direction, bool activelyTargeting)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
        }

        Vector3 dir = direction.normalized;
        Vector3 horizontal = new Vector3(dir.x, 0f, dir.z);
        if (horizontal.sqrMagnitude <= 0.0001f)
        {
            horizontal = new Vector3(transform.forward.x, 0f, transform.forward.z);
            if (horizontal.sqrMagnitude <= 0.0001f)
            {
                horizontal = Vector3.forward;
            }
        }
        horizontal.Normalize();

        float maxPitch = Mathf.Clamp(MaxNormalPitchAngle + (activelyTargeting ? FeedingPitchAllowance : 0f), 5f, 88f);
        float maxY = Mathf.Sin(maxPitch * Mathf.Deg2Rad);
        float clampedY = Mathf.Clamp(dir.y, -maxY, maxY);
        float horizontalMagnitude = Mathf.Sqrt(Mathf.Max(0.0001f, 1f - clampedY * clampedY));
        return (horizontal * horizontalMagnitude + Vector3.up * clampedY).normalized;
    }

    private Quaternion BuildUprightFishRotation(Vector3 forward)
    {
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
        }

        forward.Normalize();
        Vector3 up = Vector3.up;

        if (Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.92f)
        {
            Vector3 safeRight = Vector3.Cross(Vector3.up, transform.forward);
            if (safeRight.sqrMagnitude <= 0.0001f)
            {
                safeRight = transform.right.sqrMagnitude > 0.0001f ? transform.right : Vector3.right;
            }
            up = Vector3.Cross(forward, safeRight.normalized).normalized;
        }

        return Quaternion.LookRotation(forward, up);
    }

    private void UpdateStuckDetection(Vector3 combinedSteering)
    {
        Vector3 position = rb != null ? rb.position : transform.position;
        float moved = Vector3.Distance(position, lastPosition);
        bool shouldBeMoving = combinedSteering.sqrMagnitude > 0.2f;
        float actualSpeed = currentVelocity.magnitude;
        bool barelyMoved = moved < StuckPositionDeltaThreshold * Time.fixedDeltaTime * 4f || actualSpeed < StuckSpeedThreshold;

        TrackStaticResourceProgress();

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
            currentVelocity = stuckEscapeDirection * Mathf.Max(EffectiveStats.Speed * 0.35f, currentVelocity.magnitude * 0.35f);
            MaybeAbandonStuckStaticResource();
        }

        lastPosition = position;
    }

    private void TrackStaticResourceProgress()
    {
        Vector3? target = GetPrimaryStaticFoodTargetPosition();
        if (!target.HasValue)
        {
            staticTargetNoProgressTimer = 0f;
            lastPrimaryStaticTargetDistance = float.MaxValue;
            return;
        }

        float distance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
        if ((target.Value - lastPrimaryStaticTargetPosition).sqrMagnitude > 0.25f)
        {
            staticTargetNoProgressTimer = 0f;
            lastPrimaryStaticTargetPosition = target.Value;
            lastPrimaryStaticTargetDistance = distance;
            return;
        }

        if (distance < lastPrimaryStaticTargetDistance - StuckResourceDistanceEpsilon)
        {
            staticTargetNoProgressTimer = 0f;
            lastPrimaryStaticTargetDistance = distance;
        }
        else
        {
            staticTargetNoProgressTimer += Time.fixedDeltaTime;
        }
    }

    private void MaybeAbandonStuckStaticResource()
    {
        if (staticTargetNoProgressTimer < StuckResourceProgressTime)
        {
            return;
        }

        if (nearestFood != null && !nearestFood.IsConsumed)
        {
            temporarilyIgnoredFood = nearestFood;
            nearestFood = null;
        }

        if (nearestCarrion != null && !nearestCarrion.IsConsumed)
        {
            temporarilyIgnoredCarrion = nearestCarrion;
            nearestCarrion = null;
        }

        ignoredResourceTimer = ResourceAbandonDuration;
        staticTargetNoProgressTimer = 0f;
        lastPrimaryStaticTargetDistance = float.MaxValue;
    }

    private Vector3 BuildStuckEscapeDirection()
    {
        Vector3 side = GetFallbackSideDirection();
        Vector3 dir = lastSchoolSeparation + lastBoundaryPush + side * 1.25f;

        Vector3? target = GetPrimaryStaticFoodTargetPosition();
        if (target.HasValue)
        {
            Vector3 toTarget = target.Value - transform.position;
            Vector3 horizontalToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
            if (horizontalToTarget.sqrMagnitude > 0.001f)
            {
                Vector3 perpendicular = Vector3.Cross(horizontalToTarget.normalized, Vector3.up).normalized;
                float sign = ((Candidate != null ? Candidate.Id : GetInstanceID()) % 2 == 0) ? 1f : -1f;
                dir += perpendicular * sign * HardUnstickSideStepWeight;

                if (horizontalToTarget.magnitude < Mathf.Max(1.0f, GetPersonalRadius() + GetScaledMouthRadius()))
                {
                    dir -= horizontalToTarget.normalized * 1.35f;
                }
            }
        }

        if (dir.sqrMagnitude <= 0.001f)
        {
            Vector2 random = Random.insideUnitCircle.normalized;
            if (random.sqrMagnitude <= 0.001f)
            {
                random = Vector2.right;
            }
            dir = new Vector3(random.x, 0f, random.y);
        }

        dir.y *= 0.04f;
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
        Vector3 velocity = UseKinematicSwimming ? currentVelocity : (rb != null ? rb.linearVelocity : currentVelocity);
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

    private Vector3? GetPrimaryMovementTargetPosition()
    {
        if (nearestPrey != null && CanAttackPrey(nearestPrey))
        {
            return nearestPrey.GetBiteTargetPosition();
        }

        Vector3? staticTarget = GetPrimaryStaticFoodTargetPosition();
        if (staticTarget.HasValue)
        {
            return GetMovementTargetForStaticResource(staticTarget.Value);
        }

        if (currentMateTarget != null && ShouldSeekMate())
        {
            return currentMateTarget.transform.position;
        }

        return null;
    }

    private Vector3 GetMovementTargetForStaticResource(Vector3 resourcePosition)
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return resourcePosition;
        }

        int crowd = manager.CountCreaturesNearPoint(resourcePosition, ResourceCrowdRadius, this);
        int comfort = GetComfortableFeederLimit();
        float distanceToResource = Vector3.Distance(transform.position, resourcePosition);

        // When close enough to actually feed, commit to the resource centre. Otherwise, crowded
        // resources get individual side slots so a school does not compress into one vertical pile.
        float contactDistance = Mathf.Max(StaticFoodContactConsumeDistance, GetPersonalRadius() + GetScaledMouthRadius() + 0.35f);
        if (crowd < Mathf.Max(2, comfort) || distanceToResource <= contactDistance)
        {
            return resourcePosition;
        }

        float radius = Mathf.Max(0.35f, ResourceApproachSlotRadius + GetPersonalRadius() * 0.45f);
        float id = Candidate != null ? Candidate.Id : GetInstanceID();
        float angle = id * 137.508f * Mathf.Deg2Rad;
        Vector3 ring = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        // Do not force the slot to the target's exact depth. The fish can approach laterally first,
        // then make a short vertical correction when it has space.
        Vector3 slot = resourcePosition + ring * radius;
        slot.y = Mathf.Lerp(transform.position.y, resourcePosition.y, 0.35f);
        return slot;
    }

    private bool WantsStaticResourceNow()
    {
        if (feedingHoldTimer > 0f)
        {
            return true;
        }

        if (Candidate == null || Candidate.Genome == null)
        {
            return true;
        }

        if (GetHealthRatio() <= DesperateHealthRatio)
        {
            return true;
        }

        if (IsSatisfiedEnoughToLeaveResource())
        {
            return false;
        }

        if (IsHungryEnoughToSearch())
        {
            return true;
        }

        float opportunism = Candidate.Genome.Selfishness * 0.45f + Candidate.Genome.HungerDrive * 0.25f + Candidate.Genome.ExplorationDrive * 0.15f;
        return GetStomachFullness01() < 0.18f && GetEffectiveEnergyRatio() < Mathf.Lerp(0.45f, 0.62f, opportunism);
    }

    private bool IsSatisfiedEnoughToLeaveResource()
    {
        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats != null ? EffectiveStats.EnergyCapacity : Candidate.Genome.EnergyCapacity));
        float stomachRatio = GetStomachFullness01();
        float hungerDrive = Candidate != null && Candidate.Genome != null ? Candidate.Genome.HungerDrive : 0.5f;
        float selfish = Candidate != null && Candidate.Genome != null ? Candidate.Genome.Selfishness : 0.2f;
        float energyTarget = Mathf.Lerp(LeaveResourceEnergyRatio * 0.86f, LeaveResourceEnergyRatio * 1.08f, hungerDrive);
        float stomachTarget = Mathf.Lerp(LeaveResourceStomachRatio * 0.82f, LeaveResourceStomachRatio * 1.12f, selfish);

        return energyRatio >= energyTarget || stomachRatio >= stomachTarget;
    }

    private Vector3? GetPrimaryStaticFoodTargetPosition()
    {
        if (!WantsStaticResourceNow())
        {
            return null;
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

        if (IsHungryEnoughToSearch() && hasFoodMemory && !rememberedFoodWasBad)
        {
            return rememberedFoodArea;
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


    private float GetMaxHealth()
    {
        float body = EffectiveStats != null ? EffectiveStats.BodySize : Candidate != null && Candidate.Genome != null ? Candidate.Genome.BodySize : 1f;
        float armour = EffectiveStats != null ? EffectiveStats.Defence : Candidate != null && Candidate.Genome != null ? Candidate.Genome.Armour : 0f;
        return BaseHealth * Mathf.Lerp(0.75f, 1.65f, Mathf.InverseLerp(0.4f, 2.8f, body)) + armour * 8f;
    }

    private float GetStomachCapacity()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return BaseStomachCapacity;
        }

        return Mathf.Max(4f, BaseStomachCapacity * Candidate.Genome.StomachSize * Mathf.Lerp(0.65f, 1.8f, Mathf.InverseLerp(0.4f, 2.8f, EffectiveStats.BodySize)));
    }

    private float GetStomachTotal()
    {
        return Mathf.Max(0f, StomachPlant + StomachMeat + StomachCarrion);
    }

    public float GetStomachFullness01()
    {
        return Mathf.Clamp01(GetStomachTotal() / Mathf.Max(0.01f, GetStomachCapacity()));
    }

    public float GetHealthRatio()
    {
        return Mathf.Clamp01(CurrentHealth / Mathf.Max(0.01f, GetMaxHealth()));
    }

    public float GetEffectiveEnergyRatio()
    {
        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats != null ? EffectiveStats.EnergyCapacity : Candidate.Genome.EnergyCapacity));
        float stomachRatio = GetStomachFullness01();
        return Mathf.Clamp01(energyRatio * 0.72f + stomachRatio * 0.28f);
    }

    public float GetHungerPressure()
    {
        return 1f - GetEffectiveEnergyRatio();
    }

    public bool IsHungryEnoughToSearch()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return true;
        }

        if (GetHealthRatio() <= DesperateHealthRatio)
        {
            return true;
        }

        float threshold = Mathf.Clamp(Candidate.Genome.HungerThreshold, 0.08f, 0.95f);
        return GetHungerPressure() >= threshold;
    }

    private void DigestStomach()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return;
        }

        float metabolism = Mathf.Max(0.1f, Candidate.Genome.Metabolism);
        float digestBudget = BaseDigestionPerSecond * metabolism * Time.fixedDeltaTime;
        float total = GetStomachTotal();
        if (total <= 0.001f || digestBudget <= 0f)
        {
            return;
        }

        float amount = Mathf.Min(total, digestBudget);
        float plantShare = StomachPlant / total;
        float meatShare = StomachMeat / total;
        float carrionShare = StomachCarrion / total;

        float plantDigested = Mathf.Min(StomachPlant, amount * plantShare);
        float meatDigested = Mathf.Min(StomachMeat, amount * meatShare);
        float carrionDigested = Mathf.Min(StomachCarrion, amount * carrionShare);

        StomachPlant -= plantDigested;
        StomachMeat -= meatDigested;
        StomachCarrion -= carrionDigested;

        float gained = plantDigested * Mathf.Lerp(0.45f, 1.18f, Candidate.Genome.PlantDiet);
        gained += meatDigested * Mathf.Lerp(0.55f, 1.25f, Candidate.Genome.MeatDiet);
        gained += carrionDigested * Mathf.Lerp(0.35f, 1.18f, Candidate.Genome.CarrionDiet);

        CurrentEnergy = Mathf.Min(CurrentEnergy + gained, EffectiveStats.EnergyCapacity);
        Candidate.EnergyGained += gained;
    }

    private float GetBiteMass(bool meatBite)
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return BaseBiteMass;
        }

        float jaw = Candidate.Genome.JawSize * 0.55f + Candidate.Genome.JawWidth * 0.25f + Candidate.Genome.JawLength * 0.20f;
        float bite = BaseBiteMass * Mathf.Clamp(jaw, 0.35f, 2.6f);
        if (meatBite)
        {
            bite *= Mathf.Lerp(0.65f, 1.25f, Candidate.Genome.MeatDiet + Candidate.Genome.CarrionDiet * 0.35f);
        }
        return Mathf.Max(0.25f, bite);
    }

    private float GetRemainingStomachSpace()
    {
        return Mathf.Max(0f, GetStomachCapacity() - GetStomachTotal());
    }

    private void AddToStomach(float plant, float meat, float carrion)
    {
        float space = GetRemainingStomachSpace();
        float total = Mathf.Max(0f, plant + meat + carrion);
        if (space <= 0f || total <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(1f, space / total);
        StomachPlant += plant * scale;
        StomachMeat += meat * scale;
        StomachCarrion += carrion * scale;
    }

    private void RememberFoodArea(Vector3 foodPosition, bool badMemory)
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        float memory = Mathf.Clamp01(Candidate.Genome.FoodMemoryStrength);
        if (memory <= 0.05f)
        {
            return;
        }

        Vector2 noise = Random.insideUnitCircle * FoodMemoryAreaRadius * Mathf.Lerp(1.2f, 0.25f, memory);
        rememberedFoodArea = new Vector3(foodPosition.x + noise.x, foodPosition.y, foodPosition.z + noise.y);
        foodMemoryTimer = (badMemory ? BadFoodMemoryDuration : FoodMemoryDuration) * Mathf.Lerp(0.45f, 1.45f, memory);
        rememberedFoodWasBad = badMemory;
        hasFoodMemory = true;
    }

    private void UpdateFoodMemoryTimers()
    {
        if (!hasFoodMemory)
        {
            return;
        }

        foodMemoryTimer -= Time.fixedDeltaTime;

        if (!rememberedFoodWasBad && Vector3.Distance(transform.position, rememberedFoodArea) <= FoodMemoryAreaRadius * 0.45f)
        {
            bool foundSomething = (nearestFood != null && !nearestFood.IsConsumed) || (nearestCarrion != null && !nearestCarrion.IsConsumed);
            if (!foundSomething)
            {
                rememberedFoodWasBad = true;
                foodMemoryTimer = Mathf.Min(foodMemoryTimer, BadFoodMemoryDuration);
            }
        }

        if (foodMemoryTimer <= 0f)
        {
            hasFoodMemory = false;
            rememberedFoodWasBad = false;
        }
    }

    private void DrainEnergy()
    {
        float environmentDrain = 1f;
        if (EvolutionEcosystemManager.Instance != null && EvolutionEcosystemManager.Instance.Environment != null)
        {
            environmentDrain = EvolutionEcosystemManager.Instance.Environment.EnergyDrainMultiplier;
        }

        float movementCost = currentVelocity.magnitude / Mathf.Max(0.1f, EffectiveStats.Speed);
        float metabolism = Candidate != null && Candidate.Genome != null ? Mathf.Max(0.1f, Candidate.Genome.Metabolism) : 1f;
        float drain = BaseEnergyDrainPerSecond * EffectiveStats.EnergyDrainMultiplier * environmentDrain;
        drain *= Mathf.Lerp(0.78f, 1.35f, metabolism);
        drain += movementCost * 0.18f;
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - drain * Time.fixedDeltaTime);

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats.EnergyCapacity));
        float stomachRatio = GetStomachFullness01();
        if (energyRatio <= 0.01f && stomachRatio <= 0.01f)
        {
            float damage = StarvationHealthDamagePerSecond * Mathf.Lerp(0.75f, 1.45f, metabolism) * Time.fixedDeltaTime;
            CurrentHealth -= damage;
            if (Candidate != null)
            {
                Candidate.StarvationDamageTaken += damage;
            }
        }
        else if (energyRatio > 0.65f || stomachRatio > 0.35f)
        {
            CurrentHealth = Mathf.Min(GetMaxHealth(), CurrentHealth + HealthRecoveryPerSecond * Time.fixedDeltaTime);
        }
    }

    private void TryEatFood()
    {
        if (feedingHoldTimer > 0f || !WantsStaticResourceNow() || nearestFood == null || nearestFood.IsConsumed || GetRemainingStomachSpace() <= 0.05f)
        {
            return;
        }

        if (!CanConsumeStaticTarget(nearestFood.transform.position))
        {
            return;
        }

        float biteMass = Mathf.Min(GetBiteMass(false), GetRemainingStomachSpace());
        float eatenMass = nearestFood.ConsumeBiteBy(biteMass, Candidate != null ? Candidate.Id : 0);
        if (eatenMass <= 0f)
        {
            RememberFoodArea(nearestFood.transform.position, true);
            nearestFood = null;
            return;
        }

        AddToStomach(eatenMass, 0f, 0f);
        Candidate.PlantEnergyConsumed += eatenMass;
        Candidate.FoodEaten++;
        Candidate.Genome.ReinforceDietUsage(Candidate.PlantEnergyConsumed, Candidate.MeatEnergyConsumed, Candidate.CarrionEnergyConsumed, DietLearningRate);
        OnSuccessfulStaticBite(nearestFood.transform.position, true);
        if (nearestFood == null || nearestFood.IsConsumed)
        {
            nearestFood = null;
        }
    }

    private void TryEatCarrion()
    {
        if (feedingHoldTimer > 0f || !WantsStaticResourceNow() || nearestCarrion == null || nearestCarrion.IsConsumed || GetRemainingStomachSpace() <= 0.05f)
        {
            return;
        }

        if (!CanConsumeStaticTarget(nearestCarrion.transform.position))
        {
            return;
        }

        float biteMass = Mathf.Min(GetBiteMass(true), GetRemainingStomachSpace());
        float eatenMass = nearestCarrion.ConsumeBiteBy(biteMass, Candidate != null ? Candidate.Id : 0);
        if (eatenMass <= 0f)
        {
            RememberFoodArea(nearestCarrion.transform.position, true);
            nearestCarrion = null;
            return;
        }

        AddToStomach(0f, 0f, eatenMass);
        Candidate.CarrionEnergyConsumed += eatenMass;
        Candidate.CarrionEaten++;
        Candidate.Genome.ReinforceDietUsage(Candidate.PlantEnergyConsumed, Candidate.MeatEnergyConsumed, Candidate.CarrionEnergyConsumed, DietLearningRate);
        OnSuccessfulStaticBite(nearestCarrion.transform.position, false);
        if (nearestCarrion == null || nearestCarrion.IsConsumed)
        {
            nearestCarrion = null;
        }
    }

    private void OnSuccessfulStaticBite(Vector3 resourcePosition, bool plantFood)
    {
        feedingHoldTimer = FeedingHoldAfterBite;
        currentStaticFeedingPoint = resourcePosition;
        hasStaticFeedingPoint = true;
        currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, FeedingHoldDamping * Time.fixedDeltaTime);
        RememberFoodArea(resourcePosition, false);

        if (!IsSatisfiedEnoughToLeaveResource())
        {
            return;
        }

        if (plantFood && nearestFood != null)
        {
            temporarilyIgnoredFood = nearestFood;
            nearestFood = null;
            retainedFood = null;
        }
        else if (!plantFood && nearestCarrion != null)
        {
            temporarilyIgnoredCarrion = nearestCarrion;
            nearestCarrion = null;
            retainedCarrion = null;
        }

        ignoredResourceTimer = Mathf.Max(ignoredResourceTimer, LeaveResourceIgnoreTime);
        retainedTargetTimer = 0f;
        staticTargetNoProgressTimer = 0f;
        PickNewWanderDirection();
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
        float meatStored = Mathf.Min(energyGained, GetRemainingStomachSpace());
        AddToStomach(0f, meatStored, 0f);
        Candidate.MeatEnergyConsumed += meatStored;
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

        float bodyDistance = Vector3.Distance(transform.position, targetPosition);
        float bodyContact = Mathf.Max(StaticFoodContactConsumeDistance, StaticBodyContactConsumeDistance, GetPersonalRadius() + GetScaledMouthRadius() * 0.85f);
        if (bodyDistance <= bodyContact)
        {
            return true;
        }

        float mouthDistance = Vector3.Distance(GetMouthWorldPosition(), targetPosition);
        float mouthContact = Mathf.Max(GetScaledMouthRadius() * 1.85f, StaticMouthContactConsumeDistance, StaticFoodContactConsumeDistance * 0.9f);
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
        if (CurrentHealth <= 0f)
        {
            return true;
        }

        float defence = EffectiveStats != null ? EffectiveStats.Defence : Candidate.Genome.Armour;
        float reduction = Mathf.Clamp01(defence * ArmourDamageReductionPerPoint);
        float damage = Mathf.Max(0.25f, incomingDamage * (1f - reduction));
        CurrentHealth -= damage;
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - damage * 0.2f);
        if (attacker != null)
        {
            RememberDangerArea(attacker.transform.position);
        }

        if (CurrentHealth <= 0f)
        {
            float size = EffectiveStats != null ? EffectiveStats.BodySize : Candidate.Genome.BodySize;
            float gainMultiplier = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance.BiteEnergyGainMultiplier : 0.32f;
            energyGainedByAttacker = Mathf.Max(5f, size * 22f) * gainMultiplier;
            Die(false);
            return true;
        }

        return false;
    }

    private void UpdateHabitatMemory()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (!hasHomeArea)
        {
            hasHomeArea = true;
            homeArea = transform.position;
            homeConfidence = 0.45f;
        }

        bool threatened = lastThreatCount > 0 || GetHealthRatio() < 0.35f;
        bool fed = GetEffectiveEnergyRatio() > 0.55f || GetStomachFullness01() > 0.25f;

        if (threatened)
        {
            RememberDangerArea(transform.position);
            homeConfidence = Mathf.Max(0f, homeConfidence - UnsafeHomeDecayRate * Time.fixedDeltaTime);

            if (Vector3.Distance(transform.position, homeArea) < HomeAttractionDistance * 1.25f)
            {
                Vector3 away = transform.position - rememberedDangerArea;
                away.y = 0f;
                if (away.sqrMagnitude <= 0.001f)
                {
                    away = GetFallbackSideDirection();
                }
                homeArea += away.normalized * SafeHomeLearningRate * 5f;
            }
            return;
        }

        if (fed && !IsHungryEnoughToSearch())
        {
            float learning = SafeHomeLearningRate * Mathf.Lerp(0.45f, 1.35f, Candidate.Genome.FoodMemoryStrength);
            homeArea = Vector3.Lerp(homeArea, transform.position, learning * Time.fixedDeltaTime);
            homeConfidence = Mathf.Clamp01(homeConfidence + learning * Time.fixedDeltaTime);
        }
    }

    private void RememberDangerArea(Vector3 dangerPosition)
    {
        rememberedDangerArea = dangerPosition;
        dangerMemoryTimer = DangerMemoryDuration * Mathf.Lerp(0.5f, 1.4f, Candidate != null && Candidate.Genome != null ? Candidate.Genome.FoodMemoryStrength : 0.5f);
    }


    private bool ShouldSeekMate()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return false;
        }

        if (!IsMatureForMating() || !HasMatingEnergy())
        {
            return false;
        }

        if (IsHungryEnoughToSearch())
        {
            return false;
        }

        float stomachReady = GetStomachFullness01();
        float energyReady = GetEffectiveEnergyRatio();
        if (energyReady < MateSeekingEnergyRatio && stomachReady < MateSeekingStomachRatio)
        {
            return false;
        }

        return true;
    }

    private void UpdateMateTarget()
    {
        if (!ShouldSeekMate() || EvolutionEcosystemManager.Instance == null)
        {
            currentMateTarget = null;
            mateTargetTimer = 0f;
            return;
        }

        if (currentMateTarget != null && currentMateTarget.Candidate != null && currentMateTarget.Candidate.Genome != null)
        {
            bool stillValid = currentMateTarget.IsMatureForMating()
                && currentMateTarget.HasMatingEnergy()
                && Candidate.Genome.GetMorphSimilarity(currentMateTarget.Candidate.Genome) >= RequiredMateMorphSimilarity
                && Vector3.Distance(transform.position, currentMateTarget.transform.position) <= MateSearchRadius * 1.25f
                && ((Candidate.Genome.SexGene >= 0.5f) != (currentMateTarget.Candidate.Genome.SexGene >= 0.5f));

            if (stillValid && mateTargetTimer > 0f)
            {
                return;
            }
        }

        currentMateTarget = EvolutionEcosystemManager.Instance.GetBestMateFor(this, MateSearchRadius, RequiredMateMorphSimilarity);
        mateTargetTimer = Mathf.Max(0.25f, MateTargetRefreshTime);
    }

    private Vector3 GetMateSeekingPull(float hungerPressure)
    {
        if (!ShouldSeekMate())
        {
            return Vector3.zero;
        }

        if (currentMateTarget == null)
        {
            UpdateMateTarget();
        }

        if (currentMateTarget == null)
        {
            return Vector3.zero;
        }

        Vector3 toMate = currentMateTarget.transform.position - transform.position;
        if (toMate.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        float distance = toMate.magnitude;
        float closeSlow = Mathf.Clamp01(distance / Mathf.Max(0.1f, MatePairDistance * 1.8f));
        float drive = Mathf.Lerp(0.35f, 1f, closeSlow);
        drive *= Mathf.Lerp(0.6f, 1.35f, Candidate.Genome.MateDrive);
        drive *= Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(hungerPressure));

        return toMate.normalized * MateSeekingWeight * drive;
    }

    private Vector3 GetCachedTerrainAvoidancePull()
    {
        if (terrainScanTimer <= 0f)
        {
            terrainScanTimer = Mathf.Max(0.02f, TerrainScanInterval) * Random.Range(0.85f, 1.15f);
            cachedTerrainPull = GetTerrainAvoidancePull();
        }

        return cachedTerrainPull;
    }

    private Vector3 GetTerrainAvoidancePull()
    {
        lastTerrainAvoidance = Vector3.zero;

        if (!UseTerrainAvoidance || EvolutionEcosystemManager.Instance == null)
        {
            return Vector3.zero;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        Vector3 position = rb != null ? rb.position : transform.position;
        Vector3 pull = Vector3.zero;

        if (manager.TryGetTerrainHeight(position, out float terrainY))
        {
            float clearance = position.y - terrainY;
            float wantedClearance = Mathf.Max(0.1f, TerrainFloorClearance + GetPersonalRadius() * 0.45f);
            if (clearance < wantedClearance)
            {
                float t = 1f - Mathf.Clamp01(clearance / wantedClearance);
                pull += Vector3.up * TerrainFloorLiftWeight * t;
            }
        }

        Vector3 baseForward = wantedDirection.sqrMagnitude > 0.01f ? wantedDirection.normalized : transform.forward;
        Vector3 origin = position + Vector3.up * TerrainProbeForwardHeight;
        float radius = Mathf.Max(0.15f, GetPersonalRadius() * 0.45f);
        LayerMask mask = manager.TerrainRaycastMask;

        AddTerrainRayAvoidance(origin, baseForward, TerrainLookAhead, radius, mask, ref pull);

        Vector3 flatForward = new Vector3(baseForward.x, 0f, baseForward.z);
        if (flatForward.sqrMagnitude <= 0.001f)
        {
            flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        }
        if (flatForward.sqrMagnitude > 0.001f)
        {
            flatForward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
            AddTerrainRayAvoidance(origin, (flatForward + right * 0.55f).normalized, TerrainSideLookAhead, radius, mask, ref pull);
            AddTerrainRayAvoidance(origin, (flatForward - right * 0.55f).normalized, TerrainSideLookAhead, radius, mask, ref pull);
        }

        if (pull.sqrMagnitude > 0.001f)
        {
            pull = pull.normalized * Mathf.Min(TerrainAvoidanceWeight, pull.magnitude);
        }

        lastTerrainAvoidance = pull;
        return pull;
    }

    private void AddTerrainRayAvoidance(Vector3 origin, Vector3 direction, float distance, float radius, LayerMask mask, ref Vector3 pull)
    {
        if (direction.sqrMagnitude <= 0.001f || distance <= 0f)
        {
            return;
        }

        if (Physics.SphereCast(origin, radius, direction.normalized, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
        {
            float closeness = 1f - Mathf.Clamp01(hit.distance / Mathf.Max(0.01f, distance));
            Vector3 normal = hit.normal;
            Vector3 slide = Vector3.ProjectOnPlane(direction, normal);
            if (slide.sqrMagnitude <= 0.001f)
            {
                slide = GetFallbackSideDirection();
            }

            Vector3 away = (normal * TerrainAvoidanceWeight + slide.normalized * TerrainWallSlideWeight) * closeness;
            if (normal.y < 0.25f)
            {
                away.y *= 0.25f;
            }

            pull += away;
        }
    }

    private void TryMateAndLayEggs()
    {
        if (eggLayTimer > 0f || EvolutionEcosystemManager.Instance == null || Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (!IsMatureForMating() || !HasMatingEnergy())
        {
            return;
        }

        bool isFemale = Candidate.Genome.SexGene >= 0.5f;
        if (!isFemale)
        {
            eggLayTimer = Mathf.Max(2f, EggLayCooldown * 0.25f);
            return;
        }

        MarineCreatureAgent mate = currentMateTarget;
        if (mate == null || !mate.HasMatingEnergy())
        {
            mate = EvolutionEcosystemManager.Instance.GetBestMateFor(this, MateSearchRadius, RequiredMateMorphSimilarity);
        }

        if (mate == null || !mate.HasMatingEnergy())
        {
            eggLayTimer = Mathf.Max(3f, EggLayCooldown * 0.18f);
            return;
        }

        if (Vector3.Distance(transform.position, mate.transform.position) > MatePairDistance)
        {
            eggLayTimer = Mathf.Min(eggLayTimer, 1.0f);
            return;
        }

        float body = EffectiveStats != null ? EffectiveStats.BodySize : Candidate.Genome.BodySize;
        float energyRatio = GetEffectiveEnergyRatio();
        int eggs = Mathf.RoundToInt(Mathf.Lerp(MinimumEggsPerClutch, MaximumEggsPerClutch, Mathf.Clamp01((body - 0.4f) / 2.4f)) * Mathf.Lerp(0.65f, 1.25f, energyRatio));
        eggs = Mathf.Clamp(eggs, Mathf.Max(1, MinimumEggsPerClutch), Mathf.Max(MinimumEggsPerClutch, MaximumEggsPerClutch));

        List<EvolutionCandidate> children = new List<EvolutionCandidate>();
        float mutationMultiplier = EvolutionEcosystemManager.Instance.Environment != null ? EvolutionEcosystemManager.Instance.Environment.MutationMultiplier : 1f;
        mutationMultiplier *= Mathf.Lerp(0.95f, 1.2f, EvolutionEcosystemManager.Instance.GetCurrentStressAt(transform.position));

        for (int i = 0; i < eggs; i++)
        {
            EvolutionGenome childGenome = EvolutionBreedingUtility.CreateChildGenome(Candidate.Genome, mate.Candidate.Genome, mutationMultiplier);
            EvolutionCandidate child = new EvolutionCandidate(childGenome);
            child.ParentId = Candidate.Id;
            children.Add(child);
        }

        Vector3 eggPosition = EvolutionEcosystemManager.Instance.FindSafeEggPositionNear(transform.position, Candidate.Genome, NestSearchRadius, NestSearchSamples);
        float eggHealth = Mathf.Max(8f, EggHealthPerBodySize * body) * Mathf.Lerp(0.75f, 1.35f, Candidate.Genome.EggProtection);
        float eggMass = eggs * EggMassPerEgg * Mathf.Lerp(0.75f, 1.35f, body);
        EvolutionEcosystemManager.Instance.SpawnEggCluster(this, mate, eggPosition, children, EggHatchTime, eggHealth, eggMass);

        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - EggLayEnergyCost * Mathf.Lerp(0.75f, 1.45f, eggs / Mathf.Max(1f, (float)MaximumEggsPerClutch)));
        mate.CurrentEnergy = Mathf.Max(0f, mate.CurrentEnergy - EggLayEnergyCost * 0.35f);
        eggLayTimer = EggLayCooldown * Mathf.Lerp(0.75f, 1.35f, 1f - Candidate.Genome.NestingDrive);
        reproductionTimer = ReproductionCooldown;
        Candidate.ReproductionCount += eggs;
        Candidate.EggsLaid += eggs;
    }

    public bool IsMatureForMating()
    {
        return !IsJuvenile && AgeSeconds >= MaturityAgeSeconds;
    }

    public bool HasMatingEnergy()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return false;
        }

        return GetEffectiveEnergyRatio() >= MateEnergyRatioRequired && CurrentHealth >= GetMaxHealth() * 0.55f;
    }

    public void SetJuvenileOnHatch()
    {
        IsJuvenile = true;
        AgeSeconds = 0f;
        ApplyJuvenileScale();
    }

    private void UpdateJuvenileGrowth()
    {
        if (!IsJuvenile)
        {
            return;
        }

        ApplyJuvenileScale();
        if (AgeSeconds >= JuvenileGrowTime)
        {
            IsJuvenile = false;
            transform.localScale = Vector3.one;
        }
    }

    private void ApplyJuvenileScale()
    {
        float t = Mathf.Clamp01(AgeSeconds / Mathf.Max(1f, JuvenileGrowTime));
        transform.localScale = Vector3.one * Mathf.Lerp(JuvenileStartScale, 1f, t);
    }

    public void AddMeatToStomachFromEgg(float mass)
    {
        if (mass <= 0f)
        {
            return;
        }

        AddToStomach(0f, mass, 0f);
        if (Candidate != null)
        {
            Candidate.MeatEnergyConsumed += mass;
            Candidate.FoodMassConsumed += mass;
        }
    }

    private void UpdateMetrics()
    {
        if (Candidate == null)
        {
            return;
        }

        Candidate.SurvivalTime = aliveTimer;
        Candidate.FinalEnergy = CurrentEnergy;
        Candidate.FinalHealth = CurrentHealth;
        Candidate.FinalStomachFullness = GetStomachFullness01();
        float currentSpeed = currentVelocity.magnitude;
        Candidate.DistanceTravelled += currentSpeed * Time.fixedDeltaTime;
        Candidate.AverageSpeedUsed = Mathf.Lerp(Candidate.AverageSpeedUsed, currentSpeed, 0.02f);

        AddBrainModeMetric(Time.fixedDeltaTime);

        if (Candidate.Genome != null)
        {
            Candidate.Genome.DecayUnusedBehaviourTraits(BehaviourDecayRate * Time.fixedDeltaTime);
        }
    }

    private void AddBrainModeMetric(float deltaTime)
    {
        if (Candidate == null)
        {
            return;
        }

        switch (currentBrainMode)
        {
            case FishAutonomousBehaviourMode.Resting: Candidate.RestingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Exploring: Candidate.ExploringTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Schooling: Candidate.SchoolingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.FollowingLeader: Candidate.SchoolingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Foraging: Candidate.ForagingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Feeding: Candidate.FeedingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.SeekingMate: Candidate.MateSeekingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Nesting: Candidate.MateSeekingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Hunting: Candidate.HuntingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Fleeing: Candidate.FleeingTime += deltaTime; break;
            case FishAutonomousBehaviourMode.Recovering: Candidate.RecoveryTime += deltaTime; break;
        }
    }

    private void UpdateDebugMovementState(float hungerPressure, Vector3 targetPull, Vector3 dangerPull, Vector3 schoolPull, Vector3 boundaryPull, Vector3 emergencyPull)
    {
        if (stuckEscapeTimer > 0f || emergencyPull.sqrMagnitude > 0.01f)
        {
            debugMoveState = "Recovering";
        }
        else
        {
            debugMoveState = currentBrainMode.ToString();
        }

        if (Mathf.Abs(wantedDirection.y) > 0.35f)
        {
            if (targetPull.sqrMagnitude > 0.05f && Mathf.Abs(targetPull.y) > 0.15f) debugVerticalReason = "Target depth";
            else if (dangerPull.sqrMagnitude > 0.05f && Mathf.Abs(dangerPull.y) > 0.15f) debugVerticalReason = "Threat depth";
            else if (boundaryPull.sqrMagnitude > 0.05f && Mathf.Abs(boundaryPull.y) > 0.15f) debugVerticalReason = "Bounds";
            else if (lastTerrainAvoidance.sqrMagnitude > 0.05f && Mathf.Abs(lastTerrainAvoidance.y) > 0.15f) debugVerticalReason = "Terrain";
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
        if (settings == null || settings.DrawVelocityRays) Debug.DrawRay(transform.position, currentVelocity * velocityScale, Color.white, duration);
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
        if (lastTerrainAvoidance.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastTerrainAvoidance, new Color(1f, 0.45f, 0.05f), duration);
        if (currentMateTarget != null && ShouldSeekMate()) Debug.DrawLine(transform.position, currentMateTarget.transform.position, new Color(1f, 0.3f, 1f), duration);
    }

    public string GetDebugSummary()
    {
        if (Candidate == null || Candidate.Genome == null || EffectiveStats == null)
        {
            return "Uninitialised creature";
        }

        return DebugName +
               " | Energy " + CurrentEnergy.ToString("F0") + "/" + EffectiveStats.EnergyCapacity.ToString("F0") +
               " | Health " + CurrentHealth.ToString("F0") + "/" + GetMaxHealth().ToString("F0") +
               " | Age " + AgeSeconds.ToString("F0") + "s" +
               " | Sex " + (Candidate.Genome.SexGene >= 0.5f ? "F" : "M") +
               " | Stage " + (IsJuvenile ? "Juvenile" : IsMatureForMating() ? "Adult" : "Young") +
               " | Stomach " + GetStomachFullness01().ToString("P0") +
               " | Speed " + EffectiveStats.Speed.ToString("F1") +
               " | Vision " + EffectiveStats.VisionRange.ToString("F1") +
               " | Mouth " + GetScaledMouthRadius().ToString("F2") +
               " | Bite " + GetBiteDamage().ToString("F1") +
               " | Def " + EffectiveStats.Defence.ToString("F1") +
               " | Danger " + EffectiveStats.DangerFactor.ToString("F1") +
               " | State " + debugMoveState +
               " | Leader " + (currentHungryLeader != null ? currentHungryLeader.DebugName : "None") +
               " | Memory " + (hasFoodMemory ? (rememberedFoodWasBad ? "Bad" : "Food") : "None") +
               " | School F/D/T " + lastFriendlyCount + "/" + lastDifferentCount + "/" + lastThreatCount +
               " | Crowd " + lastDenseCrowdCount + "/Food" + lastFeedingCrowdCount +
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

    public string GetHabitatDebugSummary()
    {
        if (!hasHomeArea)
        {
            return "No home area";
        }

        float homeDistance = Vector3.Distance(transform.position, homeArea);
        string danger = dangerMemoryTimer > 0f ? " danger memory" : " safe";
        return "Home " + homeDistance.ToString("F1") + "m | Confidence " + homeConfidence.ToString("F2") + danger;
    }

    public string GetDebugMoveState()
    {
        return debugMoveState;
    }

    public string GetBrainDebugSummary()
    {
        return currentBrainMode + " | " + brainReason + " | F/M/S/E/H/T "
            + brainFoodDesire.ToString("F2") + "/"
            + brainMateDesire.ToString("F2") + "/"
            + brainSchoolDesire.ToString("F2") + "/"
            + brainExploreDesire.ToString("F2") + "/"
            + brainHomeDesire.ToString("F2") + "/"
            + brainFleeDesire.ToString("F2");
    }

    public FishAutonomousBehaviourMode GetBrainMode()
    {
        return currentBrainMode;
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

        string label = DebugName + "\nE " + CurrentEnergy.ToString("F0") + " H " + CurrentHealth.ToString("F0") + " S " + GetStomachFullness01().ToString("P0") + " | " + CreatureDebugTypeUtility.GetMorphologyName(Candidate.Genome);
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
