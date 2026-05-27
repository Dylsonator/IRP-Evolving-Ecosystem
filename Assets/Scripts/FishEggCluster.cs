using System.Collections.Generic;
using UnityEngine;

// Egg clutch that can hatch into fish or get eaten.
public class FishEggCluster : MonoBehaviour
{
    [Header("Egg State")]
    public List<EvolutionCandidate> Children = new List<EvolutionCandidate>();
    public float HatchTime = 32f;
    public float Age;
    public float Health = 36f;
    public float EggMass = 20f;
    public float ProtectionRadius = 7f;
    public float PredatorScanInterval = 0.75f;
    public float PredatorBiteMass = 2.4f;
    public bool SpawnChildrenOnHatch = true;

    [Header("Parent / Guarding")]
    public int MotherId;
    public int FatherId;
    public MarineCreatureAgent Mother;
    public MarineCreatureAgent Father;

    [Header("Visual")]
    public float MinimumScale = 0.25f;
    public float MaximumScale = 1.4f;

    [Header("Performance")]
    public float ScaleRefreshInterval = 0.2f;

    private Vector3 initialScale;
    private float predatorScanTimer;
    private float scaleRefreshTimer;

    // Sets up cached references and safe starting values before the sim runs
    private void Awake()
    {
        initialScale = transform.localScale;
    }

    // Registers this object with the ecosystem when Unity enables it
    private void OnEnable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterEggCluster(this);
        }
    }

    // Unregisters this object so the manager does not keep old references
    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterEggCluster(this);
        }
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        Age += Time.deltaTime;
        predatorScanTimer -= Time.deltaTime;

        if (predatorScanTimer <= 0f)
        {
            predatorScanTimer = PredatorScanInterval;
            HandlePredationPressure();
        }

        scaleRefreshTimer -= Time.deltaTime;
        if (scaleRefreshTimer <= 0f)
        {
            scaleRefreshTimer = Mathf.Max(0.02f, ScaleRefreshInterval);
            UpdateScale();
        }

        if (Health <= 0f || EggMass <= 0.01f)
        {
            Destroy(gameObject);
            return;
        }

        if (Age >= HatchTime)
        {
            Hatch();
        }
    }

    // Handles initialise
    public void Initialise(List<EvolutionCandidate> children, float hatchTime, float health, float mass, MarineCreatureAgent mother = null, MarineCreatureAgent father = null)
    {
        Children = children ?? new List<EvolutionCandidate>();
        HatchTime = Mathf.Max(3f, hatchTime);
        Health = Mathf.Max(1f, health);
        EggMass = Mathf.Max(1f, mass);
        Mother = mother;
        Father = father;
        MotherId = mother != null && mother.Candidate != null ? mother.Candidate.Id : 0;
        FatherId = father != null && father.Candidate != null ? father.Candidate.Id : 0;
        UpdateScale();
    }

    // Handles predation pressure using the current input or sim state
    private void HandlePredationPressure()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        List<MarineCreatureAgent> creatures = manager.GetNearbyCreatures(transform.position, ProtectionRadius);
        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent fish = creatures[i];
            if (fish == null || fish.Candidate == null || fish.Candidate.Genome == null)
            {
                continue;
            }

            if (IsParentOrGuardian(fish) && fish.Candidate.Genome.EggProtection > 0.18f)
            {
                continue;
            }

            float distance = Vector3.Distance(fish.transform.position, transform.position);
            if (distance > ProtectionRadius)
            {
                continue;
            }

            float eggEatingDrive = fish.Candidate.Genome.MeatDiet * 0.6f + fish.Candidate.Genome.CarrionDiet * 0.25f + fish.Candidate.Genome.Aggression * 0.25f;
            if (eggEatingDrive < 0.45f && fish.GetHealthRatio() > 0.35f)
            {
                continue;
            }

            float bite = Mathf.Min(PredatorBiteMass * Mathf.Lerp(0.7f, 1.8f, fish.Candidate.Genome.JawSize), EggMass);
            EggMass -= bite;
            Health -= bite * Mathf.Lerp(0.45f, 1.2f, fish.Candidate.Genome.Aggression + fish.Candidate.Genome.MeatDiet);
            fish.AddMeatToStomachFromEgg(bite);
        }
    }

    // Handles hatch
    private void Hatch()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager != null)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                EvolutionCandidate child = Children[i];
                if (child == null)
                {
                    continue;
                }

                manager.RegisterOffspring(child);
                if (SpawnChildrenOnHatch && manager.CanSpawnMoreActiveCreatures())
                {
                    Vector3 spawn = transform.position + Random.insideUnitSphere * 1.5f;
                    spawn = manager.ClampToSimulationArea(spawn);
                    MarineCreatureAgent hatched = manager.SpawnCreature(child, spawn);
                    if (hatched != null)
                    {
                        hatched.SetJuvenileOnHatch();
                    }
                }
            }
        }

        Destroy(gameObject);
    }


    // Checks if it is parent or guardian
    public bool IsParentOrGuardian(MarineCreatureAgent fish)
    {
        if (fish == null || fish.Candidate == null)
        {
            return false;
        }

        if (fish == Mother || fish == Father)
        {
            return true;
        }

        int id = fish.Candidate.Id;
        return id != 0 && (id == MotherId || id == FatherId);
    }

    // Updates scale using the current sim state
    private void UpdateScale()
    {
        float amount = Mathf.Max(0.1f, Children != null ? Children.Count : 1);
        float massRatio = Mathf.Clamp01(EggMass / Mathf.Max(1f, amount * 8f));
        float scale = Mathf.Lerp(MinimumScale, MaximumScale, Mathf.Clamp01(amount / 10f)) * Mathf.Lerp(0.55f, 1f, massRatio);
        transform.localScale = initialScale * scale;
    }
}
