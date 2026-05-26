using UnityEngine;

public class EcosystemDebugSettings : MonoBehaviour
{
    public static EcosystemDebugSettings Instance { get; private set; }

    [Header("Observation Debug Drawing")]
    public bool DrawCreatureMovementRays = false;
    public bool DrawFoodTargetRays = false;
    public bool DrawCarrionTargetRays = false;
    public bool DrawPreyTargetRays = false;
    public bool DrawSocialTargetRays = false;
    public bool DrawVelocityRays = false;
    public bool DrawWantedDirectionRays = false;
    public bool DrawMouthRange = false;
    public bool DrawBiteRange = false;
    public bool DrawVisionRange = false;
    public bool DrawBoundaryPush = false;
    public bool DrawBoidRays = false;

    [Header("Labels / Panels")]
    public bool ShowCreatureLabels = false;
    public bool ShowDietInLabels = true;
    public bool ShowSelectedFishPanel = false;
    public bool HighlightSelectedFish = false;
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

        DrawCreatureMovementRays = false;
        DrawFoodTargetRays = false;
        DrawCarrionTargetRays = false;
        DrawPreyTargetRays = false;
        DrawSocialTargetRays = false;
        DrawVelocityRays = false;
        DrawWantedDirectionRays = false;
        DrawMouthRange = false;
        DrawBiteRange = false;
        DrawVisionRange = false;
        DrawBoundaryPush = false;
        DrawBoidRays = false;
        ShowCreatureLabels = false;
        ShowSelectedFishPanel = false;
        HighlightSelectedFish = false;
        AutoCaptureGenerationScreenshots = false;
    }
}
