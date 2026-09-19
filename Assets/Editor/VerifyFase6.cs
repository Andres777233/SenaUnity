using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Popayork.Core;
using Popayork.Missions;
using Popayork.Player;
using Popayork.UI;
using Popayork.Vehicles;
using Popayork.World;

public static class VerifyFase6
{
    private const string ScenePath = "Assets/Scenes/Mision3.unity";

    [MenuItem("Popayork/Verify Fase 6")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 6: PASS" : "Verify Fase 6: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFase6.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        UnityEngine.Time.timeScale = 1f;
        Debug.Log(ok ? "Verify Fase 6: PASS" : "Verify Fase 6: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Fase 6: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckSlope();
        ok &= CheckCrash();
        ok &= CheckRiver();
        ok &= CheckStaticStats();
        return ok;
    }

    private static void OpenMission()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static CardboardController FindCarton()
    {
        return Object.FindAnyObjectByType<CardboardController>();
    }

    private static bool CheckSlope()
    {
        OpenMission();
        CardboardController carton = FindCarton();
        if (carton == null)
        {
            Debug.Log("Verify Fase 6 [Pendiente]: FAIL — sin cartón.");
            return false;
        }
        for (int i = 0; i < 30; i++)
        {
            carton.Simulate(0.1f, 0f, false);
        }
        bool pass = carton.Speed > 3f;
        Debug.Log(pass
            ? "Verify Fase 6 [Pendiente]: PASS — el cartón acelera cuesta abajo (" + carton.Speed.ToString("F1") + " m/s)."
            : "Verify Fase 6 [Pendiente]: FAIL — velocidad=" + carton.Speed.ToString("F1") + ".");
        return pass;
    }

    private static bool CheckCrash()
    {
        OpenMission();
        CardboardController carton = FindCarton();
        var health = Object.FindAnyObjectByType<PlayerHealth>();
        var controller = Object.FindAnyObjectByType<Mission3Controller>();
        if (carton == null || health == null || controller == null)
        {
            Debug.Log("Verify Fase 6 [Choque]: FAIL — sin piezas.");
            return false;
        }
        controller.StartDescent();
        health.RespawnNow();
        for (int i = 0; i < 10; i++)
        {
            carton.Simulate(0.1f, 0f, false);
        }
        float speedBefore = carton.Speed;
        TrackObstacle[] obstacles = Object.FindObjectsByType<TrackObstacle>(FindObjectsInactive.Include);
        TrackObstacle rock = null;
        for (int i = 0; i < obstacles.Length; i++)
        {
            if (obstacles[i] != null && !obstacles[i].isRamp)
            {
                rock = obstacles[i];
                break;
            }
        }
        if (rock == null)
        {
            Debug.Log("Verify Fase 6 [Choque]: FAIL — sin rocas.");
            return false;
        }
        carton.transform.position = rock.transform.position;
        carton.Simulate(0.1f, 0f, false);
        bool penalized = carton.Speed < speedBefore && health.Health < health.MaxHealth;
        bool alive = !health.IsDead && controller.State == Mission3State.Riding;
        bool pass = penalized && alive;
        Debug.Log(pass
            ? "Verify Fase 6 [Choque]: PASS — chocar frena y daña sin matar (" + speedBefore.ToString("F1") + "->" + carton.Speed.ToString("F1") + ")."
            : "Verify Fase 6 [Choque]: FAIL — penalized=" + penalized + " alive=" + alive + ".");
        return pass;
    }

    private static bool CheckRiver()
    {
        OpenMission();
        CardboardController carton = FindCarton();
        var controller = Object.FindAnyObjectByType<Mission3Controller>();
        var config = AssetDatabase.LoadAssetAtPath<Mission3Config>("Assets/Config/Mission3Config.asset");
        if (carton == null || controller == null || config == null)
        {
            Debug.Log("Verify Fase 6 [Rio]: FAIL — sin piezas.");
            return false;
        }
        string backup = null;
        bool hadBackup = System.IO.File.Exists(SaveSystem.SavePath);
        if (hadBackup)
        {
            backup = System.IO.File.ReadAllText(SaveSystem.SavePath);
        }
        try
        {
            if (System.IO.File.Exists(SaveSystem.SavePath))
            {
                System.IO.File.Delete(SaveSystem.SavePath);
            }
            controller.StartDescent();
            carton.transform.position = config.riverCenter;
            controller.Simulate(0.2f);
            var ui = Object.FindAnyObjectByType<MissionUI>();
            SaveData save = SaveSystem.LoadFreshWithoutWriting();
            bool pass = controller.State == Mission3State.Finished
                && ui != null && ui.ResultVisible
                && save.completedMissions.Contains(GameConfig.Mission3Scene);
            Debug.Log(pass
                ? "Verify Fase 6 [Rio]: PASS — llegada al río, resultados y Mision3 guardada."
                : "Verify Fase 6 [Rio]: FAIL — estado=" + controller.State + ".");
            return pass;
        }
        finally
        {
            if (hadBackup)
            {
                System.IO.File.WriteAllText(SaveSystem.SavePath, backup);
            }
            else if (System.IO.File.Exists(SaveSystem.SavePath))
            {
                System.IO.File.Delete(SaveSystem.SavePath);
            }
        }
    }

    private static bool CheckStaticStats()
    {
        OpenMission();
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        int renderers = 0;
        long tris = 0;
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
        }
        Debug.Log("Verify Fase 6 [Stats]: renderers=" + renderers + " tris=" + tris + " (FPS con render: medir en Editor).");
        return renderers > 0 && tris > 0;
    }
}
