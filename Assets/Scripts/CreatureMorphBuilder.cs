using System.Collections.Generic;
using UnityEngine;

public class CreatureMorphBuilder : MonoBehaviour
{
    [Header("Library")]
    public CreatureMorphLibrary MorphLibrary;

    [Header("Runtime Model")]
    public string ModelRootName = "MorphModel";
    public bool DestroyOldModelOnRebuild = true;
    public bool DisableGeneratedColliders = true;
    public bool UseTypeColour = true;

    private Transform modelRoot;
    private readonly List<GameObject> spawnedParts = new List<GameObject>();
    private MaterialPropertyBlock block;

    public void Build(EvolutionGenome genome, CreatureEffectiveStats effectiveStats, Color typeColour)
    {
        if (genome == null)
        {
            return;
        }

        if (MorphLibrary == null)
        {
            MorphLibrary = CreatureMorphLibrary.ActiveLibrary;
        }

        EnsureRoot();

        if (DestroyOldModelOnRebuild)
        {
            ClearSpawnedParts();
        }

        BuildSlot(genome, effectiveStats, CreatureMorphSlot.Body, genome.BodyMorphId, Vector3.zero, Vector3.zero, new Vector3(0.75f * genome.BodyWidth, 0.45f * genome.BodyWidth, 1.15f * genome.BodyLength), false, typeColour, PrimitiveType.Capsule);
        BuildSlot(genome, effectiveStats, CreatureMorphSlot.Tail, genome.TailMorphId, new Vector3(0f, 0f, -0.85f * genome.BodyLength), Vector3.zero, new Vector3(0.22f * genome.TailWidth, 0.18f * genome.TailSize, 0.48f * genome.TailLength), false, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, effectiveStats, CreatureMorphSlot.Fins, genome.FinMorphId, new Vector3(-0.62f * genome.BodyWidth, 0f, -0.05f), new Vector3(0f, 0f, -16f), new Vector3(0.46f * genome.FinLength, 0.055f, 0.24f * genome.FinWidth), true, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, effectiveStats, CreatureMorphSlot.Jaw, genome.JawMorphId, new Vector3(0f, 0f, 0.78f * genome.BodyLength), Vector3.zero, new Vector3(0.28f * genome.JawWidth, 0.20f * genome.JawSize, 0.24f * genome.JawLength), false, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, effectiveStats, CreatureMorphSlot.Sensors, genome.SensorMorphId, new Vector3(-0.24f * genome.BodyWidth, 0.23f * genome.BodyWidth, 0.52f * genome.BodyLength), Vector3.zero, Vector3.one * (0.14f * genome.SensorSize), true, typeColour, PrimitiveType.Sphere);
        BuildSlot(genome, effectiveStats, CreatureMorphSlot.Armour, genome.ArmourMorphId, new Vector3(0f, 0.02f, -0.05f), Vector3.zero, new Vector3(0.82f * genome.BodyWidth, 0.12f + genome.Armour * 0.08f, 0.78f * genome.BodyLength), false, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, effectiveStats, CreatureMorphSlot.DorsalFin, genome.DorsalFinMorphId, new Vector3(0f, 0.42f * genome.BodyWidth, -0.12f), new Vector3(0f, 0f, 0f), new Vector3(0.14f, 0.42f * genome.DorsalFinSize, 0.42f * genome.DorsalFinSize), false, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, effectiveStats, CreatureMorphSlot.Spikes, genome.SpikeMorphId, new Vector3(0f, 0.52f * genome.BodyWidth, -0.05f), Vector3.zero, new Vector3(0.14f * genome.SpikeSize, 0.34f * genome.SpikeLength, 0.14f * genome.SpikeSize), false, typeColour, PrimitiveType.Capsule);
        BuildSlot(genome, effectiveStats, CreatureMorphSlot.Gills, genome.GillMorphId, new Vector3(-0.48f * genome.BodyWidth, 0.05f, 0.30f * genome.BodyLength), new Vector3(0f, 0f, 0f), new Vector3(0.12f * genome.GillSize, 0.22f * genome.GillSize, 0.08f * genome.GillSize), true, typeColour, PrimitiveType.Cube);
    }

    private void BuildSlot(EvolutionGenome genome, CreatureEffectiveStats stats, CreatureMorphSlot slot, string partId, Vector3 fallbackPosition, Vector3 fallbackRotation, Vector3 fallbackScale, bool mirrorOnX, Color typeColour, PrimitiveType fallbackPrimitive)
    {
        if (ShouldSkipPart(partId))
        {
            return;
        }

        CreatureMorphPartData part = MorphLibrary != null ? MorphLibrary.FindPart(slot, partId) : null;

        if (part != null)
        {
            SpawnPartFromData(part, typeColour);
            return;
        }

        SpawnFallbackPart(slot, partId, fallbackPosition, fallbackRotation, fallbackScale, mirrorOnX, typeColour, fallbackPrimitive);
    }

