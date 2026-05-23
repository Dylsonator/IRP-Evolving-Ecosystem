using UnityEngine;

public class FoodSource : MonoBehaviour
{
    [Header("Food Mass")]
    [Tooltip("Total usable food mass/energy. Fish now nibble from this instead of deleting the whole object in one bite.")]
    public float EnergyValue = 44f;
    public float MaxMass = 44f;
    public float RemainingMass = -1f;
    public float MinimumVisibleScale = 0.18f;

    [Header("Physics Safety")]
    [Tooltip("Recommended on. If food has a solid collider, creatures can physically get stuck while their mouth check fails.")]
    public bool DisablePhysicalColliders = true;

    public bool IsConsumed { get; private set; }

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
        if (MaxMass <= 0f)
        {
            MaxMass = Mathf.Max(1f, EnergyValue);
        }

        if (RemainingMass < 0f)
        {
            RemainingMass = MaxMass;
        }

        if (DisablePhysicalColliders)
        {
            DisableBlockingPhysics();
        }

        UpdateVisualScale();
    }

    private void DisableBlockingPhysics()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.useGravity = false;
            body.isKinematic = true;
        }
    }

    public float ConsumeBite(float requestedMass)
    {
        if (IsConsumed || requestedMass <= 0f)
        {
            return 0f;
        }

        float eaten = Mathf.Min(requestedMass, RemainingMass);
        RemainingMass -= eaten;
        EnergyValue = RemainingMass;

        if (RemainingMass <= 0.01f)
        {
            ConsumeFully();
        }
        else
        {
            UpdateVisualScale();
        }

        return eaten;
    }

    public float Consume()
    {
        return ConsumeBite(RemainingMass > 0f ? RemainingMass : EnergyValue);
    }

    private void ConsumeFully()
    {
        if (IsConsumed)
        {
            return;
        }

        IsConsumed = true;

        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterFood(this);
        }

        Destroy(gameObject);
    }

    private void UpdateVisualScale()
    {
        float ratio = MaxMass > 0f ? Mathf.Clamp01(RemainingMass / MaxMass) : 0f;
        float scale = Mathf.Lerp(MinimumVisibleScale, 1f, Mathf.Pow(ratio, 1f / 3f));
        transform.localScale = initialScale * scale;
    }
}
