using System.Collections.Generic;
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
using Popayork.World;

public static class Fase6Builder
{
    private const string ScenePath = "Assets/Scenes/Mision3.unity";

    [MenuItem("Popayork/Construir Fase 6")]
    public static void BuildAll()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureBoot();
        EnsureEventSystem();

        Vector3[] route = BuildTrack(out _, out _, out Vector3 riverCenter);
        BuildRiver(riverCenter);
        BuildForest(route);
        BuildPlayer(route[0]);
        CardboardData cardboardData = BuildCardboardData();
        BuildCarton(route[0], cardboardData, route);
        BuildGates(route, riverCenter, out CheckpointDef[] gates);
        Mission3Config config = BuildMission3Config(route, gates, riverCenter);
        MissionUI missionUI = BuildMissionUI();
        Mission3Controller controller = GetOrCreate<Mission3Controller>("Mission3");
        controller.Bind(config, missionUI);
        BuildSubtitles();
        BuildEnemies(route[0]);
        BuildSky();
        BuildBoundaries(route, riverCenter);
        AddPauseMenu();
        UiLayout.FixSceneUI();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Rebake(route[0]);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Fase 6: PASS — descenso en cartón hasta el río.");
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

    private static Material Mat(Color color)
    {
        Material mat = MaterialFactory.New();
        mat.color = color;
        return mat;
    }

    // ---------- Pista ----------

