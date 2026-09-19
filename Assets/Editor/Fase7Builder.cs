using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Popayork.Core;
using Popayork.UI;

public static class Fase7Builder
{
    private static readonly string[] AllScenes = new string[] {
        "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Campaign.unity",
        "Assets/Scenes/Mision1.unity", "Assets/Scenes/Mision2.unity",
        "Assets/Scenes/Mision3.unity", "Assets/Scenes/TestArena.unity" };

    [MenuItem("Popayork/Construir Fase 7")]
    public static void BuildAll()
    {
        PlayerSettings.productName = "Popayork";
        foreach (string path in AllScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            EnsureAudioManager();
            EnsureAudioListener();
            if (path.EndsWith("MainMenu.unity"))
            {
                PatchMainMenu();
            }
            if (path.EndsWith("Mision1.unity") || path.EndsWith("Mision2.unity"))
            {
                OptimizeCity();
            }
            EditorSceneManager.SaveScene(scene, path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Fase 7: PASS — audio, créditos, calidad y ProductName.");
    }

    [MenuItem("Popayork/Compilar Build")]
    public static void BuildLinux()
    {
        var levels = EditorBuildSettings.scenes;
        string[] enabled = new string[levels.Length];
        int n = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].enabled)
            {
                enabled[n] = levels[i].path;
                n++;
            }
        }
        System.Array.Resize(ref enabled, n);
        string dir = "Builds/Linux";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        string target = dir + "/Popayork.x86_64";
        var report = BuildPipeline.BuildPlayer(enabled, target,
            BuildTarget.StandaloneLinux64, BuildOptions.None);
        Debug.Log(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded
            ? "Popayork/Compilar Build: PASS — " + target + " (" + report.summary.totalSize + " bytes)."
            : "Popayork/Compilar Build: FAIL — " + report.summary.result + ".");
    }

    private static void EnsureAudioManager()
    {
        if (Object.FindAnyObjectByType<AudioManager>() == null)
        {
            new GameObject("Audio").AddComponent<AudioManager>();
        }
    }

    private static void EnsureAudioListener()
    {
        if (Object.FindAnyObjectByType<AudioListener>() != null)
        {
            return;
        }
        Camera cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null && cam.GetComponent<AudioListener>() == null)
        {
            cam.gameObject.AddComponent<AudioListener>();
        }
    }

    private static void OptimizeCity()
    {
        GameObject ciudad = GameObject.Find("CiudadBase");
        if (ciudad == null)
        {
            return;
        }
        Renderer[] renderers = ciudad.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderers[i].receiveShadows = false;
        }
        Debug.Log("Fase7: sombras OFF en CiudadBase (" + renderers.Length + " renderers).");
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

    private static void PatchMainMenu()
    {
        GameObject canvasGo = GameObject.Find("MainMenuCanvas");
        MainMenuUI ui = Object.FindAnyObjectByType<MainMenuUI>();
        if (canvasGo == null || ui == null)
        {
            Debug.LogWarning("Fase7: sin MainMenuCanvas para créditos.");
            return;
        }
        Button btnCamp = FindButton(canvasGo.transform, "BtnCampana");
        Button btnCred = GetOrCreateButton(canvasGo.transform, "BtnCreditos", "Créditos",
            btnCamp != null ? btnCamp.transform.localPosition + new Vector3(0f, -210f, 0f) : new Vector3(0f, -150f, 0f));
        UnityEventTools.AddVoidPersistentListener(btnCred.onClick, ui.OnCreditsPressed);

        GameObject credits = GameObject.Find("CreditsPanel");
        if (credits == null)
        {
            credits = new GameObject("CreditsPanel");
            credits.transform.SetParent(canvasGo.transform, false);
            Image img = credits.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.07f, 0.97f);
            RectTransform rt = credits.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 460f);
        }
        Text body = credits.GetComponentInChildren<Text>();
        if (body == null)
        {
            GameObject go = new GameObject("CreditsText");
            go.transform.SetParent(credits.transform, false);
            body = go.AddComponent<Text>();
            body.font = DefaultFont();
            body.fontSize = 22;
            body.alignment = TextAnchor.MiddleCenter;
            body.color = new Color(1f, 0.95f, 0.7f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f);
            rt.anchorMax = new Vector2(0.95f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        body.text = "POPAYORK — Sena Vs Universitarios\n\nDiseño y código: equipo Popayork\nModelos: Sketchfab / JJ / ciudad de Popayán\nMúsica y SFX: generados por código (placeholder)\n\nHecho con Unity en Popayán, Colombia";
        Button back = GetOrCreateButton(credits.transform, "BtnVolverCred", "Volver", new Vector2(0f, -180f));
        // Limpia oyentes viejos del botón Volver antes de cablear.
        for (int i = back.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(back.onClick, i);
        }
        UnityEventTools.AddVoidPersistentListener(back.onClick, ui.OnCreditsPressed);
        credits.SetActive(false);
        ui.BindCredits(credits);

        GameObject options = GameObject.Find("OptionsPanel");
        if (options != null)
        {
            Button alta = GetOrCreateButton(options.transform, "BtnCalidadAlta", "Calidad: Alta", new Vector2(0f, -140f));
            Button baja = GetOrCreateButton(options.transform, "BtnCalidadBaja", "Calidad: Baja", new Vector2(0f, -200f));
            UnityEventTools.AddVoidPersistentListener(alta.onClick, ui.OnQualityHigh);
            UnityEventTools.AddVoidPersistentListener(baja.onClick, ui.OnQualityLow);
        }
    }

    private static Button FindButton(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static Button GetOrCreateButton(Transform parent, string name, string label, Vector3 localPos)
    {
        Transform t = parent.Find(name);
        GameObject go = t == null ? new GameObject(name) : t.gameObject;
        go.transform.SetParent(parent, false);
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
        if (rt == null)
        {
            rt = go.AddComponent<RectTransform>();
        }
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(localPos.x, localPos.y);
        rt.sizeDelta = new Vector2(300f, 52f);
        Transform lt = go.transform.Find("Label");
        GameObject labelGo = lt == null ? new GameObject("Label") : lt.gameObject;
        labelGo.transform.SetParent(go.transform, false);
        Text text = labelGo.GetComponent<Text>();
        if (text == null)
        {
            text = labelGo.AddComponent<Text>();
        }
        text.font = DefaultFont();
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        if (lrt == null)
        {
            lrt = labelGo.AddComponent<RectTransform>();
        }
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        return b;
    }
}
