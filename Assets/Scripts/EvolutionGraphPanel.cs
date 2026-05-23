using UnityEngine;

public class EvolutionGraphPanel : MonoBehaviour
{
    public bool ShowGraph = true;
    public Rect WindowRect = new Rect(20f, 600f, 520f, 230f);
    public EvolutionStatsTracker StatsTracker;

    private void OnGUI()
    {
        if (!ShowGraph)
        {
            return;
        }

        if (StatsTracker == null)
        {
            StatsTracker = EvolutionEcosystemManager.Instance != null ? EvolutionEcosystemManager.Instance.StatsTracker : FindFirstObjectByType<EvolutionStatsTracker>();
        }

        WindowRect = GUI.Window(811022, WindowRect, DrawWindow, "Evolution Graphs");
    }

    private void DrawWindow(int id)
    {
        if (StatsTracker == null || StatsTracker.GenerationHistory.Count < 2)
        {
            GUILayout.Label("Graphs appear after at least two recorded generations.");
            GUI.DragWindow();
            return;
        }

        Rect graphRect = GUILayoutUtility.GetRect(480f, 145f);
        GUI.Box(graphRect, GUIContent.none);
        DrawLineGraph(graphRect, StatsTracker.AveragePlantDietHistory, Color.green, 1f);
        DrawLineGraph(graphRect, StatsTracker.AverageMeatDietHistory, Color.red, 1f);
        DrawLineGraph(graphRect, StatsTracker.AverageCarrionDietHistory, new Color(0.6f, 0.35f, 0.1f), 1f);
        DrawLineGraph(graphRect, StatsTracker.BehaviourDiversityHistory, Color.cyan, 1f);

        GUILayout.Label("Green plant | Red meat | Brown carrion | Cyan diversity");
        GUILayout.Label("Dominant: " + StatsTracker.DominantBehaviourGroup + " | Diet: " + StatsTracker.DominantDietMode + " | Niches: " + StatsTracker.ActiveNicheCount);
        GUI.DragWindow();
    }

    private void DrawLineGraph(Rect rect, System.Collections.Generic.List<float> values, Color colour, float maxValue)
    {
        if (values == null || values.Count < 2)
        {
            return;
        }

        int start = 0;
        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        if (settings != null && values.Count > settings.MaxGraphPoints)
        {
            start = values.Count - settings.MaxGraphPoints;
        }

        int count = values.Count - start;
        if (count < 2)
        {
            return;
        }

        Color old = GUI.color;
        GUI.color = colour;
        Texture2D tex = Texture2D.whiteTexture;

        Vector2 prev = GraphPoint(rect, 0, count, Mathf.Clamp01(values[start] / Mathf.Max(0.0001f, maxValue)));
        for (int i = 1; i < count; i++)
        {
            Vector2 next = GraphPoint(rect, i, count, Mathf.Clamp01(values[start + i] / Mathf.Max(0.0001f, maxValue)));
            DrawLine(prev, next, tex, 2f);
            prev = next;
        }

        GUI.color = old;
    }

    private Vector2 GraphPoint(Rect rect, int index, int count, float value01)
    {
        float x = Mathf.Lerp(rect.x + 6f, rect.xMax - 6f, index / Mathf.Max(1f, count - 1f));
        float y = Mathf.Lerp(rect.yMax - 6f, rect.y + 6f, value01);
        return new Vector2(x, y);
    }

    private void DrawLine(Vector2 a, Vector2 b, Texture2D tex, float width)
    {
        Matrix4x4 matrix = GUI.matrix;
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
        float length = Vector2.Distance(a, b);
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width), tex);
        GUI.matrix = matrix;
    }
}
