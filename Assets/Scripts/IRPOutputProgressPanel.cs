using UnityEngine;

public class IRPOutputProgressPanel : MonoBehaviour
{
    public bool ShowPanel = true;
    public Rect WindowRect = new Rect(560f, 20f, 390f, 310f);
    public EvolutionEcosystemManager Manager;
    public EvolutionStatsTracker StatsTracker;

    private void OnGUI()
    {
        if (!ShowPanel)
        {
            return;
        }

        if (Manager == null)
        {
            Manager = EvolutionEcosystemManager.Instance;
        }

        if (StatsTracker == null && Manager != null)
        {
            StatsTracker = Manager.StatsTracker;
        }

        WindowRect = GUI.Window(912710, WindowRect, DrawWindow, "IRP Output Progress");
    }

    private void DrawWindow(int id)
    {
        if (Manager == null)
        {
            GUILayout.Label("No ecosystem manager found.");
            GUI.DragWindow();
            return;
        }

        GUILayout.Label("Target: autonomous evolutionary marine ecosystem");
        GUILayout.Space(4f);
        DrawCheck("Autonomous agents", true);
        DrawCheck("Genomes and inherited traits", true);
        DrawCheck("Mutation and selection", true);
        DrawCheck("Generational cycle", Manager.CurrentGeneration > 0);
        DrawCheck("Environmental pressure zones", Manager.PressureZones != null && Manager.PressureZones.Count > 0);
        DrawCheck("Diet, digestion and health", true);
        DrawCheck("Predation and carrion loop", Manager.EnablePredation);
        DrawCheck("Habitat memory", true);
        DrawCheck("Behaviour diversity metrics", StatsTracker != null && StatsTracker.BehaviourDiversity >= 0f);
        DrawCheck("CSV evaluation output", StatsTracker != null && StatsTracker.WriteCsvLog);

        GUILayout.Space(6f);
        GUILayout.Label("Generation: " + Manager.CurrentGeneration + " | Time: " + Manager.GenerationTimer.ToString("F1") + " / " + Manager.GenerationDuration.ToString("F0"));
        GUILayout.Label("Creatures: " + Manager.GetActiveCreatures().Count + " | Offspring: " + Manager.GetOffspringPool().Count);
        GUILayout.Label("Food: " + Manager.GetActiveFood().Count + " | Carrion: " + Manager.GetActiveCarrion().Count);
        GUILayout.Label("Pressure zones: " + (Manager.PressureZones != null ? Manager.PressureZones.Count : 0));

        if (StatsTracker != null)
        {
            GUILayout.Space(4f);
            GUILayout.Label("Dominant: " + StatsTracker.DominantBehaviourGroup + " | Diet: " + StatsTracker.DominantDietMode);
            GUILayout.Label("Diversity: " + StatsTracker.BehaviourDiversity.ToString("F2") + " | Morph groups: " + StatsTracker.MorphGroupCount);
            GUILayout.Label("Locked diets P/M/C: " + StatsTracker.PlantLockedCount + " / " + StatsTracker.MeatLockedCount + " / " + StatsTracker.CarrionLockedCount);
        }

        GUI.DragWindow();
    }

    private void DrawCheck(string label, bool active)
    {
        GUILayout.Label((active ? "✓ " : "• ") + label);
    }
}
