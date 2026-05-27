using System.Collections.Generic;
using UnityEngine;

// Procedural plant/resource spawner that grows edible buds and handles regrowth between generations.
public class PlantResource : MonoBehaviour
{
    [Header("Procedural Shape")]
    public bool GenerateProceduralPlant = true;
    public bool RegenerateOnStart = true;
    public bool ClearPreviousProceduralParts = true;
    public int RandomSeed = 0;
    public bool UseRandomSeed = true;
    public int MinimumStalks = 3;
    public int MaximumStalks = 7;
    public float StalkLengthMin = 1.2f;
    public float StalkLengthMax = 3.4f;
    public float StalkThicknessMin = 0.08f;
    public float StalkThicknessMax = 0.18f;
    public float BaseSpreadRadius = 0.45f;
    public float TipSpreadRadius = 0.85f;
    public float LeanAmount = 0.55f;
    public float UpwardBias = 0.85f;
    public bool GenerateLeaves = true;
    public int LeavesPerStalk = 2;
    public float LeafLength = 0.65f;
    public float LeafWidth = 0.18f;
    public Material StalkMaterial;
    public Material LeafMaterial;

    [Header("Buds")]
    public FoodSource BudPrefab;
    public bool UseBudSockets = true;
    public string BudSocketNameContains = "Bud";
    public int RandomBudCount = 7;
    public float RandomBudRadius = 1.25f;
    public float BudMass = 38f;
    public float BudNutritionQuality = 1.25f;
    public float DetachChancePerMinute = 0.06f;
    public float DetachedBudLifeTime = 55f;
    public bool BudsDetachWhenBitten = true;
    public float DetachImpulseMin = 0.25f;
    public float DetachImpulseMax = 0.85f;

    [Header("Regrowth")]
    public bool Regrows = true;
    public float RegrowDelay = 7f;
    public float RegrowInterval = 3.0f;
    public int MaxActiveBuds = 10;
    public bool UseFoodSupplySafeguards = true;

    [Header("Terrain Placement")]
    public bool SnapToTerrainOnStart = true;
    public float TerrainOffset = 0.05f;
    public bool AlignToTerrainNormal = false;

    private readonly List<PlantBudResource> activeBuds = new List<PlantBudResource>();
    private readonly List<Transform> sockets = new List<Transform>();
    [Header("Performance")]
    public float PlantTickInterval = 0.35f;

    private Transform proceduralRoot;
    private float regrowTimer;
    private float detachTimer;
    private float plantTickTimer;

