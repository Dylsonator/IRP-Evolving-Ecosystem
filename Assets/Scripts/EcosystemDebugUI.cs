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
    public Rect WindowRect = new Rect(20f, 20f, 360f, 330f);

#if ENABLE_INPUT_SYSTEM
    [Header("New Input System")]
    public Key ToggleKey = Key.F1;
    public Key ExtinctionEventKey = Key.F2;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    [Header("Old Input Manager")]
    public KeyCode LegacyToggleKey = KeyCode.F1;
    public KeyCode LegacyExtinctionEventKey = KeyCode.F2;
#endif

    private MonoBehaviour cachedManager;
    private MonoBehaviour cachedEnvironment;
    private MonoBehaviour cachedStats;

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

        GUILayout.Label("Manager: " + manager.GetType().Name);
        GUILayout.Space(4f);

        DrawValue("Generation", manager, "CurrentGeneration");
        DrawTimer(manager);
        DrawCount("Active Creatures", manager, "GetActiveCreatures", "GetCreatures", "activeCreatures", "creatures");
        DrawCount("Offspring Pool", manager, "GetOffspringPool", null, "offspringPool", null);
        DrawValue("Offspring Count", manager, "OffspringCount");
        GUILayout.Space(6f);

        if (environment != null)
        {
            GUILayout.Label("Season: " + GetString(environment, "CurrentSeason", "N/A"));
            DrawValue("Food Multiplier", environment, "FoodSpawnMultiplier");
            DrawValue("Energy Drain Multiplier", environment, "EnergyDrainMultiplier");
            DrawValue("Mutation Multiplier", environment, "MutationMultiplier");
            GUILayout.Space(6f);
        }

        if (stats != null)
        {
            DrawValue("Average Fitness", stats, "AverageFitness");
            DrawValue("Average Speed", stats, "AverageSpeed");
            DrawFirstAvailableValue("Average Vision", stats, "AverageVisionRange", "AverageSenseRadius");
            DrawFirstAvailableValue("Average Size", stats, "AverageBodySize", "AverageSize");
            DrawValue("Behaviour Diversity", stats, "BehaviourDiversity");
        }

        GUILayout.Space(8f);
        GUILayout.Label("F1: Toggle UI");
        GUILayout.Label("F2: Manual extinction event");

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
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();

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

    private string GetString(object target, string memberName, string fallback)
    {
        object value = GetMemberValue(target, memberName);
        return value != null ? value.ToString() : fallback;
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
