using System.Collections.Generic;
using UnityEngine;

// Dead fish become meat for predators or scavengers.
public class CarrionSource : MonoBehaviour
{
    [Header("Carrion Mass")]
    public float EnergyValue = 48f;
    public float MaxMass = 48f;
    public float RemainingMass = -1f;
    public float DecayTime = 55f;
    public float Age;
    public float MinimumVisibleScale = 0.18f;

    [Header("Settling / Currents")]
    public bool SettleToTerrain = true;
    public float TerrainClearance = 0.18f;
    public float SinkSpeedByMass = 0.45f;
    public float CurrentDriftMultiplier = 0.55f;

    [Header("Physics Safety")]
    public bool DisablePhysicalColliders = true;

    public bool IsConsumed { get; private set; }

    [Header("Feeding Pressure")]
    public float RecentFeederMemoryTime = 4.0f;

    [Header("Performance")]
    public float UpdateInterval = 0.16f;

    private readonly List<int> recentFeederIds = new List<int>();
    private readonly List<float> recentFeederTimes = new List<float>();
    private Vector3 initialScale;
    private float updateTimer;

    // Registers this object with the ecosystem when Unity enables it
    private void OnEnable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterCarrion(this);
        }
    }

    // Unregisters this object so the manager does not keep old references
    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterCarrion(this);
        }
    }

    // Sets up cached references and safe starting values before the sim runs
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

    // Ages the carrion and only runs drift/decay work on a timer
    private void Update()
    {
        float dt = Time.deltaTime;
        Age += dt;
        updateTimer -= dt;

        if (updateTimer > 0f)
        {
            return;
        }

        updateTimer = Mathf.Max(0.02f, UpdateInterval);
        ApplySettlingAndCurrentDrift();

        if (Age >= DecayTime)
        {
            RemoveWithoutEnergy();
        }
    }


    // Moves carrion with the current, then settles it down onto the terrain floor
    private void ApplySettlingAndCurrentDrift()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        Vector3 movement = manager.GetCurrentVelocityAt(transform.position) * CurrentDriftMultiplier;
        if (SettleToTerrain && manager.TryGetTerrainHeight(transform.position, out float groundY))
        {
            float targetY = groundY + TerrainClearance;
            if (transform.position.y > targetY + 0.05f)
            {
                float massFactor = Mathf.Lerp(0.35f, 1.4f, Mathf.Clamp01(MaxMass / 120f));
                movement += Vector3.down * SinkSpeedByMass * massFactor;
            }
            else
            {
                Vector3 p = transform.position;
                p.y = Mathf.Max(p.y, targetY);
                transform.position = p;
                movement.y = Mathf.Max(0f, movement.y);
            }
        }

        transform.position += movement * Time.deltaTime;
        transform.position = manager.ClampToSimulationArea(transform.position);
    }

    // Disables colliders and physics so dead meat does not block fish movement
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

    // Uses the bite system with no specific feeder ID
    public float ConsumeBite(float requestedMass)
    {
        return ConsumeBiteBy(requestedMass, 0);
    }

    // Removes a small amount of meat, tracks the feeder, then shrinks or destroys the carrion
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

    // Cleans old feeder entries, then returns how many fish recently ate here
    public int GetRecentFeederCount()
    {
        CleanRecentFeeders();
        return recentFeederIds.Count;
    }

    // Returns how much meat is left as a 0 to 1 value
    public float GetMassRatio()
    {
        return MaxMass > 0f ? Mathf.Clamp01(RemainingMass / MaxMass) : 0f;
    }

    // Stores the fish ID so crowded feeding can be detected for a short time
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


    // Removes feeder IDs after their short memory timer runs out
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

    // Consumes all remaining meat through the normal bite path
    public float Consume()
    {
        return ConsumeBite(RemainingMass > 0f ? RemainingMass : EnergyValue);
    }


    // Marks the carrion as used, unregisters it, then destroys the object
    private void ConsumeFully()
    {
        if (IsConsumed)
        {
            return;
        }

        IsConsumed = true;

        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterCarrion(this);
        }

        Destroy(gameObject);
    }

    // Removes old carrion without giving any fish extra energy
    private void RemoveWithoutEnergy()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterCarrion(this);
        }

        Destroy(gameObject);
    }

    // Shrinks the carrion based on how much meat is left
    private void UpdateVisualScale()
    {
        float ratio = MaxMass > 0f ? Mathf.Clamp01(RemainingMass / MaxMass) : 0f;
        float scale = Mathf.Lerp(MinimumVisibleScale, 1f, Mathf.Pow(ratio, 1f / 3f));
        transform.localScale = initialScale * scale;
    }
}
