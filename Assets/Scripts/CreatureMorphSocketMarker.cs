using UnityEngine;

/// <summary>
/// Place this component on empty child objects inside a BODY prefab.
/// The morph builder will use these markers as attachment points for tails, fins, jaws, eyes, armour, spikes, etc.
/// </summary>
public class CreatureMorphSocketMarker : MonoBehaviour
{
    [Header("Socket Identity")]
    public CreatureMorphSlot Slot = CreatureMorphSlot.Tail;
    public string SocketName = "";

    [Header("Spawn")]
    public bool SpawnPart = true;
    public bool MirrorOnX;

    [Header("Transform Correction")]
    [Tooltip("Optional small offset in this marker's local space.")]
    public Vector3 PartPositionOffset = Vector3.zero;

    [Tooltip("Optional rotation added after the marker rotation.")]
    public Vector3 PartRotationOffset = Vector3.zero;

    [Tooltip("Extra scale multiplier for the spawned part.")]
    public Vector3 PartScaleMultiplier = Vector3.one;

    [Tooltip("Only used if CreatureMorphBuilder.UseMarkerScaleForAttachedParts is enabled. Keep off for normal prefab parts.")]
    public bool UseMarkerScale = false;

    [Header("Debug")]
    public Color GizmoColour = Color.yellow;
    public float GizmoSize = 0.08f;

    private void OnDrawGizmos()
    {
        Gizmos.color = GizmoColour;
        Gizmos.DrawWireSphere(transform.position, GizmoSize);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * (GizmoSize * 2.5f));
    }
}
