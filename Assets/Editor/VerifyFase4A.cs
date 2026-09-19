using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Popayork.Missions;

public static class VerifyFase4A
{
    private const string ScenePath = "Assets/Scenes/Mision1.unity";
    private const string ConfigPath = "Assets/Config/ParqueConfig.asset";

    [MenuItem("Popayork/Verify Fase 4A")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 4A: PASS" : "Verify Fase 4A: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFase4A.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 4A: PASS" : "Verify Fase 4A: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Fase 4A: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckTorre();
        ok &= CheckNavMeshPath();
        ok &= CheckConfig();
        return ok;
    }

    private static bool CheckTorre()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject torre = GameObject.Find("TorreDelReloj");
        GameObject objetivo = GameObject.Find("Objetivo_Torre");
        bool pass = torre != null && objetivo != null;
        Debug.Log(pass
            ? "Verify Fase 4A [Torre]: PASS — Torre del Reloj como objetivo."
            : "Verify Fase 4A [Torre]: FAIL — falta TorreDelReloj u Objetivo_Torre.");
        return pass;
    }

    private static bool CheckNavMeshPath()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject spawn = GameObject.Find("Spawn_Jugador");
        GameObject objetivo = GameObject.Find("Objetivo_Torre");
        if (spawn == null || objetivo == null)
        {
            Debug.Log("Verify Fase 4A [NavMesh]: FAIL — sin spawn u objetivo.");
            return false;
        }
        var path = new NavMeshPath();
        bool ok = NavMesh.CalculatePath(spawn.transform.position, objetivo.transform.position, NavMesh.AllAreas, path)
            && (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial);
        Debug.Log(ok
            ? "Verify Fase 4A [NavMesh]: PASS — ruta spawn→torre (" + path.status + ")."
            : "Verify Fase 4A [NavMesh]: FAIL — sin ruta spawn→torre.");
        return ok;
    }

    private static bool CheckConfig()
    {
        ParqueConfig config = AssetDatabase.LoadAssetAtPath<ParqueConfig>(ConfigPath);
        if (config == null)
        {
            Debug.Log("Verify Fase 4A [Config]: FAIL — sin ParqueConfig.");
            return false;
        }
        int medido = config.Contar(ValorFuente.Medido);
        int estimado = config.Contar(ValorFuente.Estimado);
        bool pass = medido >= 3 && estimado >= 3;
        Debug.Log(pass
            ? "Verify Fase 4A [Config]: PASS — MEDIDO=" + medido + " ESTIMADO=" + estimado + "."
            : "Verify Fase 4A [Config]: FAIL — MEDIDO=" + medido + " ESTIMADO=" + estimado + " (mínimo 3 y 3).");
        return pass;
    }
}
