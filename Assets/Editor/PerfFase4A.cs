using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Medición de rendimiento de Mision1 en batch (sin render: lógica + física).
// Uso: Popayork/Medir FPS Mision1. El promedio real con GPU se confirma en el Editor.
public static class PerfFase4A
{
    private const string ScenePath = "Assets/Scenes/Mision1.unity";
    private const int TargetFrames = 600;

    private static int frames;
    private static double startTime;
    private static bool measuring;

    [MenuItem("Popayork/Medir FPS Mision1")]
    public static void RunFromMenu()
    {
        Run();
    }

    // Punto de entrada para batch: -executeMethod PerfFase4A.Run (sale solo con EditorApplication.Exit).
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        LogStaticStats();
        frames = 0;
        measuring = true;
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.isPlaying = true;
        startTime = Time.realtimeSinceStartupAsDouble;
    }

    private static void OnEditorUpdate()
    {
        if (!measuring || !EditorApplication.isPlaying)
        {
            return;
        }
        frames++;
        if (frames >= TargetFrames)
        {
            measuring = false;
            EditorApplication.update -= OnEditorUpdate;
            double seconds = Time.realtimeSinceStartupAsDouble - startTime;
            double avgMs = seconds * 1000.0 / frames;
            double fps = frames / seconds;
            Debug.Log("Perf Fase 4A [FPS-logica]: frames=" + frames + " avg=" + avgMs.ToString("F2") + "ms fps=" + fps.ToString("F1") + " (batch sin render).");
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }

    private static void LogStaticStats()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        int renderers = 0;
        long tris = 0;
        int colliders = 0;
        for (int i = 0; i < roots.Length; i++)
        {
            Renderer[] rs = roots[i].GetComponentsInChildren<Renderer>(true);
            renderers += rs.Length;
            MeshFilter[] fs = roots[i].GetComponentsInChildren<MeshFilter>(true);
            for (int j = 0; j < fs.Length; j++)
            {
                if (fs[j].sharedMesh != null)
                {
                    tris += fs[j].sharedMesh.triangles.Length / 3;
                }
            }
            Collider[] cs = roots[i].GetComponentsInChildren<Collider>(true);
            colliders += cs.Length;
        }
        Debug.Log("Perf Fase 4A [estatico]: renderers=" + renderers + " tris=" + tris + " colliders=" + colliders + ".");
    }
}
