using System.IO;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FishSelectionDebugPanel : MonoBehaviour
{
    [Header("Selection")]
    public MarineCreatureAgent SelectedFish;
    public bool ShowPanel = true;
    public Camera SelectionCamera;
    public LayerMask SelectionMask = ~0;
    public float SelectionDistance = 500f;

    [Header("Panel")]
    public Vector2 PanelPosition = new Vector2(12f, 235f);
    public Vector2 PanelSize = new Vector2(430f, 520f);

    [Header("Saving")]
    public string SnapshotFolder = "IRP_SelectedFishSnapshots";

    [Header("Performance")]
    public bool AllowSelectionWhenPanelHidden = true;
    public bool OpenPanelOnSelect = true;
    public bool UseSphereCastFallback = true;
    public float SelectionSphereRadius = 0.45f;
    public bool UseScreenDistanceFallback = true;
    public float FallbackScreenPickRadiusPixels = 55f;
    public float CameraRefreshInterval = 1f;

    private float cameraRefreshTimer;

    private void Awake()
    {
        if (SelectionCamera == null)
        {
            SelectionCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!ShowPanel && !AllowSelectionWhenPanelHidden)
        {
            return;
        }

        TrySelectFishFromMouse();
    }

    private void TrySelectFishFromMouse()
    {
        if (!WasLeftMousePressedThisFrame())
        {
            return;
        }

        if (SelectionCamera == null)
        {
            cameraRefreshTimer -= Time.deltaTime;
            if (cameraRefreshTimer <= 0f)
            {
                cameraRefreshTimer = Mathf.Max(0.05f, CameraRefreshInterval);
                SelectionCamera = Camera.main;
            }
        }

        if (SelectionCamera == null)
        {
            return;
        }

        Vector2 mousePosition = GetMouseScreenPosition();
        Ray ray = SelectionCamera.ScreenPointToRay(mousePosition);
        MarineCreatureAgent creature = null;

        if (Physics.Raycast(ray, out RaycastHit hit, SelectionDistance, SelectionMask, QueryTriggerInteraction.Collide))
        {
            creature = hit.collider.GetComponentInParent<MarineCreatureAgent>();

            CreatureHurtbox hurtbox = creature == null ? hit.collider.GetComponentInParent<CreatureHurtbox>() : null;
            if (hurtbox != null)
            {
                creature = hurtbox.Owner;
            }
        }

        if (creature == null && UseSphereCastFallback)
        {
            creature = TrySpherePick(ray);
        }

        if (creature == null && UseScreenDistanceFallback)
        {
            creature = TryScreenDistancePick(mousePosition);
        }

        SelectFish(creature);
    }


    private MarineCreatureAgent TrySpherePick(Ray ray)
    {
        float radius = Mathf.Max(0.05f, SelectionSphereRadius);
        RaycastHit[] hits = Physics.SphereCastAll(ray, radius, SelectionDistance, SelectionMask, QueryTriggerInteraction.Collide);
        MarineCreatureAgent best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            MarineCreatureAgent creature = hitCollider.GetComponentInParent<MarineCreatureAgent>();
            if (creature == null)
            {
                CreatureHurtbox hurtbox = hitCollider.GetComponentInParent<CreatureHurtbox>();
                if (hurtbox != null)
                {
                    creature = hurtbox.Owner;
                }
            }

            if (creature == null)
            {
                continue;
            }

            float distance = hits[i].distance;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = creature;
            }
        }

        return best;
    }

    private MarineCreatureAgent TryScreenDistancePick(Vector2 mousePosition)
    {
        if (SelectionCamera == null || EvolutionEcosystemManager.Instance == null)
        {
            return null;
        }

        System.Collections.Generic.List<MarineCreatureAgent> creatures = EvolutionEcosystemManager.Instance.GetActiveCreatures();
        if (creatures == null || creatures.Count == 0)
        {
            return null;
        }

        MarineCreatureAgent best = null;
        float bestScreenDistance = Mathf.Max(4f, FallbackScreenPickRadiusPixels);
        float bestDepth = float.MaxValue;

        for (int i = 0; i < creatures.Count; i++)
        {
            MarineCreatureAgent creature = creatures[i];
            if (creature == null)
            {
                continue;
            }

            Vector3 screen = SelectionCamera.WorldToScreenPoint(creature.transform.position);
            if (screen.z <= 0f || screen.z > SelectionDistance)
            {
                continue;
            }

            float screenDistance = Vector2.Distance(mousePosition, new Vector2(screen.x, screen.y));
            if (screenDistance <= bestScreenDistance && screen.z < bestDepth)
            {
                bestScreenDistance = screenDistance;
                bestDepth = screen.z;
                best = creature;
            }
        }

        return best;
    }

    private void SelectFish(MarineCreatureAgent creature)
    {
        if (creature == null)
        {
            return;
        }

        SelectedFish = creature;

        if (OpenPanelOnSelect)
        {
            ShowPanel = true;
            EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
            if (settings != null)
            {
                settings.ShowSelectedFishPanel = true;
            }
        }
    }

    private bool WasLeftMousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private Vector2 GetMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private void OnGUI()
    {
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        if (!ShowPanel || (settings != null && !settings.ShowSelectedFishPanel))
        {
            return;
        }

        GUILayout.BeginArea(new Rect(PanelPosition.x, PanelPosition.y, PanelSize.x, PanelSize.y), GUI.skin.box);
        GUILayout.Label("Selected Fish");

        if (SelectedFish == null || SelectedFish.Candidate == null || SelectedFish.Candidate.Genome == null)
        {
            GUILayout.Label("Click a fish to inspect its evolved genome and behaviour.");
            GUILayout.EndArea();
            return;
        }

        EvolutionGenome g = SelectedFish.Candidate.Genome;
        EvolutionCandidate c = SelectedFish.Candidate;
        CreatureEffectiveStats s = SelectedFish.EffectiveStats;

        GUILayout.Label("Name: " + SelectedFish.DebugName);
        GUILayout.Label("Group: " + CreatureDebugTypeUtility.GetSpeciesGroupName(g));
        GUILayout.Label("Morph: " + CreatureDebugTypeUtility.GetMorphologyName(g));
        GUILayout.Label("Move: " + SelectedFish.GetDebugMoveState() + " | Vertical: " + SelectedFish.GetDebugVerticalReason());
        GUILayout.Label("Brain: " + SelectedFish.GetBrainDebugSummary());
        GUILayout.Label("Habitat: " + SelectedFish.GetHabitatDebugSummary());
        GUILayout.Label("Schoolmates: " + SelectedFish.GetFriendlySchoolmateCount() + " | Threats: " + SelectedFish.GetThreatCount());
        GUILayout.Space(4f);
        GUILayout.Label("Energy: " + SelectedFish.CurrentEnergy.ToString("F1") + " / " + (s != null ? s.EnergyCapacity.ToString("F1") : g.EnergyCapacity.ToString("F1")));
        GUILayout.Label("Health: " + SelectedFish.CurrentHealth.ToString("F1") + " | Stomach: " + SelectedFish.GetStomachFullness01().ToString("P0"));
        GUILayout.Label("Age: " + SelectedFish.AgeSeconds.ToString("F0") + "s | Sex: " + (g.SexGene >= 0.5f ? "F" : "M") + " | Mature: " + SelectedFish.IsMatureForMating() + " | Juvenile: " + SelectedFish.IsJuvenile);
        GUILayout.Label("Stomach P/M/C: " + SelectedFish.StomachPlant.ToString("F1") + " / " + SelectedFish.StomachMeat.ToString("F1") + " / " + SelectedFish.StomachCarrion.ToString("F1"));
        GUILayout.Label("Fitness: " + c.GetFitness().ToString("F2") + " | Survival: " + c.SurvivalTime.ToString("F1"));
        GUILayout.Space(4f);
        GUILayout.Label("Diet P/M/C: " + g.PlantDiet.ToString("F2") + " / " + g.MeatDiet.ToString("F2") + " / " + g.CarrionDiet.ToString("F2"));
        GUILayout.Label("Aggression: " + g.Aggression.ToString("F2") + " | Risk: " + g.RiskTolerance.ToString("F2") + " | Danger: " + (s != null ? s.DangerFactor.ToString("F2") : g.DangerFactor.ToString("F2")));
        GUILayout.Label("Grouping: " + g.GroupingChance.ToString("F2") + " | Tightness: " + g.SchoolTightness.ToString("F2") + " | Leader: " + g.Leadership.ToString("F2"));
        GUILayout.Label("Hunger T: " + g.HungerThreshold.ToString("F2") + " | Metabolism: " + g.Metabolism.ToString("F2") + " | Stomach Size: " + g.StomachSize.ToString("F2"));
        GUILayout.Label("Bravery: " + g.Bravery.ToString("F2") + " | Selfish: " + g.Selfishness.ToString("F2") + " | Memory: " + g.FoodMemoryStrength.ToString("F2"));
        GUILayout.Label("Diet Lock P/M/C: " + g.PlantDietLocked + " / " + g.MeatDietLocked + " / " + g.CarrionDietLocked);
        GUILayout.Label("Depth: " + g.PreferredDepth01.ToString("F2") + " | Flexibility: " + g.DepthFlexibility.ToString("F2"));
        GUILayout.Label("Mate/Nest/Egg: " + g.MateDrive.ToString("F2") + " / " + g.NestingDrive.ToString("F2") + " / " + g.EggProtection.ToString("F2") + " | Stealth/Hearing: " + g.Stealth.ToString("F2") + " / " + g.HearingSensitivity.ToString("F2"));
        GUILayout.Space(4f);
        GUILayout.Label("Speed: " + (s != null ? s.Speed.ToString("F2") : g.Speed.ToString("F2")) + " | Turn: " + (s != null ? s.TurnRate.ToString("F1") : g.TurnRate.ToString("F1")) + " | Vision: " + (s != null ? s.VisionRange.ToString("F1") : g.VisionRange.ToString("F1")));
        GUILayout.Label("Body: " + g.BodyMorphId + " | Tail: " + g.TailMorphId);
        GUILayout.Label("Fins: " + g.FinMorphId + " | Jaw: " + g.JawMorphId);
        GUILayout.Label("Sensors: " + g.SensorMorphId + " | Armour: " + g.ArmourMorphId);
        GUILayout.Label("Dorsal: " + g.DorsalFinMorphId + " | Spikes: " + g.SpikeMorphId + " | Gills: " + g.GillMorphId);

        GUILayout.Space(8f);
        if (GUILayout.Button("Save Selected Fish Snapshot"))
        {
            SaveSelectedFishSnapshot();
        }

        if (GUILayout.Button("Reset Simulation"))
        {
            if (EvolutionEcosystemManager.Instance != null)
            {
                EvolutionEcosystemManager.Instance.ResetSimulation();
            }
        }

        GUILayout.EndArea();
    }

    public void SaveSelectedFishSnapshot()
    {
        if (SelectedFish == null || SelectedFish.Candidate == null || SelectedFish.Candidate.Genome == null)
        {
            Debug.LogWarning("No selected fish to save.");
            return;
        }

        string folder = Path.Combine(Application.persistentDataPath, SnapshotFolder);
        Directory.CreateDirectory(folder);
        string fileName = "selected_fish_gen" + SelectedFish.Candidate.GenerationBorn + "_id" + SelectedFish.Candidate.Id + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
        string path = Path.Combine(folder, fileName);
        File.WriteAllText(path, BuildJsonSnapshot(SelectedFish));
        Debug.Log("Saved selected fish snapshot: " + path);
    }

    private string BuildJsonSnapshot(MarineCreatureAgent fish)
    {
        EvolutionGenome g = fish.Candidate.Genome;
        EvolutionCandidate c = fish.Candidate;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        AppendJson(sb, "id", c.Id, true);
        AppendJson(sb, "generation", c.GenerationBorn, true);
        AppendJson(sb, "name", fish.DebugName, true);
        AppendJson(sb, "group", CreatureDebugTypeUtility.GetSpeciesGroupName(g), true);
        AppendJson(sb, "morphology", CreatureDebugTypeUtility.GetMorphologyName(g), true);
        AppendJson(sb, "fitness", c.GetFitness(), true);
        AppendJson(sb, "energy", fish.CurrentEnergy, true);
        AppendJson(sb, "health", fish.CurrentHealth, true);
        AppendJson(sb, "stomachFullness", fish.GetStomachFullness01(), true);
        AppendJson(sb, "ageSeconds", fish.AgeSeconds, true);
        AppendJson(sb, "sex", g.SexGene >= 0.5f ? "female" : "male", true);
        AppendJson(sb, "mature", fish.IsMatureForMating(), true);
        AppendJson(sb, "plantDiet", g.PlantDiet, true);
        AppendJson(sb, "meatDiet", g.MeatDiet, true);
        AppendJson(sb, "carrionDiet", g.CarrionDiet, true);
        AppendJson(sb, "aggression", g.Aggression, true);
        AppendJson(sb, "groupingChance", g.GroupingChance, true);
        AppendJson(sb, "schoolTightness", g.SchoolTightness, true);
        AppendJson(sb, "preferredDepth", g.PreferredDepth01, true);
        AppendJson(sb, "hungerThreshold", g.HungerThreshold, true);
        AppendJson(sb, "metabolism", g.Metabolism, true);
        AppendJson(sb, "stomachSize", g.StomachSize, true);
        AppendJson(sb, "bravery", g.Bravery, true);
        AppendJson(sb, "selfishness", g.Selfishness, true);
        AppendJson(sb, "foodMemoryStrength", g.FoodMemoryStrength, true);
        AppendJson(sb, "habitat", fish.GetHabitatDebugSummary(), true);
        AppendJson(sb, "brain", fish.GetBrainDebugSummary(), true);
        AppendJson(sb, "bodyMorph", g.BodyMorphId, true);
        AppendJson(sb, "tailMorph", g.TailMorphId, true);
        AppendJson(sb, "finMorph", g.FinMorphId, true);
        AppendJson(sb, "jawMorph", g.JawMorphId, true);
        AppendJson(sb, "sensorMorph", g.SensorMorphId, true);
        AppendJson(sb, "armourMorph", g.ArmourMorphId, true);
        AppendJson(sb, "spikeMorph", g.SpikeMorphId, false);
        sb.AppendLine("}");
        return sb.ToString();
    }

    private void AppendJson(StringBuilder sb, string key, string value, bool comma)
    {
        sb.Append("  \"").Append(key).Append("\": \"").Append(value).Append("\"");
        sb.AppendLine(comma ? "," : string.Empty);
    }

    private void AppendJson(StringBuilder sb, string key, int value, bool comma)
    {
        sb.Append("  \"").Append(key).Append("\": ").Append(value);
        sb.AppendLine(comma ? "," : string.Empty);
    }

    private void AppendJson(StringBuilder sb, string key, float value, bool comma)
    {
        sb.Append("  \"").Append(key).Append("\": ").Append(value.ToString("F4"));
        sb.AppendLine(comma ? "," : string.Empty);
    }

    private void AppendJson(StringBuilder sb, string key, bool value, bool comma)
    {
        sb.Append("  \"").Append(key).Append("\": ").Append(value ? "true" : "false");
        sb.AppendLine(comma ? "," : string.Empty);
    }
}
