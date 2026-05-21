using UnityEngine;

/// <summary>
/// Builds a simple readable fish-like body from genome values.
/// This is not meant to be final art. It is a debug/assessment visualisation so the evolved traits are visible.
/// </summary>
public class CreaturePhenotypeVisuals : MonoBehaviour
{
    [Header("Generated Parts")]
    public Transform BodyCore;
    public Transform Tail;
    public Transform LeftFin;
    public Transform RightFin;
    public Transform Mouth;
    public Transform LeftSensor;
    public Transform RightSensor;

    [Header("Visual Settings")]
    public bool AutoCreateParts = false; // Legacy fallback visuals disabled by default. v16 uses CreatureMorphBuilder.
    public bool ApplyGenomeShapeOnStart;
    public bool UseTypeColour = true;
    public Color FallbackColour = Color.white;

    private MaterialPropertyBlock propertyBlock;
    private Renderer[] partRenderers;

    private void Awake()
    {
        EnsureParts();
    }

    private void Start()
    {
        if (!ApplyGenomeShapeOnStart)
        {
            return;
        }

        MarineCreatureAgent agent = GetComponent<MarineCreatureAgent>();
        if (agent != null && agent.Candidate != null && agent.Candidate.Genome != null)
        {
            Color colour = CreatureDebugTypeUtility.GetTypeColour(agent.Candidate.BehaviourType);
            ApplyGenome(agent.Candidate.Genome, colour, UseTypeColour);
        }
    }

    public void ApplyGenome(EvolutionGenome genome, Color typeColour, bool applyColour)
    {
        if (genome == null)
        {
            return;
        }

        genome.ClampValues();
        EnsureParts();

        // Root scale remains BodySize on MarineCreatureAgent.
        // These child scales show which body traits caused that creature type.
        float armour = Mathf.Clamp(genome.Armour, 0f, 2f);
        float muscle = Mathf.Clamp(genome.Muscle, 0.25f, 2.5f);
        float fin = Mathf.Clamp(genome.FinSize, 0.35f, 2.5f);
        float tail = Mathf.Clamp(genome.TailSize, 0.35f, 2.5f);
        float jaw = Mathf.Clamp(genome.JawSize, 0.35f, 2.5f);
        float sensor = Mathf.Clamp(genome.SensorSize, 0.35f, 2.5f);

        if (BodyCore != null)
        {
            BodyCore.localPosition = Vector3.zero;
            BodyCore.localRotation = Quaternion.identity;
            BodyCore.localScale = new Vector3(
                0.72f + armour * 0.18f,
                0.42f + armour * 0.08f,
                1.05f + muscle * 0.22f
            );
        }

        if (Tail != null)
        {
            Tail.localPosition = new Vector3(0f, 0f, -0.78f - tail * 0.12f);
            Tail.localRotation = Quaternion.identity;
            Tail.localScale = new Vector3(
                0.18f + tail * 0.08f,
                0.18f + tail * 0.04f,
                0.38f + tail * 0.22f
            );
        }

        if (LeftFin != null)
        {
            LeftFin.localPosition = new Vector3(-0.55f - fin * 0.06f, 0f, -0.05f);
            LeftFin.localRotation = Quaternion.Euler(0f, 0f, -18f);
            LeftFin.localScale = new Vector3(0.36f + fin * 0.14f, 0.055f, 0.18f + fin * 0.08f);
        }

        if (RightFin != null)
        {
            RightFin.localPosition = new Vector3(0.55f + fin * 0.06f, 0f, -0.05f);
            RightFin.localRotation = Quaternion.Euler(0f, 0f, 18f);
            RightFin.localScale = new Vector3(0.36f + fin * 0.14f, 0.055f, 0.18f + fin * 0.08f);
        }

        if (Mouth != null)
        {
            Mouth.localPosition = new Vector3(0f, 0f, 0.78f + jaw * 0.11f);
            Mouth.localRotation = Quaternion.identity;
            Mouth.localScale = new Vector3(0.22f + jaw * 0.12f, 0.16f + jaw * 0.06f, 0.18f + jaw * 0.12f);
        }

        if (LeftSensor != null)
        {
            LeftSensor.localPosition = new Vector3(-0.22f - sensor * 0.045f, 0.22f + sensor * 0.03f, 0.55f);
            LeftSensor.localRotation = Quaternion.identity;
            LeftSensor.localScale = Vector3.one * (0.11f + sensor * 0.055f);
        }

        if (RightSensor != null)
        {
            RightSensor.localPosition = new Vector3(0.22f + sensor * 0.045f, 0.22f + sensor * 0.03f, 0.55f);
            RightSensor.localRotation = Quaternion.identity;
            RightSensor.localScale = Vector3.one * (0.11f + sensor * 0.055f);
        }

        if (applyColour)
        {
            ApplyColour(typeColour);
        }
    }

    public void ApplyColour(Color colour)
    {
        EnsureRenderers();

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < partRenderers.Length; i++)
        {
            Renderer rendererToColour = partRenderers[i];
            if (rendererToColour == null)
            {
                continue;
            }

            rendererToColour.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", colour);
            propertyBlock.SetColor("_Color", colour);
            rendererToColour.SetPropertyBlock(propertyBlock);
        }
    }

    private void EnsureParts()
    {
        if (!AutoCreateParts)
        {
            return;
        }

        BodyCore = EnsurePart(BodyCore, "Phenotype_Body", PrimitiveType.Sphere);
        Tail = EnsurePart(Tail, "Phenotype_Tail", PrimitiveType.Cube);
        LeftFin = EnsurePart(LeftFin, "Phenotype_LeftFin", PrimitiveType.Cube);
        RightFin = EnsurePart(RightFin, "Phenotype_RightFin", PrimitiveType.Cube);
        Mouth = EnsurePart(Mouth, "Phenotype_Mouth", PrimitiveType.Sphere);
        LeftSensor = EnsurePart(LeftSensor, "Phenotype_LeftSensor", PrimitiveType.Sphere);
        RightSensor = EnsurePart(RightSensor, "Phenotype_RightSensor", PrimitiveType.Sphere);
    }

    private Transform EnsurePart(Transform existing, string partName, PrimitiveType primitiveType)
    {
        if (existing != null)
        {
            return existing;
        }

        Transform found = transform.Find(partName);
        if (found != null)
        {
            return found;
        }

        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        return part.transform;
    }

    private void EnsureRenderers()
    {
        EnsureParts();
        partRenderers = GetComponentsInChildren<Renderer>();
    }
}
