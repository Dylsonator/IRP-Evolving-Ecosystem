using System.Collections.Generic;
using UnityEngine;

// Edible resource script for plant buds and loose food.
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

    [Header("Feeding Pressure")]
    public float RecentFeederMemoryTime = 4.0f;

    private readonly List<int> recentFeederIds = new List<int>();
    private readonly List<float> recentFeederTimes = new List<float>();
    private Vector3 initialScale;
    private PlantBudResource cachedPlantBud;

    // Registers this object with the ecosystem when Unity enables it
    private void OnEnable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterFood(this);
        }
    }

    // Unregisters this object so the manager does not keep old references
    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterFood(this);
        }
    }

    // Sets up cached references and safe starting values before the sim runs
    private void Awake()
    {
        cachedPlantBud = GetComponent<PlantBudResource>();
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

    // Handles Disables blocking physics
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

    // Handles consume bite
    public float ConsumeBite(float requestedMass)
    {
        return ConsumeBiteBy(requestedMass, 0);
    }

    // Handles consume bite by
    public float ConsumeBiteBy(float requestedMass, int feederId)
    {
        if (IsConsumed || requestedMass <= 0f)
        {
            return 0f;
        }

        RegisterRecentFeeder(feederId);

        float eaten = Mathf.Min(requestedMass, RemainingMass);
        RemainingMass -= eaten;
        EnergyValue = RemainingMass;

        if (cachedPlantBud == null)
        {
            cachedPlantBud = GetComponent<PlantBudResource>();
        }

        if (cachedPlantBud != null)
        {
            cachedPlantBud.NotifyBitten(eaten, feederId);
        }

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

    // Handles was recently fed by
    public bool WasRecentlyFedBy(int feederId)
    {
        if (feederId == 0)
        {
            return false;
        }

        CleanRecentFeeders();
        return recentFeederIds.Contains(feederId);
    }

    // Checks if it is detached plant bud
    public bool IsDetachedPlantBud()
    {
        if (cachedPlantBud == null)
        {
            cachedPlantBud = GetComponent<PlantBudResource>();
        }

        return cachedPlantBud != null && !cachedPlantBud.AttachedToPlant;
    }

    // Gets the recent feeder count used by the sim
    public int GetRecentFeederCount()
    {
        CleanRecentFeeders();
        return recentFeederIds.Count;
    }

    // Gets the mass ratio used by the sim
    public float GetMassRatio()
    {
        return MaxMass > 0f ? Mathf.Clamp01(RemainingMass / MaxMass) : 0f;
    }

    // Registers recent feeder with the manager list
    private void RegisterRecentFeeder(int feederId)
    {
        if (feederId == 0)
        {
            return;
        }

        CleanRecentFeeders();
        int index = recentFeederIds.IndexOf(feederId);
        if (index >= 0)
        {
            recentFeederTimes[index] = Time.time;
            return;
        }

        recentFeederIds.Add(feederId);
        recentFeederTimes.Add(Time.time);
    }

    // Handles clean recent feeders
    private void CleanRecentFeeders()
    {
        float cutoff = Time.time - Mathf.Max(0.1f, RecentFeederMemoryTime);
        for (int i = recentFeederTimes.Count - 1; i >= 0; i--)
        {
            if (recentFeederTimes[i] < cutoff)
            {
                recentFeederTimes.RemoveAt(i);
                recentFeederIds.RemoveAt(i);
            }
        }
    }

    // Handles consume
    public float Consume()
    {
        return ConsumeBite(RemainingMass > 0f ? RemainingMass : EnergyValue);
    }

    // Handles consume fully
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

    // Updates visual scale using the current sim state
    private void UpdateVisualScale()
    {
        float ratio = MaxMass > 0f ? Mathf.Clamp01(RemainingMass / MaxMass) : 0f;
        float scale = Mathf.Lerp(MinimumVisibleScale, 1f, Mathf.Pow(ratio, 1f / 3f));
        transform.localScale = initialScale * scale;
    }
}
