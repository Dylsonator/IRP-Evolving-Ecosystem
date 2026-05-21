using UnityEngine;

public class CarrionSource : MonoBehaviour
{
    [Header("Carrion")]
    public float EnergyValue = 48f;
    public float DecayTime = 55f;
    public float Age;

    [Header("Physics Safety")]
    public bool DisablePhysicalColliders = true;

    public bool IsConsumed { get; private set; }

    private void Awake()
    {
        if (DisablePhysicalColliders)
        {
            DisableBlockingPhysics();
        }
    }

    private void Update()
    {
        Age += Time.deltaTime;

        if (Age >= DecayTime)
        {
            RemoveWithoutEnergy();
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
            EvolutionEcosystemManager.Instance.UnregisterCarrion(this);
        }

        Destroy(gameObject);
        return EnergyValue;
    }

    private void RemoveWithoutEnergy()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterCarrion(this);
        }

        Destroy(gameObject);
    }
}