    private static Vector3[] BuildTrack(out List<Vector3> rampSpots, out List<Vector3> rockSpots, out Vector3 riverCenter)
    {
        rampSpots = new List<Vector3>();
        rockSpots = new List<Vector3>();
        Vector3[] keys = new Vector3[14];
        for (int i = 0; i < 14; i++)
        {
            float z = -i * 46f;
            float x = Mathf.Sin(i * 0.9f) * 45f;
            float y = 90f - i * 3.5f - Mathf.Pow(i, 1.6f) * 0.6f;
            keys[i] = new Vector3(x, y, z);
        }
        List<Vector3> route = new List<Vector3>();
        for (int i = 0; i < keys.Length - 1; i++)
        {
            float segLen = (keys[i + 1] - keys[i]).magnitude;
            int steps = Mathf.Max(2, Mathf.RoundToInt(segLen / 8f));
            for (int s = 0; s < steps; s++)
            {
                route.Add(Vector3.Lerp(keys[i], keys[i + 1], s / (float)steps));
            }
        }
        route.Add(keys[keys.Length - 1]);
        Vector3[] pts = route.ToArray();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector3 a = pts[i];
            Vector3 b = pts[i + 1];
            Vector3 dir = b - a;
            dir.y = 0f;
            dir.Normalize();
            Vector3 side = new Vector3(-dir.z, 0f, dir.x) * 7f;
            int bi = verts.Count;
            verts.Add(a - side + Vector3.up * 0.2f);
            verts.Add(a + side + Vector3.up * 0.2f);
            verts.Add(b - side + Vector3.up * 0.2f);
            verts.Add(b + side + Vector3.up * 0.2f);
            tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2);
            tris.Add(bi + 1); tris.Add(bi + 3); tris.Add(bi + 2);
            BuildRail(a - side * 1.15f, b - side * 1.15f);
            BuildRail(a + side * 1.15f, b + side * 1.15f);
        }
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        GameObject navScope = GameObject.Find("NavMeshScope");
        if (navScope == null)
        {
            navScope = new GameObject("NavMeshScope");
        }
        GameObject track = new GameObject("PistaCarton");
        track.transform.SetParent(navScope.transform, true);
        MeshFilter mf = track.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = track.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Mat(new Color(0.5f, 0.42f, 0.3f));
        MeshCollider col = track.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;

        int[] rampIdx = new int[] { pts.Length / 3, pts.Length / 2, pts.Length * 2 / 3 };
        for (int i = 0; i < rampIdx.Length; i++)
        {
            Vector3 p = pts[rampIdx[i]];
            rampSpots.Add(p);
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Rampa_" + i;
            ramp.transform.position = p + Vector3.up * 0.8f;
            ramp.transform.localScale = new Vector3(5f, 0.6f, 6f);
            Vector3 dir = pts[Mathf.Min(rampIdx[i] + 2, pts.Length - 1)] - p;
            dir.y = 0f;
            ramp.transform.rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(-18f, 0f, 0f);
            ramp.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.6f, 0.4f, 0.2f));
            TrackObstacle ob = ramp.AddComponent<TrackObstacle>();
            ob.isRamp = true;
            ob.rampBoost = 7f;
            ob.radius = 3f;
        }
        int[] rockIdx = new int[] { 8, 15, 22, 30, 38, 46, 54, 62 };
        for (int i = 0; i < rockIdx.Length; i++)
        {
            int idx = Mathf.Min(rockIdx[i], pts.Length - 1);
            Vector3 p = pts[idx] + new Vector3((i % 2 == 0 ? 2.5f : -2.5f), 0.4f, 0f);
            rockSpots.Add(p);
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Roca_" + i;
            rock.transform.position = p;
            rock.transform.localScale = new Vector3(1.6f, 1.1f, 1.6f);
            rock.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.45f, 0.43f, 0.4f));
            Object.DestroyImmediate(rock.GetComponent<SphereCollider>());
            TrackObstacle ob = rock.AddComponent<TrackObstacle>();
            ob.radius = 1.6f;
            ob.speedKeep = 0.5f;
            ob.damage = 10f;
            ob.spinDegrees = 40f;
        }
        riverCenter = pts[pts.Length - 1] + new Vector3(0f, -1.5f, -45f);
        Debug.Log("Fase6 pista: " + pts.Length + " puntos, " + rampSpots.Count + " rampas, " + rockSpots.Count + " rocas.");
        return pts;
    }

    private static void BuildRail(Vector3 a, Vector3 b)
    {
        Vector3 mid = (a + b) * 0.5f;
        float len = (b - a).magnitude;
        if (len < 0.5f)
        {
            return;
        }
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "BordePista";
        GameObject navScope = GameObject.Find("NavMeshScope");
        rail.transform.SetParent(navScope != null ? navScope.transform : null, true);
        rail.transform.position = new Vector3(mid.x, mid.y + 0.6f, mid.z);
        rail.transform.localScale = new Vector3(0.5f, 1.2f, len);
        rail.transform.rotation = Quaternion.LookRotation(b - a);
        rail.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.55f, 0.5f, 0.45f));
    }

    private static void BuildRiver(Vector3 center)
    {
        GameObject water = new GameObject("Rio");
        water.transform.position = new Vector3(center.x, center.y, center.z);
        MeshFilter mf = water.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        float hx = 110f;
        float hz = 65f;
        mesh.vertices = new Vector3[] {
            new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz),
            new Vector3(-hx, 0f, hz), new Vector3(hx, 0f, hz) };
        mesh.triangles = new int[] { 0, 1, 2, 1, 3, 2 };
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;
        MeshRenderer mr = water.AddComponent<MeshRenderer>();
        Material waterMat = MaterialFactory.New();
        waterMat.color = new Color(0.15f, 0.45f, 0.75f, 0.8f);
        mr.sharedMaterial = waterMat;
        Transform[] foam = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            f.name = "Espuma_" + i;
            f.transform.SetParent(water.transform, false);
            f.transform.localPosition = new Vector3(-90f + i * 36f, 0.15f, -50f + (i % 3) * 40f);
            f.transform.localScale = new Vector3(6f, 0.1f, 1.5f);
            f.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(1f, 1f, 1f, 0.7f));
            Object.DestroyImmediate(f.GetComponent<BoxCollider>());
            foam[i] = f.transform;
        }
        WaterFX fx = water.AddComponent<WaterFX>();
        fx.Bind(foam, waterMat);
    }

    private static void BuildForest(Vector3[] route)
    {
        Material trunk = Mat(new Color(0.4f, 0.28f, 0.18f));
        Material leaf = Mat(new Color(0.18f, 0.45f, 0.22f));
        for (int i = 0; i < 24; i++)
        {
            int idx = (i * 7 + 3) % route.Length;
            Vector3 p = route[idx] + new Vector3((i % 2 == 0 ? 11f : -11f), 0f, 0f);
            GameObject tree = new GameObject("Pino_" + i);
            tree.transform.position = new Vector3(p.x, p.y, p.z);
            GameObject trunkGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunkGo.transform.SetParent(tree.transform, false);
            trunkGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            trunkGo.transform.localScale = new Vector3(0.4f, 2f, 0.4f);
            trunkGo.GetComponent<MeshRenderer>().sharedMaterial = trunk;
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.transform.SetParent(tree.transform, false);
            top.transform.localPosition = new Vector3(0f, 3.6f, 0f);
            top.transform.localScale = new Vector3(2f, 3f, 2f);
            top.GetComponent<MeshRenderer>().sharedMaterial = leaf;
            Object.DestroyImmediate(top.GetComponent<CapsuleCollider>());
        }
    }

    // ---------- Actores ----------

    private static void BuildPlayer(Vector3 start)
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
            player.transform.position = start + Vector3.up;
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.SetSpawn(start + Vector3.up, 0f);
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

    private static CardboardData BuildCardboardData()
    {
        const string path = "Assets/Config/CardboardData.asset";
        CardboardData data = AssetDatabase.LoadAssetAtPath<CardboardData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<CardboardData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.gravity = 22f;
        data.maxSpeed = 28f;
        data.drag = 0.12f;
        data.brakeDrag = 1.6f;
        data.turnRate = 90f;
        data.corridorHalfWidth = 9f;
        data.baseFov = 75f;
        data.maxFovBoost = 15f;
        data.seatHeight = 0.7f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void BuildCarton(Vector3 start, CardboardData data, Vector3[] route)
    {
        GameObject carton = GameObject.Find("Carton");
        if (carton == null)
        {
            carton = new GameObject("Carton");
        }
        carton.transform.position = start + Vector3.up * 0.5f;
        CharacterController cc = carton.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = carton.AddComponent<CharacterController>();
        }
        cc.height = 1.2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 0.6f, 0f);
        GameObject tabla = GameObject.Find("Carton_Tabla");
        if (tabla == null)
        {
            tabla = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tabla.name = "Carton_Tabla";
        }
        tabla.transform.SetParent(carton.transform, false);
        tabla.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        tabla.transform.localScale = new Vector3(1.2f, 0.15f, 1.8f);
        tabla.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.65f, 0.48f, 0.3f));
        Object.DestroyImmediate(tabla.GetComponent<BoxCollider>());
        CardboardController ctrl = carton.GetComponent<CardboardController>();
        if (ctrl == null)
        {
            ctrl = carton.AddComponent<CardboardController>();
        }
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("data").objectReferenceValue = data;
        SerializedProperty routeProp = so.FindProperty("routePoints");
        routeProp.arraySize = route.Length;
        for (int i = 0; i < route.Length; i++)
        {
            routeProp.GetArrayElementAtIndex(i).vector3Value = route[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        // Viento: dos estelas blancas junto a la cámara.
        GameObject player = GameObject.Find("Player");
        Camera cam = player != null ? player.GetComponentInChildren<Camera>() : null;
        if (cam != null)
        {
            MakeWind(cam.transform, "VientoL", new Vector3(-0.9f, 0f, 1f));
            MakeWind(cam.transform, "VientoR", new Vector3(0.9f, 0f, 1f));
        }
    }

    private static void MakeWind(Transform parent, string name, Vector3 localPos)
    {
        GameObject old = GameObject.Find(name);
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(0.03f, 0.03f, 2.5f);
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(1f, 1f, 1f, 0.35f));
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.SetActive(false);
    }

    private static void BuildGates(Vector3[] route, Vector3 riverCenter, out CheckpointDef[] gates)
    {
        gates = new CheckpointDef[3];
        int[] idx = new int[] { route.Length / 4, route.Length / 2, route.Length * 3 / 4 };
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = route[idx[i]];
            GameObject gate = new GameObject("Puerta_" + (i + 1));
            gate.transform.position = p;
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.transform.SetParent(gate.transform, false);
                pole.transform.localPosition = new Vector3(s * 7f, 2f, 0f);
                pole.transform.localScale = new Vector3(0.3f, 4f, 0.3f);
            }
            CheckpointDef cp = new CheckpointDef();
            cp.nombre = "Puerta " + (i + 1);
            cp.kind = CheckpointKind.Ride;
            cp.position = p;
            cp.radius = 9f;
            cp.fightWave = null;
            gates[i] = cp;
        }
    }

    private static Mission3Config BuildMission3Config(Vector3[] route, CheckpointDef[] gates, Vector3 riverCenter)
    {
        const string path = "Assets/Config/Mission3Config.asset";
        Mission3Config config = AssetDatabase.LoadAssetAtPath<Mission3Config>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<Mission3Config>();
            AssetDatabase.CreateAsset(config, path);
        }
        config.routePoints = route;
        config.gates = gates;
        config.riverCenter = riverCenter;
        config.riverRadius = 20f;
        EditorUtility.SetDirty(config);
        return config;
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
        GameObject viejo = GameObject.Find("MissionCanvas");
        if (viejo != null)
        {
            Object.DestroyImmediate(viejo);
        }
        GameObject canvasGo = new GameObject("MissionCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        MissionUI ui = canvasGo.AddComponent<MissionUI>();

        GameObject intro = Panel(canvasGo.transform, "IntroPanel", new Vector2(680f, 520f));
        Text title = Label(intro.transform, "IntroTitle", "RÍO ABAJO EN CARTÓN", 48);
        Text body = Label(intro.transform, "IntroBody",
            "Nos botamos del Morro en cartón, como en la universidad pública.\n\nA/D o flechas: dirección · S: frenar · P: pausa\nEsquiva las rocas, toma las rampas y no te salgas.\n\n¡Al río, parce!",
            20);
        Button goBtn = Btn(intro.transform, "BtnVamos", "¡Nos tiramos!");
        Mission3Controller ctrl = GetOrCreate<Mission3Controller>("Mission3");
        goBtn.onClick.RemoveAllListeners();
        UnityEventTools.AddVoidPersistentListener(goBtn.onClick, ctrl.StartDescent);

        GameObject objBar = Panel(canvasGo.transform, "ObjectiveBar", new Vector2(560f, 70f));
        RectTransform objRt = objBar.GetComponent<RectTransform>();
        objRt.anchorMin = new Vector2(0.5f, 1f);
        objRt.anchorMax = new Vector2(0.5f, 1f);
        objRt.anchoredPosition = new Vector2(0f, -45f);
        Text objText = Label(objBar.transform, "ObjectiveText", "¡Deslízate en cartón hasta el río!", 22);
        Image fill = Bar(objBar.transform, "ProgressFill", new Color(0.2f, 0.6f, 1f));

        GameObject result = Panel(canvasGo.transform, "ResultPanel", new Vector2(640f, 400f));
        Text resultText = Label(result.transform, "ResultText", "", 32);
        Button retryBtn = Btn(result.transform, "BtnReintentar", "Reintentar");
        Button menuBtn = Btn(result.transform, "BtnMenu", "Volver al menú");
        retryBtn.onClick.RemoveAllListeners();
        menuBtn.onClick.RemoveAllListeners();
        UnityEventTools.AddVoidPersistentListener(retryBtn.onClick, ctrl.Retry);
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
        img.color = new Color(0.15f, 0.35f, 0.6f, 0.95f);
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
        GameObject bg = new GameObject("ProgressBG");
        bg.transform.SetParent(parent, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.05f, 0.08f);
        bgRt.anchorMax = new Vector2(0.95f, 0.3f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
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

    private static void BuildSubtitles()
    {
        GameObject viejoSub = GameObject.Find("SubtitleCanvas");
        if (viejoSub != null)
        {
            Object.DestroyImmediate(viejoSub);
        }
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

    // ---------- Enemigos y ambiente ----------

    private static void BuildEnemies(Vector3 start)
    {
        AgentData patrol = AssetDatabase.LoadAssetAtPath<AgentData>("Assets/Config/Agents/Policia_Patrullero.asset");
        NpcPhrases phrases = AssetDatabase.LoadAssetAtPath<NpcPhrases>("Assets/Config/NpcPhrases.asset");
        AgentPool pool = GetOrCreate<AgentPool>("AgentPool");
        pool.Clear();
        GameObject viejoAgents = GameObject.Find("Agents");
        if (viejoAgents != null)
        {
            Object.DestroyImmediate(viejoAgents);
        }
        GameObject agentsRoot = new GameObject("Agents");
        Vector3[] covers = new Vector3[] { start + new Vector3(-8f, 0.5f, 6f), start + new Vector3(8f, 0.5f, 6f) };
        for (int i = 0; i < 3; i++)
        {
            GameObject go = new GameObject("AgentM3_" + i);
            go.transform.SetParent(agentsRoot.transform, false);
            go.transform.position = start + new Vector3(-6f + i * 6f, 0.5f, 8f);
            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.speed = patrol.moveSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = patrol.attackRange * 0.8f;
            agent.radius = 0.32f;
            agent.height = 1.7f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            go.AddComponent<AgentHealth>();
            go.AddComponent<AgentMovement>();
            AgentBrain brain = go.AddComponent<AgentBrain>();
            GameObject view = new GameObject("View");
            view.transform.SetParent(go.transform, false);
            GameObject modelSrc = FindFbxChild(patrol.modelFbxPath, patrol.modelRootName);
            if (modelSrc != null)
            {
                GameObject model = Object.Instantiate(modelSrc);
                model.transform.SetParent(view.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * 1.14f;
            }
            brain.Setup(patrol, pool, i, phrases, covers, start);
            pool.Register(brain);
        }
        AITickScheduler scheduler = GetOrCreate<AITickScheduler>("AIScheduler");
        scheduler.Bind(pool);
        WaveManager waves = GetOrCreate<WaveManager>("WaveManager");
        GameObject sp = new GameObject("M3_Spawn");
        sp.transform.position = start + new Vector3(0f, 0.5f, 10f);
        GameObject ob = new GameObject("M3_Objective");
        ob.transform.position = start;
        waves.Bind(pool, new Transform[] { sp.transform }, ob.transform);
        pool.Spawn(Faction.Police, sp.transform.position, start, "M3_Spawn");
        pool.Spawn(Faction.Police, sp.transform.position + new Vector3(3f, 0f, 0f), start, "M3_Spawn_E");
        pool.Spawn(Faction.Police, sp.transform.position + new Vector3(-3f, 0f, 0f), start, "M3_Spawn_O");
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

    private static void BuildBoundaries(Vector3[] route, Vector3 river)
    {
        Bounds bounds = new Bounds(route[0], Vector3.zero);
        for (int i = 1; i < route.Length; i++)
        {
            bounds.Encapsulate(route[i]);
        }
        bounds.Encapsulate(river);
        Vector3 c = bounds.center;
        float hx = bounds.size.x * 0.5f + 30f;
        float hz = bounds.size.z * 0.5f + 30f;
        float y = bounds.min.y;
        MakeWall("Limite_N", new Vector3(c.x, y + 30f, c.z - hz), new Vector3(hx * 2f, 80f, 2f));
        MakeWall("Limite_S", new Vector3(c.x, y + 30f, c.z + hz), new Vector3(hx * 2f, 80f, 2f));
        MakeWall("Limite_E", new Vector3(c.x + hx, y + 30f, c.z), new Vector3(2f, 80f, hz * 2f));
        MakeWall("Limite_W", new Vector3(c.x - hx, y + 30f, c.z), new Vector3(2f, 80f, hz * 2f));
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
        Button bCont = PauseButton(panel.transform, "BtnContinuar", "Continuar (P)", new Vector2(0f, 60f));
        Button bMenu = PauseButton(panel.transform, "BtnMenu", "Volver al menu", new Vector2(0f, -10f));
        bCont.onClick.RemoveAllListeners();
        bMenu.onClick.RemoveAllListeners();
        UnityEventTools.AddVoidPersistentListener(bCont.onClick, ui.OnContinuePressed);
        UnityEventTools.AddVoidPersistentListener(bMenu.onClick, ui.OnMenuPressed);
        ui.Bind(panel, null, null, null);
        panel.SetActive(false);
    }

    private static Button PauseButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        go.transform.SetParent(parent, false);
        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
        }
        Image img = go.GetComponent<Image>();
        if (img == null)
        {
            img = go.AddComponent<Image>();
        }
        img.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        Button b = go.GetComponent<Button>();
        if (b == null)
        {
            b = go.AddComponent<Button>();
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(320f, 52f);
        Label(go.transform, "Label", label, 22);
        return b;
    }

    private static void Rebake(Vector3 top)
    {
        GameObject bakeGo = GameObject.Find("NavMeshBake");
        if (bakeGo == null)
        {
            bakeGo = new GameObject("NavMeshBake");
        }
        GameObject go = GameObject.Find("NavTop");
        if (go == null)
        {
            go = new GameObject("NavTop");
        }
        go.transform.SetParent(bakeGo.transform, false);
        GameObject scope = GameObject.Find("NavMeshScope");
        if (scope != null)
        {
            scope.transform.SetParent(go.transform, true);
        }
        NavMeshSurface surface = go.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = go.AddComponent<NavMeshSurface>();
        }
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.defaultArea = 0;
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Debug.Log("Fase6 NavMesh: rebake verts=" + tri.vertices.Length + ".");
    }
}
