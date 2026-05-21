using System.Collections;
using System.Reflection;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EcosystemDebugUI : MonoBehaviour
{
    [Header("Display")]
    public bool ShowUI = true;
    public Rect WindowRect = new Rect(20f, 20f, 430f, 560f);

#if ENABLE_INPUT_SYSTEM
    [Header("New Input System")]
    public Key ToggleKey = Key.F1;
    public Key ExtinctionEventKey = Key.F2;
    public Key ToggleMovementRaysKey = Key.F3;
    public Key ToggleRangeDebugKey = Key.F4;
    public Key ToggleLabelsKey = Key.F5;
    public Key ToggleDietLabelsKey = Key.F6;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    [Header("Old Input Manager")]
    public KeyCode LegacyToggleKey = KeyCode.F1;
    public KeyCode LegacyExtinctionEventKey = KeyCode.F2;
    public KeyCode LegacyToggleMovementRaysKey = KeyCode.F3;
    public KeyCode LegacyToggleRangeDebugKey = KeyCode.F4;
    public KeyCode LegacyToggleLabelsKey = KeyCode.F5;
    public KeyCode LegacyToggleDietLabelsKey = KeyCode.F6;
#endif

    private MonoBehaviour cachedManager;
    private MonoBehaviour cachedEnvironment;
    private MonoBehaviour cachedStats;
    private EcosystemDebugSettings cachedDebugSettings;

    private void Update()
    {
        if (WasTogglePressed())
        {
            ShowUI = !ShowUI;
        }

        if (WasExtinctionPressed())
        {
            MonoBehaviour manager = FindManager();

            if (manager != null)
            {
                InvokeNoArgumentMethod(manager, "TriggerExtinctionEvent");
            }
        }

        EcosystemDebugSettings settings = FindDebugSettings();
        if (settings != null)
        {
            if (WasMovementRaysPressed())
            {
                settings.DrawCreatureMovementRays = !settings.DrawCreatureMovementRays;
            }

            if (WasRangeDebugPressed())
            {
                bool newState = !(settings.DrawMouthRange || settings.DrawVisionRange || settings.DrawBiteRange);
                settings.DrawMouthRange = newState;
                settings.DrawBiteRange = newState;
                settings.DrawVisionRange = newState;
            }

            if (WasLabelsPressed())
            {
                settings.ShowCreatureLabels = !settings.ShowCreatureLabels;
            }

            if (WasDietLabelsPressed())
            {
                settings.ShowDietInLabels = !settings.ShowDietInLabels;
            }
        }
    }

    private bool WasTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[ToggleKey].wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(LegacyToggleKey))
        {
            return true;
        }
#endif

        return false;
    }

    private bool WasExtinctionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[ExtinctionEventKey].wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(LegacyExtinctionEventKey))
        {
            return true;
        }
#endif

        return false;
    }

    private bool WasMovementRaysPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[ToggleMovementRaysKey].wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(LegacyToggleMovementRaysKey))
        {
            return true;
        }
#endif

        return false;
    }

    private bool WasRangeDebugPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[ToggleRangeDebugKey].wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(LegacyToggleRangeDebugKey))
        {
            return true;
        }
#endif

        return false;
    }

    private bool WasLabelsPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[ToggleLabelsKey].wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(LegacyToggleLabelsKey))
        {
            return true;
        }
#endif

        return false;
    }


    private bool WasDietLabelsPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[ToggleDietLabelsKey].wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(LegacyToggleDietLabelsKey))
        {
            return true;
        }
