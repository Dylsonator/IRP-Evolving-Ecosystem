using UnityEngine;

// Optional trigger for body parts so predator bites can hit visible fish parts
// Optional child trigger so bites can hit spawned body parts and still find the fish root
[RequireComponent(typeof(Collider))]
public class CreatureHurtbox : MonoBehaviour
{
    public MarineCreatureAgent Owner;

    // Finds the fish root and makes this child collider act as a bite trigger
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

    // Keeps the hurtbox collider as a trigger while editing in Unity
    private void OnValidate()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = true;
        }
    }
}
