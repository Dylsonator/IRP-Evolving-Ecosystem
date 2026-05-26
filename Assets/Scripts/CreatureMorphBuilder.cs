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

    [Header("Body Prefab Socket Markers")]
    [Tooltip("When true, non-body parts first look for CreatureMorphSocketMarker components placed inside the active body prefab.")]
    public bool UsePrefabBodySocketMarkers = true;

    [Tooltip("If a body prefab has no socket markers, the builder can still use socket data stored on the body morph asset as a fallback.")]
    public bool UseAssetSocketNodesFallback = true;

    [Tooltip("Draws small gizmos for the active prefab/asset sockets when the creature is selected.")]
    public bool DrawSocketGizmos = true;

    public float SocketGizmoSize = 0.08f;

    [Header("Prefab Scale Safety")]
    [Tooltip("Keep enabled for authored prefab morph parts. Prefab roots keep their own local scale instead of being resized by fallback/genome scale values.")]
    public bool PreserveAuthoredPrefabScale = true;

    [Tooltip("Optional. Only enable if you intentionally want socket marker scale to resize spawned parts.")]
    public bool UseMarkerScaleForAttachedParts = false;

    [Header("Morph Lookup Debug")]
    public bool LogMissingMorphParts = true;
    public bool LogMissingPrefabs = true;
    public bool NameFallbackPartsClearly = true;

    private Transform modelRoot;
    private readonly List<GameObject> spawnedParts = new List<GameObject>();
    private readonly List<CreatureMorphSocketMarker> activePrefabSockets = new List<CreatureMorphSocketMarker>();

    private MaterialPropertyBlock block;
    private EvolutionGenome activeGenome;
    private CreatureMorphPartData activeBodyPart;
    private GameObject activeBodyInstance;

    public void Build(EvolutionGenome genome, CreatureEffectiveStats effectiveStats, Color typeColour)
    {
        if (genome == null)
        {
            return;
        }

        activeGenome = genome;
        activeBodyInstance = null;
        activePrefabSockets.Clear();

        if (MorphLibrary == null)
        {
            MorphLibrary = CreatureMorphLibrary.ActiveLibrary;
        }

        activeBodyPart = MorphLibrary != null ? MorphLibrary.FindPart(CreatureMorphSlot.Body, genome.BodyMorphId) : null;

        EnsureRoot();

        if (DestroyOldModelOnRebuild)
        {
            ClearSpawnedParts();
        }

        activeBodyInstance = BuildSlot(genome, CreatureMorphSlot.Body, genome.BodyMorphId, Vector3.zero, Vector3.zero, new Vector3(0.75f * genome.BodyWidth, 0.45f * genome.BodyWidth, 1.15f * genome.BodyLength), false, typeColour, PrimitiveType.Capsule);
        CollectPrefabSocketsFromBody();

        BuildSlot(genome, CreatureMorphSlot.Tail, genome.TailMorphId, new Vector3(0f, 0f, -0.85f * genome.BodyLength), Vector3.zero, new Vector3(0.22f * genome.TailWidth, 0.18f * genome.TailSize, 0.48f * genome.TailLength), false, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, CreatureMorphSlot.Fins, genome.FinMorphId, new Vector3(-0.62f * genome.BodyWidth, 0f, -0.05f), new Vector3(0f, 0f, -16f), new Vector3(0.46f * genome.FinLength, 0.055f, 0.24f * genome.FinWidth), true, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, CreatureMorphSlot.Jaw, genome.JawMorphId, new Vector3(0f, 0f, 0.78f * genome.BodyLength), Vector3.zero, new Vector3(0.28f * genome.JawWidth, 0.20f * genome.JawSize, 0.24f * genome.JawLength), false, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, CreatureMorphSlot.Sensors, genome.SensorMorphId, new Vector3(-0.24f * genome.BodyWidth, 0.23f * genome.BodyWidth, 0.52f * genome.BodyLength), Vector3.zero, Vector3.one * (0.14f * genome.SensorSize), true, typeColour, PrimitiveType.Sphere);
        BuildSlot(genome, CreatureMorphSlot.Armour, genome.ArmourMorphId, new Vector3(0f, 0.02f, -0.05f), Vector3.zero, new Vector3(0.82f * genome.BodyWidth, 0.12f + genome.Armour * 0.08f, 0.78f * genome.BodyLength), false, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, CreatureMorphSlot.DorsalFin, genome.DorsalFinMorphId, new Vector3(0f, 0.42f * genome.BodyWidth, -0.12f), Vector3.zero, new Vector3(0.14f, 0.42f * genome.DorsalFinSize, 0.42f * genome.DorsalFinSize), false, typeColour, PrimitiveType.Cube);
        BuildSlot(genome, CreatureMorphSlot.Spikes, genome.SpikeMorphId, new Vector3(0f, 0.52f * genome.BodyWidth, -0.05f), Vector3.zero, new Vector3(0.14f * genome.SpikeSize, 0.34f * genome.SpikeLength, 0.14f * genome.SpikeSize), false, typeColour, PrimitiveType.Capsule);
        BuildSlot(genome, CreatureMorphSlot.Gills, genome.GillMorphId, new Vector3(-0.48f * genome.BodyWidth, 0.05f, 0.30f * genome.BodyLength), Vector3.zero, new Vector3(0.12f * genome.GillSize, 0.22f * genome.GillSize, 0.08f * genome.GillSize), true, typeColour, PrimitiveType.Cube);
    }

    private GameObject BuildSlot(EvolutionGenome genome, CreatureMorphSlot slot, string partId, Vector3 fallbackPosition, Vector3 fallbackRotation, Vector3 fallbackScale, bool fallbackMirrorOnX, Color typeColour, PrimitiveType fallbackPrimitive)
    {
        if (ShouldSkipPart(partId))
        {
            return null;
        }

        CreatureMorphPartData part = MorphLibrary != null ? MorphLibrary.FindPart(slot, partId) : null;

        if (slot != CreatureMorphSlot.Body)
        {
            List<CreatureMorphSocketMarker> prefabMarkers = GetPrefabMarkersForSlot(slot, part);
            if (prefabMarkers.Count > 0)
            {
                GameObject firstSpawned = null;

                for (int i = 0; i < prefabMarkers.Count; i++)
                {
                    ResolvedMorphTransform resolved = ResolveTransformFromPrefabMarker(prefabMarkers[i]);

                    // Prefab socket markers already place the part on the body.
                    // For prefab-authored parts, only use PartData for tiny position/rotation correction.
                    // Do not apply fallback/genome/socket scale here because it shrinks authored prefabs.
                    ApplyPrefabSocketPartOffsetsNoScale(part, ref resolved);

                    GameObject spawned = SpawnResolvedPart(slot, partId, part, resolved, typeColour, fallbackPrimitive);
                    if (firstSpawned == null)
                    {
                        firstSpawned = spawned;
                    }
                }

                return firstSpawned;
            }
        }

        ResolvedMorphTransform singleResolved = ResolveTransformFromFallbackOrAssetSocket(genome, slot, part, fallbackPosition, fallbackRotation, fallbackScale, fallbackMirrorOnX);
        return SpawnResolvedPart(slot, partId, part, singleResolved, typeColour, fallbackPrimitive);
    }

    private bool ShouldSkipPart(string partId)
    {
        if (string.IsNullOrEmpty(partId))
        {
            return false;
        }

        return partId.Contains("none") || partId.Contains("reduced") && partId.Contains("spikes");
    }

    private void CollectPrefabSocketsFromBody()
    {
        activePrefabSockets.Clear();

        if (!UsePrefabBodySocketMarkers || activeBodyInstance == null)
        {
            return;
        }

        CreatureMorphSocketMarker[] markers = activeBodyInstance.GetComponentsInChildren<CreatureMorphSocketMarker>(true);
        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] != null && markers[i].SpawnPart)
            {
                activePrefabSockets.Add(markers[i]);
            }
        }
    }

    private List<CreatureMorphSocketMarker> GetPrefabMarkersForSlot(CreatureMorphSlot slot, CreatureMorphPartData part)
    {
        List<CreatureMorphSocketMarker> result = new List<CreatureMorphSocketMarker>();

        if (!UsePrefabBodySocketMarkers || activePrefabSockets.Count == 0)
        {
            return result;
        }

        if (part != null && !part.UseBodySocket)
        {
            return result;
        }

        string preferredSocketName = part != null ? part.PreferredSocketName : null;
        bool hasPreferredName = !string.IsNullOrEmpty(preferredSocketName);

        for (int i = 0; i < activePrefabSockets.Count; i++)
        {
            CreatureMorphSocketMarker marker = activePrefabSockets[i];
            if (marker == null || !marker.SpawnPart || marker.Slot != slot)
            {
                continue;
            }

            if (hasPreferredName && marker.SocketName != preferredSocketName)
            {
                continue;
            }

            result.Add(marker);
        }

        return result;
    }

    private ResolvedMorphTransform ResolveTransformFromPrefabMarker(CreatureMorphSocketMarker marker)
    {
        ResolvedMorphTransform resolved = new ResolvedMorphTransform();

        if (marker == null || modelRoot == null)
        {
            resolved.LocalPosition = Vector3.zero;
            resolved.LocalRotationEuler = Vector3.zero;
            resolved.LocalScale = Vector3.one;
            resolved.MirrorOnX = false;
            return resolved;
        }

        Vector3 socketWorldPosition = marker.transform.TransformPoint(marker.PartPositionOffset);
        Quaternion socketWorldRotation = marker.transform.rotation * Quaternion.Euler(marker.PartRotationOffset);

        resolved.LocalPosition = modelRoot.InverseTransformPoint(socketWorldPosition);
        resolved.LocalRotationEuler = (Quaternion.Inverse(modelRoot.rotation) * socketWorldRotation).eulerAngles;

        if (UseMarkerScaleForAttachedParts && marker.UseMarkerScale)
        {
            resolved.LocalScale = DivideVector(marker.transform.lossyScale, modelRoot.lossyScale);
        }
        else
        {
            // Normal path: socket markers position and rotate the part, but the prefab keeps its own authored scale.
            resolved.LocalScale = Vector3.one;
        }

        resolved.LocalScale = MultiplyVector(resolved.LocalScale, marker.PartScaleMultiplier == Vector3.zero ? Vector3.one : marker.PartScaleMultiplier);
        resolved.MirrorOnX = marker.MirrorOnX;
        return resolved;
    }

    private ResolvedMorphTransform ResolveTransformFromFallbackOrAssetSocket(EvolutionGenome genome, CreatureMorphSlot slot, CreatureMorphPartData part, Vector3 fallbackPosition, Vector3 fallbackRotation, Vector3 fallbackScale, bool fallbackMirrorOnX)
    {
        ResolvedMorphTransform resolved = new ResolvedMorphTransform();
        resolved.LocalPosition = fallbackPosition;
        resolved.LocalRotationEuler = fallbackRotation;
        resolved.LocalScale = fallbackScale == Vector3.zero ? Vector3.one : fallbackScale;
        resolved.MirrorOnX = fallbackMirrorOnX;

        bool hasSocket = false;
        CreatureMorphSocketDefinition socket;

        if (slot != CreatureMorphSlot.Body && TryGetAssetSocketForSlot(slot, part, out socket))
        {
            hasSocket = true;

            Vector3 socketPosition = socket.LocalPosition;
            Vector3 socketScale = socket.LocalScale == Vector3.zero ? Vector3.one : socket.LocalScale;

            if (socket.ScalePositionByBodyShape)
            {
                socketPosition = ApplyBodyShapeToPosition(socketPosition, genome);
            }

            if (socket.ScaleSocketByBodyShape)
            {
                socketScale = ApplyBodyShapeToScale(socketScale, genome);
            }

            resolved.LocalPosition = socketPosition;
            resolved.LocalRotationEuler = socket.LocalRotationEuler;
            resolved.LocalScale = socketScale;
            resolved.MirrorOnX = socket.MirrorOnX;
        }

        if (slot == CreatureMorphSlot.Body && part != null)
        {
            resolved.LocalPosition = part.LocalPosition;
            resolved.LocalRotationEuler = part.LocalRotationEuler;
            Vector3 partScale = part.LocalScale == Vector3.zero ? Vector3.one : part.LocalScale;
            resolved.LocalScale = MultiplyVector(partScale, fallbackScale == Vector3.zero ? Vector3.one : fallbackScale);
            resolved.MirrorOnX = part.MirrorOnX;
            return resolved;
        }

        ApplyPartAdjustments(part, fallbackScale, hasSocket, ref resolved);
        return resolved;
    }

    private bool TryGetAssetSocketForSlot(CreatureMorphSlot slot, CreatureMorphPartData part, out CreatureMorphSocketDefinition socket)
    {
        socket = new CreatureMorphSocketDefinition();

        if (!UseAssetSocketNodesFallback || activeBodyPart == null || activeBodyPart.SocketNodes == null || activeBodyPart.SocketNodes.Count == 0)
        {
            return false;
        }

        if (part != null && !part.UseBodySocket)
        {
            return false;
        }

        string preferredSocketName = part != null ? part.PreferredSocketName : null;

        if (!string.IsNullOrEmpty(preferredSocketName))
        {
            for (int i = 0; i < activeBodyPart.SocketNodes.Count; i++)
            {
                CreatureMorphSocketDefinition candidate = activeBodyPart.SocketNodes[i];
                if (candidate.Slot == slot && candidate.SocketName == preferredSocketName)
                {
                    socket = candidate;
                    return true;
                }
            }
        }

        for (int i = 0; i < activeBodyPart.SocketNodes.Count; i++)
        {
            CreatureMorphSocketDefinition candidate = activeBodyPart.SocketNodes[i];
            if (candidate.Slot == slot)
            {
                socket = candidate;
                return true;
            }
        }

        return false;
    }

    private void ApplyPrefabSocketPartOffsetsNoScale(CreatureMorphPartData part, ref ResolvedMorphTransform resolved)
    {
        if (part == null)
        {
            return;
        }

        if (part.AddLocalOffsetToSocket)
        {
            resolved.LocalPosition += part.LocalPosition;
        }
        else if (part.LocalPosition != Vector3.zero)
        {
            resolved.LocalPosition = part.LocalPosition;
        }

        if (part.AddLocalRotationToSocket)
        {
            resolved.LocalRotationEuler += part.LocalRotationEuler;
        }
        else if (part.LocalRotationEuler != Vector3.zero)
        {
            resolved.LocalRotationEuler = part.LocalRotationEuler;
        }

        // Critical fix: keep scale as Vector3.one for prefab socket parts.
        // SpawnSinglePart will keep the prefab root scale when PreserveAuthoredPrefabScale is enabled.
        resolved.LocalScale = Vector3.one;
        resolved.MirrorOnX = resolved.MirrorOnX || part.MirrorOnX;
    }

    private void ApplyPartAdjustments(CreatureMorphPartData part, Vector3 fallbackScale, bool hasSocket, ref ResolvedMorphTransform resolved)
    {
        if (part == null)
        {
            return;
        }

        Vector3 partScale = part.LocalScale == Vector3.zero ? Vector3.one : part.LocalScale;

        if (hasSocket)
        {
            if (part.AddLocalOffsetToSocket)
            {
                resolved.LocalPosition += part.LocalPosition;
            }
            else
            {
                resolved.LocalPosition = part.LocalPosition;
            }

            if (part.AddLocalRotationToSocket)
            {
                resolved.LocalRotationEuler += part.LocalRotationEuler;
            }
            else
            {
                resolved.LocalRotationEuler = part.LocalRotationEuler;
            }

            if (part.MultiplyLocalScaleBySocketScale)
            {
                resolved.LocalScale = MultiplyVector(resolved.LocalScale, partScale);
            }
            else
            {
                resolved.LocalScale = partScale;
            }
        }
        else
        {
            resolved.LocalPosition += part.LocalPosition;
            resolved.LocalRotationEuler += part.LocalRotationEuler;
            resolved.LocalScale = MultiplyVector(partScale, fallbackScale == Vector3.zero ? Vector3.one : fallbackScale);
        }

        if (part.ApplyGenomeShapeScale)
        {
            resolved.LocalScale = MultiplyVector(resolved.LocalScale, fallbackScale == Vector3.zero ? Vector3.one : fallbackScale);
        }

        resolved.MirrorOnX = resolved.MirrorOnX || part.MirrorOnX;
    }

    private GameObject SpawnResolvedPart(CreatureMorphSlot slot, string partId, CreatureMorphPartData part, ResolvedMorphTransform resolved, Color typeColour, PrimitiveType fallbackPrimitive)
    {
        if (part == null)
        {
            if (LogMissingMorphParts)
            {
                Debug.LogWarning("[IRP Morph] Missing MorphPartData for slot '" + slot + "' with PartId '" + partId + "'. Using generated fallback primitive.", this);
            }

            return SpawnFallbackPart(slot, partId, resolved, typeColour, fallbackPrimitive);
        }

        if (part.PartPrefab == null && LogMissingPrefabs)
        {
            Debug.LogWarning("[IRP Morph] MorphPartData '" + part.PartId + "' was found, but PartPrefab is empty. Using generated fallback primitive. Assign your custom prefab to this PartData asset if you want it used.", part);
        }

        GameObject first = SpawnPartFromData(part, resolved, typeColour);
        return first;
    }

    private GameObject SpawnPartFromData(CreatureMorphPartData part, ResolvedMorphTransform resolved, Color typeColour)
    {
        if (part == null)
        {
            return null;
        }

        Color colour = part.OverrideTypeColour ? part.DebugColour : typeColour;
        string displayName = part.PartPrefab == null && NameFallbackPartsClearly
            ? "FALLBACK_MISSING_PREFAB_" + part.PartId
            : part.DisplayName;

        GameObject first = SpawnSinglePart(part.PartPrefab, displayName, resolved.LocalPosition, resolved.LocalRotationEuler, resolved.LocalScale, colour);

        if (resolved.MirrorOnX && first != null)
        {
            Vector3 mirroredPosition = resolved.LocalPosition;
            mirroredPosition.x *= -1f;
            Vector3 mirroredRotation = resolved.LocalRotationEuler;
            mirroredRotation.z *= -1f;
            SpawnSinglePart(part.PartPrefab, part.DisplayName + " Mirror", mirroredPosition, mirroredRotation, resolved.LocalScale, colour);
        }

        return first;
    }

    private GameObject SpawnFallbackPart(CreatureMorphSlot slot, string partId, ResolvedMorphTransform resolved, Color typeColour, PrimitiveType primitive)
    {
        Color colour = GetFallbackPartColour(slot, partId, typeColour);
        string label = NameFallbackPartsClearly ? ("FALLBACK_" + slot + "_" + partId) : CreatureMorphLibrary.GetFallbackDisplayName(partId);
        GameObject first = SpawnSinglePart(null, label, resolved.LocalPosition, resolved.LocalRotationEuler, resolved.LocalScale, colour, primitive);

        if (resolved.MirrorOnX && first != null)
        {
            Vector3 mirroredPosition = resolved.LocalPosition;
            mirroredPosition.x *= -1f;
            Vector3 mirroredRotation = resolved.LocalRotationEuler;
            mirroredRotation.z *= -1f;
            SpawnSinglePart(null, label + " Mirror", mirroredPosition, mirroredRotation, resolved.LocalScale, colour, primitive);
        }

        return first;
    }

    private GameObject SpawnSinglePart(GameObject prefab, string name, Vector3 localPosition, Vector3 localRotationEuler, Vector3 localScale, Color colour, PrimitiveType fallbackPrimitive = PrimitiveType.Cube)
    {
        EnsureRoot();

        GameObject instance;
        bool usedPrefab = prefab != null;

        Vector3 authoredPrefabScale = Vector3.one;

        if (usedPrefab)
        {
            // Instantiate without a parent first, so Unity gives us the prefab's authored root scale.
            // Then parent with worldPositionStays=false so the scale stays local to MorphModel.
            instance = Instantiate(prefab);
            authoredPrefabScale = instance.transform.localScale == Vector3.zero ? Vector3.one : instance.transform.localScale;
            instance.transform.SetParent(modelRoot, false);
        }
        else
        {
            instance = GameObject.CreatePrimitive(fallbackPrimitive);
            instance.transform.SetParent(modelRoot, false);
        }

        instance.name = string.IsNullOrEmpty(name) ? "MorphPart" : name;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(localRotationEuler);

        if (usedPrefab)
        {
            if (PreserveAuthoredPrefabScale)
            {
                // Genome/socket/fallback scale only affects generated fallback primitives.
                instance.transform.localScale = authoredPrefabScale;
            }
            else
            {
                instance.transform.localScale = MultiplyVector(authoredPrefabScale, localScale == Vector3.zero ? Vector3.one : localScale);
            }
        }
        else
        {
            instance.transform.localScale = localScale == Vector3.zero ? Vector3.one : localScale;
        }

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
        activePrefabSockets.Clear();
        activeBodyInstance = null;

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

    private Vector3 ApplyBodyShapeToPosition(Vector3 value, EvolutionGenome genome)
    {
        return new Vector3(value.x * genome.BodyWidth, value.y * genome.BodyWidth, value.z * genome.BodyLength);
    }

    private Vector3 ApplyBodyShapeToScale(Vector3 value, EvolutionGenome genome)
    {
        return new Vector3(value.x * genome.BodyWidth, value.y * genome.BodyWidth, value.z * genome.BodyLength);
    }

    private Vector3 MultiplyVector(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    private Vector3 DivideVector(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Mathf.Abs(b.x) < 0.0001f ? a.x : a.x / b.x,
            Mathf.Abs(b.y) < 0.0001f ? a.y : a.y / b.y,
            Mathf.Abs(b.z) < 0.0001f ? a.z : a.z / b.z
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!DrawSocketGizmos)
        {
            return;
        }

        EnsureRoot();

        if (activePrefabSockets.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < activePrefabSockets.Count; i++)
            {
                CreatureMorphSocketMarker marker = activePrefabSockets[i];
                if (marker == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(marker.transform.position, SocketGizmoSize);
                Gizmos.DrawLine(marker.transform.position, marker.transform.position + marker.transform.forward * (SocketGizmoSize * 2.5f));
            }

            return;
        }

        if (!UseAssetSocketNodesFallback || activeGenome == null || activeBodyPart == null || activeBodyPart.SocketNodes == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        for (int i = 0; i < activeBodyPart.SocketNodes.Count; i++)
        {
            CreatureMorphSocketDefinition socket = activeBodyPart.SocketNodes[i];
            Vector3 localPosition = socket.ScalePositionByBodyShape ? ApplyBodyShapeToPosition(socket.LocalPosition, activeGenome) : socket.LocalPosition;
            Vector3 worldPosition = modelRoot.TransformPoint(localPosition);
            Gizmos.DrawWireSphere(worldPosition, SocketGizmoSize);

            if (socket.MirrorOnX)
            {
                localPosition.x *= -1f;
                worldPosition = modelRoot.TransformPoint(localPosition);
                Gizmos.DrawWireSphere(worldPosition, SocketGizmoSize);
            }
        }
    }

    private struct ResolvedMorphTransform
    {
        public Vector3 LocalPosition;
        public Vector3 LocalRotationEuler;
        public Vector3 LocalScale;
        public bool MirrorOnX;
    }
}
