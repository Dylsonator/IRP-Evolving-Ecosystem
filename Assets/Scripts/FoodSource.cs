using UnityEngine;

public enum EcosystemFoodType
{
    Plant,
    FreshMeat,
    RottenMeat
}

public class FoodSource : MonoBehaviour
{
    [Header("Food")]
    public EcosystemFoodType FoodType = EcosystemFoodType.Plant;
    public float EnergyValue = 25f;

    [Header("Rotting")]
    public bool CanRot = true;
    public float FreshMeatRotTime = 25f;
    public float RottenMeatDespawnTime = 55f;

    public bool IsConsumed { get; private set; }

    private float age;
    private Renderer cachedRenderer;

    private void Awake()
    {
        cachedRenderer = GetComponentInChildren<Renderer>();
        ApplyVisuals();
    }

    private void Update()
    {
        if (IsConsumed || !CanRot)
        {
            return;
        }

        age += Time.deltaTime;

        if (FoodType == EcosystemFoodType.FreshMeat && age >= FreshMeatRotTime)
        {
            RotIntoCarrion();
            return;
        }

        if (FoodType == EcosystemFoodType.RottenMeat && age >= RottenMeatDespawnTime)
        {
            RemoveWithoutEating();
        }
    }

    public void Initialise(EcosystemFoodType type, float energyValue)
    {
        FoodType = type;
        EnergyValue = energyValue;
        age = 0f;
        ApplyVisuals();
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

    private void RotIntoCarrion()
    {
        FoodType = EcosystemFoodType.RottenMeat;
        EnergyValue *= 0.65f;
        age = 0f;
        ApplyVisuals();

        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterFoodRotted();
        }
    }

    private void RemoveWithoutEating()
    {
        IsConsumed = true;

        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterFood(this);
        }

        Destroy(gameObject);
    }

    private void ApplyVisuals()
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponentInChildren<Renderer>();
        }

        if (cachedRenderer == null)
        {
            return;
        }

        if (FoodType == EcosystemFoodType.Plant)
        {
            cachedRenderer.material.color = new Color(0.2f, 0.85f, 0.35f);
        }
        else if (FoodType == EcosystemFoodType.FreshMeat)
        {
            cachedRenderer.material.color = new Color(0.85f, 0.15f, 0.15f);
        }
        else
        {
            cachedRenderer.material.color = new Color(0.45f, 0.35f, 0.55f);
        }
    }
}
