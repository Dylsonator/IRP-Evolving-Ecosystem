using UnityEngine;

public enum EcosystemSeason
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public class SeasonalEnvironment : MonoBehaviour
{
    [Header("Season Settings")]
    public EcosystemSeason CurrentSeason = EcosystemSeason.Spring;
    public float SeasonLength = 90f;

    [Header("Runtime Multipliers")]
    public float PlantFoodSpawnMultiplier = 1f;
    public float EnergyDrainMultiplier = 1f;
    public float MutationMultiplier = 1f;
    public float MeatRotSpeedMultiplier = 1f;

    private float seasonTimer;

    private void Start()
    {
        ApplySeasonSettings();
    }

    private void Update()
    {
        seasonTimer += Time.deltaTime;

        if (seasonTimer >= SeasonLength)
        {
            seasonTimer = 0f;
            MoveToNextSeason();
        }
    }

    private void MoveToNextSeason()
    {
        CurrentSeason++;

        if ((int)CurrentSeason > 3)
        {
            CurrentSeason = EcosystemSeason.Spring;
        }

        ApplySeasonSettings();
        Debug.Log("Season changed to: " + CurrentSeason);
    }

    private void ApplySeasonSettings()
    {
        switch (CurrentSeason)
        {
            case EcosystemSeason.Spring:
                PlantFoodSpawnMultiplier = 1.45f;
                EnergyDrainMultiplier = 0.9f;
                MutationMultiplier = 1f;
                MeatRotSpeedMultiplier = 1.1f;
                break;

            case EcosystemSeason.Summer:
                PlantFoodSpawnMultiplier = 1.1f;
                EnergyDrainMultiplier = 1f;
                MutationMultiplier = 1f;
                MeatRotSpeedMultiplier = 1.25f;
                break;

            case EcosystemSeason.Autumn:
                PlantFoodSpawnMultiplier = 0.8f;
                EnergyDrainMultiplier = 1.1f;
                MutationMultiplier = 1.1f;
                MeatRotSpeedMultiplier = 1f;
                break;

            case EcosystemSeason.Winter:
                PlantFoodSpawnMultiplier = 0.45f;
                EnergyDrainMultiplier = 1.35f;
                MutationMultiplier = 1.25f;
                MeatRotSpeedMultiplier = 0.75f;
                break;
        }
    }
}
