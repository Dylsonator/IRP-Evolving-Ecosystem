using System.Collections.Generic;
using UnityEngine;

public enum EcosystemPressureZoneType
{
    OpenWater,
    PlantRich,
    Shelter,
    PredatorCover,
    CarrionRich,
    HarshWater,
    NestingGround
}

public class EcosystemPressureZone : MonoBehaviour
{
    [Header("Identity")]
    public string ZoneName = "Habitat Zone";
    public EcosystemPressureZoneType ZoneType = EcosystemPressureZoneType.OpenWater;

    [Header("Shape")]
    [Tooltip("Use child EcosystemZoneVolume colliders as the zone shape. Include volumes add area. Exclude volumes cut holes/inside gaps.")]
    public bool UseColliderVolumes = true;
    [Tooltip("Used only when no collider volumes are found, or as a rough fallback.")]
    public float Radius = 18f;
    [Tooltip("Used only when no collider volumes are found, or as a rough fallback.")]
    public float VerticalRadius = 8f;
    [Range(0f, 1f)] public float PreferredDepth01 = 0.5f;
    [Range(0f, 1f)] public float DepthInfluence = 0.55f;

    [Header("Resources")]
    [Tooltip("Higher values make plant food more likely to spawn in this area.")]
    public float PlantFoodWeight = 1f;
    [Tooltip("Higher values make carrion stay useful in this area later when carrion spawning is expanded.")]
    public float CarrionWeight = 0.25f;

    [Header("Pressure")]
    [Range(0f, 1f)] public float DangerPressure = 0f;
    [Range(0f, 1f)] public float PredatorCover = 0f;
    [Range(0f, 1f)] public float Shelter = 0.35f;
    [Range(0f, 2f)] public float EnergyDrainMultiplier = 1f;
    [Range(0f, 2f)] public float MutationPressure = 1f;

    [Header("Diet / Body Affinity")]
    [Range(0f, 1f)] public float PlantAffinity = 0.75f;
    [Range(0f, 1f)] public float MeatAffinity = 0.25f;
    [Range(0f, 1f)] public float CarrionAffinity = 0.25f;
    [Range(0f, 1f)] public float SchoolingAffinity = 0.5f;
    [Range(0f, 1f)] public float LargeBodyAffinity = 0.5f;

    [Header("Debug")]
    public bool DrawFallbackRadius = false;
    public bool DrawZoneBounds = true;

    private readonly List<EcosystemZoneVolume> includeVolumes = new List<EcosystemZoneVolume>();
    private readonly List<EcosystemZoneVolume> excludeVolumes = new List<EcosystemZoneVolume>();
    private float nextVolumeRefreshTime;

    public float GetInfluence01(Vector3 position)
    {
        if (UseColliderVolumes)
        {
            RefreshVolumesIfNeeded();

            if (includeVolumes.Count > 0)
            {
                float include = 0f;
                for (int i = 0; i < includeVolumes.Count; i++)
                {
                    if (includeVolumes[i] != null)
                    {
                        include = Mathf.Max(include, includeVolumes[i].GetInfluence01(position));
                    }
                }

                if (include <= 0f)
                {
                    return 0f;
                }

                float exclude = 0f;
                for (int i = 0; i < excludeVolumes.Count; i++)
                {
                    if (excludeVolumes[i] != null)
                    {
                        exclude = Mathf.Max(exclude, excludeVolumes[i].GetInfluence01(position));
                    }
                }

                return Mathf.Clamp01(include * (1f - Mathf.Clamp01(exclude)));
            }
        }

        return GetFallbackRadiusInfluence01(position);
    }

    public bool Contains(Vector3 position)
    {
        return GetInfluence01(position) > 0f;
    }

    public float GetFoodSpawnWeight()
    {
        float typeBonus = ZoneType == EcosystemPressureZoneType.PlantRich ? 1.75f : 1f;
        return Mathf.Max(0f, PlantFoodWeight * typeBonus * Mathf.Lerp(1.15f, 0.35f, DangerPressure));
    }

    public float GetHabitatSuitability(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return Shelter;
        }

        float diet = genome.PlantDiet * PlantAffinity + genome.MeatDiet * MeatAffinity + genome.CarrionDiet * CarrionAffinity;
        float school = 1f - Mathf.Abs(genome.GroupingChance - SchoolingAffinity);
        float size = 1f - Mathf.Abs(Mathf.InverseLerp(0.4f, 2.8f, genome.BodySize) - LargeBodyAffinity);
        float dangerFit = Mathf.Lerp(1f - DangerPressure, DangerPressure, genome.Bravery);
        float shelterFit = Mathf.Lerp(0.65f, 1.35f, Shelter * genome.HabitatLoyalty);
        float nestFit = ZoneType == EcosystemPressureZoneType.NestingGround ? genome.NestingDrive * 0.35f : 0f;

