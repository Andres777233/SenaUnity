using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Missions;
using Popayork.Player;
using Popayork.UI;
using Popayork.Vehicles;

public static class VerifyFase5A
{
    private const string ScenePath = "Assets/Scenes/Mision2.unity";

    [MenuItem("Popayork/Verify Fase 5A")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 5A: PASS" : "Verify Fase 5A: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFase5A.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 5A: PASS" : "Verify Fase 5A: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Fase 5A: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckMount();
        ok &= CheckCorridor();
        ok &= CheckCheckpoints();
        ok &= CheckRivalsAndCompass();
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

    private static bool CheckMount()
    {
        OpenMission();
        var horse = Object.FindAnyObjectByType<HorseController>();
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (horse == null || player == null)
        {
            Debug.Log("Verify Fase 5A [Montar]: FAIL — sin caballo o jugador.");
            return false;
        }
        player.transform.position = horse.transform.position + new Vector3(1.5f, 0.2f, 0f);
        horse.Mount(player.gameObject);
        bool mounted = horse.IsMounted && !player.enabled;
        horse.Dismount();
        Vector3 waitPos = horse.WaitPosition;
        bool waiting = !horse.IsMounted && player.enabled
            && FlatDistance(horse.transform.position, waitPos) < 0.01f;
        // El caballo espera: simula sin jinete y no se desplaza solo (solo se asienta).
        for (int i = 0; i < 5; i++)
        {
            horse.Simulate(0.2f, 0f, 0f, false);
        }
        bool stays = FlatDistance(horse.transform.position, waitPos) < 0.5f;
        horse.Mount(player.gameObject);
        bool remount = horse.IsMounted;
        horse.Dismount();
        bool pass = mounted && waiting && stays && remount;
        Debug.Log(pass
            ? "Verify Fase 5A [Montar]: PASS — montar/desmontar, espera en el punto y remonta."
            : "Verify Fase 5A [Montar]: FAIL — mounted=" + mounted + " waiting=" + waiting + " stays=" + stays + " remount=" + remount + ".");
        return pass;
    }

    private static bool CheckCorridor()
    {
        OpenMission();
        var horse = Object.FindAnyObjectByType<HorseController>();
        var config = AssetDatabase.LoadAssetAtPath<Mission2Config>("Assets/Config/Mission2Config.asset");
        if (horse == null || config == null || config.routePoints.Length < 2)
        {
            Debug.Log("Verify Fase 5A [Corredor]: FAIL — sin caballo o ruta.");
            return false;
        }
        // Lo lanzo 50m fuera de la ruta: debe quedar clampado al corredor.
        Vector3 off = config.routePoints[2] + new Vector3(50f, 0f, 50f);
        horse.transform.position = new Vector3(off.x, config.routePoints[2].y + 0.5f, off.z);
        horse.Simulate(0.1f, 0f, 0f, false);
        float dist = DistanceToRoute(horse.transform.position, config.routePoints);
        bool clamped = dist <= 12.5f;
        // Cabalgata: 60 pasos al frente sin caer del mapa ni atravesar límites.
        horse.transform.position = config.routePoints[1];
        float minY = float.MaxValue;
        for (int i = 0; i < 60; i++)
        {
            horse.Simulate(0.1f, 1f, 0f, true);
            if (horse.transform.position.y < minY)
            {
                minY = horse.transform.position.y;
            }
        }
        float distEnd = DistanceToRoute(horse.transform.position, config.routePoints);
        bool bounded = distEnd <= 12.5f && minY > -100f;
        bool pass = clamped && bounded;
        Debug.Log(pass
            ? "Verify Fase 5A [Corredor]: PASS — clamp a " + dist.ToString("F1") + "m y cabalgata acotada sin caídas."
            : "Verify Fase 5A [Corredor]: FAIL — dist=" + dist.ToString("F1") + " minY=" + minY.ToString("F1") + ".");
        return pass;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector3 d = a - b;
        d.y = 0f;
        return d.magnitude;
    }

    private static float DistanceToRoute(Vector3 p, Vector3[] route)
    {
        float best = float.MaxValue;
        for (int i = 0; i < route.Length - 1; i++)
        {
            Vector3 a = route[i];
            a.y = 0f;
            Vector3 b = route[i + 1];
            b.y = 0f;
            Vector3 flat = p;
            flat.y = 0f;
            Vector3 ab = b - a;
            float denom = ab.sqrMagnitude;
            if (denom < 0.000001f)
            {
                continue;
            }
            float t = Mathf.Clamp01(Vector3.Dot(flat - a, ab) / denom);
            Vector3 c = a + ab * t;
            float d = (flat - c).sqrMagnitude;
            if (d < best)
            {
                best = d;
            }
        }
        return Mathf.Sqrt(Mathf.Max(0f, best));
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

    private static bool CheckCheckpoints()
    {
        OpenMission();
        var controller = Object.FindAnyObjectByType<Mission2Controller>();
        var pool = Object.FindAnyObjectByType<AgentPool>();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var config = AssetDatabase.LoadAssetAtPath<Mission2Config>("Assets/Config/Mission2Config.asset");
        if (controller == null || pool == null || player == null || config == null)
        {
            Debug.Log("Verify Fase 5A [Checkpoints]: FAIL — sin piezas.");
            return false;
        }
        controller.StartRoute();
        pool.ReclaimAll();
        // CP1 a pie.
        player.transform.position = config.checkpoints[0].position;
        controller.Simulate(0.2f);
        bool cp1 = controller.CheckpointIndex == 1;
        // Salto a CP3 sin pasar por CP2: no debe avanzar.
        player.transform.position = config.checkpoints[2].position;
        controller.Simulate(0.2f);
        bool ordered = controller.CheckpointIndex == 1;
        // CP2 (pelea): aparecen hostiles, se eliminan y avanza.
        player.transform.position = config.checkpoints[1].position;
        controller.Simulate(0.2f);
        int guard = 0;
        while (controller.CheckpointIndex == 1 && guard < 60)
        {
            KillHostiles(pool);
            controller.Simulate(0.5f);
            guard++;
        }
        bool cp2 = controller.CheckpointIndex == 2;
        player.transform.position = config.checkpoints[2].position;
        controller.Simulate(0.2f);
        bool cp3 = controller.CheckpointIndex == 3;
        player.transform.position = config.checkpoints[3].position;
        controller.Simulate(0.2f);
        SaveData save = SaveSystem.LoadFreshWithoutWriting();
        bool arrived = controller.State == Mission2State.Arrived
            && !save.completedMissions.Contains("Mision2_Ruta");
        bool pass = cp1 && ordered && cp2 && cp3 && arrived;
        Debug.Log(pass
            ? "Verify Fase 5A [Checkpoints]: PASS — orden 1-2(pelea)-3-4 y llegada guardada."
            : "Verify Fase 5A [Checkpoints]: FAIL — cp1=" + cp1 + " orden=" + ordered + " cp2=" + cp2 + " cp3=" + cp3 + " llegada=" + arrived + ".");
        return pass;
    }

    private static bool CheckRivalsAndCompass()
    {
        AgentData rival = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Uni_Rival.asset");
        AgentData police = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Policia_Patrullero.asset");
        AgentData sena = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Aliado_SENA.asset");
        var config = AssetDatabase.LoadAssetAtPath<Mission2Config>("Assets/Config/Mission2Config.asset");
        OpenMission();
        var compass = Object.FindAnyObjectByType<CompassUI>();
        bool rivals = rival != null && rival.hostile && police != null && police.hostile
            && sena != null && !sena.hostile;
        bool compassOk = compass != null && config != null
            && (compass.Target - config.morroTop).sqrMagnitude < 1f;
        bool pass = rivals && compassOk;
        Debug.Log(pass
            ? "Verify Fase 5A [Rivales+Brujula]: PASS — Uni rival hostil y brújula al Morro."
            : "Verify Fase 5A [Rivales+Brujula]: FAIL — rivals=" + rivals + " brujula=" + compassOk + ".");
        return pass;
    }
}