    // Registers this object with the ecosystem when Unity enables it
    private void OnEnable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterPlant(this);
        }
    }

    // Unregisters this object so the manager does not keep old references
    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterPlant(this);
        }
    }

    // Starts the setup that needs other scene objects to already exist
    private void Start()
    {
        ApplyFoodSupplySafeguards();

        if (SnapToTerrainOnStart && EvolutionEcosystemManager.Instance != null)
        {
            Vector3 p = EvolutionEcosystemManager.Instance.ProjectPointToTerrain(transform.position, TerrainOffset);
            transform.position = p;
        }

        if (GenerateProceduralPlant && RegenerateOnStart)
        {
            GeneratePlant();
        }

        CacheSockets();
        FillBuds();
    }


    // Applies food supply safeguards to the current object
    private void ApplyFoodSupplySafeguards()
    {
        if (!UseFoodSupplySafeguards)
        {
            return;
        }

        // One plant should support a small group, but not become an infinite battery.
        RandomBudCount = Mathf.Clamp(RandomBudCount, 5, 9);
        MaxActiveBuds = Mathf.Clamp(MaxActiveBuds, 7, 12);
        BudMass = Mathf.Clamp(BudMass, 32f, 46f);
        BudNutritionQuality = Mathf.Clamp(BudNutritionQuality, 1.05f, 1.35f);
        RegrowDelay = Mathf.Clamp(RegrowDelay, 5f, 10f);
        RegrowInterval = Mathf.Clamp(RegrowInterval, 2.5f, 4.5f);
        DetachChancePerMinute = Mathf.Clamp(DetachChancePerMinute, 0.035f, 0.09f);
        DetachedBudLifeTime = Mathf.Max(DetachedBudLifeTime, 75f);
    }

    // Runs the normal frame checks and timers
    private void Update()
    {
        plantTickTimer -= Time.deltaTime;
        if (plantTickTimer > 0f)
        {
            return;
        }

        plantTickTimer = Mathf.Max(0.02f, PlantTickInterval);
        CleanBuds();
        HandleRegrowth();
        HandleBudDetachment();
    }

    // Handles the generate plant step.
    [ContextMenu("Generate Procedural Plant")]
    // Builds the procedural plant body and then fills it with buds
    public void GeneratePlant()
    {
        if (ClearPreviousProceduralParts)
        {
            ClearProceduralParts();
        }

        proceduralRoot = new GameObject("ProceduralPlant").transform;
        proceduralRoot.SetParent(transform, false);
        proceduralRoot.localPosition = Vector3.zero;
        proceduralRoot.localRotation = Quaternion.identity;
        proceduralRoot.localScale = Vector3.one;

        int oldState = Random.state.GetHashCode();
        if (UseRandomSeed)
        {
            int seed = RandomSeed != 0 ? RandomSeed : GetInstanceID();
            Random.InitState(seed);
        }

        int stalkCount = Random.Range(Mathf.Max(1, MinimumStalks), Mathf.Max(MinimumStalks + 1, MaximumStalks + 1));
        for (int i = 0; i < stalkCount; i++)
        {
            CreateStalk(i, stalkCount);
        }

        if (UseRandomSeed)
        {
            Random.InitState(oldState);
        }
    }

    // Clears procedural parts.
    [ContextMenu("Clear Procedural Plant")]
    // Deletes old generated plant parts before rebuilding
    public void ClearProceduralParts()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "ProceduralPlant" || child.name.StartsWith("Generated_"))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child.gameObject);
                }
                else
