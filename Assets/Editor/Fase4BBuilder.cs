using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Missions;
using Popayork.UI;

public static class Fase4BBuilder
{
    private const string ScenePath = "Assets/Scenes/Mision1.unity";
    private const string PoliceFbx = "Assets/Models3D/Policias/source/Posed People by JJ - Police vol1 with HQ.fbx";
    private const string PersonasFbx = "Assets/Models3D/personas modelos1/source/temp_export.fbx";

    [MenuItem("Popayork/Construir Fase 4B")]
    public static void BuildAll()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        ParqueConfig parque = AssetDatabase.LoadAssetAtPath<ParqueConfig>("Assets/Config/ParqueConfig.asset");
        Vector3 centro = parque != null ? parque.centroPlaza : Vector3.zero;
        float lado = parque != null ? parque.ladoPlaza : 60f;
        float suelo = parque != null ? parque.alturaSuelo : 0f;

        GameObject torreObj = GameObject.Find("Objetivo_Torre");
        Transform torre = torreObj != null ? torreObj.transform : null;

        NpcPhrases phrases = AssetDatabase.LoadAssetAtPath<NpcPhrases>("Assets/Config/NpcPhrases.asset");
        AgentData patrol = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Policia_Patrullero.asset");
        AgentData walker = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Policia_Caminante.asset");
        AgentData sena = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Aliado_SENA.asset");
        AgentData uni = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Aliado_Uni.asset");

        Mission1Config config = BuildConfig();
        Vector3[] covers = BuildCoverMarkers(centro, lado, suelo);
        Vector3 retreat = centro + new Vector3(0f, 0.5f, -lado * 0.45f);

        AgentPool pool = GetOrCreate<AgentPool>("AgentPool");
        pool.Clear();
        GameObject agentsRoot = GameObject.Find("Agents");
        if (agentsRoot == null)
        {
            agentsRoot = new GameObject("Agents");
        }
        int slot = 0;
        slot = BuildSlots(agentsRoot.transform, pool, patrol, phrases, covers, retreat, slot, 12);
        slot = BuildSlots(agentsRoot.transform, pool, walker, phrases, covers, retreat, slot, 12);
        slot = BuildSlots(agentsRoot.transform, pool, sena, phrases, covers, retreat, slot, 2);
        slot = BuildSlots(agentsRoot.transform, pool, uni, phrases, covers, retreat, slot, 2);

        AITickScheduler scheduler = GetOrCreate<AITickScheduler>("AIScheduler");
        scheduler.Bind(pool);

        Transform[] spawns = new Transform[] {
            FindMarker("Spawn_Oleada_0"), FindMarker("Spawn_Oleada_1"), FindMarker("Spawn_Oleada_2") };
        WaveManager waves = GetOrCreate<WaveManager>("WaveManager");
        waves.Bind(pool, spawns, torre);

        MissionUI missionUI = BuildMissionUI();
        Mission1Controller controller = GetOrCreate<Mission1Controller>("Mission1");
        controller.Bind(config, waves, pool, torre, missionUI);

