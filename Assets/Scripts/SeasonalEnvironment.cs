using UnityEngine;

// Applies global seasonal pressure such as food availability, energy drain and mutation pressure.
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
    public float SeasonLength = 60f;
    public bool AutoCycleSeasons = true;

    [Header("Current Pressure Values")]
    public float FoodSpawnMultiplier = 1f;
    public float EnergyDrainMultiplier = 1f;
    public float MutationMultiplier = 1f;

    private float seasonTimer;

    // Starts the setup that needs other scene objects to already exist
    private void Start()
    {
        ApplySeasonSettings();
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        if (!AutoCycleSeasons)
        {
            return;
        }

        seasonTimer += Time.deltaTime;

        if (seasonTimer >= SeasonLength)
        {
            seasonTimer = 0f;
            MoveToNextSeason();
        }
    }

    // Moves to the next season and reapplies its pressure values
    public void MoveToNextSeason()
    {
        int nextSeason = (int)CurrentSeason + 1;

        if (nextSeason > (int)EcosystemSeason.Winter)
        {
            nextSeason = 0;
        }

        CurrentSeason = (EcosystemSeason)nextSeason;
        ApplySeasonSettings();

        Debug.Log("Season changed to " + CurrentSeason);
    }

    // Sets food, drain and mutation multipliers for the current season
    public void ApplySeasonSettings()
    {
        switch (CurrentSeason)
        {
            case EcosystemSeason.Spring:
                FoodSpawnMultiplier = 1.45f;
                EnergyDrainMultiplier = 0.82f;
                MutationMultiplier = 1.0f;
                break;

            case EcosystemSeason.Summer:
                FoodSpawnMultiplier = 1.2f;
                EnergyDrainMultiplier = 0.95f;
                MutationMultiplier = 1.0f;
                break;

            case EcosystemSeason.Autumn:
                FoodSpawnMultiplier = 0.9f;
                EnergyDrainMultiplier = 1.05f;
                MutationMultiplier = 1.1f;
                break;

            case EcosystemSeason.Winter:
                FoodSpawnMultiplier = 0.65f;
                EnergyDrainMultiplier = 1.15f;
                MutationMultiplier = 1.18f;
                break;
        }
    }
}
