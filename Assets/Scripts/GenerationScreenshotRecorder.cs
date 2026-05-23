using System.IO;
using UnityEngine;

public class GenerationScreenshotRecorder : MonoBehaviour
{
    public bool CaptureOnGenerationChange = false;
    public string FolderName = "IRP_GenerationScreenshots";
    private int lastGeneration = -1;

    private void Update()
    {
        if (EvolutionEcosystemManager.Instance == null)
        {
            return;
        }

        EcosystemDebugSettings settings = EcosystemDebugSettings.Instance;
        bool enabledBySettings = settings != null && settings.AutoCaptureGenerationScreenshots;
        if (!CaptureOnGenerationChange && !enabledBySettings)
        {
            return;
        }

        int generation = EvolutionEcosystemManager.Instance.CurrentGeneration;
        if (generation == lastGeneration)
        {
            return;
        }

        lastGeneration = generation;
        CaptureGeneration(generation);
    }

    public void CaptureGeneration(int generation)
    {
        string folder = Path.Combine(Application.persistentDataPath, FolderName);
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "generation_" + generation.ToString("0000") + ".png");
        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("Queued generation screenshot: " + file);
    }
}