        BuildSubtitles();
        BuildChaos(centro, lado, suelo);
        EnsureEventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Rebake();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Fase 4B: PASS — misión 'Empieza el caos' cableada en Mision1.");
    }

    private static T GetOrCreate<T>(string name) where T : Component
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        T comp = go.GetComponent<T>();
        if (comp == null)
        {
            comp = go.AddComponent<T>();
        }
        return comp;
    }

    private static Transform FindMarker(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }

    // ---------- Datos ----------

    private static Mission1Config BuildConfig()
    {
        const string path = "Assets/Config/Mission1Config.asset";
        Mission1Config config = AssetDatabase.LoadAssetAtPath<Mission1Config>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<Mission1Config>();
            AssetDatabase.CreateAsset(config, path);
        }
        config.missionTime = 240f;
        config.captureRadius = 4f;
        config.warningRadius = 14f;
        config.waves = new WaveData[] {
            BuildWave("M1_Oleada1", Faction.Police, 4, 1.2f),
            BuildWave("M1_Oleada2", Faction.Police, 6, 0.9f),
            BuildWave("M1_Oleada3", Faction.Police, 8, 0.7f) };
        WaveData allies = BuildAlliesWave();
        config.alliesWave = allies;
        EditorUtility.SetDirty(config);
        return config;
    }

    private static WaveData BuildWave(string file, Faction faction, int count, float interval)
    {
        string path = "Assets/Config/Waves/" + file + ".asset";
        WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        if (wave == null)
        {
            wave = ScriptableObject.CreateInstance<WaveData>();
            AssetDatabase.CreateAsset(wave, path);
        }
        wave.waveName = file.Replace('_', ' ');
        wave.entries = new WaveEntry[] { new WaveEntry { faction = faction, count = count } };
        wave.spawnInterval = interval;
        EditorUtility.SetDirty(wave);
        return wave;
    }

    private static WaveData BuildAlliesWave()
    {
        const string path = "Assets/Config/Waves/M1_Aliados.asset";
        WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        if (wave == null)
        {
            wave = ScriptableObject.CreateInstance<WaveData>();
            AssetDatabase.CreateAsset(wave, path);
        }
        wave.waveName = "M1 Aliados";
        wave.entries = new WaveEntry[] {
            new WaveEntry { faction = Faction.Sena, count = 2 },
            new WaveEntry { faction = Faction.University, count = 2 } };
        wave.spawnInterval = 0.5f;
        EditorUtility.SetDirty(wave);
        return wave;
    }

    // ---------- Agentes ----------

    private static Vector3[] BuildCoverMarkers(Vector3 centro, float lado, float suelo)
    {
        Vector3[] pos = new Vector3[] {
            centro + new Vector3(-lado * 0.3f, 0.5f, lado * 0.2f),
            centro + new Vector3(lado * 0.3f, 0.5f, lado * 0.2f),
            centro + new Vector3(-lado * 0.3f, 0.5f, -lado * 0.2f),
            centro + new Vector3(lado * 0.3f, 0.5f, -lado * 0.2f) };
        Vector3[] covers = new Vector3[pos.Length];
        for (int i = 0; i < pos.Length; i++)
        {
            GameObject c = GameObject.Find("CoverM1_" + i);
            if (c == null)
            {
                c = new GameObject("CoverM1_" + i);
                c.transform.position = pos[i];
            }
            covers[i] = c.transform.position;
        }
        return covers;
    }

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

    private static int BuildSlots(Transform parent, AgentPool pool, AgentData data, NpcPhrases phrases, Vector3[] covers, Vector3 retreat, int firstSlot, int count)
    {
        if (data == null)
        {
            Debug.LogWarning("Fase4B: falta AgentData.");
            return firstSlot;
        }
        for (int i = 0; i < count; i++)
        {
            int slot = firstSlot + i;
            GameObject go = new GameObject("AgentM1_" + data.faction + "_" + slot);
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

            CapsuleCollider hitbox = go.AddComponent<CapsuleCollider>();
            hitbox.isTrigger = true;
            hitbox.radius = 0.4f;
            hitbox.height = 1.8f;
            hitbox.center = new Vector3(0f, 0.9f, 0f);

            go.AddComponent<AgentHealth>();
            go.AddComponent<AgentMovement>();
            AgentBrain brain = go.AddComponent<AgentBrain>();

            GameObject view = new GameObject("View");
            view.transform.SetParent(go.transform, false);
            string fbx = data.faction == Faction.Police ? PoliceFbx : PersonasFbx;
            GameObject modelSrc = FindFbxChild(fbx, data.modelRootName);
            if (modelSrc != null)
            {
                GameObject model = Object.Instantiate(modelSrc);
                model.name = data.modelRootName + "_View";
                model.transform.SetParent(view.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int r = 1; r < renderers.Length; r++)
                    {
                        b.Encapsulate(renderers[r].bounds);
                    }
                    float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                    if (longest > 0.001f)
                    {
                        model.transform.localScale = Vector3.one * (data.targetHeightMeters / longest);
                    }
                }
                DressRing(view.transform, data);
            }
            brain.Setup(data, pool, slot, phrases, covers, retreat);
            pool.Register(brain);
        }
        return firstSlot + count;
    }

    private static void DressRing(Transform view, AgentData data)
    {
        Material ringMat = new Material(Shader.Find("Standard"));
        ringMat.color = data.ringColor;
        ringMat.EnableKeyword("_EMISSION");
        ringMat.SetColor("_EmissionColor", data.ringColor * 0.6f);
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(view, false);
        ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        ring.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
        ring.GetComponent<MeshRenderer>().sharedMaterial = ringMat;
        Object.DestroyImmediate(ring.GetComponent<CapsuleCollider>());
    }

    // ---------- UI ----------

    private static Font DefaultFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null)
        {
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return f;
    }

    private static MissionUI BuildMissionUI()
    {
        GameObject canvasGo = GameObject.Find("MissionCanvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("MissionCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        MissionUI ui = canvasGo.GetComponent<MissionUI>();
        if (ui == null)
        {
            ui = canvasGo.AddComponent<MissionUI>();
        }

        GameObject intro = GetChild(canvasGo.transform, "IntroPanel", true);
        Text title = GetLabel(intro.transform, "IntroTitle", "EMPIEZA EL CAOS", 52, TextAnchor.MiddleCenter);
        Text body = GetLabel(intro.transform, "IntroBody",
            "La marcha se descontroló, parce. La tomba viene por la Torre del Reloj.\n\nWASD moverse · Mouse mirar · Shift correr · Espacio saltar\nClick disparar · 1/2 armas · R recargar · P pausa\n\n¡Aguanta con tus parceros SENA y de la U!",
            20, TextAnchor.MiddleCenter);
        Button goBtn = GetButton(intro.transform, "BtnVamos", "¡A defender la Torre!");
        var controller = GameObject.Find("Mission1");
        Mission1Controller ctrl = controller != null ? controller.GetComponent<Mission1Controller>() : null;
        if (ctrl == null)
        {
            GameObject cgo = controller != null ? controller : new GameObject("Mission1");
            cgo.name = "Mission1";
            ctrl = cgo.GetComponent<Mission1Controller>();
            if (ctrl == null)
            {
                ctrl = cgo.AddComponent<Mission1Controller>();
            }
        }
        UnityEventTools.AddVoidPersistentListener(goBtn.onClick, ctrl.StartMission);

        GameObject objBar = GetChild(canvasGo.transform, "ObjectiveBar", false);
        RectTransform objRt = objBar.GetComponent<RectTransform>();
        objRt.anchorMin = new Vector2(0f, 1f);
        objRt.anchorMax = new Vector2(1f, 1f);
        objRt.offsetMin = new Vector2(120f, -80f);
        objRt.offsetMax = new Vector2(-120f, -10f);
        Text objText = GetLabel(objBar.transform, "ObjectiveText", "Defiende la Torre del Reloj", 22, TextAnchor.MiddleCenter);
        GameObject barBg = GetChild(objBar.transform, "ProgressBG", false);
        Image bgImg = barBg.GetComponent<Image>();
        if (bgImg == null)
        {
            bgImg = barBg.AddComponent<Image>();
        }
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);
        GameObject barFill = GetChild(objBar.transform, "ProgressFill", false);
        Image fillImg = barFill.GetComponent<Image>();
        if (fillImg == null)
        {
            fillImg = barFill.AddComponent<Image>();
        }
        fillImg.color = new Color(0.2f, 0.8f, 0.3f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;

        GameObject result = GetChild(canvasGo.transform, "ResultPanel", true);
        Text resultText = GetLabel(result.transform, "ResultText", "", 34, TextAnchor.MiddleCenter);
        Button retryBtn = GetButton(result.transform, "BtnReintentar", "Reintentar");
        Button menuBtn = GetButton(result.transform, "BtnMenu", "Volver al menú");
        UnityEventTools.AddVoidPersistentListener(retryBtn.onClick, ctrl.Retry);
        UnityEventTools.AddVoidPersistentListener(menuBtn.onClick, ctrl.ToMenu);
        result.SetActive(false);

        ui.Bind(intro, title, body, objBar, objText, fillImg, result, resultText);
        ui.ShowIntro();
        return ui;
    }

    private static GameObject GetChild(Transform parent, string name, bool center)
    {
        Transform t = parent.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = t.gameObject;
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = go.AddComponent<RectTransform>();
        }
        if (center)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(640f, 480f);
            if (go.GetComponent<Image>() == null)
            {
                Image img = go.AddComponent<Image>();
                img.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
            }
        }
        return go;
    }

    private static Text GetLabel(Transform parent, string name, string content, int size, TextAnchor align)
    {
        Transform t = parent.Find(name);
        GameObject go = t == null ? new GameObject(name) : t.gameObject;
        go.transform.SetParent(parent, false);
        Text label = go.GetComponent<Text>();
        if (label == null)
        {
            label = go.AddComponent<Text>();
        }
        label.font = DefaultFont();
        label.text = content;
        label.fontSize = size;
        label.alignment = align;
        label.color = new Color(1f, 0.95f, 0.7f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 0.9f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return label;
    }

    private static Button GetButton(Transform parent, string name, string label)
    {
        Transform t = parent.Find(name);
        GameObject go = t == null ? new GameObject(name) : t.gameObject;
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        if (img == null)
        {
            img = go.AddComponent<Image>();
        }
        img.color = new Color(0.6f, 0.2f, 0.1f, 0.95f);
        Button b = go.GetComponent<Button>();
        if (b == null)
        {
            b = go.AddComponent<Button>();
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = go.AddComponent<RectTransform>();
        }
        rt.anchorMin = new Vector2(0.5f, 0.15f);
        rt.anchorMax = new Vector2(0.5f, 0.15f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(340f, 56f);
        GetLabel(go.transform, "Label", label, 22, TextAnchor.MiddleCenter);
        return b;
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

    // ---------- Caos ----------

    private static void BuildChaos(Vector3 centro, float lado, float suelo)
    {
        Material debrisMat = new Material(Shader.Find("Standard"));
        debrisMat.color = new Color(0.25f, 0.22f, 0.2f);
        GameObject chaosRoot = GameObject.Find("Chaos");
        if (chaosRoot == null)
        {
            chaosRoot = new GameObject("Chaos");
        }
        Vector3[] spots = new Vector3[] {
            centro + new Vector3(-lado * 0.35f, 0f, lado * 0.1f),
            centro + new Vector3(lado * 0.35f, 0f, -lado * 0.1f),
            centro + new Vector3(0f, 0f, lado * 0.35f),
            centro + new Vector3(-lado * 0.2f, 0f, -lado * 0.35f) };
        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 p = new Vector3(spots[i].x, suelo + 0.3f, spots[i].z);
            GameObject fire = BuildEmitter("Fuego_" + i, p, new Color(1f, 0.45f, 0.1f), 1.2f, 3f, 0.5f);
            fire.transform.SetParent(chaosRoot.transform, true);
            GameObject smoke = BuildEmitter("Humo_" + i, p + Vector3.up * 1.5f, new Color(0.2f, 0.2f, 0.2f, 0.8f), 2.5f, 1.5f, 1.2f);
            smoke.transform.SetParent(chaosRoot.transform, true);
        }
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            Vector3 p = centro + new Vector3(Mathf.Cos(a) * lado * 0.32f, suelo + 0.25f, Mathf.Sin(a) * lado * 0.32f);
            GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cube);
            d.name = "Escombro_" + i;
            d.transform.SetParent(chaosRoot.transform, false);
            d.transform.position = p;
            d.transform.rotation = Quaternion.Euler(0f, i * 23f, 0f);
            d.transform.localScale = new Vector3(0.6f + (i % 3) * 0.5f, 0.5f, 0.6f + ((i + 1) % 3) * 0.4f);
            d.GetComponent<MeshRenderer>().sharedMaterial = debrisMat;
        }
        ChaosManager chaos = chaosRoot.GetComponent<ChaosManager>();
        if (chaos == null)
        {
            chaos = chaosRoot.AddComponent<ChaosManager>();
        }
        Vector3[] blasts = new Vector3[] { spots[0], spots[1], spots[2], spots[3] };
        for (int i = 0; i < blasts.Length; i++)
        {
            blasts[i] = new Vector3(blasts[i].x, suelo + 0.5f, blasts[i].z);
        }
        chaos.Bind(12f, 8f, 25f, 30f, blasts);
    }

    private static GameObject BuildEmitter(string name, Vector3 pos, Color color, float size, float speed, float lifetime)
    {
        GameObject old = GameObject.Find(name);
        if (old != null)
        {
            return old;
        }
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.duration = 2f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.maxParticles = 60;
        var emission = ps.emission;
        emission.rateOverTime = 20f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.6f;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material fxMat = new Material(Shader.Find("Standard"));
        fxMat.color = color;
        fxMat.EnableKeyword("_EMISSION");
        fxMat.SetColor("_EmissionColor", color);
        renderer.sharedMaterial = fxMat;
        return go;
    }

    // ---------- Sistemas ----------

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private static void Rebake()
    {
        GameObject bakeGo = GameObject.Find("NavMeshBake");
        if (bakeGo == null)
        {
            return;
        }
        NavMeshSurface surface = bakeGo.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            return;
        }
        surface.BuildNavMesh();
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Debug.Log("Fase4B NavMesh: rebake verts=" + tri.vertices.Length + ".");
    }
}