        return Mathf.Max(0.01f, diet * 0.38f + school * 0.18f + size * 0.12f + dangerFit * 0.20f + shelterFit * 0.12f + nestFit);
    }

    public float GetStressAt(Vector3 position)
    {
        float influence = GetInfluence01(position);
        return influence * Mathf.Clamp01(DangerPressure + Mathf.Max(0f, EnergyDrainMultiplier - 1f) * 0.5f);
    }

    public Vector3 GetRandomPointInside(EvolutionEcosystemManager manager)
    {
        if (UseColliderVolumes)
        {
            RefreshVolumesIfNeeded(true);
            Vector3 volumePoint;
            if (TryGetRandomPointFromVolumes(manager, out volumePoint))
            {
                return volumePoint;
            }
        }

        return GetRandomPointInsideFallback(manager);
    }

    public Bounds GetWorldBounds()
    {
        RefreshVolumesIfNeeded(true);

        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < includeVolumes.Count; i++)
        {
            EcosystemZoneVolume volume = includeVolumes[i];
            if (volume == null || volume.VolumeCollider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = volume.VolumeCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(volume.VolumeCollider.bounds);
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(transform.position, new Vector3(Radius * 2f, VerticalRadius * 2f, Radius * 2f));
        }

        return bounds;
    }

    private void RefreshVolumesIfNeeded(bool force = false)
    {
        if (!force && Application.isPlaying && Time.time < nextVolumeRefreshTime)
        {
            return;
        }

        nextVolumeRefreshTime = Application.isPlaying ? Time.time + 1f : 0f;
        includeVolumes.Clear();
        excludeVolumes.Clear();

        EcosystemZoneVolume[] volumes = GetComponentsInChildren<EcosystemZoneVolume>(true);
        for (int i = 0; i < volumes.Length; i++)
        {
            if (volumes[i] == null || !volumes[i].enabled)
            {
                continue;
            }

            if (volumes[i].Mode == EcosystemZoneVolumeMode.Exclude)
            {
                excludeVolumes.Add(volumes[i]);
            }
            else
            {
                includeVolumes.Add(volumes[i]);
            }
        }
    }

    private bool TryGetRandomPointFromVolumes(EvolutionEcosystemManager manager, out Vector3 point)
    {
        point = transform.position;

        if (includeVolumes.Count == 0)
        {
            return false;
        }

        for (int attempt = 0; attempt < 80; attempt++)
        {
            EcosystemZoneVolume volume = includeVolumes[Random.Range(0, includeVolumes.Count)];
            if (volume == null)
            {
                continue;
            }

            Vector3 candidate = volume.GetRandomPointInBounds();
            if (manager != null)
            {
                candidate = manager.ClampToSimulationArea(candidate);
            }

            if (Contains(candidate))
            {
                point = candidate;
                return true;
            }
        }

        Bounds bounds = GetWorldBounds();
        for (int attempt = 0; attempt < 120; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            if (manager != null)
            {
                candidate = manager.ClampToSimulationArea(candidate);
            }

            if (Contains(candidate))
            {
                point = candidate;
                return true;
            }
        }

        return false;
    }

    private float GetFallbackRadiusInfluence01(Vector3 position)
    {
        Vector3 delta = position - transform.position;
        float horizontal = new Vector2(delta.x, delta.z).magnitude / Mathf.Max(0.01f, Radius);
        float vertical = Mathf.Abs(delta.y) / Mathf.Max(0.01f, VerticalRadius);
        float distance01 = Mathf.Max(horizontal, vertical);
        return Mathf.Clamp01(1f - distance01);
    }

    private Vector3 GetRandomPointInsideFallback(EvolutionEcosystemManager manager)
    {
        Vector2 circle = Random.insideUnitCircle * Radius;
        Vector3 point = transform.position + new Vector3(circle.x, 0f, circle.y);

        if (manager != null)
        {
            Vector3 centre = manager.transform.position;
            Vector3 half = manager.SimulationAreaSize * 0.5f;
            float low = centre.y - half.y + manager.SpawnPaddingFromBounds;
            float high = centre.y + half.y - manager.SpawnPaddingFromBounds;
            float preferredY = Mathf.Lerp(low, high, PreferredDepth01);
            point.y = preferredY + Random.Range(-VerticalRadius, VerticalRadius) * 0.35f;
            point = manager.ClampToSimulationArea(point);
        }
        else
        {
            point.y += Random.Range(-VerticalRadius, VerticalRadius) * 0.35f;
        }

        return point;
    }

    private Color GetZoneColour()
    {
        Color colour = Color.cyan;
        if (ZoneType == EcosystemPressureZoneType.PlantRich) colour = Color.green;
        else if (ZoneType == EcosystemPressureZoneType.PredatorCover) colour = Color.red;
        else if (ZoneType == EcosystemPressureZoneType.Shelter) colour = Color.blue;
        else if (ZoneType == EcosystemPressureZoneType.CarrionRich) colour = new Color(0.5f, 0.25f, 0.1f);
        else if (ZoneType == EcosystemPressureZoneType.HarshWater) colour = Color.magenta;
        else if (ZoneType == EcosystemPressureZoneType.NestingGround) colour = Color.yellow;
        return colour;
    }

    private void OnDrawGizmosSelected()
    {
        Color colour = GetZoneColour();
        Gizmos.color = colour;

        if (DrawFallbackRadius && (!UseColliderVolumes || GetComponentsInChildren<EcosystemZoneVolume>(true).Length == 0))
        {
            Gizmos.DrawWireSphere(transform.position, Radius);
        }

        if (DrawZoneBounds)
        {
            Bounds bounds = GetWorldBounds();
            colour.a = 0.35f;
            Gizmos.color = colour;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
