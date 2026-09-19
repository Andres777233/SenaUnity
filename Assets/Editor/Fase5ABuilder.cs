using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using Popayork.Player;
using Popayork.UI;
using Popayork.Vehicles;

public static class Fase5ABuilder
{
    private const string ScenePath = "Assets/Scenes/Mision2.unity";
    private const string CityFbx = "Assets/MapaPopayan/source/model.fbx";
    private const string HorseMeshTxt = "Assets/Models3D/Caballo low poly/source/HorseMesh.txt";
    private const string PersonasFbx = "Assets/Models3D/personas modelos1/source/temp_export.fbx";

    [MenuItem("Popayork/Construir Fase 5A")]
    public static void BuildAll()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureBoot();
        EnsureEventSystem();
        MigrateHostileFlags();

        GameObject ciudad = BuildCityBase();
        Vector3 parkStart = ReadParkStart();
        Vector3 morroTop = FindMorroTop(ciudad, parkStart);
        Vector3[] route = BuildRoute(parkStart, morroTop);
        BuildRoad(ciudad, route);
        BuildCollisionTiles(ciudad, route);
        BuildMorroMonument(morroTop);
        BuildCamp(route[0]);

        HorseData horseData = BuildHorseData(route);
        GameObject horse = BuildHorse(horseData, route, route[0]);
        BuildPlayer(route[0], horse.transform.position);

        NpcPhrases phrases = AssetDatabase.LoadAssetAtPath<NpcPhrases>("Assets/Config/NpcPhrases.asset");
        AgentData[] datas = LoadAgentDatas();
        Vector3[] covers = BuildCoverMarkers(route);
        Vector3 retreat = route[0];
        AgentPool pool = GetOrCreate<AgentPool>("AgentPool");
        pool.Clear();
        GameObject agentsRoot = new GameObject("Agents");
        int slot = 0;
        slot = BuildSlots(agentsRoot.transform, pool, datas[0], phrases, covers, retreat, slot, 8);
        slot = BuildSlots(agentsRoot.transform, pool, datas[1], phrases, covers, retreat, slot, 8);
        slot = BuildSlots(agentsRoot.transform, pool, datas[2], phrases, covers, retreat, slot, 4);
        slot = BuildSlots(agentsRoot.transform, pool, datas[3], phrases, covers, retreat, slot, 6);
        slot = BuildSlots(agentsRoot.transform, pool, datas[4], phrases, covers, retreat, slot, 4);

        AITickScheduler scheduler = GetOrCreate<AITickScheduler>("AIScheduler");
        scheduler.Bind(pool);

        Mission2Config config = BuildMission2Config(route, morroTop);
        CreateMarker("CP2", route[3]);
        CreateMarker("CP2_SpawnA", route[3] + new Vector3(-15f, 0.5f, 8f));
        CreateMarker("CP2_SpawnB", route[3] + new Vector3(15f, 0.5f, 8f));
        WaveManager waves = GetOrCreate<WaveManager>("WaveManager");
        Transform[] fightSpawns = new Transform[] { FindMarker("CP2_SpawnA"), FindMarker("CP2_SpawnB") };
        waves.Bind(pool, fightSpawns, FindMarker("CP2"));

