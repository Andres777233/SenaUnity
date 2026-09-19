using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Missions;

public static class Fase5BBuilder
{
    private const string ScenePath = "Assets/Scenes/Mision2.unity";
    private const string PoliceFbx = "Assets/Models3D/Policias/source/Posed People by JJ - Police vol1 with HQ.fbx";

    [MenuItem("Popayork/Construir Fase 5B")]
    public static void BuildAll()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Mission2Config config = AssetDatabase.LoadAssetAtPath<Mission2Config>("Assets/Config/Mission2Config.asset");
        if (config == null)
        {
            Debug.LogError("Fase5B: sin Mission2Config.");
            return;
        }
        Vector3 morro = config.morroTop;

        AgentData smart = BuildSmartAsset();
        BuildDefenseWaves();
        Vector3[] covers = BuildDefenseCovers(morro);
        Vector3 exit = BuildCartones(morro);

        AgentPool pool = FindPool();
        pool.Clear();
        GameObject agentsRoot = GameObject.Find("Agents");
        if (agentsRoot == null)
        {
            agentsRoot = new GameObject("Agents");
        }
        else
        {
            while (agentsRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(agentsRoot.transform.GetChild(0).gameObject);
            }
        }
        NpcPhrases phrases = AssetDatabase.LoadAssetAtPath<NpcPhrases>("Assets/Config/NpcPhrases.asset");
        AgentData patrol = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Policia_Patrullero.asset");
        AgentData walker = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Policia_Caminante.asset");
        AgentData sena = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Aliado_SENA.asset");
        AgentData rival = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Uni_Rival.asset");
        AgentData uni = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Aliado_Uni.asset");
        Vector3 retreat = config.routePoints.Length > 0 ? config.routePoints[0] : morro;
        int slot = 0;
        slot = BuildSlots(agentsRoot.transform, pool, patrol, phrases, covers, retreat, slot, 6);
        slot = BuildSlots(agentsRoot.transform, pool, walker, phrases, covers, retreat, slot, 6);
        slot = BuildSlots(agentsRoot.transform, pool, sena, phrases, covers, retreat, slot, 4);
        slot = BuildSlots(agentsRoot.transform, pool, rival, phrases, covers, retreat, slot, 4);
        slot = BuildSlots(agentsRoot.transform, pool, uni, phrases, covers, retreat, slot, 2);
        slot = BuildSlots(agentsRoot.transform, pool, smart, phrases, covers, retreat, slot, 8);

        WaveManager defense = GetOrCreate<WaveManager>("DefenseWaves");
        Transform[] spawns = new Transform[] {
            Marker("M2Def_SpawnA", morro + new Vector3(-35f, 1f, 20f)),
            Marker("M2Def_SpawnB", morro + new Vector3(35f, 1f, 20f)),
            Marker("M2Def_SpawnC", morro + new Vector3(0f, 1f, 40f)) };
        defense.Bind(pool, spawns, Marker("MorroTop", morro));
        Mission2Controller controller = Object.FindAnyObjectByType<Mission2Controller>();
        if (controller != null)
        {
            controller.BindDefense(defense);
        }

        config.retreatThreshold = 12;
        config.exitPoint = exit;
        config.exitRadius = 5f;
        config.maxDeaths = 3;
        config.defenseGrace = 5f;
        EditorUtility.SetDirty(config);
        UiLayout.FixSceneUI();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Rebake();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Fase 5B: PASS — defensa del Morro y retirada a los cartones.");
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

    private static Transform Marker(string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        go.transform.position = pos;
        return go.transform;
    }

    private static AgentPool FindPool()
    {
        GameObject go = GameObject.Find("AgentPool");
        if (go == null)
        {
            go = new GameObject("AgentPool");
        }
        AgentPool pool = go.GetComponent<AgentPool>();
        if (pool == null)
        {
            pool = go.AddComponent<AgentPool>();
        }
        return pool;
    }

    // ---------- Datos ----------

