using System.Collections.Generic;
using UnityEngine;

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
    public int RandomBudCount = 5;
    public float RandomBudRadius = 1.25f;
    public float BudMass = 18f;
    public float BudNutritionQuality = 1f;
    public float DetachChancePerMinute = 0.08f;
    public float DetachedBudLifeTime = 55f;
    public bool BudsDetachWhenBitten = true;
    public float DetachImpulseMin = 0.25f;
    public float DetachImpulseMax = 0.85f;

    [Header("Regrowth")]
    public bool Regrows = true;
    public float RegrowDelay = 18f;
    public float RegrowInterval = 9f;
    public int MaxActiveBuds = 7;

    [Header("Terrain Placement")]
    public bool SnapToTerrainOnStart = true;
    public float TerrainOffset = 0.05f;
    public bool AlignToTerrainNormal = false;

    private readonly List<PlantBudResource> activeBuds = new List<PlantBudResource>();
    private readonly List<Transform> sockets = new List<Transform>();
    private Transform proceduralRoot;
    private float regrowTimer;
    private float detachTimer;

    private void OnEnable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterPlant(this);
        }
    }

    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterPlant(this);
        }
    }

    private void Start()
    {
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

    private void Update()
    {
        CleanBuds();
        HandleRegrowth();
        HandleBudDetachment();
    }

    [ContextMenu("Generate Procedural Plant")]
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

    [ContextMenu("Clear Procedural Plant")]
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

    [ContextMenu("Cache Bud Sockets")]
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

    [ContextMenu("Fill Buds")]
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

    public void NotifyBudRemoved(PlantBudResource bud)
    {
        activeBuds.Remove(bud);
        if (Regrows)
        {
            regrowTimer = Mathf.Max(regrowTimer, RegrowDelay);
        }
    }

    public void NotifyBudDetached(PlantBudResource bud)
    {
        activeBuds.Remove(bud);
        if (Regrows)
        {
            regrowTimer = Mathf.Max(regrowTimer, RegrowDelay);
        }
    }

    private void HandleRegrowth()
    {
        if (!Regrows || activeBuds.Count >= MaxActiveBuds)
        {
            return;
        }

        regrowTimer -= Time.deltaTime;
        if (regrowTimer > 0f)
        {
            return;
        }

        SpawnBud(activeBuds.Count);
        regrowTimer = RegrowInterval;
    }

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
        bud.DetachImpulseMin = DetachImpulseMin;
        bud.DetachImpulseMax = DetachImpulseMax;
        activeBuds.Add(bud);

        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterFood(food);
        }
    }

    private void CleanBuds()
    {
        activeBuds.RemoveAll(bud => bud == null || bud.GetComponent<FoodSource>() == null || bud.GetComponent<FoodSource>().IsConsumed);
    }

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