        MissionUI missionUI = BuildMissionUI();
        CompassUI compass = BuildCompass(morroTop);
        Mission2Controller controller = GetOrCreate<Mission2Controller>("Mission2");
        controller.Bind(config, waves, pool, missionUI, compass);
        BuildSubtitles();
        BuildSky();
        BuildBoundaries(route);
        AddPauseMenu();
        AppendBuildSettings();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Rebake(route[0], route[3], morroTop);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Fase 5A: PASS — ruta al Morro con caballo y 4 checkpoints.");
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
        if (go == null)
        {
            go = new GameObject(name);
        }
        return go.transform;
    }

    private static void EnsureBoot()
    {
        GameObject boot = GameObject.Find("Boot");
        if (boot == null)
        {
            boot = new GameObject("Boot");
        }
        if (boot.GetComponent<GameManager>() == null)
        {
            boot.AddComponent<GameManager>();
        }
        if (boot.GetComponent<SceneLoader>() == null)
        {
            boot.AddComponent<SceneLoader>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    // ---------- Datos ----------

    private static void MigrateHostileFlags()
    {
        SetHostile("Assets/Config/Agents/Policia_Patrullero.asset", true);
        SetHostile("Assets/Config/Agents/Policia_Caminante.asset", true);
        SetHostile("Assets/Config/Agents/Aliado_SENA.asset", false);
        SetHostile("Assets/Config/Agents/Aliado_Uni.asset", false);
        string rivalPath = "Assets/Config/Agents/Uni_Rival.asset";
        AgentData rival = AssetDatabase.LoadAssetAtPath<AgentData>(rivalPath);
        if (rival == null)
        {
            rival = ScriptableObject.CreateInstance<AgentData>();
            AssetDatabase.CreateAsset(rival, rivalPath);
        }
        rival.displayName = "Universitario rival";
        rival.faction = Faction.University;
        rival.hostile = true;
        rival.modelFbxPath = PersonasFbx;
        rival.modelRootName = "Woman02";
        rival.maxHealth = 90f;
        rival.moveSpeed = 4.2f;
        rival.targetHeightMeters = 1.75f;
        rival.ringColor = new Color(0.6f, 0.1f, 0.1f);
        rival.attackRange = 2.4f;
        rival.attackDamage = 12f;
        rival.attackCooldown = 1.1f;
        rival.retreatHealthFraction = 0.2f;
        rival.repathInterval = 1f;
        rival.sightRange = 25f;
        EditorUtility.SetDirty(rival);
    }

    private static void SetHostile(string path, bool value)
    {
        AgentData data = AssetDatabase.LoadAssetAtPath<AgentData>(path);
        if (data != null)
        {
            data.hostile = value;
            EditorUtility.SetDirty(data);
        }
    }

    private static AgentData[] LoadAgentDatas()
    {
        return new AgentData[] {
            AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Policia_Patrullero.asset"),
            AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Policia_Caminante.asset"),
            AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Aliado_SENA.asset"),
            AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Uni_Rival.asset"),
            AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Aliado_Uni.asset") };
    }

    private static HorseData BuildHorseData(Vector3[] route)
    {
        const string path = "Assets/Config/HorseData.asset";
        HorseData data = AssetDatabase.LoadAssetAtPath<HorseData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<HorseData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.trotSpeed = 6f;
        data.gallopSpeed = 11f;
        data.turnSpeed = 120f;
        data.gravity = 22f;
        data.mountRange = 3.5f;
        data.saddleHeight = 1.55f;
        data.saddleForward = 0.1f;
        data.targetLengthMeters = 2.2f;
        data.corridorHalfWidth = 12f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static WaveData BuildFightWave()
    {
        const string path = "Assets/Config/Waves/M2_Emboscada.asset";
        WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        if (wave == null)
        {
            wave = ScriptableObject.CreateInstance<WaveData>();
            AssetDatabase.CreateAsset(wave, path);
        }
        wave.waveName = "M2 Emboscada";
        wave.entries = new WaveEntry[] {
            new WaveEntry { faction = Faction.Police, count = 3 },
            new WaveEntry { faction = Faction.University, count = 2 } };
        wave.spawnInterval = 0.8f;
        EditorUtility.SetDirty(wave);
        return wave;
    }

    private static WaveData BuildEscortWave()
    {
        const string path = "Assets/Config/Waves/M2_Escolta.asset";
        WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        if (wave == null)
        {
            wave = ScriptableObject.CreateInstance<WaveData>();
            AssetDatabase.CreateAsset(wave, path);
        }
        wave.waveName = "M2 Escolta";
        wave.entries = new WaveEntry[] { new WaveEntry { faction = Faction.Sena, count = 2 } };
        wave.spawnInterval = 0.5f;
        EditorUtility.SetDirty(wave);
        return wave;
    }

    private static Mission2Config BuildMission2Config(Vector3[] route, Vector3 morroTop)
    {
        const string path = "Assets/Config/Mission2Config.asset";
        Mission2Config config = AssetDatabase.LoadAssetAtPath<Mission2Config>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<Mission2Config>();
            AssetDatabase.CreateAsset(config, path);
        }
        WaveData fight = BuildFightWave();
        BuildEscortWave();
        CheckpointDef[] cps = new CheckpointDef[4];
        cps[0] = NewCp("Portón del sur", CheckpointKind.Ride, route[2], 10f, null);
        cps[1] = NewCp("Emboscada en el cruce", CheckpointKind.Fight, route[3], 14f, fight);
        cps[2] = NewCp("Base del Morro", CheckpointKind.Ride, route[4], 10f, null);
        cps[3] = NewCp("Cima del Morro", CheckpointKind.Arrival, morroTop, 8f, null);
        config.routePoints = route;
        config.checkpoints = cps;
        config.corridorHalfWidth = 12f;
        config.morroTop = morroTop;
        EditorUtility.SetDirty(config);
        return config;
    }

    private static CheckpointDef NewCp(string nombre, CheckpointKind kind, Vector3 pos, float radius, WaveData wave)
    {
        CheckpointDef cp = new CheckpointDef();
        cp.nombre = nombre;
        cp.kind = kind;
        cp.position = pos;
        cp.radius = radius;
        cp.fightWave = wave;
        return cp;
    }

    // ---------- Terreno y ruta ----------

    private static GameObject BuildCityBase()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CityFbx);
        GameObject ciudad = Object.Instantiate(fbx);
        ciudad.name = "CiudadBase";
        return ciudad;
    }

    private static Vector3 ReadParkStart()
    {
        ParqueConfig parque = AssetDatabase.LoadAssetAtPath<ParqueConfig>("Assets/Config/ParqueConfig.asset");
        if (parque != null)
        {
            return parque.spawnJugador;
        }
        return Vector3.zero;
    }

    private static float GroundAt(Vector3 p)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(p.x, 600f, p.z), Vector3.down, out hit, 2000f))
        {
            return hit.point.y;
        }
        return p.y;
    }

    // El Morro = punto más alto suavizado fuera del parque (medido por script).
    private static Vector3 FindMorroTop(GameObject ciudad, Vector3 park)
    {
        MeshCollider probe = ciudad.AddComponent<MeshCollider>();
        float best = -10000f;
        Vector3 bestPos = park + new Vector3(0f, 0f, -300f);
        float[,] grid = new float[31, 31];
        for (int gx = 0; gx < 31; gx++)
        {
            for (int gz = 0; gz < 31; gz++)
            {
                Vector3 p = park + new Vector3((gx - 15) * 40f, 0f, (gz - 15) * 40f);
                grid[gx, gz] = GroundAt(p);
            }
        }
        for (int gx = 1; gx < 30; gx++)
        {
            for (int gz = 1; gz < 30; gz++)
            {
                Vector3 center = park + new Vector3((gx - 15) * 40f, 0f, (gz - 15) * 40f);
                if ((center - park).sqrMagnitude < 150f * 150f)
                {
                    continue;
                }
                float avg = 0f;
                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oz = -1; oz <= 1; oz++)
                    {
                        avg += grid[gx + ox, gz + oz];
                    }
                }
                avg /= 9f;
                if (avg > best)
                {
                    best = avg;
                    bestPos = new Vector3(center.x, grid[gx, gz], center.z);
                }
            }
        }
        Object.DestroyImmediate(probe);
        Debug.Log("Fase5A Morro: cima medida en " + bestPos.ToString("F1") + ".");
        return bestPos;
    }

    private static Vector3[] BuildRoute(Vector3 park, Vector3 morro)
    {
        Vector3 dir = morro - park;
        dir.y = 0f;
        float dist = dir.magnitude;
        dir /= Mathf.Max(1f, dist);
        Vector3 side = new Vector3(-dir.z, 0f, dir.x);
        Vector3[] route = new Vector3[6];
        route[0] = park;
        route[1] = park + dir * dist * 0.2f + side * 40f;
        route[2] = park + dir * dist * 0.4f + side * -30f;
        route[3] = park + dir * dist * 0.6f + side * 30f;
        route[4] = morro - dir * 60f;
        route[5] = morro;
        for (int i = 0; i < route.Length; i++)
        {
            route[i] = new Vector3(route[i].x, GroundAt(route[i]) + 0.3f, route[i].z);
        }
        Debug.Log("Fase5A ruta: " + dist.ToString("F0") + "m del parque al Morro.");
        return route;
    }

    private static Material Mat(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }

    private static void BuildRoad(GameObject ciudad, Vector3[] route)
    {
        MeshCollider probe = ciudad.AddComponent<MeshCollider>();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        for (int i = 0; i < route.Length - 1; i++)
        {
            Vector3 a = route[i];
            Vector3 b = route[i + 1];
            Vector3 dir = b - a;
            dir.y = 0f;
            dir.Normalize();
            Vector3 side = new Vector3(-dir.z, 0f, dir.x) * 3f;
            int baseIndex = verts.Count;
            verts.Add(a - side + Vector3.up * 0.25f);
            verts.Add(a + side + Vector3.up * 0.25f);
            verts.Add(b - side + Vector3.up * 0.25f);
            verts.Add(b + side + Vector3.up * 0.25f);
            tris.Add(baseIndex); tris.Add(baseIndex + 1); tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 1); tris.Add(baseIndex + 3); tris.Add(baseIndex + 2);
            // Barandas bajas a los lados (colisión del corredor).
            BuildRail(a - side * 2.2f, b - side * 2.2f);
            BuildRail(a + side * 2.2f, b + side * 2.2f);
        }
        Object.DestroyImmediate(probe);
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        GameObject road = new GameObject("RutaMorro");
        MeshFilter mf = road.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = road.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Mat(new Color(0.55f, 0.45f, 0.33f));
        MeshCollider col = road.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;
    }

    private static void BuildRail(Vector3 a, Vector3 b)
    {
        Vector3 mid = (a + b) * 0.5f;
        float len = (b - a).magnitude;
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "BordeRuta";
        rail.transform.position = new Vector3(mid.x, mid.y + 0.5f, mid.z);
        rail.transform.localScale = new Vector3(0.6f, 1f, len);
        rail.transform.rotation = Quaternion.LookRotation(b - a);
        rail.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.7f, 0.68f, 0.64f));
    }

    // Suelo con colisión bajo el corredor (la malla visual no tiene collider).
    private static void BuildCollisionTiles(GameObject ciudad, Vector3[] route)
    {
        MeshCollider probe = ciudad.AddComponent<MeshCollider>();
        Bounds bounds = new Bounds(route[0], Vector3.zero);
        for (int i = 1; i < route.Length; i++)
        {
            bounds.Encapsulate(route[i]);
        }
        GameObject tiles = new GameObject("SueloColision");
        int n = 0;
        for (float x = bounds.min.x - 20f; x <= bounds.max.x + 20f; x += 30f)
        {
            for (float z = bounds.min.z - 20f; z <= bounds.max.z + 20f; z += 30f)
            {
                Vector3 p = new Vector3(x, 0f, z);
                if (DistanceToRoute(p, route) > 25f)
                {
                    continue;
                }
                RaycastHit hit;
                if (!Physics.Raycast(new Vector3(x, 600f, z), Vector3.down, out hit, 2000f))
                {
                    continue;
                }
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "Tile_" + n;
                tile.transform.SetParent(tiles.transform, false);
                tile.transform.position = new Vector3(x, hit.point.y - 0.55f, z);
                tile.transform.localScale = new Vector3(30f, 1f, 30f);
                tile.GetComponent<MeshRenderer>().enabled = false;
                n++;
            }
        }
        Object.DestroyImmediate(probe);
        Debug.Log("Fase5A suelo: " + n + " baldosas de colisión.");
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

    private static void BuildMorroMonument(Vector3 top)
    {
        GameObject morro = new GameObject("MorroTulcan");
        morro.transform.position = top;
        GameObject pira = new GameObject("Piramide");
        pira.transform.SetParent(morro.transform, false);
        pira.transform.localPosition = new Vector3(0f, 0f, 0f);
        MeshFilter mf = pira.AddComponent<MeshFilter>();
        mf.sharedMesh = BuildPyramidMesh(6f, 9f);
        MeshRenderer mr = pira.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Mat(new Color(0.6f, 0.58f, 0.54f));
        MeshCollider col = pira.AddComponent<MeshCollider>();
        col.sharedMesh = mf.sharedMesh;
        GameObject asta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        asta.name = "Asta";
        asta.transform.SetParent(morro.transform, false);
        asta.transform.localPosition = new Vector3(0f, 12f, 0f);
        asta.transform.localScale = new Vector3(0.3f, 6f, 0.3f);
        GameObject bandera = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bandera.name = "Bandera";
        bandera.transform.SetParent(morro.transform, false);
        bandera.transform.localPosition = new Vector3(1.1f, 14f, 0f);
        bandera.transform.localScale = new Vector3(2f, 1.2f, 0.1f);
        bandera.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(1f, 0.45f, 0f));
        Object.DestroyImmediate(bandera.GetComponent<BoxCollider>());
    }

    private static Mesh BuildPyramidMesh(float half, float height)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half),
            new Vector3(half, 0f, half), new Vector3(-half, 0f, half),
            new Vector3(0f, height, 0f) };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3, 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0 };
        mesh.RecalculateNormals();
        return mesh;
    }

    private static void BuildCamp(Vector3 start)
    {
        GameObject camp = new GameObject("Campamento");
        camp.transform.position = start;
        Material lona = Mat(new Color(0.5f, 0.42f, 0.3f));
        for (int i = 0; i < 2; i++)
        {
            GameObject carpa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carpa.name = "Carpa_" + i;
            carpa.transform.SetParent(camp.transform, false);
            carpa.transform.localPosition = new Vector3(-6f + i * 12f, 1.2f, -6f);
            carpa.transform.localScale = new Vector3(3f, 2.4f, 3f);
            carpa.GetComponent<MeshRenderer>().sharedMaterial = lona;
        }
    }

    // ---------- Caballo ----------

    private static Mesh BuildHorseMesh(HorseData data)
    {
        string path = Application.dataPath + "/Models3D/Caballo low poly/source/HorseMesh.txt";
        string[] lines = File.ReadAllLines(path);
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int[]> subs = new List<int[]>();
        List<Color> colors = new List<Color>();
        int i = 0;
        List<Vector3> current = null;
        List<int> currentTris = null;
        while (i < lines.Length)
        {
            string[] parts = lines[i].Split(' ');
            if (parts[0] == "VERTS" || parts[0] == "NORMALS")
            {
                int n = int.Parse(parts[1]);
                current = parts[0] == "VERTS" ? verts : normals;
                for (int k = 0; k < n; k++)
                {
                    i++;
                    string[] v = lines[i].Split(' ');
                    current.Add(new Vector3(
                        float.Parse(v[0], CultureInfo.InvariantCulture),
                        float.Parse(v[1], CultureInfo.InvariantCulture),
                        float.Parse(v[2], CultureInfo.InvariantCulture)));
                }
            }
            else if (parts[0] == "SUBS")
            {
                i++;
            }
            else if (parts[0] == "TRIS")
            {
                int n = int.Parse(parts[1]);
                currentTris = new List<int>();
                for (int k = 0; k < n; k++)
                {
                    i++;
                    string[] t = lines[i].Split(' ');
                    currentTris.Add(int.Parse(t[0]));
                    currentTris.Add(int.Parse(t[1]));
                    currentTris.Add(int.Parse(t[2]));
                }
                subs.Add(currentTris.ToArray());
            }
            else if (parts[0] == "MAT")
            {
                colors.Add(new Color(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)));
            }
            i++;
        }
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.normals = normals.ToArray();
        mesh.subMeshCount = subs.Count;
        for (int s = 0; s < subs.Count; s++)
        {
            mesh.SetTriangles(subs[s], s);
        }
        float longest = 0.1f;
        Bounds b = mesh.bounds;
        longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        float scale = data.targetLengthMeters / longest;
        Debug.Log("Fase5A caballo: medido=" + longest.ToString("F2") + "m escala=" + scale.ToString("F3") + " (" + verts.Count + " verts).");
        return mesh;
    }

    private static GameObject BuildHorse(HorseData data, Vector3[] route, Vector3 start)
    {
        Mesh mesh = BuildHorseMesh(data);
        GameObject horse = new GameObject("Caballo");
        horse.transform.position = new Vector3(start.x + 4f, start.y + 0.5f, start.z);
        MeshFilter mf = horse.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = horse.AddComponent<MeshRenderer>();
        Material[] mats = new Material[3];
        Color[] fallback = new Color[] { new Color(0.84f, 0.44f, 0.25f), new Color(0.17f, 0.08f, 0.05f), Color.white };
        for (int i = 0; i < 3; i++)
        {
            mats[i] = Mat(fallback[i]);
        }
        mr.sharedMaterials = mats;
        CharacterController cc = horse.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 1.1f, 0f);
        HorseController ctrl = horse.AddComponent<HorseController>();
        ctrl.Setup(data, route);
        // Escala por código al tamaño objetivo.
        Bounds b = mesh.bounds;
        float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        horse.transform.localScale = Vector3.one * (data.targetLengthMeters / Mathf.Max(0.1f, longest));
        PrefabUtility.SaveAsPrefabAsset(horse, "Assets/Prefabs/Caballo.prefab");
        return horse;
    }

    // ---------- Actores ----------

    private static void BuildPlayer(Vector3 start, Vector3 horsePos)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        GameObject player = GameObject.Find("Player");
        if (player == null && prefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player";
        }
        if (player != null)
        {
            player.transform.position = start;
            player.transform.rotation = Quaternion.identity;
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.SetSpawn(start, 0f);
            }
        }
        GameObject pools = GameObject.Find("CombatPools");
        if (pools == null)
        {
            pools = new GameObject("CombatPools");
            pools.AddComponent<Popayork.Weapons.ProjectilePool>();
            pools.AddComponent<Popayork.Weapons.ImpactPool>();
        }
    }

    private static Vector3[] BuildCoverMarkers(Vector3[] route)
    {
        Vector3 mid = route[3];
        Vector3[] pos = new Vector3[] {
            mid + new Vector3(-8f, 0.5f, 4f), mid + new Vector3(8f, 0.5f, 4f),
            mid + new Vector3(-8f, 0.5f, -6f), mid + new Vector3(8f, 0.5f, -6f) };
        for (int i = 0; i < pos.Length; i++)
        {
            GameObject c = new GameObject("CoverM2_" + i);
            c.transform.position = pos[i];
        }
        return pos;
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
            GameObject go = new GameObject("AgentM2_" + data.faction + "_" + slot);
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
            }
            brain.Setup(data, pool, slot, phrases, covers, retreat);
            pool.Register(brain);
        }
        return firstSlot + count;
    }

    // ---------- UI y ambiente ----------

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
        GameObject canvasGo = new GameObject("MissionCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        MissionUI ui = canvasGo.AddComponent<MissionUI>();

        GameObject intro = Panel(canvasGo.transform, "IntroPanel", new Vector2(680f, 520f));
        Text title = Label(intro.transform, "IntroTitle", "RUMBO AL MORRO", 48);
        Text body = Label(intro.transform, "IntroBody",
            "Ganamos la Torre, pero los universitarios nos dieron la espalda:\ndicen que vendimos el paro por un convenio.\n\nAhora toca subir al Morro de Tulcán. Los caballos esperan.\n\nWASD + Shift (galope) a caballo · E montar/desmontar\nClick disparar · R recargar · P pausa\n\nSigue la flecha naranja hacia el Morro.",
            20);
        Button goBtn = Btn(intro.transform, "BtnVamos", "¡Al Morro!");
        Mission2Controller ctrl = GetOrCreate<Mission2Controller>("Mission2");
        UnityEventTools.AddVoidPersistentListener(goBtn.onClick, ctrl.StartRoute);

        GameObject objBar = Panel(canvasGo.transform, "ObjectiveBar", new Vector2(560f, 70f));
        RectTransform objRt = objBar.GetComponent<RectTransform>();
        objRt.anchorMin = new Vector2(0.5f, 1f);
        objRt.anchorMax = new Vector2(0.5f, 1f);
        objRt.anchoredPosition = new Vector2(0f, -45f);
        Text objText = Label(objBar.transform, "ObjectiveText", "A caballo hasta el Morro de Tulcán", 22);
        Image fill = Bar(objBar.transform, "ProgressFill", new Color(1f, 0.55f, 0.1f));

        GameObject result = Panel(canvasGo.transform, "ResultPanel", new Vector2(640f, 400f));
        Text resultText = Label(result.transform, "ResultText", "", 32);
        Button retryBtn = Btn(result.transform, "BtnReintentar", "Reintentar");
        Button menuBtn = Btn(result.transform, "BtnMenu", "Volver al menú");
        UnityEventTools.AddVoidPersistentListener(retryBtn.onClick, ctrl.StartRoute);
        UnityEventTools.AddVoidPersistentListener(menuBtn.onClick, ctrl.ToMenu);
        result.SetActive(false);

        ui.Bind(intro, title, body, objBar, objText, fill, result, resultText);
        ui.ShowIntro();
        return ui;
    }

    private static GameObject Panel(Transform parent, string name, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        return go;
    }

    private static Text Label(Transform parent, string name, string content, int size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text label = go.AddComponent<Text>();
        label.font = DefaultFont();
        label.text = content;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.95f, 0.7f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.05f);
        rt.anchorMax = new Vector2(0.95f, 0.95f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return label;
    }

    private static Button Btn(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.6f, 0.35f, 0.1f, 0.95f);
        Button b = go.AddComponent<Button>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.12f);
        rt.anchorMax = new Vector2(0.5f, 0.12f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(340f, 56f);
        Label(go.transform, "Label", label, 22);
        return b;
    }

    private static Image Bar(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.08f);
        rt.anchorMax = new Vector2(0.95f, 0.3f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return img;
    }

    private static CompassUI BuildCompass(Vector3 morroTop)
    {
        GameObject go = new GameObject("Compass");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;
        GameObject arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(go.transform, false);
        Image arrowImg = arrowGo.AddComponent<Image>();
        arrowImg.color = new Color(1f, 0.55f, 0.1f);
        RectTransform rt = arrowGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.85f);
        rt.anchorMax = new Vector2(0.5f, 0.85f);
        rt.sizeDelta = new Vector2(20f, 44f);
        arrowImg.type = Image.Type.Filled;
        arrowImg.fillMethod = Image.FillMethod.Vertical;
        GameObject labelGo = new GameObject("Distance");
        labelGo.transform.SetParent(go.transform, false);
        Text label = labelGo.AddComponent<Text>();
        label.font = DefaultFont();
        label.fontSize = 20;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.8f, 0.4f);
        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 0.78f);
        lrt.anchorMax = new Vector2(0.5f, 0.78f);
        lrt.sizeDelta = new Vector2(300f, 30f);
        CompassUI compass = go.AddComponent<CompassUI>();
        compass.Bind(rt, label, morroTop);
        return compass;
    }

    private static void BuildSubtitles()
    {
        GameObject canvasGo = new GameObject("SubtitleCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        SubtitleSystem subtitles = canvasGo.AddComponent<SubtitleSystem>();
        GameObject go = new GameObject("SubtitleText");
        go.transform.SetParent(canvasGo.transform, false);
        Text label = go.AddComponent<Text>();
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
        subtitles.Bind(label);
    }

    private static void BuildSky()
    {
        GameObject sun = new GameObject("SolAtardecer");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.75f, 0.55f);
        light.intensity = 1.1f;
        sun.transform.rotation = Quaternion.Euler(18f, -115f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.55f, 0.52f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.96f, 0.72f, 0.55f);
        RenderSettings.fogDensity = 0.004f;
    }

    private static void BuildBoundaries(Vector3[] route)
    {
        Bounds bounds = new Bounds(route[0], Vector3.zero);
        for (int i = 1; i < route.Length; i++)
        {
            bounds.Encapsulate(route[i]);
        }
        Vector3 c = bounds.center;
        float hx = bounds.size.x * 0.5f + 30f;
        float hz = bounds.size.z * 0.5f + 30f;
        float y = bounds.min.y;
        MakeWall("Limite_N", new Vector3(c.x, y + 8f, c.z - hz), new Vector3(hx * 2f, 30f, 2f));
        MakeWall("Limite_S", new Vector3(c.x, y + 8f, c.z + hz), new Vector3(hx * 2f, 30f, 2f));
        MakeWall("Limite_E", new Vector3(c.x + hx, y + 8f, c.z), new Vector3(2f, 30f, hz * 2f));
        MakeWall("Limite_W", new Vector3(c.x - hx, y + 8f, c.z), new Vector3(2f, 30f, hz * 2f));
    }

    private static void MakeWall(string name, Vector3 pos, Vector3 size)
    {
        GameObject w = new GameObject(name);
        w.transform.position = pos;
        BoxCollider box = w.AddComponent<BoxCollider>();
        box.size = size;
    }

    private static void AddPauseMenu()
    {
        GameObject canvasGo = GameObject.Find("PauseCanvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("PauseCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        PauseMenuUI ui = canvasGo.GetComponent<PauseMenuUI>();
        if (ui == null)
        {
            ui = canvasGo.AddComponent<PauseMenuUI>();
        }
        GameObject panel = canvasGo.transform.Find("PausePanel") != null
            ? canvasGo.transform.Find("PausePanel").gameObject : new GameObject("PausePanel");
        panel.transform.SetParent(canvasGo.transform, false);
        Image img = panel.GetComponent<Image>();
        if (img == null)
        {
            img = panel.AddComponent<Image>();
        }
        img.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = panel.AddComponent<RectTransform>();
        }
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 420f);
        ui.Bind(panel, null, null, null);
        panel.SetActive(false);
    }

    private static void AppendBuildSettings()
    {
        const string path = "Assets/Scenes/Mision2.unity";
        var current = EditorBuildSettings.scenes;
        foreach (var s in current)
        {
            if (s.path == path)
            {
                return;
            }
        }
        var next = new EditorBuildSettingsScene[current.Length + 1];
        current.CopyTo(next, 0);
        next[current.Length] = new EditorBuildSettingsScene(path, true);
        EditorBuildSettings.scenes = next;
    }

    private static void Rebake(Vector3 camp, Vector3 fight, Vector3 morro)
    {
        GameObject bakeGo = GameObject.Find("NavMeshBake");
        if (bakeGo == null)
        {
            bakeGo = new GameObject("NavMeshBake");
        }
        // 3 volúmenes (el bake de ruta completa es muy pesado).
        BakeVolume(bakeGo, "NavCamp", camp, 70f);
        BakeVolume(bakeGo, "NavFight", fight, 90f);
        BakeVolume(bakeGo, "NavMorro", morro, 80f);
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Debug.Log("Fase5A NavMesh: rebake verts=" + tri.vertices.Length + ".");
    }

    private static void CreateMarker(string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        go.transform.position = pos;
    }

    private static void BakeVolume(GameObject parent, string name, Vector3 center, float size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        NavMeshSurface surface = go.AddComponent<NavMeshSurface>();
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.center = center;
        surface.size = new Vector3(size, 14f, size);
        surface.defaultArea = 0;
        surface.BuildNavMesh();
    }
}
