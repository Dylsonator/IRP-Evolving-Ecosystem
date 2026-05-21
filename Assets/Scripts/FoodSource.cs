using UnityEngine;

public class FoodSource : MonoBehaviour
{
    [Header("Food")]
    public float EnergyValue = 44f;

    [Header("Physics Safety")]
    [Tooltip("Recommended on. If food has a solid collider, creatures can physically get stuck while their mouth check fails.")]
    public bool DisablePhysicalColliders = true;

    public bool IsConsumed { get; private set; }

    private void Awake()
    {
        if (DisablePhysicalColliders)
        {
            DisableBlockingPhysics();
        }
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

    public float Consume()
    {
        if (IsConsumed)
        {
            return 0f;
        }

        IsConsumed = true;

        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterFood(this);
        }

        Destroy(gameObject);
        return EnergyValue;
    }
}
