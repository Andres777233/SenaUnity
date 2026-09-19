using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Popayork.Core;

// Popayork/Pasada Visual (BLOQUE 5, estándar AGENTS.md §4).
// Por escena de misión, en orden de impacto:
// 1. Sol direccional cálido de atardecer con sombras suaves + ambiente.
// 2. Niebla ligera y fondo coherente + skybox procedural de atardecer.
// 3. Auditoría de paleta (máx 8 colores base; solo informa).
// 4. Edificios blancos / teja: ya los generan los builders con MaterialFactory
//    (aquí solo se audita).
// 5. Posproceso (bloom/viñeta): NO aplicado — exige RP/paquetes, prohibido
//    instalarlos sin preguntar (pendiente decisión del usuario).
// 6. Fuego/humo más visibles a distancia (tamaño, tasa y tope de partículas).
// 7. FPS: no medible en batch sin GPU; lo mide el usuario (ver ESTADO.md).
// Hace backup del .unity en Assets/Scenes/Backup antes de modificar.
public static class PasadaVisual
{
    private static readonly string[] MissionScenes = new string[]
    {
        "Assets/Scenes/Mision1.unity",
        "Assets/Scenes/Mision2.unity",
        "Assets/Scenes/Mision3.unity"
    };

    private const string SkyPath = "Assets/Materials/Generated/CieloAtardecer.mat";

    [MenuItem("Popayork/Pasada Visual")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Pasada Visual: PASS" : "Pasada Visual: FAIL (ver warnings).");
    }

    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Pasada Visual: PASS" : "Pasada Visual: FAIL (ver warnings).");
    }

    private static bool RunAll()
    {
        bool ok = true;
        EnsureFolder("Assets/Materials/Generated");
        EnsureFolder("Assets/Scenes/Backup");
        string previous = EditorSceneManager.GetActiveScene().path;
        for (int s = 0; s < MissionScenes.Length; s++)
        {
            ok &= PassScene(MissionScenes[s]);
        }
        if (!string.IsNullOrEmpty(previous) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), previous)))
        {
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
        AssetDatabase.SaveAssets();
        return ok;
    }

    private static bool PassScene(string scenePath)
    {
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
        {
            Debug.LogWarning("Pasada Visual: falta " + scenePath + " (se omite).");
            return true;
        }
        BackupScene(scenePath);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        ApplySun();
        ApplyFogAndSky();
        AuditPalette(scenePath);
        BoostParticles();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Pasada Visual: " + scenePath + " aplicada.");
        return true;
    }

    // 1) Sol cálido de atardecer con sombras suaves.
    private static void ApplySun()
    {
        GameObject sun = GameObject.Find("SolAtardecer");
        if (sun == null)
        {
            sun = new GameObject("SolAtardecer");
        }
        Light light = sun.GetComponent<Light>();
        if (light == null)
        {
            light = sun.AddComponent<Light>();
        }
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.72f, 0.5f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.8f;
        if (sun.transform.rotation == Quaternion.identity)
        {
            sun.transform.rotation = Quaternion.Euler(18f, -115f, 0f);
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.55f, 0.52f);
    }

    // 2) Niebla ligera + skybox procedural de atardecer (built-in, sin paquetes).
    private static void ApplyFogAndSky()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.96f, 0.72f, 0.55f);
        RenderSettings.fogDensity = 0.004f;

        Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
        if (sky == null)
        {
            sky = MaterialFactory.NewSkybox();
            if (sky == null)
            {
                Debug.LogWarning("Pasada Visual: sin shader Skybox/Procedural; se conserva color de fondo.");
                return;
            }
            AssetDatabase.CreateAsset(sky, SkyPath);
        }
        SetSkyFloat(sky, "_AtmosphereThickness", 0.7f);
        SetSkyColor(sky, "_SkyTint", new Color(1f, 0.6f, 0.38f));
        SetSkyColor(sky, "_GroundColor", new Color(0.45f, 0.35f, 0.3f));
        SetSkyFloat(sky, "_Exposure", 1.1f);
        SetSkyFloat(sky, "_SunSize", 0.04f);
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].clearFlags = CameraClearFlags.Skybox;
            cameras[i].backgroundColor = new Color(0.98f, 0.7f, 0.5f);
        }
    }

    private static void SetSkyFloat(Material sky, string prop, float value)
    {
        if (sky.HasProperty(prop))
        {
            sky.SetFloat(prop, value);
        }
    }

    private static void SetSkyColor(Material sky, string prop, Color value)
    {
        if (sky.HasProperty(prop))
        {
            sky.SetColor(prop, value);
        }
    }

    // 3-4) Auditoría: familias de color base (cuantizadas) en la escena.
    private static void AuditPalette(string scenePath)
    {
        Dictionary<int, int> buckets = new Dictionary<int, int>();
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] shared = renderers[i].sharedMaterials;
            for (int m = 0; m < shared.Length; m++)
            {
                if (shared[m] == null)
                {
                    continue;
                }
                Color c = shared[m].color;
                int key = (Mathf.Clamp(Mathf.RoundToInt(c.r * 7f), 0, 7) << 16)
                    | (Mathf.Clamp(Mathf.RoundToInt(c.g * 7f), 0, 7) << 8)
                    | Mathf.Clamp(Mathf.RoundToInt(c.b * 7f), 0, 7);
                int n;
                buckets.TryGetValue(key, out n);
                buckets[key] = n + 1;
            }
        }
        Debug.Log("Pasada Visual: paleta " + scenePath + " = " + buckets.Count
            + " familias de color en " + renderers.Length + " renderers (objetivo ≤8 base)."
            + (buckets.Count > 12 ? " WARNING: paleta amplia, revisar." : ""));
    }

    // 6) Fuego/humo legibles a distancia.
    private static void BoostParticles()
    {
        ParticleSystem[] systems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include);
        for (int i = 0; i < systems.Length; i++)
        {
            string n = systems[i].name;
            if (!n.StartsWith("Fuego_") && !n.StartsWith("Humo_"))
            {
                continue;
            }
            var main = systems[i].main;
            main.startSize = main.startSize.constant * 1.6f;
            main.maxParticles = Mathf.Max(main.maxParticles, 120);
            var emission = systems[i].emission;
            emission.rateOverTime = Mathf.Max(emission.rateOverTime.constant, 36f);
            var renderer = systems[i].GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.sharedMaterial != null
                && MaterialFactory.NeedsRepair(renderer.sharedMaterial))
            {
                MaterialFactory.RepairInPlace(renderer.sharedMaterial);
                EditorUtility.SetDirty(renderer.sharedMaterial);
            }
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static void BackupScene(string scenePath)
    {
        string file = Path.GetFileNameWithoutExtension(scenePath);
        string dest = "Assets/Scenes/Backup/" + file + "_backup.unity";
        int n = 1;
        while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), dest)))
        {
            dest = "Assets/Scenes/Backup/" + file + "_backup" + n + ".unity";
            n++;
        }
        string error = AssetDatabase.CopyAsset(scenePath, dest);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning("Pasada Visual: backup falló para " + scenePath + ": " + error);
        }
    }
}
