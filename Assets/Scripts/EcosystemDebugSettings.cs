using UnityEngine;

public class EcosystemDebugSettings : MonoBehaviour
{
    public static EcosystemDebugSettings Instance { get; private set; }

    [Header("Observation Debug Drawing")]
    public bool DrawCreatureMovementRays = false;
    public bool DrawFoodTargetRays = true;
    public bool DrawCarrionTargetRays = true;
    public bool DrawPreyTargetRays = true;
    public bool DrawSocialTargetRays = true;
    public bool DrawVelocityRays = true;
    public bool DrawWantedDirectionRays = true;
    public bool DrawMouthRange = true;
    public bool DrawBiteRange = true;
    public bool DrawVisionRange = false;
    public bool DrawBoundaryPush = false;
    public bool DrawBoidRays = true;

    [Header("Labels / Panels")]
    public bool ShowCreatureLabels = false;
    public bool ShowDietInLabels = true;
    public bool ShowSelectedFishPanel = true;
    public bool HighlightSelectedFish = true;
    public bool ShowMovementStateInLabels = true;
    public bool ShowVerticalReasonInLabels = true;
    public float LabelMaxDistance = 80f;
    public Vector2 LabelOffset = new Vector2(0f, -14f);
    public int MaxLabelCount = 80;

    [Header("Screenshots / Graphs")]
    public bool AutoCaptureGenerationScreenshots = false;
    public bool DrawPreferredDepthLine = false;
    public int MaxGraphPoints = 120;

    [Header("Ray Lengths")]
    public float WantedDirectionRayLength = 4f;
    public float VelocityRayScale = 0.4f;
    public float FoodRayDuration = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}
