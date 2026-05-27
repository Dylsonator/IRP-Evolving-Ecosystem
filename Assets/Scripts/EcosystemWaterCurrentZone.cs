using System.Collections.Generic;
using UnityEngine;

// Current zone that pushes fish and adds pressure for adaptation tests.
public enum EcosystemCurrentType
{
    GentleCurrent,
    ColdCurrent,
    WarmCurrent,
    HarshCurrent
}

public class EcosystemWaterCurrentZone : MonoBehaviour
{
    [Header("Current Type")]
    public EcosystemCurrentType CurrentType = EcosystemCurrentType.GentleCurrent;

    [Header("Current Movement")]
    public Vector3 LocalFlowDirection = Vector3.forward;
    public float FlowStrength = 1.5f;
    public float AgainstCurrentDifficulty = 0.45f;

    [Header("Pressure")]
    public float EnergyDrainPressure = 0.05f;
    public float HealthPressure = 0f;
    public float MutationPressure = 1f;
    public float AvoidanceMemoryStrength = 0.35f;

    [Header("Shape")]
    public bool UseChildTriggerVolumes = true;
    public float RadiusFallback = 12f;
    public float EdgeFadeDistance = 3f;

    [Header("Drift")]
    public bool Drifts = false;
    public bool UseSimulationBounds = true;
    public float DriftSpeed = 1.5f;
    public float NewDriftTargetInterval = 35f;
    public float DriftHeight01 = 0.55f;

    private readonly List<Collider> triggerVolumes = new List<Collider>();
    private Vector3 driftTarget;
    private float driftTimer;

    // Registers this object with the ecosystem when Unity enables it
    private void OnEnable()
    {
        RefreshVolumes();
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterCurrentZone(this);
        }
        PickNewDriftTarget();
    }

    // Unregisters this object so the manager does not keep old references
    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterCurrentZone(this);
        }
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        if (!Drifts)
        {
            return;
        }

        driftTimer -= Time.deltaTime;
        if (driftTimer <= 0f || Vector3.Distance(transform.position, driftTarget) < 1.5f)
        {
            PickNewDriftTarget();
        }

        transform.position = Vector3.MoveTowards(transform.position, driftTarget, Mathf.Max(0f, DriftSpeed) * Time.deltaTime);
    }

    // Refreshes volumes.
    [ContextMenu("Refresh Current Volumes")]
    // Handles refresh volumes
    public void RefreshVolumes()
    {
        triggerVolumes.Clear();
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null)
            {
                continue;
            }

            c.isTrigger = true;
            triggerVolumes.Add(c);
        }
    }

    // Gets the influence01 used by the sim
    public float GetInfluence01(Vector3 worldPosition)
    {
        if (UseChildTriggerVolumes && triggerVolumes.Count > 0)
        {
            float best = 0f;
            for (int i = 0; i < triggerVolumes.Count; i++)
            {
                Collider volume = triggerVolumes[i];
                if (volume == null)
                {
                    continue;
                }

                Vector3 closest = volume.ClosestPoint(worldPosition);
                float distance = Vector3.Distance(worldPosition, closest);
                if (distance <= 0.01f || volume.bounds.Contains(worldPosition))
                {
                    best = Mathf.Max(best, 1f);
                    continue;
                }

                float fade = Mathf.Clamp01(1f - distance / Mathf.Max(0.1f, EdgeFadeDistance));
                best = Mathf.Max(best, fade);
            }
            return best;
        }

        float radius = Mathf.Max(0.1f, RadiusFallback);
        float d = Vector3.Distance(transform.position, worldPosition);
        return Mathf.Clamp01(1f - d / radius);
    }

    // Gets the current velocity used by the sim
    public Vector3 GetCurrentVelocity(Vector3 worldPosition)
    {
        float influence = GetInfluence01(worldPosition);
        if (influence <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 direction = transform.TransformDirection(LocalFlowDirection);
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
        }

        return direction.normalized * FlowStrength * influence;
    }

    // Gets the resistance against used by the sim
    public float GetResistanceAgainst(Vector3 movementDirection, Vector3 worldPosition)
    {
        float influence = GetInfluence01(worldPosition);
        if (influence <= 0f || movementDirection.sqrMagnitude <= 0.001f)
        {
            return 0f;
        }

        Vector3 current = GetCurrentVelocity(worldPosition);
        if (current.sqrMagnitude <= 0.001f)
        {
            return 0f;
        }

        float against = Mathf.Clamp01(Vector3.Dot(movementDirection.normalized, -current.normalized));
        return against * AgainstCurrentDifficulty * influence;
    }

    // Handles pick new drift target
    private void PickNewDriftTarget()
    {
        driftTimer = Mathf.Max(3f, NewDriftTargetInterval) * Random.Range(0.75f, 1.25f);
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (!UseSimulationBounds || manager == null)
        {
            driftTarget = transform.position + Random.insideUnitSphere * 12f;
            return;
        }

        driftTarget = manager.GetRandomPointInSimulationArea();
        Vector3 half = manager.SimulationAreaSize * 0.5f;
        float minY = manager.transform.position.y - half.y;
        float maxY = manager.transform.position.y + half.y;
        driftTarget.y = Mathf.Lerp(minY, maxY, Mathf.Clamp01(DriftHeight01));
        driftTarget = manager.ClampToSimulationArea(driftTarget);
    }

    // Draws selected-only gizmos so setup can be checked without clutter
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = CurrentType == EcosystemCurrentType.HarshCurrent ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, RadiusFallback);
        Vector3 direction = transform.TransformDirection(LocalFlowDirection);
        if (direction.sqrMagnitude > 0.001f)
        {
            Gizmos.DrawRay(transform.position, direction.normalized * 5f);
        }
    }
}
