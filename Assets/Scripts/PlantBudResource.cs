using UnityEngine;

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
    public float DetachImpulseMin = 0.25f;
    public float DetachImpulseMax = 0.85f;

    private FoodSource food;
    private float detachedTimer;
    private Vector3 driftVelocity;
    private bool hasBeenBitten;

    private void Awake()
    {
        food = GetComponent<FoodSource>();
    }

    private void OnEnable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.RegisterFood(GetComponent<FoodSource>());
        }
    }

    private void OnDisable()
    {
        if (EvolutionEcosystemManager.Instance != null)
        {
            EvolutionEcosystemManager.Instance.UnregisterFood(GetComponent<FoodSource>());
        }
    }

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

        detachedTimer += Time.deltaTime;

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

            driftVelocity = Vector3.Lerp(driftVelocity, wanted, Time.deltaTime * 2.5f);
            transform.position += driftVelocity * Time.deltaTime;
            transform.position = manager.ClampToSimulationArea(transform.position);
        }

        if (CanSpoilWhenDetached && detachedTimer >= DetachedLifeTime)
        {
            NotifyParentConsumed();
            Destroy(gameObject);
        }
    }

    public void NotifyBitten(float massTaken, int feederId)
    {
        if (hasBeenBitten || massTaken <= 0f)
        {
            return;
        }

        hasBeenBitten = true;
        if (DetachWhenBitten && AttachedToPlant)
        {
            DetachFromPlant(BuildBiteImpulse());
        }
    }

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

    private void NotifyParentConsumed()
    {
        if (ParentPlant != null)
        {
            ParentPlant.NotifyBudRemoved(this);
            ParentPlant = null;
        }
    }
}
