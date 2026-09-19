using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Popayork.UI;

// Popayork/Arreglar UI + estándar de interfaz (BLOQUE 3).
// Todo Canvas: CanvasScaler "Scale With Screen Size", 1920x1080, match 0.5.
// Intro: título arriba, historia al centro, controles debajo, botón abajo
//   (VerticalLayoutGroup; sin superposiciones).
// HUD: objetivo arriba-centro, progreso debajo del objetivo, vida abajo-izq,
//   munición abajo-der, mira al centro (anclas explícitas, nada se tapa).
// Idempotente: los builders lo llaman antes de guardar; también se puede
// ejecutar sobre la escena abierta con Popayork/Arreglar UI.
public static class UiLayout
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public const float ReferenceMatch = 0.5f;

    [MenuItem("Popayork/Arreglar UI")]
    public static void RunFromMenu()
    {
        int n = FixSceneUI();
        Debug.Log("Arreglar UI: " + n + " canvas revisados en la escena abierta.");
    }

    // Revisa todos los Canvas de la escena abierta. Devuelve cuántos tocó.
    public static int FixSceneUI()
    {
        int n = 0;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            FixCanvas(canvases[i].gameObject);
            n++;
        }
        FixByName("IntroPanel", FixIntroPanel);
        FixByName("ObjectiveBar", FixObjectiveBar);
        FixByName("ResultPanel", FixResultPanel);
        FixByName("HUDCanvas", FixHud);
        FixByName("PausePanel", FixPausePanel);
        EnsureMissionHud();
        return n;
    }

    public static void FixCanvas(GameObject canvasGo)
    {
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasGo.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = ReferenceMatch;
    }

    private delegate void PanelFix(GameObject panel);

    private static void FixByName(string name, PanelFix fix)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == name)
            {
                fix(all[i]);
            }
        }
    }

    // ---------- Intro: título arriba, cuerpo centro, botón abajo ----------

    private static void FixIntroPanel(GameObject intro)
    {
        RectTransform rt = intro.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.3f, 0.17f);
        rt.anchorMax = new Vector2(0.7f, 0.83f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        VerticalLayoutGroup group = intro.GetComponent<VerticalLayoutGroup>();
        if (group == null)
        {
            group = intro.AddComponent<VerticalLayoutGroup>();
        }
        group.padding = new RectOffset(28, 28, 24, 24);
        group.spacing = 14f;
        group.childAlignment = TextAnchor.UpperCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        LayoutChild(intro, "IntroTitle", 64f, 0f, 0);
        LayoutChild(intro, "IntroBody", 0f, 1f, 1);
        LayoutChild(intro, "BtnVamos", 60f, 0f, 2);
    }

    // ---------- Objetivo arriba-centro, progreso debajo (sin taparse) ----------

    private static void FixObjectiveBar(GameObject objBar)
    {
        RectTransform rt = objBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -43f);
        rt.sizeDelta = new Vector2(560f, 70f);

        SetAnchors(objBar, "ObjectiveText",
            new Vector2(0.02f, 0.42f), new Vector2(0.98f, 0.98f));
        SetAnchors(objBar, "ProgressBG",
            new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.36f));
        SetAnchors(objBar, "ProgressFill",
            new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.36f));
    }

    // ---------- Resultados / victoria / derrota ----------

    private static void FixResultPanel(GameObject result)
    {
        RectTransform rt = result.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(640f, 400f);

        VerticalLayoutGroup group = result.GetComponent<VerticalLayoutGroup>();
        if (group == null)
        {
            group = result.AddComponent<VerticalLayoutGroup>();
        }
        group.padding = new RectOffset(28, 28, 24, 24);
        group.spacing = 14f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        LayoutChild(result, "ResultText", 0f, 1f, 0);
        LayoutChild(result, "BtnReintentar", 56f, 0f, 1);
        LayoutChild(result, "BtnMenu", 56f, 0f, 2);
    }

    // ---------- HUD ----------

    private static void FixHud(GameObject hudCanvas)
    {
        RectTransform canvasRt = hudCanvas.GetComponent<RectTransform>();
        if (canvasRt != null)
        {
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;
        }
        SetAnchors(hudCanvas, "HealthBG",
            new Vector2(0.03f, 0.04f), new Vector2(0.28f, 0.09f));
        SetAnchors(hudCanvas, "HealthFill",
            new Vector2(0.03f, 0.04f), new Vector2(0.28f, 0.09f));
        SetBottomRight(hudCanvas, "AmmoText", -280f, -86f, -16f, -46f);
        SetBottomRight(hudCanvas, "WeaponText", -280f, -132f, -16f, -92f);
        SetCenter(hudCanvas, "Crosshair", 12f);
        SetCenter(hudCanvas, "Hitmarker", 28f);
        SetCenter(hudCanvas, "RespawnText", 400f, 50f);
    }

    // Si la escena tiene MissionCanvas pero ningún HUD (misiones), crea el HUD
    // de misión (vida, munición, mira, hitmarker, flechas, respawn) y lo enlaza.
    private static void EnsureMissionHud()
    {
        if (Object.FindAnyObjectByType<HUD>() != null)
        {
            return;
        }
        GameObject mission = GameObject.Find("MissionCanvas");
        if (mission == null)
        {
            return;
        }
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null)
        {
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        GameObject bg = new GameObject("HealthBG");
        bg.transform.SetParent(mission.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);
        GameObject fill = new GameObject("HealthFill");
        fill.transform.SetParent(mission.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.3f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        Text ammo = NewHudText(mission.transform, "AmmoText", "30 / 90", 26, TextAnchor.MiddleRight, f);
        Text weapon = NewHudText(mission.transform, "WeaponText", "", 20, TextAnchor.MiddleRight, f);
        Text respawn = NewHudText(mission.transform, "RespawnText", "Reapareciendo...", 30, TextAnchor.MiddleCenter, f);
        respawn.enabled = false;
        GameObject cross = new GameObject("Crosshair");
        cross.transform.SetParent(mission.transform, false);
        cross.AddComponent<Image>().color = Color.white;
        GameObject marker = new GameObject("Hitmarker");
        marker.transform.SetParent(mission.transform, false);
        Image markerImg = marker.AddComponent<Image>();
        markerImg.color = Color.white;
        markerImg.enabled = false;
        marker.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        Image[] arrows = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject a = new GameObject("DamageArrow_" + i);
            a.transform.SetParent(mission.transform, false);
            arrows[i] = a.AddComponent<Image>();
            arrows[i].color = new Color(1f, 0.15f, 0.1f, 0.9f);
            arrows[i].enabled = false;
            RectTransform art = a.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.5f, 0.5f);
            art.anchorMax = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(36f, 36f);
            art.anchoredPosition = new Vector2(0f, 90f);
            art.rotation = Quaternion.Euler(0f, 0f, -90f * i);
        }
        HUD hud = mission.AddComponent<HUD>();
        hud.Bind(fillImg, ammo, weapon, cross, markerImg, arrows, respawn);
        FixHud(mission);
        Debug.Log("UiLayout: HUD de misión creado en MissionCanvas.");
    }

    private static Text NewHudText(Transform parent, string name, string content, int size, TextAnchor align, Font f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = f;
        t.text = content;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        return t;
    }

    // ---------- Pausa ----------

    private static void FixPausePanel(GameObject panel)
    {
        VerticalLayoutGroup group = panel.GetComponent<VerticalLayoutGroup>();
        if (group == null)
        {
            group = panel.AddComponent<VerticalLayoutGroup>();
        }
        group.padding = new RectOffset(28, 28, 24, 24);
        group.spacing = 12f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        LayoutChild(panel, "PauseTitle", 56f, 0f, 0);
        LayoutChild(panel, "BtnContinuar", 52f, 0f, 1);
        LayoutChild(panel, "BtnOpciones", 52f, 0f, 2);
        LayoutChild(panel, "BtnMenu", 52f, 0f, 3);
        LayoutChild(panel, "MiniOptions", 120f, 0f, 4);
    }

    // ---------- Utilidades ----------

    private static void SetAnchors(GameObject parent, string childName, Vector2 min, Vector2 max)
    {
        Transform t = parent.transform.Find(childName);
        if (t == null)
        {
            return;
        }
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetBottomRight(GameObject parent, string childName, float xMin, float yMin, float xMax, float yMax)
    {
        Transform t = parent.transform.Find(childName);
        if (t == null)
        {
            return;
        }
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.offsetMin = new Vector2(xMin, yMin);
        rt.offsetMax = new Vector2(xMax, yMax);
    }

    private static void SetCenter(GameObject parent, string childName, float size)
    {
        SetCenter(parent, childName, size, size);
    }

    private static void SetCenter(GameObject parent, string childName, float w, float h)
    {
        Transform t = parent.transform.Find(childName);
        if (t == null)
        {
            return;
        }
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void LayoutChild(GameObject parent, string childName, float minHeight, float flexibleHeight, int siblingIndex)
    {
        Transform t = parent.transform.Find(childName);
        if (t == null)
        {
            return;
        }
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
        }
        LayoutElement element = t.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = t.gameObject.AddComponent<LayoutElement>();
        }
        element.minHeight = minHeight;
        element.flexibleHeight = flexibleHeight;
        t.SetSiblingIndex(siblingIndex);
    }
}
