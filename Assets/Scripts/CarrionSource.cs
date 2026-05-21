using UnityEngine;

public class CarrionSource : MonoBehaviour
{
    [Header("Carrion")]
    public float EnergyValue = 45f;
    public float DecayTime = 50f;
    public float Age;

    public bool IsConsumed { get; private set; }

    private void Update()
    {
        Age += Time.deltaTime;

        if (Age >= DecayTime)
        {
            RemoveWithoutEnergy();
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