#endif
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    // Creates one procedural stalk using a cylinder and optional leaves
    private void CreateStalk(int index, int stalkCount)
    {
        float angle = stalkCount > 0 ? (360f / stalkCount) * index + Random.Range(-18f, 18f) : Random.Range(0f, 360f);
        float angleRad = angle * Mathf.Deg2Rad;
        Vector3 radial = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));

        Vector3 baseLocal = radial * Random.Range(0f, BaseSpreadRadius);
        float length = Random.Range(StalkLengthMin, StalkLengthMax);
        float thickness = Random.Range(StalkThicknessMin, StalkThicknessMax);
        Vector3 lean = radial * Random.Range(0.15f, LeanAmount) + Random.insideUnitSphere * 0.12f;
        lean.y = 0f;

        Vector3 tipLocal = baseLocal + Vector3.up * (length * Mathf.Max(0.15f, UpwardBias)) + lean * TipSpreadRadius;
        Vector3 direction = tipLocal - baseLocal;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.up;
            tipLocal = baseLocal + Vector3.up * length;
        }

        Transform stalkRoot = new GameObject("Generated_Stalk_" + index).transform;
        stalkRoot.SetParent(proceduralRoot != null ? proceduralRoot : transform, false);
        stalkRoot.localPosition = Vector3.zero;
        stalkRoot.localRotation = Quaternion.identity;
        stalkRoot.localScale = Vector3.one;

        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(stalkRoot, false);
        stem.transform.localPosition = (baseLocal + tipLocal) * 0.5f;
        stem.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        stem.transform.localScale = new Vector3(thickness, direction.magnitude * 0.5f, thickness);
        ApplyMaterial(stem, StalkMaterial);
        RemoveSolidCollider(stem);

        if (GenerateLeaves)
        {
            for (int leaf = 0; leaf < Mathf.Max(0, LeavesPerStalk); leaf++)
            {
                CreateLeaf(stalkRoot, baseLocal, tipLocal, radial, leaf);
            }
        }

        Transform budPoint = new GameObject("BudPoint_" + index).transform;
        budPoint.SetParent(stalkRoot, false);
        budPoint.localPosition = tipLocal;
        budPoint.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        budPoint.localScale = Vector3.one;
    }

    // Creates one flat leaf object and places it on a stalk
    private void CreateLeaf(Transform stalkRoot, Vector3 baseLocal, Vector3 tipLocal, Vector3 radial, int leafIndex)
    {
        float t = Random.Range(0.28f, 0.78f);
        Vector3 centre = Vector3.Lerp(baseLocal, tipLocal, t);
        Vector3 side = Vector3.Cross(Vector3.up, radial).normalized;
        if (leafIndex % 2 == 1)
        {
            side = -side;
        }

        GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leaf.name = "Leaf";
        leaf.transform.SetParent(stalkRoot, false);
        leaf.transform.localPosition = centre + side * (LeafLength * 0.28f);
        leaf.transform.localRotation = Quaternion.LookRotation(side, Vector3.up) * Quaternion.Euler(0f, 0f, Random.Range(-24f, 24f));
        leaf.transform.localScale = new Vector3(LeafWidth, 0.025f, LeafLength);
        ApplyMaterial(leaf, LeafMaterial != null ? LeafMaterial : StalkMaterial);
        RemoveSolidCollider(leaf);
    }

    // Applies the chosen material to a generated plant part
    private void ApplyMaterial(GameObject target, Material material)
    {
        if (material == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    // Removes primitive colliders so generated plant parts do not block fish
    private void RemoveSolidCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(collider);
            }
            else
#endif
            {
                Destroy(collider);
            }
        }
    }

    // Handles the cache sockets step.
    [ContextMenu("Cache Bud Sockets")]
    // Finds bud sockets on the plant for bud spawning
    public void CacheSockets()
    {
        sockets.Clear();
        Transform[] children = GetComponentsInChildren<Transform>();
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == transform)
            {
                continue;
            }

            if (child.name.Contains(BudSocketNameContains))
            {
                sockets.Add(child);
            }
        }
    }

    // Handles the fill buds step.
    [ContextMenu("Fill Buds")]
    // Spawns buds until the plant reaches its active bud limit
    public void FillBuds()
    {
        CleanBuds();
        int target = UseBudSockets && sockets.Count > 0 ? sockets.Count : Mathf.Max(1, RandomBudCount);
        target = Mathf.Min(target, Mathf.Max(1, MaxActiveBuds));
        while (activeBuds.Count < target)
        {
            SpawnBud(activeBuds.Count);
        }
    }

    // Resets buds for new generation.
    [ContextMenu("Reset Buds For New Generation")]
    // Clears old buds and refills the plant for a clean generation start
    public void ResetBudsForNewGeneration()
    {
        for (int i = activeBuds.Count - 1; i >= 0; i--)
        {
            PlantBudResource bud = activeBuds[i];
            if (bud != null)
            {
                Destroy(bud.gameObject);
            }
        }

        activeBuds.Clear();
        regrowTimer = 0f;
        detachTimer = 0f;
        CacheSockets();
        FillBuds();
    }

    // Removes a consumed bud from the active list
    public void NotifyBudRemoved(PlantBudResource bud)
    {
        activeBuds.Remove(bud);
        if (Regrows)
        {
            regrowTimer = Mathf.Max(regrowTimer, RegrowDelay);
        }
    }

    // Removes a detached bud from the plant list but leaves it in the world
    public void NotifyBudDetached(PlantBudResource bud)
    {
        activeBuds.Remove(bud);
        if (Regrows)
        {
            regrowTimer = Mathf.Max(regrowTimer, RegrowDelay);
        }
    }

    // Regrows new buds over time if the plant is below its limit
    private void HandleRegrowth()
    {
        if (!Regrows || activeBuds.Count >= MaxActiveBuds)
        {
            return;
        }

        float foodOpportunity = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance.GetLocalFoodOpportunityMultiplier(transform.position) : 1f;
        regrowTimer -= Time.deltaTime * Mathf.Max(0.1f, foodOpportunity);
        if (regrowTimer > 0f)
        {
            return;
        }

        SpawnBud(activeBuds.Count);
        regrowTimer = RegrowInterval / Mathf.Max(0.25f, foodOpportunity);
    }

    // Randomly detaches mature buds so some food floats around
    private void HandleBudDetachment()
    {
        if (DetachChancePerMinute <= 0f || activeBuds.Count == 0)
        {
            return;
        }

        detachTimer += Time.deltaTime;
        if (detachTimer < 1f)
        {
            return;
        }
        detachTimer = 0f;

        float chancePerSecond = DetachChancePerMinute / 60f;
        if (Random.value > chancePerSecond)
        {
            return;
        }

        PlantBudResource bud = activeBuds[Random.Range(0, activeBuds.Count)];
        if (bud != null && bud.AttachedToPlant)
        {
            bud.DetachFromPlant(GetRandomDetachImpulse(bud.transform.position));
        }
    }

    // Builds a small current-influenced impulse for detached buds
    private Vector3 GetRandomDetachImpulse(Vector3 worldPosition)
    {
        Vector3 impulse = Random.insideUnitSphere;
        impulse.y = Mathf.Abs(impulse.y) * 0.2f;
        if (EvolutionEcosystemManager.Instance != null)
        {
            Vector3 current = EvolutionEcosystemManager.Instance.GetCurrentVelocityAt(worldPosition);
            if (current.sqrMagnitude > 0.001f)
            {
                impulse += current.normalized;
            }
        }
        return impulse.normalized * Random.Range(DetachImpulseMin, DetachImpulseMax);
    }

    // Spawns one food bud either on a socket or near the plant
    private void SpawnBud(int index)
    {
        Transform socket = UseBudSockets && sockets.Count > 0 ? sockets[Mathf.Clamp(index % sockets.Count, 0, sockets.Count - 1)] : null;
        Vector3 position = socket != null ? socket.position : transform.position + Random.insideUnitSphere * RandomBudRadius;
        Quaternion rotation = socket != null ? socket.rotation : Random.rotation;

        FoodSource food;
        if (BudPrefab != null)
        {
            food = Instantiate(BudPrefab, position, rotation, socket != null ? socket : transform);
        }
        else
        {
            GameObject budObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            budObject.name = "Plant_Bud";
            budObject.transform.SetParent(socket != null ? socket : transform, true);
            budObject.transform.position = position;
            budObject.transform.rotation = rotation;
            budObject.transform.localScale = Vector3.one * 0.28f;
            food = budObject.AddComponent<FoodSource>();
        }

        food.MaxMass = Mathf.Max(1f, BudMass * Mathf.Max(0.1f, BudNutritionQuality));
        food.RemainingMass = food.MaxMass;
        food.EnergyValue = food.RemainingMass;

        PlantBudResource bud = food.GetComponent<PlantBudResource>();
        if (bud == null)
        {
            bud = food.gameObject.AddComponent<PlantBudResource>();
        }

        bud.ParentPlant = this;
        bud.Socket = socket;
        bud.AttachedToPlant = socket != null;
        bud.DetachedLifeTime = DetachedBudLifeTime;
        bud.DetachWhenBitten = BudsDetachWhenBitten;
        bud.DetachOnlyBelowMassRatio = 0.02f;
        bud.DetachImpulseMin = DetachImpulseMin;
        bud.DetachImpulseMax = DetachImpulseMax;
        activeBuds.Add(bud);

        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterFood(food);
        }
    }

    // Handles clean buds
    private void CleanBuds()
    {
        for (int i = activeBuds.Count - 1; i >= 0; i--)
        {
            PlantBudResource bud = activeBuds[i];
            if (bud == null)
            {
                activeBuds.RemoveAt(i);
                continue;
            }

            FoodSource food = bud.GetComponent<FoodSource>();
            if (food == null || food.IsConsumed)
            {
                activeBuds.RemoveAt(i);
            }
        }
    }

    // Draws selected-only gizmos so setup can be checked without clutter
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, RandomBudRadius);

        if (UseBudSockets)
        {
            Gizmos.color = Color.yellow;
            Transform[] children = GetComponentsInChildren<Transform>();
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != transform && child.name.Contains(BudSocketNameContains))
                {
                    Gizmos.DrawWireSphere(child.position, 0.12f);
                    Gizmos.DrawRay(child.position, child.forward * 0.35f);
                }
            }
        }
    }
}
