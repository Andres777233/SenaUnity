using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Popayork.Core;
using Popayork.Enemies;

// Popayork/Verify Arte (BLOQUES 1-3, 5).
// BLOQUE 1: falla si queda Shader.Find/new Material fuera de MaterialFactory,
//   o si algún material de las escenas de misión tiene shader inválido.
// (Bloques 2-3 amplían este mismo archivo con NavMesh e interfaz.)
public static class VerifyArte
{
    private static readonly string[] MissionScenes = new string[]
    {
        "Assets/Scenes/Mision1.unity",
        "Assets/Scenes/Mision2.unity",
        "Assets/Scenes/Mision3.unity"
    };

    [MenuItem("Popayork/Verify Arte")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Arte: PASS" : "Verify Arte: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyArte.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Arte: PASS" : "Verify Arte: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Arte: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckSourceHygiene();
        ok &= CheckMissionMaterials();
        ok &= CheckNavMeshWaves();
        ok &= CheckUILayout();
        ok &= CheckPasadaVisual();
        return ok;
    }

    // Nadie salvo MaterialFactory.cs puede usar Shader.Find ni new Material().
    private static bool CheckSourceHygiene()
    {
        bool ok = true;
        string[] roots = new string[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Assets/Scripts"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets/Editor")
        };
        Regex shaderFind = new Regex(@"Shader\s*\.\s*Find", RegexOptions.Compiled);
        Regex newMaterial = new Regex(@"new\s+Material\s*\(", RegexOptions.Compiled);
        for (int r = 0; r < roots.Length; r++)
        {
            if (!Directory.Exists(roots[r]))
            {
                continue;
            }
            string[] files = Directory.GetFiles(roots[r], "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (Path.GetFileName(files[i]) == "MaterialFactory.cs")
                {
                    continue;
                }
                string text = File.ReadAllText(files[i]);
                string code = StripComments(text);
                if (shaderFind.IsMatch(code))
                {
                    Debug.LogError("Verify Arte: FAIL higiene — Shader.Find en " + files[i] + " (usar MaterialFactory).");
                    ok = false;
                }
                if (newMaterial.IsMatch(code))
                {
                    Debug.LogError("Verify Arte: FAIL higiene — new Material() en " + files[i] + " (usar MaterialFactory).");
                    ok = false;
                }
            }
        }
        if (ok)
        {
            Debug.Log("Verify Arte: higiene de fuentes PASS (solo MaterialFactory crea materiales).");
        }
        return ok;
    }

    // Quita comentarios de línea y de bloque antes de buscar en el código.
    private static string StripComments(string text)
    {
        string noBlock = blockComments.Replace(text, string.Empty);
        string[] lines = noBlock.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = StripLineComment(lines[i]);
        }
        return string.Join("\n", lines);
    }

    private static string StripLineComment(string line)
    {
        bool inString = false;
        for (int i = 0; i + 1 < line.Length; i++)
        {
            if (line[i] == '"' && (i == 0 || line[i - 1] != '\\'))
            {
                inString = !inString;
            }
            if (!inString && line[i] == '/' && line[i + 1] == '/')
            {
                return line.Substring(0, i);
            }
        }
        return line;
    }

    private static readonly Regex blockComments = new Regex(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);

    // Todo Renderer de Mision1/2/3 debe tener materiales con shader válido.
    private static bool CheckMissionMaterials()
    {
        bool ok = true;
        string previous = EditorSceneManager.GetActiveScene().path;
        for (int s = 0; s < MissionScenes.Length; s++)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), MissionScenes[s])))
            {
                Debug.LogError("Verify Arte: FAIL — falta escena " + MissionScenes[s] + ".");
                ok = false;
                continue;
            }
            EditorSceneManager.OpenScene(MissionScenes[s], OpenSceneMode.Single);
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            int bad = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] shared = renderers[i].sharedMaterials;
                for (int m = 0; m < shared.Length; m++)
                {
                    Material mat = shared[m];
                    if (mat == null)
                    {
                        Debug.LogError("Verify Arte: FAIL material nulo en " + FullName(renderers[i]) + " (" + MissionScenes[s] + ").");
                        bad++;
                    }
                    else if (MaterialFactory.NeedsRepair(mat))
                    {
                        string shaderName = mat.shader != null ? mat.shader.name : "null";
                        Debug.LogError("Verify Arte: FAIL shader '" + shaderName + "' en material '"
                            + mat.name + "' de " + FullName(renderers[i]) + " (" + MissionScenes[s] + ").");
                        bad++;
                    }
                }
            }
            if (bad == 0)
            {
                Debug.Log("Verify Arte: materiales " + MissionScenes[s] + " PASS (" + renderers.Length + " renderers).");
            }
            else
            {
                ok = false;
            }
        }
        if (!string.IsNullOrEmpty(previous) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), previous)))
        {
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
        return ok;
    }

    // BLOQUE 2: simula una oleada completa por escena sin activar nada:
    // cada slot de agente debe colocarse (SamplePosition, radio 4 m) en un
    // marker *Spawn* y tener ruta al objetivo. Falla si alguno no se coloca.
    private static bool CheckNavMeshWaves()
    {
        bool ok = true;
        string previous = EditorSceneManager.GetActiveScene().path;
        string[] objectiveNames = new string[] { "TorreDelReloj", "MorroTop", "M3_Objective", "Rio" };
        for (int s = 0; s < MissionScenes.Length; s++)
        {
            EditorSceneManager.OpenScene(MissionScenes[s], OpenSceneMode.Single);
            AgentBrain[] brains = Object.FindObjectsByType<AgentBrain>(FindObjectsInactive.Include);
            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            List<Transform> spawns = new List<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name.Contains("Spawn") && !all[i].name.Contains("Jugador"))
                {
                    spawns.Add(all[i]);
                }
            }
            if (brains.Length == 0)
            {
                Debug.LogError("Verify Arte: FAIL oleada — " + MissionScenes[s] + " sin agentes en el pool.");
                ok = false;
                continue;
            }
            if (spawns.Count == 0)
            {
                Debug.LogError("Verify Arte: FAIL oleada — " + MissionScenes[s] + " sin markers *Spawn*.");
                ok = false;
                continue;
            }
            Transform objective = null;
            for (int o = 0; o < objectiveNames.Length && objective == null; o++)
            {
                GameObject go = GameObject.Find(objectiveNames[o]);
                if (go != null)
                {
                    objective = go.transform;
                }
            }
            NavMeshPath path = new NavMeshPath();
            int placed = 0;
            for (int i = 0; i < brains.Length; i++)
            {
                Transform spot = spawns[i % spawns.Count];
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(spot.position, out hit, 4.0f, NavMesh.AllAreas))
                {
                    Debug.LogError("Verify Arte: FAIL oleada — agente #" + i + " (" + brains[i].Faction
                        + ") no se coloca en '" + spot.name + "' (" + MissionScenes[s] + ").");
                    ok = false;
                    continue;
                }
                if (objective != null)
                {
                    bool found = NavMesh.CalculatePath(hit.position, objective.position, NavMesh.AllAreas, path)
                        && (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial);
                    if (!found)
                    {
                        Debug.LogError("Verify Arte: FAIL oleada — agente #" + i + " sin ruta '"
                            + spot.name + "' -> '" + objective.name + "' (" + MissionScenes[s] + ").");
                        ok = false;
                        continue;
                    }
                }
                placed++;
            }
            if (placed == brains.Length)
            {
                Debug.Log("Verify Arte: oleada " + MissionScenes[s] + " PASS (" + placed + "/" + brains.Length + " colocados).");
            }
        }
        if (!string.IsNullOrEmpty(previous) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), previous)))
        {
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
        return ok;
    }

    // BLOQUE 3: todo Canvas con scaler 1920x1080 match 0.5; intro con layout;
    // objetivo y progreso sin solaparse; HUD sin solapes a 3 resoluciones.
    private static bool CheckUILayout()
    {
        bool ok = true;
        string previous = EditorSceneManager.GetActiveScene().path;
        List<string> scenes = new List<string>(MissionScenes);
        scenes.Add("Assets/Scenes/MainMenu.unity");
        scenes.Add("Assets/Scenes/Campaign.unity");
        scenes.Add("Assets/Scenes/TestArena.unity");
        Vector2[] resolutions = new Vector2[]
        {
            new Vector2(1920f, 1080f),
            new Vector2(1280f, 720f),
            new Vector2(2560f, 1440f)
        };
        string[] hudNames = new string[]
        {
            "ObjectiveText", "ProgressFill", "HealthFill", "AmmoText", "WeaponText", "Crosshair"
        };
        for (int s = 0; s < scenes.Count; s++)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenes[s])))
            {
                Debug.LogWarning("Verify Arte: UI — falta " + scenes[s] + " (se omite).");
                continue;
            }
            EditorSceneManager.OpenScene(scenes[s], OpenSceneMode.Single);
            bool isMission = scenes[s].Contains("Mision");

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                CanvasScaler scaler = canvases[i].GetComponent<CanvasScaler>();
                if (scaler == null
                    || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                    || scaler.referenceResolution != new Vector2(1920f, 1080f)
                    || Mathf.Abs(scaler.matchWidthOrHeight - 0.5f) > 0.01f)
                {
                    Debug.LogError("Verify Arte: FAIL UI — canvas '" + canvases[i].name
                        + "' sin scaler 1920x1080 match 0.5 (" + scenes[s] + ").");
                    ok = false;
                }
            }

            GameObject intro = GameObject.Find("IntroPanel");
            GameObject objBar = GameObject.Find("ObjectiveBar");
            if (isMission)
            {
                if (GameObject.Find("MissionCanvas") == null)
                {
                    Debug.LogError("Verify Arte: FAIL UI — " + scenes[s] + " sin MissionCanvas.");
                    ok = false;
                }
                if (GameObject.Find("HealthFill") == null
                    || GameObject.Find("AmmoText") == null
                    || GameObject.Find("Crosshair") == null)
                {
                    Debug.LogError("Verify Arte: FAIL UI — " + scenes[s] + " sin HUD (HealthFill/AmmoText/Crosshair).");
                    ok = false;
                }
                if (intro == null || objBar == null)
                {
                    Debug.LogError("Verify Arte: FAIL UI — " + scenes[s] + " sin IntroPanel/ObjectiveBar.");
                    ok = false;
                    continue;
                }
                if (intro.GetComponent<VerticalLayoutGroup>() == null)
                {
                    Debug.LogError("Verify Arte: FAIL UI — IntroPanel sin VerticalLayoutGroup (" + scenes[s] + ").");
                    ok = false;
                }
                if (intro.transform.Find("IntroTitle") == null
                    || intro.transform.Find("IntroBody") == null
                    || intro.transform.Find("BtnVamos") == null)
                {
                    Debug.LogError("Verify Arte: FAIL UI — intro incompleta (título/historia/botón) en " + scenes[s] + ".");
                    ok = false;
                }
            }
            if (intro == null || objBar == null)
            {
                continue;
            }

            List<RectTransform> hudRects = new List<RectTransform>();
            List<string> hudLabels = new List<string>();
            RectTransform[] allRects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
            for (int i = 0; i < allRects.Length; i++)
            {
                for (int h = 0; h < hudNames.Length; h++)
                {
                    if (allRects[i].name == hudNames[h] && !HasLayoutAncestor(allRects[i].transform))
                    {
                        hudRects.Add(allRects[i]);
                        hudLabels.Add(hudNames[h]);
                    }
                }
            }
            for (int r = 0; r < resolutions.Length; r++)
            {
                float sf = ScalerFactor(resolutions[r].x, resolutions[r].y);
                for (int a = 0; a < hudRects.Count; a++)
                {
                    Rect ra;
                    if (!TryPixelRect(hudRects[a], resolutions[r].x, resolutions[r].y, sf, out ra))
                    {
                        continue;
                    }
                    for (int b = a + 1; b < hudRects.Count; b++)
                    {
                        Rect rb;
                        if (!TryPixelRect(hudRects[b], resolutions[r].x, resolutions[r].y, sf, out rb))
                        {
                            continue;
                        }
                        if (PixelsOverlap(ra, rb))
                        {
                            Debug.LogError("Verify Arte: FAIL UI — solape '" + hudLabels[a] + "' vs '"
                                + hudLabels[b] + "' a " + resolutions[r].x + "x" + resolutions[r].y
                                + " (" + scenes[s] + ").");
                            ok = false;
                        }
                    }
                }
            }
            Debug.Log("Verify Arte: UI " + scenes[s] + " revisada (" + hudRects.Count + " elementos HUD).");
        }
        if (!string.IsNullOrEmpty(previous) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), previous)))
        {
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
        return ok;
    }

    private static bool HasLayoutAncestor(Transform t)
    {
        Transform p = t.parent;
        while (p != null)
        {
            if (p.GetComponent<VerticalLayoutGroup>() != null || p.GetComponent<HorizontalLayoutGroup>() != null)
            {
                return true;
            }
            p = p.parent;
        }
        return false;
    }

    private static float ScalerFactor(float w, float h)
    {
        float logW = Mathf.Log(w / 1920f, 2f);
        float logH = Mathf.Log(h / 1080f, 2f);
        return Mathf.Pow(2f, Mathf.Lerp(logW, logH, 0.5f));
    }

    private static bool TryPixelRect(RectTransform rt, float resW, float resH, float sf, out Rect rect)
    {
        List<RectTransform> chain = new List<RectTransform>();
        Transform t = rt.transform;
        bool underCanvas = false;
        while (t != null)
        {
            if (t.GetComponent<Canvas>() != null)
            {
                underCanvas = true;
                break;
            }
            RectTransform r = t.GetComponent<RectTransform>();
            if (r != null)
            {
                chain.Add(r);
            }
            t = t.parent;
        }
        if (!underCanvas)
        {
            rect = new Rect();
            return false;
        }
        float x0 = 0f;
        float y0 = 0f;
        float w = resW;
        float h = resH;
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            RectTransform r = chain[i];
            float nx0 = x0 + r.anchorMin.x * w + r.offsetMin.x * sf;
            float ny0 = y0 + r.anchorMin.y * h + r.offsetMin.y * sf;
            float nx1 = x0 + r.anchorMax.x * w + r.offsetMax.x * sf;
            float ny1 = y0 + r.anchorMax.y * h + r.offsetMax.y * sf;
            x0 = nx0;
            y0 = ny0;
            w = nx1 - nx0;
            h = ny1 - ny0;
        }
        rect = new Rect(x0, y0, w, h);
        return w > 0f && h > 0f;
    }

    private static bool PixelsOverlap(Rect a, Rect b)
    {
        const float eps = 1f;
        return a.xMin < b.xMax - eps && b.xMin < a.xMax - eps
            && a.yMin < b.yMax - eps && b.yMin < a.yMax - eps;
    }

    // BLOQUE 5: sol cálido con sombras + niebla en cada misión.
    private static bool CheckPasadaVisual()
    {
        bool ok = true;
        string previous = EditorSceneManager.GetActiveScene().path;
        for (int s = 0; s < MissionScenes.Length; s++)
        {
            EditorSceneManager.OpenScene(MissionScenes[s], OpenSceneMode.Single);
            GameObject sun = GameObject.Find("SolAtardecer");
            Light light = sun != null ? sun.GetComponent<Light>() : null;
            if (light == null || light.type != LightType.Directional)
            {
                Debug.LogError("Verify Arte: FAIL visual — " + MissionScenes[s] + " sin sol direccional.");
                ok = false;
            }
            else if (light.shadows == LightShadows.None)
            {
                Debug.LogError("Verify Arte: FAIL visual — sol sin sombras en " + MissionScenes[s] + ".");
                ok = false;
            }
            if (!RenderSettings.fog)
            {
                Debug.LogError("Verify Arte: FAIL visual — niebla apagada en " + MissionScenes[s] + ".");
                ok = false;
            }
            else
            {
                Debug.Log("Verify Arte: visual " + MissionScenes[s] + " revisada (sol + niebla).");
            }
        }
        if (!string.IsNullOrEmpty(previous) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), previous)))
        {
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
        return ok;
    }

    private static string FullName(Component c)
    {
        List<string> parts = new List<string>();
        Transform t = c.transform;
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }
}
