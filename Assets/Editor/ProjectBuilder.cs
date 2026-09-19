using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Popayork.Core;
using Popayork.UI;

public static class ProjectBuilder
{
    private const string ScenesDir = "Assets/Scenes";

    [MenuItem("Popayork/Construir Proyecto")]
    public static void BuildAll()
    {
        EnsureFolders();
        BuildSceneMainMenu();
        BuildSceneCampaign();
        BuildSceneMission(GameConfig.Mission1Scene, "Mision 1: Empieza el caos — llega a la Torre del Reloj");
        BuildSceneMission(GameConfig.Mission2Scene, "Mision 2: Ruta al Morro (placeholder Fase 1)");
        BuildSceneMission(GameConfig.Mission3Scene, "Mision 3: Descenso al rio (placeholder Fase 1)");
        RegisterBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Proyecto: PASS — 5 escenas generadas y registradas.");
    }

    private static void EnsureFolders()
    {
        string[] dirs =
        {
            "Assets/Scenes", "Assets/Config", "Assets/Editor",
            "Assets/Scripts/Core", "Assets/Scripts/Player", "Assets/Scripts/Weapons",
            "Assets/Scripts/Enemies", "Assets/Scripts/Missions", "Assets/Scripts/UI",
            "Assets/Scripts/Vehicles", "Assets/Scripts/World"
        };
        foreach (string d in dirs)
        {
            if (!AssetDatabase.IsValidFolder(d))
            {
                string parent = Path.GetDirectoryName(d);
                string leaf = Path.GetFileName(d);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }

    private static void RegisterBuildSettings()
    {
        var list = new EditorBuildSettingsScene[GameConfig.AllScenes.Length];
        for (int i = 0; i < GameConfig.AllScenes.Length; i++)
        {
            list[i] = new EditorBuildSettingsScene(ScenesDir + "/" + GameConfig.AllScenes[i] + ".unity", true);
        }
        EditorBuildSettings.scenes = list;
    }

    // ---------- Comunes ----------

    private static Font DefaultFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null)
        {
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return f;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private static void EnsureCameraAndLight()
    {
        if (Object.FindAnyObjectByType<Camera>() == null)
        {
            var cam = new GameObject("Main Camera");
            cam.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 2f, -6f);
        }
        if (Object.FindAnyObjectByType<Light>() == null)
        {
            var light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.85f, 0.7f);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.75f);
        }
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

