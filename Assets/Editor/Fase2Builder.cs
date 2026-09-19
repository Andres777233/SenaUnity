using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Popayork.Core;
using Popayork.Player;
using Popayork.UI;
using Popayork.Weapons;
using Popayork.World;

public static class Fase2Builder
{
    private const string GunsFbx = "Assets/Models3D/armas low poly 2/source/Guns.fbx";
    private const string WeaponsDir = "Assets/Config/Weapons";
    private const string PrefabDir = "Assets/Prefabs";

    [MenuItem("Popayork/Construir Fase 2")]
    public static void BuildAll()
    {
        EnsureFolders();
        PlayerConfig playerConfig = BuildPlayerConfig();
        WeaponData msr = BuildWeaponData("Fusil_MSR", "Fusil MSR", "MSR", true, 9.0f, 22.0f, 95.0f, 30, 90, 1.6f, 1.1f, 0.22f, 0.75f, 1.0f);
        WeaponData be1 = BuildWeaponData("Subfusil_BE1", "Subfusil BE1", "BE1", true, 12.0f, 14.0f, 80.0f, 32, 96, 1.4f, 0.8f, 0.18f, 0.6f, 1.25f);
        GameObject playerPrefab = BuildPlayerPrefab(playerConfig, new WeaponData[] { msr, be1 });
        BuildTestArena(playerPrefab);
        AppendBuildSettings(GameConfig.TestArenaScene);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Fase 2: PASS — 2 armas, prefab jugador y TestArena listos.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Config", "Weapons");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }

    private static void EnsureFolder(string parent, string leaf)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + leaf))
        {
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    private static void AppendBuildSettings(string sceneName)
    {
        string path = "Assets/Scenes/" + sceneName + ".unity";
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

    // ---------- Datos ----------

    private static PlayerConfig BuildPlayerConfig()
    {
        string path = "Assets/Config/PlayerConfig.asset";
        PlayerConfig config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<PlayerConfig>();
            AssetDatabase.CreateAsset(config, path);
        }
        config.walkSpeed = 4.5f;
        config.sprintSpeed = 7.0f;
        config.jumpHeight = 1.2f;
        config.gravity = 22.0f;
        config.groundStickForce = 2.0f;
        config.lookSpeed = 2.2f;
        config.minPitch = -85.0f;
        config.maxPitch = 85.0f;
        config.normalFov = 75.0f;
        config.sprintFov = 85.0f;
        config.fovBlendSpeed = 8.0f;
        config.recoilRecoverSpeed = 10.0f;
        config.shakeDecaySpeed = 3.0f;
        config.maxHealth = 100.0f;
        config.respawnDelay = 1.5f;
        EditorUtility.SetDirty(config);
        return config;
    }

    private static WeaponData BuildWeaponData(string file, string display, string modelRoot, bool auto, float rate, float dmg, float speed, int mag, int reserve, float reload, float recoil, float shake, float length, float pitch)
    {
        string path = WeaponsDir + "/" + file + ".asset";
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.displayName = display;
        data.modelRootName = modelRoot;
        data.automatic = auto;
        data.shotsPerSecond = rate;
        data.damage = dmg;
        data.projectileSpeed = speed;
        data.magazineSize = mag;
        data.startingReserve = reserve;
        data.reloadSeconds = reload;
        data.recoilKickDegrees = recoil;
        data.shakeAmount = shake;
        data.targetLengthMeters = length;
        data.shotPitch = pitch;
        EditorUtility.SetDirty(data);
        return data;
    }

    // ---------- Jugador ----------

    private static Font DefaultFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null)
        {
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return f;
    }

    private static GameObject FindFbxChild(string rootName)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(GunsFbx);
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

    private static GameObject BuildPlayerPrefab(PlayerConfig config, WeaponData[] datas)
    {
        GameObject root = new GameObject("Player");
        var character = root.AddComponent<CharacterController>();
        character.height = 1.8f;
        character.radius = 0.4f;
        character.center = new Vector3(0f, 0.9f, 0f);
        character.skinWidth = 0.08f;

        var controller = root.AddComponent<PlayerController>();
        var health = root.AddComponent<PlayerHealth>();
        var holder = root.AddComponent<PlayerWeapons>();
        root.AddComponent<WeaponAudio>();

        GameObject camGo = new GameObject("PlayerCamera");
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = config.normalFov;
        root.tag = "Player";

        GameObject viewGo = new GameObject("WeaponView");
        viewGo.transform.SetParent(camGo.transform, false);
        viewGo.transform.localPosition = new Vector3(0.28f, -0.24f, 0.45f);

        GameObject muzzleGo = new GameObject("Muzzle");
        muzzleGo.transform.SetParent(viewGo.transform, false);

        Weapon[] weapons = new Weapon[datas.Length];
        for (int i = 0; i < datas.Length; i++)
        {
            GameObject wGo = new GameObject("Weapon_" + datas[i].name);
            wGo.transform.SetParent(viewGo.transform, false);
            wGo.transform.localPosition = Vector3.zero;
            var weapon = wGo.AddComponent<Weapon>();
            wGo.AddComponent<WeaponAudio>();
            GameObject modelSrc = FindFbxChild(datas[i].modelRootName);
            if (modelSrc != null)
            {
                GameObject model = Object.Instantiate(modelSrc);
                model.name = datas[i].modelRootName + "_View";
                model.transform.SetParent(wGo.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                var correction = WeaponViewNormalizer.Normalize(model, datas[i].targetLengthMeters);
                Debug.Log("Fase2 modelo " + datas[i].modelRootName + ": medido=" + correction.measuredLength.ToString("F2") + "m escala=" + correction.appliedScale.ToString("F3"));
                weapon.AttachViewModel(model);
            }
            else
            {
                Debug.LogWarning("Fase2: no se encontró modelo " + datas[i].modelRootName + " en " + GunsFbx);
            }
            muzzleGo.transform.localPosition = new Vector3(0f, 0.05f, 0.45f);
            weapons[i] = weapon;
            SerializedObject soWeapon = new SerializedObject(weapon);
            soWeapon.FindProperty("data").objectReferenceValue = datas[i];
            soWeapon.FindProperty("muzzle").objectReferenceValue = muzzleGo.transform;
            soWeapon.ApplyModifiedPropertiesWithoutUndo();
        }
        holder.Bind(weapons, muzzleGo.transform);

        SerializedObject soController = new SerializedObject(controller);
        soController.FindProperty("config").objectReferenceValue = config;
        soController.FindProperty("playerCamera").objectReferenceValue = cam;
        soController.FindProperty("cameraPivot").objectReferenceValue = camGo.transform;
        soController.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soHealth = new SerializedObject(health);
        soHealth.FindProperty("config").objectReferenceValue = config;
        soHealth.ApplyModifiedPropertiesWithoutUndo();

        BuildHud(camGo);

        string prefabPath = PrefabDir + "/Player.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildHud(GameObject camGo)
    {
        GameObject canvasGo = new GameObject("HUDCanvas");
        canvasGo.transform.SetParent(camGo.transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        var hud = canvasGo.AddComponent<HUD>();

        GameObject healthBg = new GameObject("HealthBG");
        healthBg.transform.SetParent(canvasGo.transform, false);
        Image bgImg = healthBg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);
        RectTransform bgRt = healthBg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.03f, 0.04f);
        bgRt.anchorMax = new Vector2(0.28f, 0.09f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        GameObject healthGo = new GameObject("HealthFill");
        healthGo.transform.SetParent(canvasGo.transform, false);
        Image fillImg = healthGo.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.3f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRt = healthGo.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0.03f, 0.04f);
        fillRt.anchorMax = new Vector2(0.28f, 0.09f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        Text ammo = CreateLabel(canvasGo.transform, "AmmoText", "30 / 90", 26, new Vector2(0.97f, 0.06f));
        Text weaponLabel = CreateLabel(canvasGo.transform, "WeaponText", "Fusil MSR", 20, new Vector2(0.97f, 0.11f));
        Text respawn = CreateLabel(canvasGo.transform, "RespawnText", "Reapareciendo...", 30, new Vector2(0.5f, 0.5f));
        respawn.enabled = false;

        GameObject cross = new GameObject("Crosshair");
        cross.transform.SetParent(canvasGo.transform, false);
        Image crossImg = cross.AddComponent<Image>();
        crossImg.color = Color.white;
        RectTransform crossRt = cross.GetComponent<RectTransform>();
        crossRt.anchorMin = new Vector2(0.5f, 0.5f);
        crossRt.anchorMax = new Vector2(0.5f, 0.5f);
        crossRt.sizeDelta = new Vector2(8f, 8f);

        GameObject markerGo = new GameObject("Hitmarker");
        markerGo.transform.SetParent(canvasGo.transform, false);
        Image markerImg = markerGo.AddComponent<Image>();
        markerImg.color = Color.white;
        markerImg.enabled = false;
        RectTransform markerRt = markerGo.GetComponent<RectTransform>();
        markerRt.anchorMin = new Vector2(0.5f, 0.5f);
        markerRt.anchorMax = new Vector2(0.5f, 0.5f);
        markerRt.sizeDelta = new Vector2(28f, 28f);
        markerRt.rotation = Quaternion.Euler(0f, 0f, 45f);

        Image[] arrows = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject aGo = new GameObject("DamageArrow_" + i);
            aGo.transform.SetParent(canvasGo.transform, false);
            Image aImg = aGo.AddComponent<Image>();
            aImg.color = new Color(1f, 0.15f, 0.1f, 0.9f);
            aImg.enabled = false;
            RectTransform aRt = aGo.GetComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0.5f, 0.5f);
            aRt.anchorMax = new Vector2(0.5f, 0.5f);
            aRt.sizeDelta = new Vector2(36f, 36f);
            aRt.anchoredPosition = new Vector2(0f, 90f);
            aRt.rotation = Quaternion.Euler(0f, 0f, -90f * i);
            arrows[i] = aImg;
        }

        hud.Bind(fillImg, ammo, weaponLabel, cross, markerImg, arrows, respawn);
    }

    private static Text CreateLabel(Transform parent, string name, string content, int size, Vector2 anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = DefaultFont();
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleRight;
        if (name == "RespawnText")
        {
            t.alignment = TextAnchor.MiddleCenter;
        }
        t.color = Color.white;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 40f);
        return t;
    }

    // ---------- TestArena ----------

    private static void BuildTestArena(GameObject playerPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureBoot();
        EnsureEventSystem();

        GameObject sun = new GameObject("Sun");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.8f, 0.6f);
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.62f, 0.66f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.95f, 0.75f, 0.6f);
        RenderSettings.fogDensity = 0.008f;

        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.75f, 0.72f, 0.68f);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "ArenaGround";
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        Material wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(0.92f, 0.9f, 0.86f);
        BuildWall("Wall_N", new Vector3(0f, 2.5f, 30f), new Vector3(62f, 5f, 1f), wallMat);
        BuildWall("Wall_S", new Vector3(0f, 2.5f, -30f), new Vector3(62f, 5f, 1f), wallMat);
        BuildWall("Wall_E", new Vector3(30f, 2.5f, 0f), new Vector3(1f, 5f, 62f), wallMat);
        BuildWall("Wall_W", new Vector3(-30f, 2.5f, 0f), new Vector3(1f, 5f, 62f), wallMat);

        Material coverMat = new Material(Shader.Find("Standard"));
        coverMat.color = new Color(0.7f, 0.45f, 0.3f);
        BuildWall("Cover_1", new Vector3(-6f, 1f, 8f), new Vector3(4f, 2f, 1f), coverMat);
        BuildWall("Cover_2", new Vector3(6f, 1f, 2f), new Vector3(4f, 2f, 1f), coverMat);
        BuildWall("Cover_3", new Vector3(0f, 1f, -8f), new Vector3(1f, 2f, 4f), coverMat);

        GameObject pools = new GameObject("CombatPools");
        pools.AddComponent<ProjectilePool>();
        pools.AddComponent<ImpactPool>();

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 0.1f, -20f);
        player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        player.AddComponent<RespawnPoint>();
        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.SetSpawn(player.transform.position, 0f);
        }

        Vector3[] dummySpots = new Vector3[]
        {
            new Vector3(-8f, 0f, 0f),
            new Vector3(0f, 0f, 2f),
            new Vector3(8f, 0f, 0f),
            new Vector3(-4f, 0f, 12f),
            new Vector3(4f, 0f, 12f),
            new Vector3(0f, 0f, 20f)
        };
        for (int i = 0; i < dummySpots.Length; i++)
        {
            BuildDummy("Diana_" + (i + 1), dummySpots[i]);
        }

        AddPauseMenu();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + GameConfig.TestArenaScene + ".unity");
    }

    private static void BuildWall(string name, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void BuildDummy(string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos + Vector3.up * 0.9f;
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.85f, 0.25f, 0.2f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(go.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.GetComponent<MeshRenderer>().sharedMaterial = mat;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Base";
        ring.transform.SetParent(go.transform, false);
        ring.transform.localPosition = new Vector3(0f, -0.85f, 0f);
        ring.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
        go.AddComponent<TargetDummy>();
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

    private static void AddPauseMenu()
    {
        GameObject canvasGo = new GameObject("PauseCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        var ui = canvasGo.AddComponent<PauseMenuUI>();

        GameObject panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvasGo.transform, false);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 420f);

        Button bCont = CreateButton(panel.transform, "BtnContinuar", "Continuar (P)", new Vector2(0f, 90f));
        Button bMenu = CreateButton(panel.transform, "BtnMenu", "Volver al menu", new Vector2(0f, 25f));
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(bCont.onClick, ui.OnContinuePressed);
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(bMenu.onClick, ui.OnMenuPressed);

        ui.Bind(panel, null, null, null);
        panel.SetActive(false);
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        Button b = go.AddComponent<Button>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(320f, 52f);
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        Text t = labelGo.AddComponent<Text>();
        t.font = DefaultFont();
        t.text = label;
        t.fontSize = 22;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        return b;
    }
}
