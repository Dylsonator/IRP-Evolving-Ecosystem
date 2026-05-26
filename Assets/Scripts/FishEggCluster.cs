using System.Collections.Generic;
using UnityEngine;

public class FishEggCluster : MonoBehaviour
{
    [Header("Egg State")]
    public List<EvolutionCandidate> Children = new List<EvolutionCandidate>();
    public float HatchTime = 45f;
    public float Age;
    public float Health = 25f;
    public float EggMass = 20f;
    public float ProtectionRadius = 6f;
    public float PredatorScanInterval = 0.5f;
    public float PredatorBiteMass = 4f;
    public bool SpawnChildrenOnHatch = true;

    [Header("Visual")]
    public float MinimumScale = 0.25f;
    public float MaximumScale = 1.4f;

    [Header("Performance")]
    public float ScaleRefreshInterval = 0.2f;

    private Vector3 initialScale;
    private float predatorScanTimer;
    private float scaleRefreshTimer;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterEggCluster(this);
        }
    }

    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterEggCluster(this);
        }
    }

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

    public void Initialise(List<EvolutionCandidate> children, float hatchTime, float health, float mass)
    {
        Children = children ?? new List<EvolutionCandidate>();
        HatchTime = Mathf.Max(3f, hatchTime);
        Health = Mathf.Max(1f, health);
        EggMass = Mathf.Max(1f, mass);
        UpdateScale();
    }

    private void HandlePredationPressure()
    {
        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        List<MarineCreatureAgent> creatures = manager.GetActiveCreatures();
        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent fish = creatures[i];
            if (fish == null || fish.Candidate == null || fish.Candidate.Genome == null)
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

    private void UpdateScale()
    {
        float amount = Mathf.Max(0.1f, Children != null ? Children.Count : 1);
        float massRatio = Mathf.Clamp01(EggMass / Mathf.Max(1f, amount * 8f));
        float scale = Mathf.Lerp(MinimumScale, MaximumScale, Mathf.Clamp01(amount / 10f)) * Mathf.Lerp(0.55f, 1f, massRatio);
        transform.localScale = initialScale * scale;
    }
}