    private static AgentData BuildSmartAsset()
    {
        const string path = "Assets/Config/Agents/SMART_Tactico.asset";
        AgentData data = AssetDatabase.LoadAssetAtPath<AgentData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<AgentData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.displayName = "SMART Táctico";
        data.faction = Faction.Police;
        data.hostile = true;
        data.modelFbxPath = PoliceFbx;
        data.modelRootName = "Police idle2_gameasset";
        data.maxHealth = 140f;
        data.moveSpeed = 4.5f;
        data.targetHeightMeters = 1.75f;
        data.ringColor = new Color(0.12f, 0.05f, 0.18f);
        data.attackRange = 2.6f;
        data.attackDamage = 14f;
        data.attackCooldown = 1f;
        data.retreatHealthFraction = 0.15f;
        data.repathInterval = 1f;
        data.sightRange = 28f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static WaveData NewWave(string file, string title, WaveEntry[] entries, float interval)
    {
        string path = "Assets/Config/Waves/" + file + ".asset";
        WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        if (wave == null)
        {
            wave = ScriptableObject.CreateInstance<WaveData>();
            AssetDatabase.CreateAsset(wave, path);
        }
        wave.waveName = title;
        wave.entries = entries;
        wave.spawnInterval = interval;
        EditorUtility.SetDirty(wave);
        return wave;
    }

    private static WaveEntry Entry(Faction faction, string variant, int count)
    {
        WaveEntry e = new WaveEntry();
        e.faction = faction;
        e.variant = variant;
        e.count = count;
        return e;
    }

    private static void BuildDefenseWaves()
    {
        const string configPath = "Assets/Config/Mission2Config.asset";
        Mission2Config config = AssetDatabase.LoadAssetAtPath<Mission2Config>(configPath);
        WaveData w1 = NewWave("M2_Defensa1", "M2 Defensa 1",
            new WaveEntry[] { Entry(Faction.Police, "Patrullero", 4) }, 1f);
        WaveData w2 = NewWave("M2_Defensa2", "M2 Defensa 2",
            new WaveEntry[] {
                Entry(Faction.Police, "Patrullero", 2),
                Entry(Faction.Police, "SMART Táctico", 3) }, 0.8f);
        WaveData w3 = NewWave("M2_Defensa3", "M2 Defensa 3",
            new WaveEntry[] {
                Entry(Faction.Police, "Caminante", 4),
                Entry(Faction.Police, "SMART Táctico", 6) }, 0.6f);
        if (config != null)
        {
            config.defenseWaves = new WaveData[] { w1, w2, w3 };
            EditorUtility.SetDirty(config);
        }
    }

    // ---------- Escena ----------

    private static Vector3[] BuildDefenseCovers(Vector3 morro)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject vieja = GameObject.Find("Trinchera_" + i);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
        }
        Vector3[] pos = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            pos[i] = morro + new Vector3(Mathf.Cos(a) * 14f, 0.6f, Mathf.Sin(a) * 14f);
            GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bag.name = "Trinchera_" + i;
            bag.transform.position = pos[i];
            bag.transform.localScale = new Vector3(3f, 1.2f, 1f);
            bag.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            Material mat = MaterialFactory.New();
            mat.color = new Color(0.6f, 0.52f, 0.38f);
            bag.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObject marker = GameObject.Find("CoverM2D_" + i);
            if (marker == null)
            {
                marker = new GameObject("CoverM2D_" + i);
            }
            marker.transform.position = pos[i];
            pos[i] = marker.transform.position;
        }
        return pos;
    }

    private static Vector3 BuildCartones(Vector3 morro)
    {
        Vector3 dir = new Vector3(0.5f, 0f, 0.5f).normalized;
        Vector3 exit = morro + dir * 32f;
        exit.y = morro.y;
        Marker("Salida_Cartones", exit);
        Material carton = MaterialFactory.New();
        carton.color = new Color(0.65f, 0.48f, 0.3f);
        for (int i = 0; i < 3; i++)
        {
            GameObject viejo = GameObject.Find("Carton_" + i);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo);
            }
        }
        for (int i = 0; i < 3; i++)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Carton_" + i;
            box.transform.position = exit + new Vector3(i * 1.1f - 1.1f, 0.6f + (i == 2 ? 1.1f : 0f), (i % 2) * 0.6f);
            box.transform.localScale = new Vector3(1f, 1.1f, 1.2f);
            box.transform.rotation = Quaternion.Euler(0f, i * 15f, 0f);
            box.GetComponent<MeshRenderer>().sharedMaterial = carton;
        }
        return exit;
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
            return firstSlot;
        }
        for (int i = 0; i < count; i++)
        {
            int slot = firstSlot + i;
            GameObject go = new GameObject("AgentM2B_" + slot);
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
            GameObject modelSrc = FindFbxChild(data.modelFbxPath, data.modelRootName);
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
                Material ringMat = MaterialFactory.New();
                ringMat.color = data.ringColor;
                ringMat.EnableKeyword("_EMISSION");
                ringMat.SetColor("_EmissionColor", data.ringColor * 0.6f);
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.transform.SetParent(view.transform, false);
                ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                ring.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
                ring.GetComponent<MeshRenderer>().sharedMaterial = ringMat;
                Object.DestroyImmediate(ring.GetComponent<CapsuleCollider>());
            }
            brain.Setup(data, pool, slot, phrases, covers, retreat);
            pool.Register(brain);
        }
        return firstSlot + count;
    }

    private static void Rebake()
    {
        GameObject bakeGo = GameObject.Find("NavMeshBake");
        if (bakeGo == null)
        {
            return;
        }
        NavMeshSurface[] surfaces = bakeGo.GetComponentsInChildren<NavMeshSurface>();
        for (int i = 0; i < surfaces.Length; i++)
        {
            surfaces[i].BuildNavMesh();
        }
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Debug.Log("Fase5B NavMesh: rebake verts=" + tri.vertices.Length + ".");
    }
}
