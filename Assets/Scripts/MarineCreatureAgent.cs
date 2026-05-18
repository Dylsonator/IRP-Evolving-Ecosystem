using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MarineCreatureAgent : MonoBehaviour
{
    private const int CircleSegments = 32;
    private static int labelFrame = -1;
    private static int labelsDrawnThisFrame;

    [Header("Runtime")]
    public EvolutionCandidate Candidate;
    public float CurrentEnergy;
    public string DebugName;
    public CreatureBehaviourType DebugBehaviourType;

    [Header("Energy")]
    public float BaseEnergyDrainPerSecond = 3.5f;
    public float ReproductionCooldown = 5f;

    [Header("Movement")]
    public float FoodEatDistance = 1.25f;
    public float SeparationDistance = 2f;
    public float SwimNoiseStrength = 0.15f;

    [Header("Mouth / Eating Area")]
    public bool UseMouthBasedEating = true;
    public float MouthForwardOffset = 0.75f;
    public float MouthRadius = 0.45f;
    [Range(5f, 180f)] public float MouthAngle = 100f;

    [Header("Boundary Safety")]
    public float BoundaryAvoidanceDistance = 6f;
    public float BoundaryAvoidanceStrength = 5f;
    public float BoundaryHardStopMargin = 0.35f;
    public float BoundaryVelocityDamping = 0.15f;
    public bool DebugBoundaryAvoidance;

    [Header("Debug Visuals")]
    public bool ApplyTypeColour = true;
    public bool LocalDebugRays;
    public bool LocalDebugLabels;

    private Rigidbody rb;
    private FoodSource nearestFood;
    private MarineCreatureAgent nearestCreature;
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock materialBlock;

    private float reproductionTimer;
    private Vector3 lastPosition;
    private Vector3 wantedDirection;
    private Vector3 lastFoodDirection;
    private Vector3 lastCreatureDirection;
    private Vector3 lastBoundaryPush;
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
        Candidate.RefreshDebugIdentity();

        DebugName = Candidate.DisplayName;
        DebugBehaviourType = Candidate.BehaviourType;
        gameObject.name = "Creature_" + DebugName;

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

        CacheRenderers();
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
        DrawRuntimeDebugRays();

        if (CurrentEnergy <= 0f)
        {
            Die(false);
        }
    }

    private void CacheRenderers()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>();
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

        lastFoodDirection = toFood;
        lastCreatureDirection = toCreature;

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

        // Social behaviour is useful, but it should not override survival when the creature is starving.
        float socialWeight = Mathf.Lerp(0.25f, 1f, energyRatio);
        Vector3 groupingPull = toCreature * Candidate.Genome.GroupingChance * Candidate.Genome.AttractionRange * socialWeight;

        Vector3 separationPush = Vector3.zero;
        if (nearestCreature != null)
        {
            float creatureDistance = Vector3.Distance(transform.position, nearestCreature.transform.position);
            float personalSpace = SeparationDistance * Candidate.Genome.BodySize;
            float threatDistance = GetThreatRange();

            if (creatureDistance < personalSpace)
            {
                separationPush += -toCreature * (1f - Candidate.Genome.RiskTolerance) * socialWeight;
            }

            if (threatDistance > 0.01f && creatureDistance < threatDistance)
            {
                float threatStrength = 1f - Mathf.Clamp01(creatureDistance / threatDistance);
                separationPush += -toCreature * threatStrength * Candidate.Genome.ThreatRange * (1f - Candidate.Genome.RiskTolerance) * socialWeight;
            }
        }

        Vector3 noise = Random.insideUnitSphere * SwimNoiseStrength;
        noise.y = 0f;

        Vector3 boundaryPush = GetBoundaryAvoidanceDirection();
        lastBoundaryPush = boundaryPush;

        wantedDirection = brainDirection + foodPull + groupingPull + separationPush + boundaryPush + noise;
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
            Candidate.Genome.TurnRate * Time.fixedDeltaTime
        );

        rb.MoveRotation(newRotation);

        Vector3 wantedVelocity = transform.forward * Candidate.Genome.Speed;
        Vector3 newVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            wantedVelocity,
            Candidate.Genome.Acceleration * Time.fixedDeltaTime
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
        if (BoundaryAvoidanceDistance <= 0f)
        {
            return;
        }

        if (distanceToEdge > BoundaryAvoidanceDistance)
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

        if (UseMouthBasedEating)
        {
            if (!IsFoodInsideMouthArea(nearestFood.transform.position))
            {
                return;
            }
        }
        else
        {
            float eatDistance = FoodEatDistance * Candidate.Genome.BodySize;

            if (Vector3.Distance(transform.position, nearestFood.transform.position) > eatDistance)
            {
                return;
            }
        }

        float energyGained = nearestFood.Consume();

        CurrentEnergy = Mathf.Min(
            CurrentEnergy + energyGained,
            Candidate.Genome.EnergyCapacity
        );

        Candidate.EnergyGained += energyGained;
        Candidate.FoodEaten++;
    }

    private bool IsFoodInsideMouthArea(Vector3 foodPosition)
    {
        Vector3 mouthPosition = GetMouthWorldPosition();
        Vector3 toFood = foodPosition - mouthPosition;
        float scaledMouthRadius = GetScaledMouthRadius();

        if (toFood.sqrMagnitude > scaledMouthRadius * scaledMouthRadius)
        {
            return false;
        }

        if (toFood.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        float dot = Vector3.Dot(transform.forward, toFood.normalized);
        float requiredDot = Mathf.Cos((MouthAngle * 0.5f) * Mathf.Deg2Rad);

        return dot >= requiredDot;
    }

    public Vector3 GetMouthWorldPosition()
    {
        float bodyScale = Candidate != null && Candidate.Genome != null ? Candidate.Genome.BodySize : transform.localScale.x;
        return transform.position + transform.forward * MouthForwardOffset * bodyScale;
    }

    public float GetScaledMouthRadius()
    {
        float bodyScale = Candidate != null && Candidate.Genome != null ? Candidate.Genome.BodySize : transform.localScale.x;
        return Mathf.Max(0.05f, MouthRadius * bodyScale);
    }

    public float GetThreatRange()
    {
        if (Candidate == null || Candidate.Genome == null)
        {
            return 0f;
        }

        return Candidate.Genome.ThreatRange * Candidate.Genome.VisionRange;
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
            Debug.DrawLine(transform.position, nearestFood.transform.position, Color.green, duration);
            Debug.DrawRay(transform.position, lastFoodDirection * 3f, Color.green, duration);
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
        if (Candidate == null || Candidate.Genome == null)
        {
            return "Uninitialised creature";
        }

        return DebugName +
               " | Energy " + CurrentEnergy.ToString("F0") + "/" + Candidate.Genome.EnergyCapacity.ToString("F0") +
               " | Speed " + Candidate.Genome.Speed.ToString("F1") +
               " | Vision " + Candidate.Genome.VisionRange.ToString("F1") +
               " | Mouth " + GetScaledMouthRadius().ToString("F2") +
               " | Threat " + GetThreatRange().ToString("F1");
    }

    public void Die(bool causedByExtinctionEvent)
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterCreature(this);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        bool selectedStyle = false;
        DrawGizmosInternal(selectedStyle);
    }

    private void OnDrawGizmosSelected()
    {
        bool selectedStyle = true;
        DrawGizmosInternal(selectedStyle);
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

        if (drawVision)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, selectedStyle ? 0.8f : 0.25f);
            Gizmos.DrawWireSphere(transform.position, Candidate.Genome.VisionRange);

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
    }

    private void OnGUI()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;

        if (!(LocalDebugLabels || (settings != null && settings.ShowCreatureLabels)))
        {
            return;
        }

        if (Candidate == null || Candidate.Genome == null)
        {
            return;
        }

        if (Camera.main == null)
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

        Vector3 worldPoint = transform.position + Vector3.up * Candidate.Genome.BodySize * 1.4f;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);

        if (screenPoint.z <= 0f)
        {
            return;
        }

        Vector2 offset = settings != null ? settings.LabelOffset : new Vector2(0f, -14f);
        Rect rect = new Rect(screenPoint.x - 70f + offset.x, Screen.height - screenPoint.y + offset.y, 140f, 40f);

        Color oldColour = GUI.color;
        GUI.color = CreatureDebugTypeUtility.GetTypeColour(DebugBehaviourType);
        GUI.Label(rect, DebugName + "\nE " + CurrentEnergy.ToString("F0"));
        GUI.color = oldColour;

        labelsDrawnThisFrame++;
    }
}