#endif

        return false;
    }

    private void OnGUI()
    {
        if (!ShowUI)
        {
            return;
        }

        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, "IRP Ecosystem Debug");
    }

    private void DrawWindow(int windowId)
    {
        MonoBehaviour manager = FindManager();

        if (manager == null)
        {
            GUILayout.Label("No ecosystem manager found.");
            GUILayout.Label("Expected class name:");
            GUILayout.Label("EvolutionEcosystemManager or EcosystemManager");
            GUI.DragWindow();
            return;
        }

        MonoBehaviour environment = FindLinkedComponent(manager, "Environment", "SeasonalEnvironment");
        MonoBehaviour stats = FindLinkedComponent(manager, "StatsTracker", "EvolutionStatsTracker");
        EcosystemDebugSettings debugSettings = FindDebugSettings();

        GUILayout.Label("Manager: " + manager.GetType().Name);
        GUILayout.Space(4f);

        DrawValue("Generation", manager, "CurrentGeneration");
        DrawTimer(manager);
        DrawCount("Active Creatures", manager, "GetActiveCreatures", "GetCreatures", "activeCreatures", "creatures");
        DrawCount("Active Food", manager, "GetActiveFood", null, "activeFood", null);
        DrawCount("Active Carrion", manager, "GetActiveCarrion", null, "activeCarrion", null);
        DrawCount("Offspring Pool", manager, "GetOffspringPool", null, "offspringPool", null);
        DrawValue("Offspring Count", manager, "OffspringCount");
        GUILayout.Space(8f);

        if (environment != null)
        {
            GUILayout.Label("<b>Environment Pressure</b>");
            DrawValue("Season", environment, "CurrentSeason");
            DrawValue("Food Multiplier", environment, "FoodSpawnMultiplier");
            DrawValue("Energy Drain Multiplier", environment, "EnergyDrainMultiplier");
            DrawValue("Mutation Multiplier", environment, "MutationMultiplier");
            GUILayout.Space(8f);
        }

        if (stats != null)
        {
            GUILayout.Label("<b>Evaluation Averages</b>");
            DrawValue("Average Fitness", stats, "AverageFitness");
            DrawValue("Average Speed", stats, "AverageSpeed");
            DrawFirstAvailableValue("Average Vision", stats, "AverageVisionRange", "AverageSenseRadius");
            DrawFirstAvailableValue("Average Size", stats, "AverageBodySize", "AverageSize");
            DrawValue("Average Hunger", stats, "AverageHungerDrive");
            DrawValue("Average Aggression", stats, "AverageAggression");
            DrawValue("Average Risk", stats, "AverageRiskTolerance");
            DrawValue("Average Carrion Eaten", stats, "AverageCarrionEaten");
            DrawValue("Average Prey Bites", stats, "AveragePreyBites");
            DrawValue("Average Prey Kills", stats, "AveragePreyKills");
            DrawValue("Average Plant Diet", stats, "AveragePlantDiet");
            DrawValue("Average Meat Diet", stats, "AverageMeatDiet");
            DrawValue("Average Carrion Diet", stats, "AverageCarrionDiet");
            DrawValue("Dominant Diet", stats, "DominantDietMode");
            GUILayout.Space(8f);

            GUILayout.Label("<b>Diversity Breakdown</b>");
            DrawValue("Behaviour Diversity", stats, "BehaviourDiversity");
            DrawValue("Movement Diversity", stats, "MovementDiversity");
            DrawValue("Feeding Diversity", stats, "FeedingDiversity");
            DrawValue("Diet Diversity", stats, "DietDiversity");
            DrawValue("Trait Diversity", stats, "TraitDiversity");
            DrawValue("Type Diversity", stats, "BehaviourTypeDiversity");
            DrawValue("Active Niches", stats, "ActiveNicheCount");
            GUILayout.Space(8f);

            GUILayout.Label("<b>Behaviour Groups</b>");
            DrawValue("Dominant Group", stats, "DominantBehaviourGroup");
            DrawValue("Groups", stats, "BehaviourGroupSummary");
            GUILayout.Space(8f);
        }

        if (debugSettings != null)
        {
            GUILayout.Label("<b>Debug Draw Toggles</b>");
            debugSettings.DrawCreatureMovementRays = GUILayout.Toggle(debugSettings.DrawCreatureMovementRays, "Draw movement rays (F3)");
            debugSettings.DrawFoodTargetRays = GUILayout.Toggle(debugSettings.DrawFoodTargetRays, "Draw food target rays");
            debugSettings.DrawCarrionTargetRays = GUILayout.Toggle(debugSettings.DrawCarrionTargetRays, "Draw carrion target rays");
            debugSettings.DrawPreyTargetRays = GUILayout.Toggle(debugSettings.DrawPreyTargetRays, "Draw prey/hunt target rays");
            debugSettings.DrawSocialTargetRays = GUILayout.Toggle(debugSettings.DrawSocialTargetRays, "Draw social/threat rays");
            debugSettings.DrawVelocityRays = GUILayout.Toggle(debugSettings.DrawVelocityRays, "Draw velocity rays");
            debugSettings.DrawWantedDirectionRays = GUILayout.Toggle(debugSettings.DrawWantedDirectionRays, "Draw wanted direction rays");
            debugSettings.DrawMouthRange = GUILayout.Toggle(debugSettings.DrawMouthRange, "Draw mouth/eat range (F4)");
            debugSettings.DrawBiteRange = GUILayout.Toggle(debugSettings.DrawBiteRange, "Draw bite range (F4)");
            debugSettings.DrawVisionRange = GUILayout.Toggle(debugSettings.DrawVisionRange, "Draw vision/threat range (F4)");
            debugSettings.DrawBoundaryPush = GUILayout.Toggle(debugSettings.DrawBoundaryPush, "Draw boundary push rays");
            debugSettings.ShowCreatureLabels = GUILayout.Toggle(debugSettings.ShowCreatureLabels, "Show creature labels (F5)");
            debugSettings.ShowDietInLabels = GUILayout.Toggle(debugSettings.ShowDietInLabels, "Show diet values in labels (F6)");
            GUILayout.Space(8f);
        }
        else
        {
            GUILayout.Label("Add EcosystemDebugSettings to the manager object for ray/label toggles.");
        }

        GUILayout.Label("F1 UI | F2 Extinction | F3 Rays | F4 Ranges | F5 Labels | F6 Diet labels");

        GUI.DragWindow();
    }

    private void DrawTimer(object target)
    {
        bool hasTimer = TryGetFloat(target, "GenerationTimer", out float timer);
        bool hasDuration = TryGetFloat(target, "GenerationDuration", out float duration);

        if (hasTimer && hasDuration)
        {
            GUILayout.Label("Generation Timer: " + timer.ToString("F1") + " / " + duration.ToString("F1"));
        }
        else if (hasTimer)
        {
            GUILayout.Label("Generation Timer: " + timer.ToString("F1"));
        }
    }

    private void DrawValue(string label, object target, string memberName)
    {
        object value = GetMemberValue(target, memberName);

        if (value == null)
        {
            return;
        }

        GUILayout.Label(label + ": " + FormatValue(value));
    }

    private void DrawFirstAvailableValue(string label, object target, string firstMemberName, string secondMemberName)
    {
        object value = GetMemberValue(target, firstMemberName);

        if (value == null)
        {
            value = GetMemberValue(target, secondMemberName);
        }

        if (value == null)
        {
            return;
        }

        GUILayout.Label(label + ": " + FormatValue(value));
    }

    private void DrawCount(string label, object target, string firstMethod, string secondMethod, string firstField, string secondField)
    {
        int count;

        if (TryGetCountFromMethod(target, firstMethod, out count) ||
            TryGetCountFromMethod(target, secondMethod, out count) ||
            TryGetCountFromMember(target, firstField, out count) ||
            TryGetCountFromMember(target, secondField, out count))
        {
            GUILayout.Label(label + ": " + count);
        }
    }

    private MonoBehaviour FindManager()
    {
        if (cachedManager != null)
        {
            return cachedManager;
        }

        cachedManager = FindComponentByClassName("EvolutionEcosystemManager");

        if (cachedManager == null)
        {
            cachedManager = FindComponentByClassName("EcosystemManager");
        }

        return cachedManager;
    }

    private EcosystemDebugSettings FindDebugSettings()
    {
        if (cachedDebugSettings != null)
        {
            return cachedDebugSettings;
        }

        cachedDebugSettings = FindFirstObjectByType<EcosystemDebugSettings>();
        return cachedDebugSettings;
    }

    private MonoBehaviour FindLinkedComponent(object owner, string fieldOrPropertyName, string fallbackClassName)
    {
        object linked = GetMemberValue(owner, fieldOrPropertyName);
        MonoBehaviour linkedBehaviour = linked as MonoBehaviour;

        if (linkedBehaviour != null)
        {
            return linkedBehaviour;
        }

        if (fallbackClassName == "SeasonalEnvironment")
        {
            if (cachedEnvironment == null)
            {
                cachedEnvironment = FindComponentByClassName(fallbackClassName);
            }

            return cachedEnvironment;
        }

        if (fallbackClassName == "EvolutionStatsTracker")
        {
            if (cachedStats == null)
            {
                cachedStats = FindComponentByClassName(fallbackClassName);
            }

            return cachedStats;
        }

        return FindComponentByClassName(fallbackClassName);
    }

    private MonoBehaviour FindComponentByClassName(string className)
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i].GetType().Name == className)
            {
                return behaviours[i];
            }
        }

        return null;
    }

    private object GetMemberValue(object target, string memberName)
    {
        if (target == null || string.IsNullOrEmpty(memberName))
        {
            return null;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = target.GetType();

        FieldInfo field = type.GetField(memberName, flags);

        if (field != null)
        {
            return field.GetValue(target);
        }

        PropertyInfo property = type.GetProperty(memberName, flags);

        if (property != null && property.GetIndexParameters().Length == 0)
        {
            return property.GetValue(target, null);
        }

        return null;
    }

    private bool TryGetFloat(object target, string memberName, out float value)
    {
        object rawValue = GetMemberValue(target, memberName);

        if (rawValue is float)
        {
            value = (float)rawValue;
            return true;
        }

        if (rawValue is int)
        {
            value = (int)rawValue;
            return true;
        }

        value = 0f;
        return false;
    }

    private string FormatValue(object value)
    {
        if (value is float)
        {
            return ((float)value).ToString("F2");
        }

        if (value is double)
        {
            return ((double)value).ToString("F2");
        }

        return value.ToString();
    }

    private void InvokeNoArgumentMethod(object target, string methodName)
    {
        if (target == null || string.IsNullOrEmpty(methodName))
        {
            return;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo method = target.GetType().GetMethod(methodName, flags, null, new System.Type[0], null);

        if (method != null)
        {
            method.Invoke(target, null);
        }
    }

    private bool TryGetCountFromMethod(object target, string methodName, out int count)
    {
        count = 0;

        if (target == null || string.IsNullOrEmpty(methodName))
        {
            return false;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo method = target.GetType().GetMethod(methodName, flags, null, new System.Type[0], null);

        if (method == null)
        {
            return false;
        }

        object result = method.Invoke(target, null);
        return TryGetCount(result, out count);
    }

    private bool TryGetCountFromMember(object target, string memberName, out int count)
    {
        count = 0;

        object value = GetMemberValue(target, memberName);
        return TryGetCount(value, out count);
    }

    private bool TryGetCount(object value, out int count)
    {
        count = 0;

        ICollection collection = value as ICollection;

        if (collection != null)
        {
            count = collection.Count;
            return true;
        }

        IEnumerable enumerable = value as IEnumerable;

        if (enumerable == null || value is string)
        {
            return false;
        }

        foreach (object item in enumerable)
        {
            count++;
        }

        return true;
    }
}
