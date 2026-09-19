using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Missions;
using Popayork.Player;
using Popayork.UI;

public static class Fase3Builder
{
    private const string PoliceFbx = "Assets/Models3D/Policias/source/Posed People by JJ - Police vol1 with HQ.fbx";
    private const string PersonasFbx = "Assets/Models3D/personas modelos1/source/temp_export.fbx";
    private const string ArenaPath = "Assets/Scenes/TestArena.unity";

    [MenuItem("Popayork/Construir Fase 3")]
    public static void BuildAll()
    {
        EnsureFolders();
        NpcPhrases phrases = BuildPhrases();
        AgentData[] datas = BuildAgentDatas();
        WaveData testWave = BuildWave("Oleada_Prueba", new WaveEntry[] {
            NewEntry(Faction.Police, 4), NewEntry(Faction.Sena, 2) });
        WaveData maxWave = BuildWave("Oleada_Maxima", new WaveEntry[] {
            NewEntry(Faction.Police, 16), NewEntry(Faction.Sena, 7), NewEntry(Faction.University, 7) });
        BuildArena(datas, phrases);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Fase 3: PASS — NavMesh + 30 agentes + oleadas (" + testWave.TotalCount + "/" + maxWave.TotalCount + ").");
    }

    private static WaveEntry NewEntry(Faction faction, int count)
    {
        return new WaveEntry { faction = faction, count = count };
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Config", "Agents");
        EnsureFolder("Assets/Config", "Waves");
    }

