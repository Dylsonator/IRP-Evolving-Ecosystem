using UnityEngine;

public enum EcosystemZoneVolumeMode
{
    Include,
    Exclude
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class EcosystemZoneVolume : MonoBehaviour
{
    [Header("Zone Volume")]
    public EcosystemZoneVolumeMode Mode = EcosystemZoneVolumeMode.Include;
    [Tooltip("Higher priority include volumes win if several zones overlap. Exclude volumes cut holes from this zone.")]
    public float Weight = 1f;

    [Header("Soft Edge")]
    [Tooltip("0 means hard edge. Higher values let the zone fade slightly around the collider edge.")]
    public float EdgeFadeDistance = 0f;

    [Header("Debug")]
    public bool DrawGizmo = true;
    public Color IncludeColour = new Color(0f, 1f, 0.35f, 0.2f);
    public Color ExcludeColour = new Color(1f, 0.15f, 0.05f, 0.25f);

    private Collider cachedCollider;

    public Collider VolumeCollider
    {
        get
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider>();
            }

            return cachedCollider;
        }
    }

    private void Reset()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        Collider collider = VolumeCollider;
        if (collider == null || !collider.enabled || !gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 closest = collider.ClosestPoint(worldPoint);
        return (closest - worldPoint).sqrMagnitude <= 0.0004f;
    }

    public float GetInfluence01(Vector3 worldPoint)
    {
        Collider collider = VolumeCollider;
        if (collider == null || !collider.enabled || !gameObject.activeInHierarchy)
        {
            return 0f;
        }

        if (ContainsPoint(worldPoint))
        {
            return Mathf.Max(0f, Weight);
        }

        if (EdgeFadeDistance <= 0.001f)
        {
            return 0f;
        }

        Vector3 closest = collider.ClosestPoint(worldPoint);
        float distance = Vector3.Distance(worldPoint, closest);
        return Mathf.Clamp01(1f - distance / EdgeFadeDistance) * Mathf.Max(0f, Weight);
    }

    public Vector3 GetRandomPointInBounds()
    {
        Collider collider = VolumeCollider;
        if (collider == null)
        {
            return transform.position;
        }

        BoxCollider box = collider as BoxCollider;
        if (box != null)
        {
            Vector3 local = box.center + new Vector3(
                Random.Range(-box.size.x * 0.5f, box.size.x * 0.5f),
                Random.Range(-box.size.y * 0.5f, box.size.y * 0.5f),
                Random.Range(-box.size.z * 0.5f, box.size.z * 0.5f));

            return box.transform.TransformPoint(local);
        }

        SphereCollider sphere = collider as SphereCollider;
        if (sphere != null)
        {
            Vector3 local = sphere.center + Random.insideUnitSphere * sphere.radius;
            return sphere.transform.TransformPoint(local);
        }

        CapsuleCollider capsule = collider as CapsuleCollider;
        if (capsule != null)
        {
            return GetRandomPointInCapsule(capsule);
        }

        Bounds bounds = collider.bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z));
    }

    private Vector3 GetRandomPointInCapsule(CapsuleCollider capsule)
    {
        float radius = Mathf.Max(0.001f, capsule.radius);
        float height = Mathf.Max(radius * 2f, capsule.height);
        float cylinderLength = Mathf.Max(0f, height - radius * 2f);

        Vector3 axis = Vector3.up;
        if (capsule.direction == 0)
        {
            axis = Vector3.right;
        }
        else if (capsule.direction == 2)
        {
            axis = Vector3.forward;
        }

        Vector3 local = capsule.center;
        local += axis * Random.Range(-cylinderLength * 0.5f, cylinderLength * 0.5f);

        Vector2 disc = Random.insideUnitCircle * radius;
        if (capsule.direction == 0)
        {
            local += new Vector3(0f, disc.x, disc.y);
        }
        else if (capsule.direction == 1)
        {
            local += new Vector3(disc.x, 0f, disc.y);
        }
        else
        {
            local += new Vector3(disc.x, disc.y, 0f);
        }

        return capsule.transform.TransformPoint(local);
    }

    private void OnDrawGizmosSelected()
    {
        if (!DrawGizmo)
        {
            return;
        }

        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        Gizmos.color = Mode == EcosystemZoneVolumeMode.Include ? IncludeColour : ExcludeColour;
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
    }
}
