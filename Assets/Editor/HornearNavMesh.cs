using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Popayork/Hornear NavMesh (BLOQUE 2).
// Re-hornea las 3 escenas de misión con parámetros coherentes con los agentes
// (agentTypeID 0; radio 0.32 / altura 1.7) y valida cada punto de aparición
// con NavMesh.SamplePosition. Copia de seguridad del .unity en
// Assets/Scenes/Backup antes de hornear.
// Nota: conserva el modo de recolección definido por los builders (Volume en
// Mision1/Mision2, Children en Mision3); excluir árboles/edificios por capas
// queda pendiente (ver docs/ESTADO.md, Bloque 2).
public static class HornearNavMesh
{
    private static readonly string[] MissionScenes = new string[]
    {
        "Assets/Scenes/Mision1.unity",
        "Assets/Scenes/Mision2.unity",
        "Assets/Scenes/Mision3.unity"
    };

    private static readonly string[] ObjectiveNames = new string[]
    {
        "TorreDelReloj", "MorroTop", "M3_Objective", "Rio"
    };

    private const float SpawnSampleRadius = 4.0f;

    [MenuItem("Popayork/Hornear NavMesh")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Hornear NavMesh: PASS" : "Hornear NavMesh: FAIL");
    }

    // Punto de entrada para batch: -executeMethod HornearNavMesh.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Hornear NavMesh: PASS" : "Hornear NavMesh: FAIL");
        if (!ok)
        {
            Debug.LogError("Hornear NavMesh: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        EnsureFolder("Assets/Scenes/Backup");
        string previous = EditorSceneManager.GetActiveScene().path;
        for (int s = 0; s < MissionScenes.Length; s++)
        {
            ok &= BakeScene(MissionScenes[s]);
        }
        if (!string.IsNullOrEmpty(previous) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), previous)))
        {
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
        AssetDatabase.SaveAssets();
        return ok;
    }

    private static bool BakeScene(string scenePath)
    {
        bool ok = true;
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
        {
            Debug.LogError("Hornear NavMesh: FAIL — falta " + scenePath + ".");
            return false;
        }
        BackupScene(scenePath);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include);
        if (surfaces.Length == 0)
        {
            Debug.LogError("Hornear NavMesh: FAIL — " + scenePath + " sin NavMeshSurface.");
            return false;
        }
        for (int i = 0; i < surfaces.Length; i++)
        {
            surfaces[i].agentTypeID = 0;
            surfaces[i].BuildNavMesh();
        }

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        if (tri.vertices.Length == 0)
        {
            Debug.LogError("Hornear NavMesh: FAIL — " + scenePath + " bake vacío (0 verts).");
            ok = false;
        }
        else
        {
            Debug.Log("Hornear NavMesh: " + scenePath + " verts=" + tri.vertices.Length + ".");
        }

        LogAgentCoherence(scenePath);

        Transform objective = FindObjective();
        List<Transform> spawns = FindSpawns();
        if (spawns.Count == 0)
        {
            Debug.LogWarning("Hornear NavMesh: " + scenePath + " sin markers *Spawn*.");
        }
        NavMeshPath path = new NavMeshPath();
        for (int i = 0; i < spawns.Count; i++)
        {
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(spawns[i].position, out hit, SpawnSampleRadius, NavMesh.AllAreas))
            {
                Debug.LogError("Hornear NavMesh: FAIL — spawn '" + spawns[i].name + "' sin NavMesh en " + scenePath + ".");
                ok = false;
                continue;
            }
            if (objective != null)
            {
                bool found = NavMesh.CalculatePath(hit.position, objective.position, NavMesh.AllAreas, path)
                    && (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial);
                if (!found)
                {
                    Debug.LogError("Hornear NavMesh: FAIL — sin ruta '" + spawns[i].name + "' -> '" + objective.name + "' en " + scenePath + ".");
                    ok = false;
                }
            }
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log(ok ? "Hornear NavMesh: " + scenePath + " PASS." : "Hornear NavMesh: " + scenePath + " FAIL.");
        return ok;
    }

    // Comprueba que el bake (agentTypeID 0) cubre a los agentes de la escena.
    private static void LogAgentCoherence(string scenePath)
    {
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        NavMeshAgent[] agents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include);
        if (agents.Length == 0)
        {
            Debug.LogWarning("Hornear NavMesh: " + scenePath + " sin NavMeshAgents para comparar.");
            return;
        }
        NavMeshAgent a = agents[0];
        Debug.Log("Hornear NavMesh: " + scenePath + " bake r=" + settings.agentRadius.ToString("F2")
            + " h=" + settings.agentHeight.ToString("F2") + " vs agente r=" + a.radius.ToString("F2")
            + " h=" + a.height.ToString("F2") + " (" + agents.Length + " agentes).");
        if (a.radius > settings.agentRadius + 0.01f || a.height > settings.agentHeight + 0.01f)
        {
            Debug.LogWarning("Hornear NavMesh: agente más grande que el bake en " + scenePath + " (puede no pasar por pasos estrechos).");
        }
    }

    private static Transform FindObjective()
    {
        for (int i = 0; i < ObjectiveNames.Length; i++)
        {
            GameObject go = GameObject.Find(ObjectiveNames[i]);
            if (go != null)
            {
                return go.transform;
            }
        }
        return null;
    }

    private static List<Transform> FindSpawns()
    {
        List<Transform> list = new List<Transform>();
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name.Contains("Spawn"))
            {
                list.Add(all[i]);
            }
        }
        return list;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static void BackupScene(string scenePath)
    {
        string file = Path.GetFileNameWithoutExtension(scenePath);
        string dest = "Assets/Scenes/Backup/" + file + "_backup.unity";
        int n = 1;
        while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), dest)))
        {
            dest = "Assets/Scenes/Backup/" + file + "_backup" + n + ".unity";
            n++;
        }
        string error = AssetDatabase.CopyAsset(scenePath, dest);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning("Hornear NavMesh: backup falló para " + scenePath + ": " + error);
        }
    }
}
