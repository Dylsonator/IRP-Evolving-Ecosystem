using UnityEngine;

// Links an edible bud to its parent plant and handles detaching, drifting and spoil timing.
[RequireComponent(typeof(FoodSource))]
public class PlantBudResource : MonoBehaviour
{
    public PlantResource ParentPlant;
    public Transform Socket;
    public bool AttachedToPlant = true;
    public bool CanSpoilWhenDetached = true;
    public float DetachedLifeTime = 45f;
    public float DriftCurrentMultiplier = 1f;
    public float SettleSpeed = 2.2f;
    public float GroundClearance = 0.18f;
    public bool DetachWhenBitten = true;
    [Range(0f, 1f)] public float DetachOnlyBelowMassRatio = 0.02f;
    public float DetachImpulseMin = 0.25f;
    public float DetachImpulseMax = 0.85f;

    [Header("Performance")]
    public float DetachedUpdateInterval = 0.12f;

    private FoodSource food;
    private float detachedTimer;
    private float detachedUpdateTimer;
    private Vector3 driftVelocity;
    private bool hasBeenBitten;

    // Sets up cached references and safe starting values before the sim runs
    private void Awake()
    {
        food = GetComponent<FoodSource>();
    }


    // Runs the normal frame checks and timers
    private void Update()
    {
        if (food != null && food.IsConsumed)
        {
            NotifyParentConsumed();
            return;
        }

        if (AttachedToPlant)
        {
            if (Socket != null)
            {
                transform.position = Socket.position;
                transform.rotation = Socket.rotation;
            }
            return;
        }

        float dt = Time.deltaTime;
        detachedTimer += dt;
        detachedUpdateTimer -= dt;

        if (detachedUpdateTimer > 0f)
        {
            if (CanSpoilWhenDetached && detachedTimer >= DetachedLifeTime)
            {
                NotifyParentConsumed();
                Destroy(gameObject);
            }
            return;
        }

        detachedUpdateTimer = Mathf.Max(0.02f, DetachedUpdateInterval);

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager != null)
        {
            Vector3 current = manager.GetCurrentVelocityAt(transform.position) * DriftCurrentMultiplier;
            Vector3 wanted = current;

            if (manager.TryGetTerrainHeight(transform.position, out float groundY))
            {
                float targetY = groundY + GroundClearance;
                if (transform.position.y > targetY + 0.05f)
                {
                    wanted += Vector3.down * SettleSpeed;
                }
                else
                {
                    Vector3 p = transform.position;
                    p.y = Mathf.Max(p.y, targetY);
                    transform.position = p;
                    wanted.y = Mathf.Max(0f, wanted.y);
                }
            }

            float step = Mathf.Max(0.02f, DetachedUpdateInterval);
            driftVelocity = Vector3.Lerp(driftVelocity, wanted, step * 2.5f);
            transform.position += driftVelocity * step;
            transform.position = manager.ClampToSimulationArea(transform.position);
        }

        if (CanSpoilWhenDetached && detachedTimer >= DetachedLifeTime)
        {
            NotifyParentConsumed();
            Destroy(gameObject);
        }
    }

    // Marks a bud as bitten and may detach it if the settings allow
    public void NotifyBitten(float massTaken, int feederId)
    {
        if (massTaken <= 0f)
        {
            return;
        }

        // Do not knock buds off on the first nibble. They only detach when essentially
        // finished, so creatures do not strip a plant and then ignore loose food.
        if (!DetachWhenBitten || !AttachedToPlant)
        {
            hasBeenBitten = true;
            return;
        }

        float massRatio = food != null ? food.GetMassRatio() : 0f;
        if (massRatio <= DetachOnlyBelowMassRatio)
        {
            hasBeenBitten = true;
            DetachFromPlant(BuildBiteImpulse());
        }
    }

    // Detaches the bud from its parent and lets it drift in the water
    public void DetachFromPlant(Vector3 impulse)
    {
        if (!AttachedToPlant)
        {
            return;
        }

        AttachedToPlant = false;
        Socket = null;
        transform.SetParent(null, true);
        driftVelocity = impulse;
        detachedTimer = 0f;

        if (ParentPlant != null)
        {
            ParentPlant.NotifyBudDetached(this);
            ParentPlant = null;
        }
    }

    // Builds a small drift impulse when a bud gets knocked loose
    private Vector3 BuildBiteImpulse()
    {
        Vector3 impulse = Random.insideUnitSphere;
        impulse.y = Mathf.Abs(impulse.y) * 0.15f;

        if (EvolutionEcosystemManager.Instance != null)
        {
            Vector3 current = EvolutionEcosystemManager.Instance.GetCurrentVelocityAt(transform.position);
            if (current.sqrMagnitude > 0.001f)
            {
                impulse += current.normalized;
            }
        }

        if (impulse.sqrMagnitude <= 0.001f)
        {
            impulse = transform.forward;
        }

        return impulse.normalized * Random.Range(DetachImpulseMin, DetachImpulseMax);
    }

    // Tells the parent plant this bud is gone
    private void NotifyParentConsumed()
    {
        if (ParentPlant != null)
        {
            ParentPlant.NotifyBudRemoved(this);
            ParentPlant = null;
        }
    }
}
