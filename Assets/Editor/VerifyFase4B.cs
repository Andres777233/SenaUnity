using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Missions;
using Popayork.UI;

public static class VerifyFase4B
{
    private const string ScenePath = "Assets/Scenes/Mision1.unity";

    [MenuItem("Popayork/Verify Fase 4B")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 4B: PASS" : "Verify Fase 4B: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFase4B.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 4B: PASS" : "Verify Fase 4B: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Fase 4B: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckStart();
        ok &= CheckDefeat();
        ok &= CheckVictoryAndSave();
        ok &= CheckChaos();
        return ok;
    }

    private static Mission1Controller OpenMission()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var pool = Object.FindAnyObjectByType<AgentPool>();
        if (pool != null)
        {
            pool.Discover();
        }
        return Object.FindAnyObjectByType<Mission1Controller>();
    }

    private static WaveManager FindWaves()
    {
        return Object.FindAnyObjectByType<WaveManager>();
    }

    private static AgentPool FindPool()
    {
        return Object.FindAnyObjectByType<AgentPool>();
    }

    private static void TickWorld(float dt, int steps)
    {
        var pool = FindPool();
        var waves = FindWaves();
        var controller = Object.FindAnyObjectByType<Mission1Controller>();
        for (int s = 0; s < steps; s++)
        {
            if (waves != null)
            {
                waves.Tick(dt);
            }
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

    private static void KillAllPolice(AgentPool pool)
    {
        for (int i = pool.ActiveCount - 1; i >= 0; i--)
        {
            AgentBrain brain = pool.GetActive(i);
            if (brain != null && brain.Faction == Faction.Police && !brain.Health.IsDead)
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

    private static bool CheckStart()
    {
        Mission1Controller controller = OpenMission();
        var waves = FindWaves();
        var ui = Object.FindAnyObjectByType<MissionUI>();
        if (controller == null || waves == null || ui == null)
        {
            Debug.Log("Verify Fase 4B [Arranque]: FAIL — sin controlador/oleadas/UI.");
            return false;
        }
        controller.StartMission();
        TickWorld(0.5f, 12);
        var pool = FindPool();
        bool pass = controller.State == MissionState.Playing
            && pool != null && pool.CountActive(Faction.Police) > 0
            && ui.ObjectiveVisible;
        Debug.Log(pass
            ? "Verify Fase 4B [Arranque]: PASS — misión arranca, oleada en camino y objetivo visible."
            : "Verify Fase 4B [Arranque]: FAIL — estado=" + controller.State + ".");
        return pass;
    }

    private static bool CheckDefeat()
    {
        Mission1Controller controller = OpenMission();
        var pool = FindPool();
        GameObject torre = GameObject.Find("Objetivo_Torre");
        if (controller == null || pool == null || torre == null)
        {
            Debug.Log("Verify Fase 4B [Derrota]: FAIL — sin controlador/pool/torre.");
            return false;
        }
        controller.StartMission();
        pool.ReclaimAll();
        // Oleada sin defensa: policías directo en la Torre.
        for (int i = 0; i < 3; i++)
        {
            pool.Spawn(Faction.Police, torre.transform.position, torre.transform.position);
        }
        TickWorld(0.2f, 10);
        var ui = Object.FindAnyObjectByType<MissionUI>();
        bool pass = controller.State == MissionState.Defeat && ui != null && ui.ResultVisible;
        Debug.Log(pass
            ? "Verify Fase 4B [Derrota]: PASS — la oleada llega a la Torre y hay pantalla de resultado."
            : "Verify Fase 4B [Derrota]: FAIL — estado=" + controller.State + ".");
        return pass;
    }

    private static bool CheckVictoryAndSave()
    {
        Mission1Controller controller = OpenMission();
        var pool = FindPool();
        var waves = FindWaves();
        Mission1Config config = AssetDatabase.LoadAssetAtPath<Mission1Config>("Assets/Config/Mission1Config.asset");
        if (controller == null || pool == null || waves == null || config == null)
        {
            Debug.Log("Verify Fase 4B [Victoria]: FAIL — sin piezas.");
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
            controller.StartMission();
            pool.ReclaimAll();
            // Flujo completo: ticks de oleada + eliminar policías hasta la victoria.
            int guard = 0;
            while (controller.State == MissionState.Playing && guard < 200)
            {
                TickWorld(0.5f, 1);
                KillAllPolice(pool);
                controller.Simulate(0.5f);
                guard++;
            }
            var ui = Object.FindAnyObjectByType<MissionUI>();
            SaveData save = SaveSystem.LoadFreshWithoutWriting();
            bool pass = controller.State == MissionState.Victory
                && ui != null && ui.ResultVisible
                && save.unlockedMissions.Contains(GameConfig.Mission2Scene)
                && save.completedMissions.Contains(GameConfig.Mission1Scene);
            Debug.Log(pass
                ? "Verify Fase 4B [Victoria]: PASS — victoria, resultado y Misión 2 desbloqueada en el guardado."
                : "Verify Fase 4B [Victoria]: FAIL — estado=" + controller.State + ".");
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

    private static bool CheckChaos()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int emitters = 0;
        int debris = 0;
        ParticleSystem[] all = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].main.loop)
            {
                emitters++;
            }
        }
        GameObject chaos = GameObject.Find("Chaos");
        if (chaos != null)
        {
            Transform[] kids = chaos.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < kids.Length; i++)
            {
                if (kids[i].name.StartsWith("Escombro_"))
                {
                    debris++;
                }
            }
        }
        bool manager = Object.FindAnyObjectByType<ChaosManager>() != null;
        bool pass = emitters >= 6 && debris >= 6 && manager;
        Debug.Log(pass
            ? "Verify Fase 4B [Caos]: PASS — " + emitters + " emisores, " + debris + " escombros y temporizador."
            : "Verify Fase 4B [Caos]: FAIL — emisores=" + emitters + " escombros=" + debris + " manager=" + manager + ".");
        return pass;
    }
}
