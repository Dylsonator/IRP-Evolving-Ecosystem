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

    [Header("Collision / Anti Stacking")]
    [Tooltip("The ecosystem uses script checks for eating/biting, so visual colliders can be disabled to stop creatures body-blocking each other.")]
    public bool DisableBlockingCreatureColliders = true;
    [Tooltip("Adds one simple trigger sphere to the creature root for selection/debug without letting visual parts block movement.")]
    public bool UseSimpleRootTriggerCollider = true;
    public float RootTriggerRadius = 0.85f;
    [Tooltip("How far creatures try to keep away from each other. This is multiplied by body size.")]
    public float NeighbourUnstickDistance = 2.05f;
    public float NeighbourUnstickStrength = 2.2f;
    public int MaxNeighboursForUnstick = 8;
    [Range(0f, 1f)] public float VerticalUnstickInfluence = 0.12f;
    [Tooltip("Pushes creatures gently back toward the middle depth when they have no strong vertical target.")]
    public float DepthStabilityStrength = 0.12f;
    [Range(0f, 1f)] public float VerticalDampingWhenCrowded = 0.45f;
    public bool DebugNeighbourUnstick;

    [Header("Harmonious Species / Threat Rules")]
    [Tooltip("Early uniform-baseline creatures are allowed to share space without treating each other as threats.")]
    public bool BaselineSpeciesAreHarmonious = true;
    [Range(0f, 1f)] public float HarmlessAggressionLimit = 0.28f;
    [Range(0f, 1f)] public float HarmlessMeatDietLimit = 0.38f;
    [Tooltip("When hungry and moving toward food, harmless neighbours are mostly ignored unless actually overlapping.")]
    [Range(0f, 1f)] public float HungerIgnoreHarmlessNeighbourAt = 0.35f;
    [Tooltip("Harmless neighbour spacing is mostly horizontal to stop up/down bobbing.")]
    public float HarmlessNeighbourSlideStrength = 0.85f;
    [Tooltip("How much harmless crowding is reduced when a creature is hungry and has a food/carrion target.")]
    [Range(0f, 1f)] public float FoodCrowdSeparationMultiplier = 0.45f;
    public float TrueThreatFleeMultiplier = 1.25f;
    [Tooltip("A starving creature can only hunt if it has evolved at least this much aggression.")]
    [Range(0f, 1f)] public float StarvingHuntAggressionLimit = 0.20f;
    [Tooltip("A starving creature can only hunt if it has evolved a strong meat diet.")]
    [Range(0f, 1f)] public float StarvingHuntMeatDietLimit = 0.58f;
    [Range(0f, 1f)] public float StarvingHuntEnergyRatio = 0.30f;

    [Header("v19 Boids Schooling Movement")]
    [Tooltip("Uses boids-style separation, alignment and cohesion instead of single-neighbour social steering.")]
    public bool UseBoidMovement = true;
    public float BoidPerceptionRadius = 7.5f;
    public float BoidSeparationRadius = 1.75f;
    public int BoidMaxNeighbours = 18;
    public float BoidSeparationWeight = 2.6f;
    public float BoidAlignmentWeight = 1.05f;
    public float BoidCohesionWeight = 1.25f;
    public float BoidDifferentGroupSpacingWeight = 0.45f;
    public float BoidThreatAvoidWeight = 3.2f;
    [Tooltip("How much same-group separation is allowed to use Y. Low values stop vertical bobbing when fish crowd each other.")]
    [Range(0f, 1f)] public float BoidSeparationVerticalInfluence = 0.12f;
    [Tooltip("Hungry fish still school, but food gets priority so they do not starve while trying to stay in formation.")]
    [Range(0f, 1f)] public float BoidHungryGroupWeightMultiplier = 0.42f;
    [Tooltip("If true, cohesion/alignment can move the school up/down. Separation still stays mostly horizontal.")]
    public bool BoidAllowVerticalSchooling = true;
    [Tooltip("If the current food/carrion/prey target is closer than this, schooling weakens and feeding wins.")]
    public float BoidCloseFoodPriorityDistance = 3.8f;

    [Header("Debug Visuals")]
    public bool ApplyTypeColour = true;

    [Header("Visual Scale Safety")]
    public bool ScaleCreatureRootByEffectiveBodySize = true;
    public bool DisableLegacyPhenotypeVisuals = true;

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
    private Vector3 lastNeighbourPush;
    private Vector3 lastBoidCohesion;
    private Vector3 lastBoidAlignment;
    private Vector3 lastBoidSeparation;
    private Vector3 lastBoidThreatAvoid;
    private Vector3 lastBoidGroupCentre;
    private int lastBoidSameGroupCount;
    private int lastBoidThreatCount;
    private Vector3 wanderDirection;
    private float wanderTimer;
    private float aliveTimer;
    private float lowSpeedNearTargetTimer;
    private string debugMoveState = "Wander";
    private string debugVerticalReason = "Stable";
    private bool debugNearestCreatureIsThreat;

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
        transform.localScale = ScaleCreatureRootByEffectiveBodySize ? Vector3.one * EffectiveStats.BodySize : Vector3.one;
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
        rb.linearDamping = 2.4f;
        rb.angularDamping = 5.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        ApplyMorphVisuals();

        if (DisableBlockingCreatureColliders)
        {
            DisableBlockingCollidersOnCreature();
        }

        if (UseSimpleRootTriggerCollider)
        {
            EnsureSimpleRootTriggerCollider();
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
        Color colour = CreatureDebugTypeUtility.GetTypeColour(Candidate.BehaviourType);
        builder.Build(Candidate.Genome, EffectiveStats, colour);
    }


    private void DisableBlockingCollidersOnCreature()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider creatureCollider = colliders[i];

            if (creatureCollider == null)
            {
                continue;
            }

            // Eating, carrion and predation are all script-driven in this prototype.
            // Physical colliders on visual morph parts make creatures wedge into each other.
            creatureCollider.enabled = false;
        }
    }

    private void EnsureSimpleRootTriggerCollider()
    {
        SphereCollider rootTrigger = GetComponent<SphereCollider>();

        if (rootTrigger == null)
        {
            rootTrigger = gameObject.AddComponent<SphereCollider>();
        }

        float size = EffectiveStats != null ? Mathf.Max(0.1f, EffectiveStats.BodySize) : 1f;
        rootTrigger.isTrigger = true;
        rootTrigger.enabled = true;
        rootTrigger.center = Vector3.zero;
        rootTrigger.radius = Mathf.Max(0.05f, RootTriggerRadius * size);
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
        bool useBoids = IsBoidMovementEnabled();
        Vector3 boidSteering = useBoids ? GetBoidSteering(hungerPressure, socialWeight) : Vector3.zero;
        Vector3 groupingPull = Vector3.zero;
        Vector3 separationPush = Vector3.zero;
        Vector3 neighbourUnstickPush = Vector3.zero;

        if (!useBoids)
        {
            bool harmlessNeighbour = IsHarmlessNeighbour(nearestCreature);
            bool ignoreHarmlessForFood = ShouldIgnoreHarmlessNeighbourForFood(nearestCreature, hungerPressure);
            float harmlessSocialMultiplier = harmlessNeighbour && ignoreHarmlessForFood ? 0f : 1f;
            groupingPull = toCreature * Candidate.Genome.GroupingChance * Candidate.Genome.AttractionRange * socialWeight * harmlessSocialMultiplier;
            separationPush = GetSocialSeparationPush(toCreature, socialWeight, hungerPressure);
            neighbourUnstickPush = GetNeighbourUnstickPush(hungerPressure);
        }

        lastNeighbourPush = useBoids ? (lastBoidSeparation + lastBoidThreatAvoid) : neighbourUnstickPush;
        Vector3 wanderPull = wanderDirection * WanderStrength * Mathf.Lerp(0.25f, 1f, energyRatio);
        brainDirection *= brainWeight;

        Vector3 noise = Random.insideUnitSphere * SwimNoiseStrength;
        noise.y *= 0.45f;

        Vector3 boundaryPush = GetBoundaryAvoidanceDirection();
        lastBoundaryPush = boundaryPush;

        wantedDirection = brainDirection + foodPull + carrionPull + preyPull + groupingPull + separationPush + neighbourUnstickPush + boidSteering + boundaryPush + wanderPull + noise;
        wantedDirection += GetCloseTargetCorrection(hungerPressure);
        wantedDirection += GetDepthStabilityCorrection();
        StabiliseVerticalMovementWhenCrowded(ref wantedDirection, neighbourUnstickPush);
        PreventOutwardDirectionAtBounds(ref wantedDirection);
        UpdateDebugMovementState(hungerPressure, foodPull, carrionPull, preyPull, separationPush, neighbourUnstickPush, boidSteering, boundaryPush);

        if (wantedDirection.sqrMagnitude < 0.05f)
        {
            wantedDirection = GetDirectionToSimulationCentre();
        }

        wantedDirection.Normalize();
        MoveTowardsWantedDirection(GetApproachSpeedScale());
    }

    private bool IsBoidMovementEnabled()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        return UseBoidMovement && (settings == null || settings.EnableBoidMovement);
    }

    private Vector3 GetBoidSteering(float hungerPressure, float socialWeight)
    {
        lastBoidCohesion = Vector3.zero;
        lastBoidAlignment = Vector3.zero;
        lastBoidSeparation = Vector3.zero;
        lastBoidThreatAvoid = Vector3.zero;
        lastBoidGroupCentre = transform.position;
        lastBoidSameGroupCount = 0;
        lastBoidThreatCount = 0;

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null || EffectiveStats == null)
        {
            return Vector3.zero;
        }

        System.Collections.Generic.List<MarineCreatureAgent> creatures = manager.GetActiveCreatures();
        if (creatures == null || creatures.Count <= 1)
        {
            return Vector3.zero;
        }

        Vector3 ownPosition = rb != null ? rb.position : transform.position;
        float ownSize = Mathf.Max(0.1f, EffectiveStats.BodySize);
        float perceptionRadius = Mathf.Max(0.5f, BoidPerceptionRadius * Mathf.Lerp(0.85f, 1.25f, Candidate.Genome.AttractionRange));
        float separationRadius = Mathf.Max(0.25f, BoidSeparationRadius * ownSize);
        Vector3 cohesionCentre = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 differentGroupSpacing = Vector3.zero;
        Vector3 threatAvoidance = Vector3.zero;
        int sameGroupCount = 0;
        int separationCount = 0;
        int differentGroupCount = 0;
        int threatCount = 0;
        int usedNeighbours = 0;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];
            if (other == null || other == this || other.EffectiveStats == null || other.Candidate == null || other.Candidate.Genome == null)
            {
                continue;
            }

            Vector3 otherPosition = other.transform.position;
            Vector3 offsetFromOther = ownPosition - otherPosition;
            float distance = offsetFromOther.magnitude;
            if (distance <= 0.001f)
            {
                Vector2 randomFlat = Random.insideUnitCircle.normalized;
                if (randomFlat.sqrMagnitude < 0.01f)
                {
                    randomFlat = Vector2.right;
                }

                offsetFromOther = new Vector3(randomFlat.x, 0f, randomFlat.y);
                distance = 0.001f;
            }

            if (distance > perceptionRadius)
            {
                continue;
            }

            bool actualThreat = other.CanAttackPrey(this);
            bool sameGroup = IsSameBoidGroup(other) || IsHarmlessNeighbour(other);
            Vector3 awayDirection = offsetFromOther / distance;

            if (actualThreat)
            {
                Vector3 threatDir = awayDirection;
                if (!IsVerticalAvoidanceEnabled())
                {
                    threatDir.y = 0f;
                }

                if (threatDir.sqrMagnitude <= 0.0001f)
                {
                    threatDir = GetHorizontalAwayFrom(otherPosition);
                }

                float threatStrength = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, perceptionRadius));
                threatAvoidance += threatDir.normalized * threatStrength * threatStrength;
                threatCount++;
                usedNeighbours++;
                if (usedNeighbours >= Mathf.Max(1, BoidMaxNeighbours)) break;
                continue;
            }

            if (sameGroup)
            {
                cohesionCentre += otherPosition;

                Vector3 otherVelocity = other.rb != null ? other.rb.linearVelocity : other.transform.forward;
                if (otherVelocity.sqrMagnitude <= 0.001f)
                {
                    otherVelocity = other.transform.forward;
                }
                alignment += otherVelocity.normalized;

                sameGroupCount++;
            }
            else if (distance < separationRadius * 1.15f)
            {
                Vector3 sideAway = awayDirection;
                sideAway.y *= BoidSeparationVerticalInfluence;
                if (sideAway.sqrMagnitude <= 0.0001f)
                {
                    sideAway = GetHorizontalAwayFrom(otherPosition);
                }
                float diffStrength = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, separationRadius * 1.15f));
                differentGroupSpacing += sideAway.normalized * diffStrength;
                differentGroupCount++;
            }

            if (distance < separationRadius)
            {
                Vector3 sepDirection = awayDirection;
                sepDirection.y *= BoidSeparationVerticalInfluence;
                if (sepDirection.sqrMagnitude <= 0.0001f)
                {
                    sepDirection = GetHorizontalAwayFrom(otherPosition);
                }

                float sepStrength = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, separationRadius));
                separation += sepDirection.normalized * sepStrength * sepStrength;
                separationCount++;
            }

            usedNeighbours++;
            if (usedNeighbours >= Mathf.Max(1, BoidMaxNeighbours))
            {
                break;
            }
        }

        bool hasFoodTarget = GetPrimaryFoodTargetPosition().HasValue;
        float foodPriorityMultiplier = 1f;
        Vector3? target = GetPrimaryFoodTargetPosition();
        if (target.HasValue)
        {
            float targetDistance = Vector3.Distance(GetMouthWorldPosition(), target.Value);
            if (targetDistance < BoidCloseFoodPriorityDistance)
            {
                foodPriorityMultiplier = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(targetDistance / Mathf.Max(0.01f, BoidCloseFoodPriorityDistance)));
            }
        }

        float groupDrive = Mathf.Clamp01(0.35f + Candidate.Genome.GroupingChance + Candidate.Genome.AttractionRange * 0.35f);
        float hungryMultiplier = hasFoodTarget ? Mathf.Lerp(1f, BoidHungryGroupWeightMultiplier, hungerPressure) : 1f;
        float groupWeight = socialWeight * groupDrive * hungryMultiplier * foodPriorityMultiplier;

        if (sameGroupCount > 0)
        {
            cohesionCentre /= sameGroupCount;
            lastBoidGroupCentre = cohesionCentre;

            Vector3 cohesionDirection = cohesionCentre - ownPosition;
            if (!BoidAllowVerticalSchooling)
            {
                cohesionDirection.y = 0f;
            }

            if (cohesionDirection.sqrMagnitude > 0.0001f)
            {
                lastBoidCohesion = cohesionDirection.normalized * BoidCohesionWeight * groupWeight;
            }

            alignment /= sameGroupCount;
            if (!BoidAllowVerticalSchooling)
            {
                alignment.y = 0f;
            }

            if (alignment.sqrMagnitude > 0.0001f)
            {
                lastBoidAlignment = alignment.normalized * BoidAlignmentWeight * groupWeight;
            }
        }

        if (separationCount > 0)
        {
            lastBoidSeparation = separation.normalized * BoidSeparationWeight;
        }

        if (differentGroupCount > 0)
        {
            lastBoidSeparation += differentGroupSpacing.normalized * BoidDifferentGroupSpacingWeight;
        }

        if (threatCount > 0)
        {
            lastBoidThreatAvoid = threatAvoidance.normalized * BoidThreatAvoidWeight * TrueThreatFleeMultiplier;
        }

        lastBoidSameGroupCount = sameGroupCount;
        lastBoidThreatCount = threatCount;
        return lastBoidCohesion + lastBoidAlignment + lastBoidSeparation + lastBoidThreatAvoid;
    }

    private bool IsSameBoidGroup(MarineCreatureAgent other)
    {
        if (other == null || Candidate == null || Candidate.Genome == null || other.Candidate == null || other.Candidate.Genome == null)
        {
            return false;
        }

        if (BaselineSpeciesAreHarmonious && IsHarmlessNeighbour(other))
        {
            return true;
        }

        return CreatureDebugTypeUtility.GetSpeciesGroupName(Candidate.Genome) == CreatureDebugTypeUtility.GetSpeciesGroupName(other.Candidate.Genome);
    }

    private Vector3 GetSocialSeparationPush(Vector3 toCreature, float socialWeight, float hungerPressure)
    {
        Vector3 separationPush = Vector3.zero;
        debugNearestCreatureIsThreat = false;

        if (nearestCreature == null)
        {
            return separationPush;
        }

        float creatureDistance = Vector3.Distance(transform.position, nearestCreature.transform.position);
        float personalSpace = SeparationDistance * EffectiveStats.BodySize;
        bool actualPredatorThreat = nearestCreature.CanAttackPrey(this);
        bool harmless = IsHarmlessNeighbour(nearestCreature);
        debugNearestCreatureIsThreat = actualPredatorThreat;

        if (harmless)
        {
            if (!IsHarmlessNeighbourAvoidanceEnabled())
            {
                return Vector3.zero;
            }

            // Harmless same-baseline creatures should not flee from each other.
            // They only slide sideways if they are genuinely too close.
            if (creatureDistance < personalSpace)
            {
                Vector3 sideways = GetHorizontalAwayFrom(nearestCreature.transform.position);
                float strength = 1f - Mathf.Clamp01(creatureDistance / Mathf.Max(0.01f, personalSpace));

                if (ShouldIgnoreHarmlessNeighbourForFood(nearestCreature, hungerPressure))
                {
                    strength *= FoodCrowdSeparationMultiplier;
                }

                separationPush += sideways * strength * HarmlessNeighbourSlideStrength;
            }

            return separationPush;
        }

        if (!actualPredatorThreat)
        {
            // Different but non-threatening creatures still get a small spacing push, not a panic flee.
            if (creatureDistance < personalSpace)
            {
                Vector3 sideways = GetHorizontalAwayFrom(nearestCreature.transform.position);
                float strength = 1f - Mathf.Clamp01(creatureDistance / Mathf.Max(0.01f, personalSpace));
                separationPush += sideways * strength * socialWeight * 0.65f;
            }

            return separationPush;
        }

        float threatDistance = GetThreatRange();
        if (threatDistance <= 0.01f)
        {
            threatDistance = personalSpace * 2.25f;
        }

        float threatStrength = 1f - Mathf.Clamp01(creatureDistance / Mathf.Max(0.01f, threatDistance));
        Vector3 fleeDirection = -toCreature;

        if (!IsVerticalAvoidanceEnabled())
        {
            fleeDirection.y = 0f;
        }

        if (fleeDirection.sqrMagnitude <= 0.0001f)
        {
            fleeDirection = GetHorizontalAwayFrom(nearestCreature.transform.position);
        }

        fleeDirection.Normalize();
        separationPush += fleeDirection * threatStrength * Candidate.Genome.ThreatRange * (1f - Candidate.Genome.RiskTolerance) * socialWeight * TrueThreatFleeMultiplier;
        return separationPush;
    }

    private Vector3 GetNeighbourUnstickPush(float hungerPressure)
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;

        if (manager == null || EffectiveStats == null)
        {
            return Vector3.zero;
        }

        System.Collections.Generic.List<MarineCreatureAgent> creatures = manager.GetActiveCreatures();

        if (creatures == null || creatures.Count <= 1)
        {
            return Vector3.zero;
        }

        Vector3 push = Vector3.zero;
        Vector3 ownPosition = rb != null ? rb.position : transform.position;
        float ownSize = Mathf.Max(0.1f, EffectiveStats.BodySize);
        int neighboursUsed = 0;
        int maxNeighbours = Mathf.Max(1, MaxNeighboursForUnstick);
        bool hasFoodTarget = GetPrimaryFoodTargetPosition().HasValue;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent other = creatures[i];

            if (other == null || other == this || other.EffectiveStats == null)
            {
                continue;
            }

            Vector3 away = ownPosition - other.transform.position;
            float distance = away.magnitude;
            float otherSize = Mathf.Max(0.1f, other.EffectiveStats.BodySize);
            float wantedSpace = NeighbourUnstickDistance * (ownSize + otherSize) * 0.5f;

            if (distance > wantedSpace)
            {
                continue;
            }

            bool actualThreat = other.CanAttackPrey(this);
            bool harmless = IsHarmlessNeighbour(other);

            if (harmless && !IsHarmlessNeighbourAvoidanceEnabled())
            {
                continue;
            }

            if (distance <= 0.001f)
            {
                Vector2 randomFlat = Random.insideUnitCircle.normalized;
                if (randomFlat.sqrMagnitude < 0.01f)
                {
                    randomFlat = Vector2.right;
                }

                away = new Vector3(randomFlat.x, 0f, randomFlat.y);
                distance = 0.001f;
            }

            Vector3 awayDirection = away / distance;

            if (harmless || !actualThreat || !IsVerticalAvoidanceEnabled())
            {
                // Harmless crowding should make creatures spread around food, not bounce vertically.
                awayDirection.y = 0f;
            }
            else
            {
                awayDirection.y *= VerticalUnstickInfluence;
            }

            if (awayDirection.sqrMagnitude <= 0.0001f)
            {
                awayDirection = GetHorizontalAwayFrom(other.transform.position);
            }

            awayDirection.Normalize();
            float strength = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, wantedSpace));

            if (harmless)
            {
                strength *= HarmlessNeighbourSlideStrength;

                if (hasFoodTarget && hungerPressure >= HungerIgnoreHarmlessNeighbourAt)
                {
                    strength *= FoodCrowdSeparationMultiplier;
                }
            }
            else if (actualThreat)
            {
                strength *= TrueThreatFleeMultiplier;
            }

            push += awayDirection * strength * strength;
            neighboursUsed++;

            if (neighboursUsed >= maxNeighbours)
            {
                break;
            }
        }

        if (push.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        float hungerReduction = hasFoodTarget ? Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(hungerPressure)) : 1f;
        Vector3 result = push.normalized * NeighbourUnstickStrength * hungerReduction;

        if (DebugNeighbourUnstick || ShouldDrawBoundaryDebug())
        {
            Debug.DrawRay(ownPosition, result, Color.cyan, Time.fixedDeltaTime);
        }

        return result;
    }

    private bool IsHarmlessNeighbour(MarineCreatureAgent other)
    {
        if (other == null || other == this || Candidate == null || Candidate.Genome == null || other.Candidate == null || other.Candidate.Genome == null)
        {
            return true;
        }

        if (other.CanAttackPrey(this) || CanAttackPrey(other))
        {
            return false;
        }

        bool ownLowAggression = Candidate.Genome.Aggression <= HarmlessAggressionLimit && Candidate.Genome.MeatDiet <= HarmlessMeatDietLimit;
        bool otherLowAggression = other.Candidate.Genome.Aggression <= HarmlessAggressionLimit && other.Candidate.Genome.MeatDiet <= HarmlessMeatDietLimit;

        if (BaselineSpeciesAreHarmonious && ownLowAggression && otherLowAggression)
        {
            return true;
        }

        bool sameRole = Candidate.BehaviourType == other.Candidate.BehaviourType;
        bool sameMorph = CreatureDebugTypeUtility.GetMorphologyName(Candidate.Genome) == CreatureDebugTypeUtility.GetMorphologyName(other.Candidate.Genome);
        return sameRole && sameMorph && ownLowAggression && otherLowAggression;
    }

    private bool ShouldIgnoreHarmlessNeighbourForFood(MarineCreatureAgent other, float hungerPressure)
    {
        if (other == null || !IsHarmlessNeighbour(other))
        {
            return false;
        }

        if (hungerPressure < HungerIgnoreHarmlessNeighbourAt)
        {
            return false;
        }

        return GetPrimaryFoodTargetPosition().HasValue;
    }

    private bool IsHarmlessNeighbourAvoidanceEnabled()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        return settings == null || settings.EnableHarmlessNeighbourAvoidance;
    }

    private bool IsVerticalAvoidanceEnabled()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        return settings == null || settings.EnableVerticalAvoidance;
    }

    private bool IsPredationDisabledByDebug()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        return settings != null && settings.DisablePredationForDebug;
    }

    private Vector3 GetHorizontalAwayFrom(Vector3 otherPosition)
    {
        Vector3 away = transform.position - otherPosition;
        away.y = 0f;

        if (away.sqrMagnitude <= 0.0001f)
        {
            Vector3 right = transform.right;
            right.y = 0f;

            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
            }

            away = right;
        }

        return away.normalized;
    }

    private void UpdateDebugMovementState(float hungerPressure, Vector3 foodPull, Vector3 carrionPull, Vector3 preyPull, Vector3 separationPush, Vector3 neighbourUnstickPush, Vector3 boidSteering, Vector3 boundaryPush)
    {
        if (nearestPrey != null && preyPull.sqrMagnitude > 0.001f)
        {
            debugMoveState = "Hunting";
        }
        else if (nearestCarrion != null && carrionPull.sqrMagnitude > 0.001f)
        {
            debugMoveState = "Carrion";
        }
        else if (nearestFood != null && foodPull.sqrMagnitude > 0.001f)
        {
            debugMoveState = "Food";
        }
        else if (debugNearestCreatureIsThreat && separationPush.sqrMagnitude > 0.001f)
        {
            debugMoveState = "Fleeing";
        }
        else if (boidSteering.sqrMagnitude > 0.001f && lastBoidThreatCount > 0)
        {
            debugMoveState = "Boid avoid";
        }
        else if (boidSteering.sqrMagnitude > 0.001f && lastBoidSameGroupCount > 0)
        {
            debugMoveState = "Schooling";
        }
        else if (neighbourUnstickPush.sqrMagnitude > 0.001f)
        {
            debugMoveState = "Sliding";
        }
        else if (boundaryPush.sqrMagnitude > 0.001f)
        {
            debugMoveState = "Bounds";
        }
        else
        {
            debugMoveState = hungerPressure > 0.55f ? "Searching" : "Wander";
        }

        Vector3? target = GetPrimaryFoodTargetPosition();
        if (Mathf.Abs(wantedDirection.y) < 0.04f)
        {
            debugVerticalReason = "Flat";
        }
        else if (target.HasValue && Mathf.Abs(target.Value.y - transform.position.y) > 0.75f)
        {
            debugVerticalReason = "Target depth";
        }
        else if (boundaryPush.sqrMagnitude > 0.001f && Mathf.Abs(boundaryPush.y) > 0.05f)
        {
            debugVerticalReason = "Bounds";
        }
        else if (debugNearestCreatureIsThreat && separationPush.sqrMagnitude > 0.001f && Mathf.Abs(separationPush.y) > 0.05f)
        {
            debugVerticalReason = "Threat";
        }
        else if (boidSteering.sqrMagnitude > 0.001f && Mathf.Abs(boidSteering.y) > 0.05f)
        {
            debugVerticalReason = "Boids";
        }
        else if (neighbourUnstickPush.sqrMagnitude > 0.001f && Mathf.Abs(neighbourUnstickPush.y) > 0.05f)
        {
            debugVerticalReason = "Neighbour";
        }
        else
        {
            debugVerticalReason = "Swim/brain";
        }
    }

    private Vector3 GetDepthStabilityCorrection()
    {
        if (EvolutionEcosystemManager.Instance == null || DepthStabilityStrength <= 0f)
        {
            return Vector3.zero;
        }

        Vector3? target = GetPrimaryFoodTargetPosition();

        if (target.HasValue)
        {
            float verticalTargetDistance = Mathf.Abs(target.Value.y - transform.position.y);

            if (verticalTargetDistance > 1.2f)
            {
                return Vector3.zero;
            }
        }

        float centreY = EvolutionEcosystemManager.Instance.transform.position.y;
        float halfHeight = Mathf.Max(0.1f, EvolutionEcosystemManager.Instance.SimulationAreaSize.y * 0.5f);
        float offset = Mathf.Clamp((centreY - transform.position.y) / halfHeight, -1f, 1f);

        return Vector3.up * offset * DepthStabilityStrength;
    }

    private void StabiliseVerticalMovementWhenCrowded(ref Vector3 direction, Vector3 neighbourUnstickPush)
    {
        if (direction.sqrMagnitude <= 0.0001f || neighbourUnstickPush.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3? target = GetPrimaryFoodTargetPosition();
        bool hasImportantVerticalTarget = false;

        if (target.HasValue)
        {
            hasImportantVerticalTarget = Mathf.Abs(target.Value.y - transform.position.y) > 1.2f;
        }

        if (hasImportantVerticalTarget)
        {
            return;
        }

        direction.y *= Mathf.Clamp01(1f - VerticalDampingWhenCrowded);
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
        if (manager == null || !manager.EnablePredation || IsPredationDisabledByDebug())
        {
            return false;
        }

        float energyRatio = Mathf.Clamp01(CurrentEnergy / Mathf.Max(0.01f, EffectiveStats.EnergyCapacity));
        bool hasNormalPredatorTraits = Candidate.Genome.MeatDiet >= manager.MinimumMeatDietToHunt && Candidate.Genome.Aggression >= manager.MinimumAggressionToHunt;
        bool hasStarvingPredatorTraits = energyRatio <= StarvingHuntEnergyRatio &&
                                       Candidate.Genome.MeatDiet >= Mathf.Max(manager.MinimumMeatDietToHunt, StarvingHuntMeatDietLimit) &&
                                       Candidate.Genome.Aggression >= StarvingHuntAggressionLimit;

        if (!hasNormalPredatorTraits && !hasStarvingPredatorTraits)
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
        float sizeDanger = Mathf.Max(0f, preySize / Mathf.Max(0.1f, ownSize) - 1f) * 0.75f;
        float preyDanger = (prey.EffectiveStats != null ? prey.EffectiveStats.DangerFactor : prey.Candidate.Genome.DangerFactor) + sizeDanger;
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

        if (settings == null || settings.DrawBoidRays)
        {
            if (lastBoidCohesion.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastBoidCohesion, new Color(0.1f, 0.9f, 1f), duration);
            if (lastBoidAlignment.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastBoidAlignment, new Color(0.55f, 0.75f, 1f), duration);
            if (lastBoidSeparation.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastBoidSeparation, Color.cyan, duration);
            if (lastBoidThreatAvoid.sqrMagnitude > 0.001f) Debug.DrawRay(transform.position, lastBoidThreatAvoid, new Color(1f, 0.35f, 0.1f), duration);

            if (settings != null && settings.DrawBoidGroupCentreRays && lastBoidSameGroupCount > 0)
            {
                Debug.DrawLine(transform.position, lastBoidGroupCentre, new Color(0.25f, 1f, 1f), duration);
            }
        }

        if ((settings == null || settings.DrawBoundaryPush) && lastBoundaryPush.sqrMagnitude > 0.001f)
        {
            Debug.DrawRay(transform.position, lastBoundaryPush, Color.yellow, duration);
        }

        if (lastNeighbourPush.sqrMagnitude > 0.001f)
        {
            Debug.DrawRay(transform.position, lastNeighbourPush, Color.cyan, duration);
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
               " | State " + debugMoveState +
               " | Boids G/T " + lastBoidSameGroupCount + "/" + lastBoidThreatCount +
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
            label += "\n" + debugMoveState;
            if (settings == null || settings.ShowBoidStateInLabels)
            {
                label += " G" + lastBoidSameGroupCount;
            }
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
