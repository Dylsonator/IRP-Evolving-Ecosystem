using UnityEngine;

public class EcosystemDebugUI : MonoBehaviour
{
    [Header("Read Only UI")]
    public bool ShowOverview = true;
    public Vector2 Position = new Vector2(12f, 12f);
    public Vector2 Size = new Vector2(420f, 210f);

    private void OnGUI()
    {
        if (!ShowOverview)
        {
            return;
        }

        EvolutionEcosystemManager manager = EvolutionEcosystemManager.Instance;
        if (manager == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(Position.x, Position.y, Size.x, Size.y), GUI.skin.box);
        GUILayout.Label("IRP Evolution Ecosystem - Read Only Debug");
        GUILayout.Label("Generation: " + manager.CurrentGeneration + " | Timer: " + manager.GenerationTimer.ToString("F1") + " / " + manager.GenerationDuration.ToString("F1"));
        GUILayout.Label("Creatures: " + manager.GetActiveCreatures().Count + " | Offspring pool: " + manager.GetOffspringPool().Count);
        GUILayout.Label("Food: " + manager.GetActiveFood().Count + " | Carrion: " + manager.GetActiveCarrion().Count);

        if (manager.Environment != null)
        {
            GUILayout.Label("Season: " + manager.Environment.CurrentSeason + " | Food x" + manager.Environment.FoodSpawnMultiplier.ToString("F2") + " | Drain x" + manager.Environment.EnergyDrainMultiplier.ToString("F2"));
        }

        if (manager.StatsTracker != null)
        {
            GUILayout.Label("Avg fitness: " + manager.StatsTracker.AverageFitness.ToString("F2") + " | Diversity: " + manager.StatsTracker.BehaviourDiversity.ToString("F2"));
            GUILayout.Label("Avg speed: " + manager.StatsTracker.AverageSpeed.ToString("F2") + " | Avg vision: " + manager.StatsTracker.AverageVisionRange.ToString("F2"));
        }

        GUILayout.Label("No behaviour toggles here: behaviour is controlled by genome, environment and selection.");
        GUILayout.EndArea();
    }
}