    private static void EnsureFolder(string parent, string leaf)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + leaf))
        {
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    // ---------- Datos ----------

    private static NpcPhrases BuildPhrases()
    {
        const string path = "Assets/Config/NpcPhrases.asset";
        NpcPhrases p = AssetDatabase.LoadAssetAtPath<NpcPhrases>(path);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<NpcPhrases>();
            AssetDatabase.CreateAsset(p, path);
        }
        EditorUtility.SetDirty(p);
        return p;
    }

    private static AgentData[] BuildAgentDatas()
    {
        AgentData patrol = BuildAgent("Policia_Patrullero", "Patrullero", Faction.Police, PoliceFbx,
            "Police idle1_gameasset", 100f, 3.5f, 2.4f, 10f, 1.2f, 0.25f, Color.red);
        AgentData walker = BuildAgent("Policia_Caminante", "Caminante", Faction.Police, PoliceFbx,
            "Police walk1_gameasset", 80f, 4.2f, 2.2f, 8f, 1.0f, 0.2f, Color.red);
        AgentData sena = BuildAgent("Aliado_SENA", "Aprendiz SENA", Faction.Sena, PersonasFbx,
            "Man01", 90f, 4.0f, 2.4f, 12f, 1.1f, 0.2f, new Color(1f, 0.45f, 0f));
        AgentData uni = BuildAgent("Aliado_Uni", "Universitario", Faction.University, PersonasFbx,
            "Woman01", 90f, 4.0f, 2.4f, 12f, 1.1f, 0.2f, new Color(0.1f, 0.7f, 1f));
        return new AgentData[] { patrol, walker, sena, uni };
    }

    private static AgentData BuildAgent(string file, string display, Faction faction, string fbx, string root,
        float hp, float speed, float range, float dmg, float cooldown, float retreat, Color ring)
    {
        string path = "Assets/Config/Agents/" + file + ".asset";
        AgentData data = AssetDatabase.LoadAssetAtPath<AgentData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<AgentData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.displayName = display;
        data.faction = faction;
        data.modelFbxPath = fbx;
        data.modelRootName = root;
        data.maxHealth = hp;
        data.moveSpeed = speed;
        data.targetHeightMeters = 1.75f;
        data.ringColor = ring;
        data.attackRange = range;
        data.attackDamage = dmg;
        data.attackCooldown = cooldown;
        data.retreatHealthFraction = retreat;
        data.repathInterval = 1.0f;
        data.sightRange = 25.0f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static WaveData BuildWave(string file, WaveEntry[] entries)
    {
        string path = "Assets/Config/Waves/" + file + ".asset";
        WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        if (wave == null)
        {
            wave = ScriptableObject.CreateInstance<WaveData>();
            AssetDatabase.CreateAsset(wave, path);
        }
        wave.waveName = file.Replace('_', ' ');
        wave.entries = entries;
        wave.spawnInterval = 0.5f;
        EditorUtility.SetDirty(wave);
        return wave;
    }

    // ---------- Arena ----------

    private static GameObject FindFbxChild(string fbxPath, string rootName)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbx == null)
        {
            return null;
        }
        Transform[] all = fbx.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == rootName)
            {
                return all[i].gameObject;
            }
        }
        return null;
    }

    private static void BuildArena(AgentData[] datas, NpcPhrases phrases)
    {
        Scene scene = EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Single);

        GameObject player = GameObject.Find("Player");
        Transform objective = player != null ? player.transform : null;

        Vector3[] spawnPos = new Vector3[] {
            new Vector3(-10f, 0.1f, 20f), new Vector3(10f, 0.1f, 20f), new Vector3(0f, 0.1f, 24f) };
        Transform[] spawns = new Transform[spawnPos.Length];
        for (int i = 0; i < spawnPos.Length; i++)
        {
            GameObject s = GameObject.Find("Spawn_" + i);
            if (s == null)
            {
                s = new GameObject("Spawn_" + i);
                s.transform.position = spawnPos[i];
            }
            spawns[i] = s.transform;
        }
        Vector3[] coverPos = new Vector3[] {
            new Vector3(-6f, 0f, 5.5f), new Vector3(6f, 0f, -0.5f),
            new Vector3(0f, 0f, -5.5f), new Vector3(0f, 0f, 8f) };
        Vector3[] covers = new Vector3[coverPos.Length];
        for (int i = 0; i < coverPos.Length; i++)
        {
            GameObject c = GameObject.Find("CoverPoint_" + i);
            if (c == null)
            {
                c = new GameObject("CoverPoint_" + i);
                c.transform.position = coverPos[i];
            }
            covers[i] = c.transform.position;
        }
        Vector3 retreat = new Vector3(0f, 0.1f, -28f);

        BakeNavMesh();

        GameObject agentsRoot = GameObject.Find("Agents");
        if (agentsRoot == null)
        {
            agentsRoot = new GameObject("Agents");
        }
        else
        {
            // Regeneración limpia del contenido generado por este menú.
            while (agentsRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(agentsRoot.transform.GetChild(0).gameObject);
            }
        }
        GameObject poolGo = GameObject.Find("AgentPool");
        if (poolGo == null)
        {
            poolGo = new GameObject("AgentPool");
        }
        AgentPool pool = poolGo.GetComponent<AgentPool>();
        if (pool == null)
        {
            poolGo.AddComponent<AgentPool>();
            pool = poolGo.GetComponent<AgentPool>();
        }

        int slot = 0;
        pool.Clear();
        slot = BuildSlots(agentsRoot.transform, pool, datas[0], phrases, covers, retreat, slot, 8);
        slot = BuildSlots(agentsRoot.transform, pool, datas[1], phrases, covers, retreat, slot, 8);
        slot = BuildSlots(agentsRoot.transform, pool, datas[2], phrases, covers, retreat, slot, 7);
        slot = BuildSlots(agentsRoot.transform, pool, datas[3], phrases, covers, retreat, slot, 7);

        GameObject schedGo = GameObject.Find("AIScheduler");
        if (schedGo == null)
        {
            schedGo = new GameObject("AIScheduler");
        }
        AITickScheduler scheduler = schedGo.GetComponent<AITickScheduler>();
        if (scheduler == null)
        {
            scheduler = schedGo.AddComponent<AITickScheduler>();
        }
        scheduler.Bind(pool);

        GameObject waveGo = GameObject.Find("WaveManager");
        if (waveGo == null)
        {
            waveGo = new GameObject("WaveManager");
        }
        WaveManager waves = waveGo.GetComponent<WaveManager>();
        if (waves == null)
        {
            waves = waveGo.AddComponent<WaveManager>();
        }
        waves.Bind(pool, spawns, objective);

        BuildSubtitles();
        EditorSceneManager.SaveScene(scene, ArenaPath);
    }

    private static void BakeNavMesh()
    {
        GameObject bakeGo = GameObject.Find("NavMeshBake");
        if (bakeGo == null)
        {
            bakeGo = new GameObject("NavMeshBake");
        }
        NavMeshSurface surface = bakeGo.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = bakeGo.AddComponent<NavMeshSurface>();
        }
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.center = new Vector3(0f, 1f, 0f);
        surface.size = new Vector3(65f, 8f, 65f);
        surface.defaultArea = 0;
        surface.BuildNavMesh();
        var path = new NavMeshPath();
        bool ok = NavMesh.CalculatePath(new Vector3(0f, 0.1f, 22f), new Vector3(0f, 0.1f, -20f), NavMesh.AllAreas, path)
            && (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial);
        Debug.Log(ok
            ? "Fase3 NavMesh: PASS — bake por script en TestArena."
            : "Fase3 NavMesh: FAIL — bake sin ruta válida.");
    }

    private static int BuildSlots(Transform parent, AgentPool pool, AgentData data, NpcPhrases phrases, Vector3[] covers, Vector3 retreat, int firstSlot, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int slot = firstSlot + i;
            GameObject go = new GameObject("Agent_" + data.faction + "_" + slot);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-40f, -10f, 0f);

            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.speed = data.moveSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = data.attackRange * 0.8f;
            agent.radius = 0.32f;
            agent.height = 1.7f;
            agent.avoidancePriority = 50 + (slot % 50);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

            go.AddComponent<AgentHealth>();
            go.AddComponent<AgentMovement>();
            AgentBrain brain = go.AddComponent<AgentBrain>();

            GameObject view = new GameObject("View");
            view.transform.SetParent(go.transform, false);
            GameObject modelSrc = FindFbxChild(data.modelFbxPath, data.modelRootName);
            if (modelSrc != null)
            {
                GameObject model = Object.Instantiate(modelSrc);
                model.name = data.modelRootName + "_View";
                model.transform.SetParent(view.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                float measured = MeasureHeight(model);
                float scale = measured > 0.001f ? data.targetHeightMeters / measured : 1f;
                model.transform.localScale = Vector3.one * scale;
                CenterPivot(model);
                Debug.Log("Fase3 modelo " + data.modelRootName + ": medido=" + measured.ToString("F2") + "m escala=" + scale.ToString("F3"));
                DressFaction(view.transform, data, measured * scale);
            }
            else
            {
                Debug.LogWarning("Fase3: modelo " + data.modelRootName + " no encontrado.");
            }

            brain.Setup(data, pool, slot, phrases, covers, retreat);
            pool.Register(brain);
        }
        return firstSlot + count;
    }

    private static float MeasureHeight(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return 0f;
        }
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b.size.y;
    }

    private static void CenterPivot(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        Vector3 localCenter = model.transform.InverseTransformPoint(b.center);
        localCenter.y = 0f;
        model.transform.localPosition -= localCenter;
    }

    private static void DressFaction(Transform view, AgentData data, float height)
    {
        Material vestMat = new Material(Shader.Find("Standard"));
        vestMat.color = data.ringColor;
        GameObject vest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vest.name = "Vest";
        vest.transform.SetParent(view, false);
        vest.transform.localPosition = new Vector3(0f, height * 0.58f, 0f);
        vest.transform.localScale = new Vector3(0.44f, 0.5f, 0.28f);
        vest.GetComponent<MeshRenderer>().sharedMaterial = vestMat;
        Object.DestroyImmediate(vest.GetComponent<BoxCollider>());

        Material ringMat = new Material(Shader.Find("Standard"));
        ringMat.color = data.ringColor;
        ringMat.EnableKeyword("_EMISSION");
        ringMat.SetColor("_EmissionColor", data.ringColor * 0.6f);
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(view, false);
        ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        ring.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
        ring.GetComponent<MeshRenderer>().sharedMaterial = ringMat;
        Object.DestroyImmediate(ring.GetComponent<CapsuleCollider>());
    }

    private static Font DefaultFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null)
        {
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return f;
    }

    private static void BuildSubtitles()
    {
        GameObject canvasGo = GameObject.Find("SubtitleCanvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("SubtitleCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        SubtitleSystem subtitles = canvasGo.GetComponent<SubtitleSystem>();
        if (subtitles == null)
        {
            subtitles = canvasGo.AddComponent<SubtitleSystem>();
        }
        Text label = canvasGo.GetComponentInChildren<Text>();
        if (label == null)
        {
            GameObject go = new GameObject("SubtitleText");
            go.transform.SetParent(canvasGo.transform, false);
            label = go.AddComponent<Text>();
            label.font = DefaultFont();
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.95f, 0.6f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.12f);
            rt.anchorMax = new Vector2(0.9f, 0.22f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            label.enabled = false;
        }
        subtitles.Bind(label);
    }
}
