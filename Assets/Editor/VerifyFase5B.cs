using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Missions;
using Popayork.Player;
using Popayork.UI;

public static class VerifyFase5B
{
    private const string ScenePath = "Assets/Scenes/Mision2.unity";

    [MenuItem("Popayork/Verify Fase 5B")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 5B: PASS" : "Verify Fase 5B: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFase5B.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 5B: PASS" : "Verify Fase 5B: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Fase 5B: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckEscalation();
        ok &= CheckRetreat();
        ok &= CheckExit();
        ok &= CheckDefeat();
        return ok;
    }

    private static void OpenMission()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var pool = Object.FindAnyObjectByType<AgentPool>();
        if (pool != null)
        {
            pool.Discover();
        }
    }

    private static int WaveTotal(WaveData wave)
    {
        int n = 0;
        if (wave != null && wave.entries != null)
        {
            for (int e = 0; e < wave.entries.Length; e++)
            {
                n += Mathf.Max(0, wave.entries[e].count);
            }
        }
        return n;
    }

    private static bool CheckEscalation()
    {
        var config = AssetDatabase.LoadAssetAtPath<Mission2Config>("Assets/Config/Mission2Config.asset");
        if (config == null || config.defenseWaves == null || config.defenseWaves.Length < 3)
        {
            Debug.Log("Verify Fase 5B [Escalada]: FAIL — sin oleadas de defensa.");
            return false;
        }
        int w1 = WaveTotal(config.defenseWaves[0]);
        int w2 = WaveTotal(config.defenseWaves[1]);
        int w3 = WaveTotal(config.defenseWaves[2]);
        bool smart = false;
        for (int e = 0; e < config.defenseWaves[2].entries.Length; e++)
        {
            if (config.defenseWaves[2].entries[e].variant == "SMART Táctico")
            {
                smart = true;
            }
        }
        bool pass = w1 < w2 && w2 < w3 && w3 >= 10 && smart;
        Debug.Log(pass
            ? "Verify Fase 5B [Escalada]: PASS — oleadas " + w1 + "/" + w2 + "/" + w3 + " con SMART."
            : "Verify Fase 5B [Escalada]: FAIL — " + w1 + "/" + w2 + "/" + w3 + " smart=" + smart + ".");
        return pass;
    }

    private static void TickDefense(float dt, int steps)
    {
        var pool = Object.FindAnyObjectByType<AgentPool>();
        var controller = Object.FindAnyObjectByType<Mission2Controller>();
        for (int s = 0; s < steps; s++)
        {
            if (pool != null)
            {
                for (int i = 0; i < pool.ActiveCount; i++)
                {
                    AgentBrain brain = pool.GetActive(i);
                    if (brain != null)
                    {
                        brain.Simulate(dt);
                    }
                }
            }
            if (controller != null)
            {
                controller.Simulate(dt);
            }
        }
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

    private static bool CheckRetreat()
    {
        OpenMission();
        var controller = Object.FindAnyObjectByType<Mission2Controller>();
        var pool = Object.FindAnyObjectByType<AgentPool>();
        if (controller == null || pool == null)
        {
            Debug.Log("Verify Fase 5B [Retirada]: FAIL — sin piezas.");
            return false;
        }
        pool.ReclaimAll();
        controller.StartDefense();
        // Oleada final completa: supera el umbral y dispara la retirada.
        var config = AssetDatabase.LoadAssetAtPath<Mission2Config>("Assets/Config/Mission2Config.asset");
        var defense = GameObject.Find("DefenseWaves");
        var manager = defense != null ? defense.GetComponent<WaveManager>() : null;
        if (manager != null && config != null && config.defenseWaves.Length > 0)
        {
            manager.SpawnWaveNow(config.defenseWaves[config.defenseWaves.Length - 1]);
        }
        TickDefense(0.5f, 4);
        bool pass = controller.State == Mission2State.Retreat && controller.ExitActive;
        Debug.Log(pass
            ? "Verify Fase 5B [Retirada]: PASS — umbral superado, aviso y salida activa."
            : "Verify Fase 5B [Retirada]: FAIL — estado=" + controller.State + ".");
        return pass;
    }

    private static bool CheckExit()
    {
        OpenMission();
        var controller = Object.FindAnyObjectByType<Mission2Controller>();
        var pool = Object.FindAnyObjectByType<AgentPool>();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var config = AssetDatabase.LoadAssetAtPath<Mission2Config>("Assets/Config/Mission2Config.asset");
        if (controller == null || pool == null || player == null || config == null)
        {
            Debug.Log("Verify Fase 5B [Salida]: FAIL — sin piezas.");
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
            pool.ReclaimAll();
            controller.StartDefense();
            var defense = GameObject.Find("DefenseWaves");
            var manager = defense != null ? defense.GetComponent<WaveManager>() : null;
            if (manager != null)
            {
                manager.SpawnWaveNow(config.defenseWaves[config.defenseWaves.Length - 1]);
            }
            TickDefense(0.5f, 2);
            player.transform.position = config.exitPoint + Vector3.up;
            controller.Simulate(0.5f);
            var ui = Object.FindAnyObjectByType<MissionUI>();
            SaveData save = SaveSystem.LoadFreshWithoutWriting();
            bool pass = controller.State == Mission2State.Victory
                && ui != null && ui.ResultVisible
                && save.completedMissions.Contains(GameConfig.Mission2Scene)
                && save.unlockedMissions.Contains(GameConfig.Mission3Scene);
            Debug.Log(pass
                ? "Verify Fase 5B [Salida]: PASS — llegada a cartones, Mision2 completa y Mision3 desbloqueada."
                : "Verify Fase 5B [Salida]: FAIL — estado=" + controller.State + ".");
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

    private static bool CheckDefeat()
    {
        OpenMission();
        var controller = Object.FindAnyObjectByType<Mission2Controller>();
        var pool = Object.FindAnyObjectByType<AgentPool>();
        var health = Object.FindAnyObjectByType<PlayerHealth>();
        if (controller == null || pool == null || health == null)
        {
            Debug.Log("Verify Fase 5B [Derrota]: FAIL — sin piezas.");
            return false;
        }
        pool.ReclaimAll();
        controller.StartDefense();
        for (int d = 0; d < 3; d++)
        {
            health.RespawnNow();
            controller.Simulate(0.1f);
            health.TakeDamage(100000f, Vector3.forward);
            controller.Simulate(0.2f);
        }
        var ui = Object.FindAnyObjectByType<MissionUI>();
        bool pass = controller.State == Mission2State.Defeat && ui != null && ui.ResultVisible;
        Debug.Log(pass
            ? "Verify Fase 5B [Derrota]: PASS — 3 caídas, derrota y reintento visible."
            : "Verify Fase 5B [Derrota]: FAIL — estado=" + controller.State + " muertes=" + controller.Deaths + ".");
        return pass;
    }
}
