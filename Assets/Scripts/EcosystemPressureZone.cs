using UnityEngine;

// Local pressure zone for things like cold current, scarcity or mutation pressure.
public enum EcosystemPressureZoneType
{
    ColdCurrent,
    WarmBloom,
    DeadZone,
    MutationHotspot,
    PredatorWake
}

// Local pressure zone used by fish/manager for energy, nesting and mutation pressure.
public class EcosystemPressureZone : MonoBehaviour
{
    [Header("Identity")]
    public EcosystemPressureZoneType ZoneType = EcosystemPressureZoneType.ColdCurrent;

    [Header("Shape")]
    public float Radius = 12f;
    public float EdgeFadeDistance = 4f;

    [Header("Pressure")]
    public float EnergyDrainMultiplier = 1.25f;
    public float HealthDamagePerSecond;
    public float FoodOpportunityMultiplier = 1f;
    public float MutationMultiplier = 1.1f;
    public float StressScore = 0.25f;

    [Header("Lifetime / Drift")]
    public bool Temporary = true;
    public float Lifetime = 65f;
    public bool Drift;
    public Vector3 DriftDirection = Vector3.forward;
    public float DriftSpeed = 0.8f;

    private float age;

    // Registers this object with the ecosystem when Unity enables it
    private void OnEnable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterPressureZone(this);
        }
    }

    // Unregisters this object so the manager does not keep old references
    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterPressureZone(this);
        }
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        if (Drift && DriftDirection.sqrMagnitude > 0.001f)
        {
            transform.position += DriftDirection.normalized * Mathf.Max(0f, DriftSpeed) * Time.deltaTime;
            if (EvolutionEcosystemManager.Instance != null)
            {
                transform.position = EvolutionEcosystemManager.Instance.ClampToSimulationArea(transform.position);
            }
        }

        if (!Temporary)
        {
            return;
        }

        age += Time.deltaTime;
        if (age >= Lifetime)
        {
            Destroy(gameObject);
        }
    }

    // Gets the influence01 used by the sim
    public float GetInfluence01(Vector3 position)
    {
        float radius = Mathf.Max(0.1f, Radius);
        float distance = Vector3.Distance(transform.position, position);
        if (distance > radius)
        {
            return 0f;
        }

        float fadeStart = Mathf.Max(0.01f, radius - Mathf.Max(0.01f, EdgeFadeDistance));
        if (distance <= fadeStart)
        {
            return 1f;
        }

        return 1f - Mathf.Clamp01((distance - fadeStart) / Mathf.Max(0.01f, radius - fadeStart));
    }

    // Handles configure for type
    public void ConfigureForType(EcosystemPressureZoneType type)
    {
        ZoneType = type;

        switch (type)
        {
            case EcosystemPressureZoneType.ColdCurrent:
                EnergyDrainMultiplier = 1.28f;
                HealthDamagePerSecond = 0f;
                FoodOpportunityMultiplier = 0.85f;
                MutationMultiplier = 1.12f;
                StressScore = 0.32f;
                Drift = true;
                break;

            case EcosystemPressureZoneType.WarmBloom:
                EnergyDrainMultiplier = 0.88f;
                HealthDamagePerSecond = 0f;
                FoodOpportunityMultiplier = 1.35f;
                MutationMultiplier = 1.02f;
                StressScore = 0.05f;
                Drift = true;
                break;

            case EcosystemPressureZoneType.DeadZone:
                EnergyDrainMultiplier = 1.18f;
                HealthDamagePerSecond = 1.8f;
                FoodOpportunityMultiplier = 0.55f;
                MutationMultiplier = 1.18f;
                StressScore = 0.55f;
                Drift = false;
                break;

            case EcosystemPressureZoneType.MutationHotspot:
                EnergyDrainMultiplier = 1.05f;
                HealthDamagePerSecond = 0.25f;
                FoodOpportunityMultiplier = 1f;
                MutationMultiplier = 1.45f;
                StressScore = 0.42f;
                Drift = true;
                break;

            case EcosystemPressureZoneType.PredatorWake:
                EnergyDrainMultiplier = 1.08f;
                HealthDamagePerSecond = 0f;
                FoodOpportunityMultiplier = 1f;
                MutationMultiplier = 1.08f;
                StressScore = 0.25f;
                Drift = true;
                break;
        }
    }

    // Draws selected-only gizmos so setup can be checked without clutter
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, Radius));
    }
}
