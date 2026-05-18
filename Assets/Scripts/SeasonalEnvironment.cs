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
    public float SeasonLength = 60f;
    public bool AutoCycleSeasons = true;

    [Header("Current Pressure Values")]
    public float FoodSpawnMultiplier = 1f;
    public float EnergyDrainMultiplier = 1f;
    public float MutationMultiplier = 1f;

    private float seasonTimer;

    private void Start()
    {
        ApplySeasonSettings();
    }

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

    public void ApplySeasonSettings()
    {
        switch (CurrentSeason)
        {
            case EcosystemSeason.Spring:
                FoodSpawnMultiplier = 1.35f;
                EnergyDrainMultiplier = 0.9f;
                MutationMultiplier = 1.0f;
                break;

            case EcosystemSeason.Summer:
                FoodSpawnMultiplier = 1.1f;
                EnergyDrainMultiplier = 1.0f;
                MutationMultiplier = 1.0f;
                break;

            case EcosystemSeason.Autumn:
                FoodSpawnMultiplier = 0.8f;
                EnergyDrainMultiplier = 1.1f;
                MutationMultiplier = 1.1f;
                break;

            case EcosystemSeason.Winter:
                FoodSpawnMultiplier = 0.45f;
                EnergyDrainMultiplier = 1.35f;
                MutationMultiplier = 1.25f;
                break;
        }
    }
}
