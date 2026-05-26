using UnityEngine;

/// <summary>
/// Optional trigger component for child body-part hit detection.
/// Add this to one or two simple trigger colliders on the visible body/jaw area if predators still look like they touch prey without biting.
/// MarineCreatureAgent will not disable colliders with this component during visual-collider cleanup.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CreatureHurtbox : MonoBehaviour
{
    public MarineCreatureAgent Owner;

    private void Awake()
    {
        if (Owner == null)
        {
            Owner = GetComponentInParent<MarineCreatureAgent>();
        }

        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = true;
            c.enabled = true;
        }
    }

    private void OnValidate()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = true;
        }
    }
}
