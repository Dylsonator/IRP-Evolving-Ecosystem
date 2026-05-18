using UnityEngine;

public class FoodSource : MonoBehaviour
{
    [Header("Food")]
    public float EnergyValue = 25f;

    public bool IsConsumed { get; private set; }

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