    private static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name);
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor align, Rect rect)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = DefaultFont();
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(rect.xMin, rect.yMin);
        rt.anchorMax = new Vector2(rect.xMax, rect.yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return t;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        Button b = go.AddComponent<Button>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var labelGo = new GameObject("Label");
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

    private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size, float min, float max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        Slider s = go.AddComponent<Slider>();
        s.minValue = min;
        s.maxValue = max;
        s.value = min;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return s;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(520f, 420f);
        return go;
    }

    // ---------- Escenas ----------

    private static void BuildSceneMainMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureBoot();
        EnsureEventSystem();
        EnsureCameraAndLight();

        Canvas canvas = CreateCanvas("MainMenuCanvas");
        MainMenuUI ui = canvas.gameObject.AddComponent<MainMenuUI>();

        CreateText(canvas.transform, "Title", "Popayork", 64, TextAnchor.MiddleCenter, new Rect(0f, 0.62f, 1f, 0.95f));
        CreateText(canvas.transform, "Subtitle", "Sena Vs Universitarios", 28, TextAnchor.MiddleCenter, new Rect(0f, 0.52f, 1f, 0.66f));

        Button bCamp = CreateButton(canvas.transform, "BtnCampana", "Campaña", new Vector2(0f, 60f), new Vector2(300f, 56f));
        Button bOpt = CreateButton(canvas.transform, "BtnOpciones", "Opciones", new Vector2(0f, -10f), new Vector2(300f, 56f));
        Button bQuit = CreateButton(canvas.transform, "BtnSalir", "Salir", new Vector2(0f, -80f), new Vector2(300f, 56f));
        UnityEventTools.AddVoidPersistentListener(bCamp.onClick, ui.OnCampaignPressed);
        UnityEventTools.AddVoidPersistentListener(bOpt.onClick, ui.OnOptionsPressed);
        UnityEventTools.AddVoidPersistentListener(bQuit.onClick, ui.OnQuitPressed);

        GameObject optPanel = CreatePanel(canvas.transform, "OptionsPanel", new Color(0.08f, 0.08f, 0.1f, 0.97f));
        CreateText(optPanel.transform, "OptTitle", "Opciones", 30, TextAnchor.MiddleCenter, new Rect(0f, 0.78f, 1f, 0.98f));
        CreateText(optPanel.transform, "VolLabel", "Volumen", 20, TextAnchor.MiddleCenter, new Rect(0f, 0.6f, 1f, 0.72f));
        Slider vol = CreateSlider(optPanel.transform, "VolumeSlider", new Vector2(0f, 60f), new Vector2(380f, 28f), 0f, 1f);
        vol.value = GameConfig.DefaultVolume;
        CreateText(optPanel.transform, "SensLabel", "Sensibilidad", 20, TextAnchor.MiddleCenter, new Rect(0f, 0.32f, 1f, 0.44f));
        Slider sens = CreateSlider(optPanel.transform, "SensitivitySlider", new Vector2(0f, -60f), new Vector2(380f, 28f), 0.1f, 5f);
        sens.value = GameConfig.DefaultSensitivity;
        UnityEventTools.AddFloatPersistentListener(vol.onValueChanged, ui.OnVolumeChanged, vol.value);
        UnityEventTools.AddFloatPersistentListener(sens.onValueChanged, ui.OnSensitivityChanged, sens.value);
        optPanel.SetActive(false);

        ui.Bind(vol, sens, optPanel);
        EditorSceneManager.SaveScene(scene, ScenesDir + "/" + GameConfig.MainMenuScene + ".unity");
    }

    private static void BuildSceneCampaign()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureBoot();
        EnsureEventSystem();
        EnsureCameraAndLight();

        Canvas canvas = CreateCanvas("MissionSelectCanvas");
        MissionSelectUI ui = canvas.gameObject.AddComponent<MissionSelectUI>();

        CreateText(canvas.transform, "Title", "Seleccion de misiones", 40, TextAnchor.MiddleCenter, new Rect(0f, 0.7f, 1f, 0.95f));
        Text status = CreateText(canvas.transform, "Status", "Solo la Mision 1 desbloqueada al inicio", 20, TextAnchor.MiddleCenter, new Rect(0f, 0.08f, 1f, 0.2f));

        Button m1 = CreateButton(canvas.transform, "BtnMision1", "Mision 1", new Vector2(0f, 80f), new Vector2(300f, 56f));
        Button m2 = CreateButton(canvas.transform, "BtnMision2", "Mision 2", new Vector2(0f, 10f), new Vector2(300f, 56f));
        Button m3 = CreateButton(canvas.transform, "BtnMision3", "Mision 3", new Vector2(0f, -60f), new Vector2(300f, 56f));
        Button back = CreateButton(canvas.transform, "BtnVolver", "Volver", new Vector2(0f, -140f), new Vector2(300f, 56f));
        UnityEventTools.AddIntPersistentListener(m1.onClick, ui.OnMissionPressed, 1);
        UnityEventTools.AddIntPersistentListener(m2.onClick, ui.OnMissionPressed, 2);
        UnityEventTools.AddIntPersistentListener(m3.onClick, ui.OnMissionPressed, 3);
        UnityEventTools.AddVoidPersistentListener(back.onClick, ui.OnBackPressed);

        ui.Bind(m1, m2, m3, status);
        AddPauseMenuToScene();
        EditorSceneManager.SaveScene(scene, ScenesDir + "/" + GameConfig.CampaignScene + ".unity");
    }

    private static void BuildSceneMission(string sceneName, string objective)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureBoot();
        EnsureEventSystem();
        EnsureCameraAndLight();

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;

        Canvas hud = CreateCanvas("HUDCanvas");
        CreateText(hud.transform, "Objective", "Objetivo: " + objective, 22, TextAnchor.UpperCenter, new Rect(0f, 0.8f, 1f, 1f));
        CreateText(hud.transform, "Hint", "Pulsa P para pausa", 18, TextAnchor.LowerCenter, new Rect(0f, 0f, 1f, 0.15f));

        AddPauseMenuToScene();
        EditorSceneManager.SaveScene(scene, ScenesDir + "/" + sceneName + ".unity");
    }

    private static void AddPauseMenuToScene()
    {
        Canvas canvas = CreateCanvas("PauseCanvas");
        PauseMenuUI ui = canvas.gameObject.AddComponent<PauseMenuUI>();

        GameObject panel = CreatePanel(canvas.transform, "PausePanel", new Color(0.05f, 0.05f, 0.07f, 0.95f));
        CreateText(panel.transform, "PauseTitle", "Pausa (P)", 30, TextAnchor.MiddleCenter, new Rect(0f, 0.78f, 1f, 0.98f));

        Button bCont = CreateButton(panel.transform, "BtnContinuar", "Continuar", new Vector2(0f, 90f), new Vector2(320f, 52f));
        Button bOpt = CreateButton(panel.transform, "BtnOpciones", "Opciones", new Vector2(0f, 25f), new Vector2(320f, 52f));
        Button bMenu = CreateButton(panel.transform, "BtnMenu", "Volver al menu", new Vector2(0f, -40f), new Vector2(320f, 52f));
        UnityEventTools.AddVoidPersistentListener(bCont.onClick, ui.OnContinuePressed);
        UnityEventTools.AddVoidPersistentListener(bOpt.onClick, ui.OnOptionsPressed);
        UnityEventTools.AddVoidPersistentListener(bMenu.onClick, ui.OnMenuPressed);

        GameObject mini = new GameObject("MiniOptions");
        mini.transform.SetParent(panel.transform, false);
        Image miniImg = mini.AddComponent<Image>();
        miniImg.color = new Color(0.12f, 0.12f, 0.14f, 0.97f);
        RectTransform mrt = mini.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0.5f, 0.5f);
        mrt.anchorMax = new Vector2(0.5f, 0.5f);
        mrt.anchoredPosition = new Vector2(0f, -160f);
        mrt.sizeDelta = new Vector2(420f, 120f);
        Slider vol = CreateSlider(mini.transform, "VolumeSlider", new Vector2(0f, 25f), new Vector2(340f, 26f), 0f, 1f);
        vol.value = GameConfig.DefaultVolume;
        Slider sens = CreateSlider(mini.transform, "SensitivitySlider", new Vector2(0f, -25f), new Vector2(340f, 26f), 0.1f, 5f);
        sens.value = GameConfig.DefaultSensitivity;
        UnityEventTools.AddFloatPersistentListener(vol.onValueChanged, ui.OnVolumeChanged, vol.value);
        UnityEventTools.AddFloatPersistentListener(sens.onValueChanged, ui.OnSensitivityChanged, sens.value);
        mini.SetActive(false);

        ui.Bind(panel, vol, sens, mini);
        panel.SetActive(false);
    }
}
