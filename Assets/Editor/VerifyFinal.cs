using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Missions;
using Popayork.Player;
using Popayork.UI;
using Popayork.Vehicles;

public static class VerifyFinal
{
    [MenuItem("Popayork/Verify Final")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Final: PASS" : "Verify Final: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFinal.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Final: PASS" : "Verify Final: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Final: FAIL");
        }
    }

    private static bool RunAll()
    {
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
            bool ok = true;
            ok &= PlayMission1();
            ok &= PlayMission2();
            ok &= PlayMission3();
            return ok;
        }
        finally
        {
            UnityEngine.Time.timeScale = 1f;
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

    private static AgentPool OpenScene(string path)
    {
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var pool = Object.FindAnyObjectByType<AgentPool>();
        if (pool != null)
        {
            pool.Discover();
        }
        return pool;
    }

    private static void KillHostiles(AgentPool pool)
    {
        for (int i = pool.ActiveCount - 1; i >= 0; i--)
        {
            AgentBrain brain = pool.GetActive(i);
            if (brain != null && brain.IsHostile && !brain.Health.IsDead)
            {
                brain.Health.TakeDamage(100000f);
            }
        }
        for (int s = 0; s < 30; s++)
        {
            for (int i = 0; i < pool.ActiveCount; i++)
            {
                AgentBrain brain = pool.GetActive(i);
                if (brain != null)
                {
                    brain.Simulate(0.1f);
                }
            }
        }
    }

    private static bool PlayMission1()
    {
        AgentPool pool = OpenScene("Assets/Scenes/Mision1.unity");
        var controller = Object.FindAnyObjectByType<Mission1Controller>();
        var waves = Object.FindAnyObjectByType<WaveManager>();
        if (controller == null || pool == null || waves == null)
        {
            Debug.Log("Verify Final [M1]: FAIL — sin piezas.");
            return false;
        }
        pool.ReclaimAll();
        controller.StartMission();
        int guard = 0;
        while (controller.State == MissionState.Playing && guard < 200)
        {
            waves.Tick(0.5f);
            KillHostiles(pool);
            controller.Simulate(0.5f);
            guard++;
        }
        SaveData save = SaveSystem.LoadFreshWithoutWriting();
        bool pass = controller.State == MissionState.Victory
            && save.completedMissions.Contains(GameConfig.Mission1Scene)
            && save.unlockedMissions.Contains(GameConfig.Mission2Scene);
        Debug.Log(pass
            ? "Verify Final [M1]: PASS — victoria y Misión 2 desbloqueada."
            : "Verify Final [M1]: FAIL — estado=" + controller.State + ".");
        return pass;
    }

    private static bool PlayMission2()
    {
        AgentPool pool = OpenScene("Assets/Scenes/Mision2.unity");
        var controller = Object.FindAnyObjectByType<Mission2Controller>();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var config = AssetDatabase.LoadAssetAtPath<Mission2Config>("Assets/Config/Mission2Config.asset");
        GameObject defenseGo = GameObject.Find("DefenseWaves");
        WaveManager defense = defenseGo != null ? defenseGo.GetComponent<WaveManager>() : null;
        if (controller == null || pool == null || player == null || config == null || defense == null)
        {
            Debug.Log("Verify Final [M2]: FAIL — sin piezas.");
            return false;
        }
        pool.ReclaimAll();
        controller.StartRoute();
        player.transform.position = config.checkpoints[0].position;
        controller.Simulate(0.2f);
        player.transform.position = config.checkpoints[1].position;
        controller.Simulate(0.2f);
        int guard = 0;
        while (controller.CheckpointIndex == 1 && guard < 60)
        {
            KillHostiles(pool);
            controller.Simulate(0.5f);
            guard++;
        }
        player.transform.position = config.checkpoints[2].position;
        controller.Simulate(0.2f);
        player.transform.position = config.checkpoints[3].position;
        controller.Simulate(0.2f);
        bool arrived = controller.State == Mission2State.Arrived;
        controller.Simulate(6f);
        bool defending = controller.State == Mission2State.Defense;
        guard = 0;
        while ((controller.State == Mission2State.Defense) && guard < 300)
        {
            defense.Tick(0.5f);
            KillHostiles(pool);
            controller.Simulate(0.5f);
            guard++;
        }
        if (controller.State == Mission2State.Retreat)
        {
            player.transform.position = config.exitPoint + Vector3.up;
            controller.Simulate(0.5f);
        }
        SaveData save = SaveSystem.LoadFreshWithoutWriting();
        bool pass = arrived && defending
            && controller.State == Mission2State.Victory
            && save.completedMissions.Contains(GameConfig.Mission2Scene)
            && save.unlockedMissions.Contains(GameConfig.Mission3Scene);
        Debug.Log(pass
            ? "Verify Final [M2]: PASS — ruta, defensa, victoria y Misión 3 desbloqueada."
            : "Verify Final [M2]: FAIL — arrived=" + arrived + " defending=" + defending + " estado=" + controller.State + ".");
        return pass;
    }

    private static bool PlayMission3()
    {
        OpenScene("Assets/Scenes/Mision3.unity");
        var controller = Object.FindAnyObjectByType<Mission3Controller>();
        var config = AssetDatabase.LoadAssetAtPath<Mission3Config>("Assets/Config/Mission3Config.asset");
        var ui = Object.FindAnyObjectByType<MissionUI>();
        if (controller == null || config == null)
        {
            Debug.Log("Verify Final [M3]: FAIL — sin piezas.");
            return false;
        }
        controller.StartDescent();
        var carton = Object.FindAnyObjectByType<CardboardController>();
        if (carton != null)
        {
            carton.transform.position = config.riverCenter;
        }
        controller.Simulate(0.2f);
        SaveData save = SaveSystem.LoadFreshWithoutWriting();
        bool pass = controller.State == Mission3State.Finished
            && ui != null && ui.ResultVisible
            && save.completedMissions.Contains(GameConfig.Mission3Scene)
            && save.completedMissions.Contains("Campania_Completa");
        Debug.Log(pass
            ? "Verify Final [M3]: PASS — río, campaña completa y guardado encadenado."
            : "Verify Final [M3]: FAIL — estado=" + controller.State + ".");
        return pass;
    }
}
