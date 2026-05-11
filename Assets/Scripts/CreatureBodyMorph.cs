using UnityEngine;

public class CreatureBodyMorph : MonoBehaviour
{
    [Header("Auto Primitive Body")]
    public bool AutoBuildPrimitiveBody = true;
    public bool HideOriginalRenderer = true;

    private Transform body;
    private Transform tail;
    private Transform leftFin;
    private Transform rightFin;
    private Transform jaw;
    private Transform sensor;

    private Renderer[] generatedRenderers;
    private bool built;

    public void ApplyGenome(EvolutionGenome genome)
    {
        if (genome == null)
        {
            return;
        }

        if (AutoBuildPrimitiveBody && !built)
        {
            BuildBody();
        }

        if (body == null)
        {
            return;
        }

        Color bodyColour = SpeciesUtility.GetBodyColour(genome);
        Color darker = Color.Lerp(bodyColour, Color.black, 0.25f);
        Color lighter = Color.Lerp(bodyColour, Color.white, 0.25f);

        body.localScale = new Vector3(0.9f + genome.Muscle * 0.22f, 0.65f + genome.Armour * 0.18f, 1.35f + genome.BodySize * 0.28f);
        tail.localScale = new Vector3(0.28f + genome.FinSize * 0.08f, 0.18f + genome.FinSize * 0.05f, 0.75f + genome.TailSize * 0.48f);
        tail.localPosition = new Vector3(0f, 0f, -0.98f - genome.TailSize * 0.18f);

        leftFin.localScale = new Vector3(0.18f + genome.FinSize * 0.16f, 0.08f, 0.55f + genome.FinSize * 0.22f);
        rightFin.localScale = leftFin.localScale;

        jaw.localScale = new Vector3(0.35f + genome.JawSize * 0.2f, 0.18f + genome.JawSize * 0.1f, 0.28f + genome.JawSize * 0.18f);
        jaw.localPosition = new Vector3(0f, -0.03f, 0.82f + genome.JawSize * 0.08f);

        sensor.localScale = Vector3.one * (0.16f + genome.SensorSize * 0.09f);
        sensor.localPosition = new Vector3(0f, 0.32f + genome.SensorSize * 0.03f, 0.58f);

        SetRendererColour(body, bodyColour);
        SetRendererColour(tail, darker);
        SetRendererColour(leftFin, darker);
        SetRendererColour(rightFin, darker);
        SetRendererColour(jaw, Color.Lerp(darker, Color.red, genome.Aggression * 0.25f));
        SetRendererColour(sensor, lighter);
    }

    private void BuildBody()
    {
        built = true;

        if (HideOriginalRenderer)
        {
            Renderer ownRenderer = GetComponent<Renderer>();
            if (ownRenderer != null)
            {
                ownRenderer.enabled = false;
            }
        }

        body = CreatePrimitivePart("EvolvedBody", PrimitiveType.Sphere).transform;
        tail = CreatePrimitivePart("TailSpeedPart", PrimitiveType.Cube).transform;
        leftFin = CreatePrimitivePart("LeftFinTurnPart", PrimitiveType.Cube).transform;
        rightFin = CreatePrimitivePart("RightFinTurnPart", PrimitiveType.Cube).transform;
        jaw = CreatePrimitivePart("JawAttackPart", PrimitiveType.Cube).transform;
        sensor = CreatePrimitivePart("SensorVisionPart", PrimitiveType.Sphere).transform;

        body.SetParent(transform, false);
        tail.SetParent(transform, false);
        leftFin.SetParent(transform, false);
        rightFin.SetParent(transform, false);
        jaw.SetParent(transform, false);
        sensor.SetParent(transform, false);

        body.localPosition = Vector3.zero;
        tail.localPosition = new Vector3(0f, 0f, -1f);
        leftFin.localPosition = new Vector3(-0.58f, 0f, 0.05f);
        rightFin.localPosition = new Vector3(0.58f, 0f, 0.05f);
        jaw.localPosition = new Vector3(0f, -0.03f, 0.9f);
        sensor.localPosition = new Vector3(0f, 0.35f, 0.55f);

        leftFin.localRotation = Quaternion.Euler(0f, 0f, -18f);
        rightFin.localRotation = Quaternion.Euler(0f, 0f, 18f);

        generatedRenderers = GetComponentsInChildren<Renderer>();
    }

    private GameObject CreatePrimitivePart(string partName, PrimitiveType primitiveType)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;

        Collider col = part.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        return part;
    }

    private void SetRendererColour(Transform part, Color colour)
    {
        if (part == null)
        {
            return;
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = colour;
        }
    }
}
