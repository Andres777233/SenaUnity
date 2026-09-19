using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Popayork.Enemies;
using Popayork.Missions;
using Popayork.UI;

public static class VerifyFase3
{
    private const string ArenaPath = "Assets/Scenes/TestArena.unity";

    [MenuItem("Popayork/Verify Fase 3")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 3: PASS" : "Verify Fase 3: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFase3.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 3: PASS" : "Verify Fase 3: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Fase 3: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckNavMesh();
        ok &= CheckWaveSpawns();
        ok &= CheckEnemiesAdvance();
        ok &= CheckEnemiesDie();
        ok &= CheckNoGcSpike30();
        ok &= CheckChatter();
        return ok;
    }

    private static AgentPool OpenArena()
    {
        EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Single);
        var pool = Object.FindAnyObjectByType<AgentPool>();
        if (pool != null)
        {
            pool.Discover();
        }
        return pool;
    }

    private static bool CheckNavMesh()
    {
        OpenArena();
        var surface = Object.FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();
        Vector3 from = new Vector3(0f, 0.1f, 22f);
        Vector3 to = new Vector3(0f, 0.1f, -20f);
        var path = new NavMeshPath();
        bool calculated = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
        bool pass = surface != null && calculated &&
            (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial);
        Debug.Log(pass
            ? "Verify Fase 3 [NavMesh]: PASS — superficie + ruta calculada (" + path.status + ")."
            : "Verify Fase 3 [NavMesh]: FAIL — sin superficie o sin ruta.");
        return pass;
    }

    private static bool CheckWaveSpawns()
    {
        OpenArena();
        var waves = Object.FindAnyObjectByType<WaveManager>();
        var pool = Object.FindAnyObjectByType<AgentPool>();
        WaveData test = AssetDatabase.LoadAssetAtPath<WaveData>("Assets/Config/Waves/Oleada_Prueba.asset");
        if (waves == null || pool == null || test == null)
        {
            Debug.Log("Verify Fase 3 [Wave]: FAIL — sin WaveManager/pool/oleada.");
            return false;
        }
        pool.ReclaimAll();
        int spawned = waves.SpawnWaveNow(test);
        bool pass = spawned == test.TotalCount && pool.ActiveCount == test.TotalCount;
        Debug.Log(pass
            ? "Verify Fase 3 [Wave]: PASS — oleada completa (" + spawned + "/" + test.TotalCount + ")."
            : "Verify Fase 3 [Wave]: FAIL — spawned=" + spawned + " esperados=" + test.TotalCount + ".");
        return pass;
    }

    private static bool CheckEnemiesAdvance()
    {
        OpenArena();
        var pool = Object.FindAnyObjectByType<AgentPool>();
        GameObject player = GameObject.Find("Player");
        if (pool == null || player == null)
        {
            Debug.Log("Verify Fase 3 [Advance]: FAIL — sin pool o jugador.");
            return false;
        }
        pool.ReclaimAll();
        Vector3 objective = player.transform.position;
        Vector3[] spawns = new Vector3[] {
            new Vector3(-10f, 0.1f, 20f), new Vector3(10f, 0.1f, 20f),
            new Vector3(0f, 0.1f, 24f), new Vector3(-5f, 0.1f, 22f) };
        for (int i = 0; i < spawns.Length; i++)
        {
            pool.Spawn(Faction.Police, spawns[i], objective);
        }
        float before = AveragePoliceDistance(pool, objective);
        for (int step = 0; step < 120; step++)
        {
            for (int i = 0; i < pool.ActiveCount; i++)
            {
                AgentBrain brain = pool.GetActive(i);
                if (brain != null && brain.Faction == Faction.Police)
                {
                    brain.Simulate(0.1f);
                }
            }
        }
        float after = AveragePoliceDistance(pool, objective);
        pool.ReclaimAll();
        bool pass = before - after > 1.5f;
        Debug.Log(pass
            ? "Verify Fase 3 [Advance]: PASS — avanzan al objetivo (" + before.ToString("F1") + "m -> " + after.ToString("F1") + "m)."
            : "Verify Fase 3 [Advance]: FAIL — dist antes=" + before.ToString("F1") + " después=" + after.ToString("F1") + ".");
        return pass;
    }

    private static float AveragePoliceDistance(AgentPool pool, Vector3 objective)
    {
        float sum = 0f;
        int n = 0;
        for (int i = 0; i < pool.ActiveCount; i++)
        {
            AgentBrain brain = pool.GetActive(i);
            if (brain != null && brain.Faction == Faction.Police && !brain.Health.IsDead)
            {
                Vector3 d = brain.transform.position - objective;
                d.y = 0f;
                sum += d.magnitude;
                n++;
            }
        }
        return n > 0 ? sum / n : 0f;
    }

    private static bool CheckEnemiesDie()
    {
        OpenArena();
        var pool = Object.FindAnyObjectByType<AgentPool>();
        GameObject player = GameObject.Find("Player");
        if (pool == null || player == null)
        {
            Debug.Log("Verify Fase 3 [Death]: FAIL — sin pool o jugador.");
            return false;
        }
        pool.ReclaimAll();
        AgentBrain brain = pool.Spawn(Faction.Police, new Vector3(0f, 0.1f, 10f), player.transform.position);
        if (brain == null)
        {
            Debug.Log("Verify Fase 3 [Death]: FAIL — no apareció agente.");
            return false;
        }
        int before = pool.ActiveCount;
        brain.Health.TakeDamage(100000f);
        bool dead = brain.Health.IsDead;
        brain.Simulate(2.5f);
        bool reclaimed = pool.ActiveCount == before - 1;
        bool pass = dead && reclaimed;
        Debug.Log(pass
            ? "Verify Fase 3 [Death]: PASS — muere al recibir daño y se retira sin gore."
            : "Verify Fase 3 [Death]: FAIL — dead=" + dead + " reclaimed=" + reclaimed + ".");
        return pass;
    }

    private static bool CheckNoGcSpike30()
    {
        OpenArena();
        var pool = Object.FindAnyObjectByType<AgentPool>();
        var waves = Object.FindAnyObjectByType<WaveManager>();
        var scheduler = Object.FindAnyObjectByType<AITickScheduler>();
        WaveData max = AssetDatabase.LoadAssetAtPath<WaveData>("Assets/Config/Waves/Oleada_Maxima.asset");
        if (pool == null || waves == null || scheduler == null || max == null)
        {
            Debug.Log("Verify Fase 3 [GC30]: FAIL — falta pool/waves/scheduler/oleada.");
            return false;
        }
        pool.ReclaimAll();
        int spawned = waves.SpawnWaveNow(max);
        if (spawned != WaveManager.MaxActiveAgents || pool.ActiveCount != WaveManager.MaxActiveAgents)
        {
            Debug.Log("Verify Fase 3 [GC30]: FAIL — oleada de 30 incompleta (" + spawned + ").");
            return false;
        }
        for (int i = 0; i < pool.ActiveCount; i++)
        {
            AgentBrain brain = pool.GetActive(i);
            if (brain != null)
            {
                brain.SetMute(true);
            }
        }
        for (int i = 0; i < 10; i++)
        {
            scheduler.Tick(0.05f);
        }
        System.GC.Collect();
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 300; i++)
        {
            scheduler.Tick(0.05f);
        }
        long after = System.GC.GetAllocatedBytesForCurrentThread();
        pool.ReclaimAll();
        bool pass = after == before;
        Debug.Log(pass
            ? "Verify Fase 3 [GC30]: PASS — 0 bytes con 30 agentes en 300 ticks."
            : "Verify Fase 3 [GC30]: FAIL — " + (after - before) + " bytes con 30 agentes.");
        return pass;
    }

    private static bool CheckChatter()
    {
        NpcPhrases phrases = AssetDatabase.LoadAssetAtPath<NpcPhrases>("Assets/Config/NpcPhrases.asset");
        if (phrases == null)
        {
            Debug.Log("Verify Fase 3 [Chatter]: FAIL — sin asset de frases.");
            return false;
        }
        bool allLines = true;
        Faction[] factions = new Faction[] { Faction.Police, Faction.Sena, Faction.University };
        PhraseKind[] kinds = new PhraseKind[] { PhraseKind.Spawn, PhraseKind.Attack, PhraseKind.Death, PhraseKind.Retreat, PhraseKind.Idle };
        for (int f = 0; f < factions.Length && allLines; f++)
        {
            for (int k = 0; k < kinds.Length && allLines; k++)
            {
                if (string.IsNullOrEmpty(phrases.Pick(factions[f], kinds[k])))
                {
                    allLines = false;
                }
            }
        }
        OpenArena();
        var subtitles = Object.FindAnyObjectByType<SubtitleSystem>();
        bool shown = false;
        if (subtitles != null)
        {
            subtitles.ShowLine("¡Aguante, parceros!", 2.5f);
            shown = subtitles.CurrentText == "¡Aguante, parceros!";
        }
        bool pass = allLines && shown;
        Debug.Log(pass
            ? "Verify Fase 3 [Chatter]: PASS — frases colombianas por facción + subtítulo visible."
            : "Verify Fase 3 [Chatter]: FAIL — allLines=" + allLines + " shown=" + shown + ".");
        return pass;
    }
}