    private bool ShouldSkipPart(string partId)
    {
        if (string.IsNullOrEmpty(partId))
        {
            return false;
        }

        return partId.Contains("none") || partId.Contains("reduced") && partId.Contains("spikes");
    }

    private void SpawnPartFromData(CreatureMorphPartData part, Color typeColour)
    {
        if (part == null)
        {
            return;
        }

        Vector3 scale = part.LocalScale == Vector3.zero ? Vector3.one : part.LocalScale;
        GameObject first = SpawnSinglePart(part.PartPrefab, part.DisplayName, part.LocalPosition, part.LocalRotationEuler, scale, part.OverrideTypeColour ? part.DebugColour : typeColour);

        if (part.MirrorOnX && first != null)
        {
            Vector3 mirroredPosition = part.LocalPosition;
            mirroredPosition.x *= -1f;
            Vector3 mirroredRotation = part.LocalRotationEuler;
            mirroredRotation.z *= -1f;
            SpawnSinglePart(part.PartPrefab, part.DisplayName + " Mirror", mirroredPosition, mirroredRotation, scale, part.OverrideTypeColour ? part.DebugColour : typeColour);
        }
    }

    private void SpawnFallbackPart(CreatureMorphSlot slot, string partId, Vector3 position, Vector3 rotation, Vector3 scale, bool mirrorOnX, Color typeColour, PrimitiveType primitive)
    {
        Color colour = GetFallbackPartColour(slot, partId, typeColour);
        string label = CreatureMorphLibrary.GetFallbackDisplayName(partId);
        GameObject first = SpawnSinglePart(null, label, position, rotation, scale, colour, primitive);

        if (mirrorOnX && first != null)
        {
            Vector3 mirroredPosition = position;
            mirroredPosition.x *= -1f;
            Vector3 mirroredRotation = rotation;
            mirroredRotation.z *= -1f;
            SpawnSinglePart(null, label + " Mirror", mirroredPosition, mirroredRotation, scale, colour, primitive);
        }
    }

    private GameObject SpawnSinglePart(GameObject prefab, string name, Vector3 localPosition, Vector3 localRotationEuler, Vector3 localScale, Color colour, PrimitiveType fallbackPrimitive = PrimitiveType.Cube)
    {
        EnsureRoot();

        GameObject instance;

        if (prefab != null)
        {
            instance = Instantiate(prefab, modelRoot);
        }
        else
        {
            instance = GameObject.CreatePrimitive(fallbackPrimitive);
            instance.transform.SetParent(modelRoot, false);
        }

        instance.name = string.IsNullOrEmpty(name) ? "MorphPart" : name;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(localRotationEuler);
        instance.transform.localScale = localScale;

        if (DisableGeneratedColliders)
        {
            DisableColliders(instance);
        }

        ApplyColour(instance, colour);
        spawnedParts.Add(instance);
        return instance;
    }

    private void EnsureRoot()
    {
        if (modelRoot != null)
        {
            return;
        }

        Transform existing = transform.Find(ModelRootName);
        if (existing != null)
        {
            modelRoot = existing;
            return;
        }

        GameObject rootObject = new GameObject(ModelRootName);
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        modelRoot = rootObject.transform;
    }

    private void ClearSpawnedParts()
    {
        for (int i = spawnedParts.Count - 1; i >= 0; i--)
        {
            if (spawnedParts[i] != null)
            {
                Destroy(spawnedParts[i]);
            }
        }

        spawnedParts.Clear();

        if (modelRoot == null)
        {
            return;
        }

        for (int i = modelRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(modelRoot.GetChild(i).gameObject);
        }
    }

    private void DisableColliders(GameObject instance)
    {
        Collider[] colliders = instance.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void ApplyColour(GameObject instance, Color colour)
    {
        if (!UseTypeColour)
        {
            return;
        }

        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererToColour = renderers[i];
            rendererToColour.GetPropertyBlock(block);
            block.SetColor("_BaseColor", colour);
            block.SetColor("_Color", colour);
            rendererToColour.SetPropertyBlock(block);
        }
    }

    private Color GetFallbackPartColour(CreatureMorphSlot slot, string partId, Color typeColour)
    {
        if (slot == CreatureMorphSlot.Armour)
        {
            return Color.Lerp(typeColour, Color.gray, 0.65f);
        }

        if (slot == CreatureMorphSlot.Jaw)
        {
            return Color.Lerp(typeColour, Color.white, 0.35f);
        }

        if (slot == CreatureMorphSlot.Sensors)
        {
            return Color.Lerp(typeColour, Color.cyan, 0.55f);
        }

        if (slot == CreatureMorphSlot.Spikes)
        {
            return Color.Lerp(typeColour, Color.red, 0.45f);
        }

        if (slot == CreatureMorphSlot.Gills)
        {
            return Color.Lerp(typeColour, Color.blue, 0.45f);
        }

        return typeColour;
    }
}
