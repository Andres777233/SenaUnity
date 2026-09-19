using System.Collections.Generic;
using UnityEngine;

// Fábrica única de materiales low poly (BLOQUE 1).
// Nadie más en el proyecto usa Shader.Find ni new Material: todo pasa por aquí.
// Elige el shader según el pipeline activo (URP: Lit; built-in: Standard),
// aplica flat shading low poly (sin metal, smoothness 0) y cachea los
// materiales compartidos en Assets/Materials/Generated (solo en Editor).
namespace Popayork.Core
{
    public static class MaterialFactory
    {
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        public const string BuiltinShaderName = "Standard";
        public const string FallbackShaderName = "Legacy Shaders/Diffuse";

        private const string GeneratedFolder = "Assets/Materials/Generated";

        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        private static Shader cachedShader;

        // True si hay un RenderPipeline activo (URP/HDRP). Sin pipeline = built-in.
        public static bool UsingScriptablePipeline()
        {
            return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        }

        public static string ActiveShaderName()
        {
            return UsingScriptablePipeline() ? UrpLitShaderName : BuiltinShaderName;
        }

        // Shader activo con cadena de respaldo. Nunca inventa nombres: si todo
        // falla, usa el material built-in por defecto antes que devolver null.
        public static Shader ActiveShader()
        {
            if (cachedShader != null)
            {
                return cachedShader;
            }
            cachedShader = Shader.Find(ActiveShaderName());
            if (cachedShader == null)
            {
                cachedShader = Shader.Find(BuiltinShaderName);
            }
            if (cachedShader == null)
            {
                cachedShader = Shader.Find(FallbackShaderName);
            }
            if (cachedShader == null)
            {
                Material def = Resources.GetBuiltinResource<Material>("Default-Material.mat");
                if (def != null && def.shader != null)
                {
                    cachedShader = def.shader;
                }
            }
            if (cachedShader == null)
            {
                Debug.LogError("MaterialFactory: ningún shader utilizable (ni Standard ni respaldos).");
            }
            return cachedShader;
        }

        // Material COMPARTIDO cacheado (no mutar: es el mismo para todo el que
        // pida igual color/emisión/textura). Emisión = color * factor.
        public static Material Get(Color color, float emission = 0f, Texture mainTex = null)
        {
            return Get(color, color * emission, mainTex);
        }

        // Material COMPARTIDO cacheado con color de emisión explícito.
        public static Material Get(Color color, Color emissionColor, Texture mainTex = null)
        {
            string key = Key(color, emissionColor, mainTex);
            Material found;
            if (cache.TryGetValue(key, out found) && found != null)
            {
                return found;
            }
#if UNITY_EDITOR
            string path = GeneratedPath(key);
            Material onDisk = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (onDisk != null && onDisk.shader != null && onDisk.shader.isSupported)
            {
                cache[key] = onDisk;
                return onDisk;
            }
#endif
            Material mat = Build(color, emissionColor, mainTex);
#if UNITY_EDITOR
            SaveGenerated(path, mat);
#endif
            cache[key] = mat;
            return mat;
        }

        // Instancia ÚNICA (para quien ajusta color/emisión después, como los
        // helpers Mat() de los builders o los pools de armas). No se cachea.
        public static Material New()
        {
            Shader shader = ActiveShader();
            if (shader == null)
            {
                return null;
            }
            Material mat = new Material(shader);
            ApplyFlat(mat, Color.white, Color.clear, null);
            return mat;
        }

        // Repara un material existente in situ: conserva color/textura/emisión,
        // cambia al shader activo y reaplica flat shading. Devuelve true si cambió.
        public static bool RepairInPlace(Material mat)
        {
            if (mat == null)
            {
                return false;
            }
            if (!NeedsRepair(mat))
            {
                ApplyFlat(mat, ReadColor(mat), ReadEmission(mat), ReadTexture(mat));
                return false;
            }
            Color color = ReadColor(mat);
            Color emission = ReadEmission(mat);
            Texture tex = ReadTexture(mat);
            Shader shader = ActiveShader();
            if (shader != null)
            {
                mat.shader = shader;
            }
            ApplyFlat(mat, color, emission, tex);
            return true;
        }

        // True si el shader es nulo, no soportado o de otro pipeline/importador.
        public static bool NeedsRepair(Material mat)
        {
            if (mat == null)
            {
                return true;
            }
            Shader shader = mat.shader;
            if (shader == null || !shader.isSupported)
            {
                return true;
            }
            string name = shader.name;
            if (UsingScriptablePipeline())
            {
                return false;
            }
            return name.IndexOf("Universal Render Pipeline") >= 0
                || name.IndexOf("HDRP") >= 0
                || name.IndexOf("HD Render Pipeline") >= 0
                || name.IndexOf("Autodesk Interactive") >= 0
                || name.IndexOf("Principled") >= 0
                || name.IndexOf("glTF") >= 0
                || name.IndexOf("gltf") >= 0
                || name == "Hidden/InternalErrorShader";
        }

        // Aplica color/textura/flat-shading al material dado (shader actual).
        public static void ApplyFlat(Material mat, Color color, Color emission, Texture tex)
        {
            if (mat == null)
            {
                return;
            }
            if (UsingScriptablePipeline())
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                if (tex != null && mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", tex);
                }
                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", 0f);
                }
                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 0f);
                }
                if (mat.HasProperty("_Surface") && color.a < 0.99f)
                {
                    mat.SetFloat("_Surface", 1f);
                }
            }
            else
            {
                mat.color = color;
                if (tex != null)
                {
                    mat.mainTexture = tex;
                }
                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", 0f);
                }
                if (mat.HasProperty("_Glossiness"))
                {
                    mat.SetFloat("_Glossiness", 0f);
                }
                ApplyTransparency(mat, color.a);
            }
            if (emission.r + emission.g + emission.b > 0.001f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
            else if (!UsingScriptablePipeline() || mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }

        // Skybox procedural de atardecer (built-in, sin paquetes).
        // Null si el shader no está disponible; el llamador usa fondo sólido.
        public static Material NewSkybox()
        {
            Shader sky = null;
            try
            {
                sky = Shader.Find("Skybox/Procedural");
            }
            catch (UnityException)
            {
                sky = null;
            }
            if (sky == null)
            {
                return null;
            }
            return new Material(sky);
        }

        private static Material Build(Color color, Color emission, Texture tex)
        {
            Material mat = new Material(ActiveShader());
            ApplyFlat(mat, color, emission, tex);
            mat.name = "Gen_" + Key(color, emission, tex).GetHashCode().ToString("X8");
            return mat;
        }

        private static void ApplyTransparency(Material mat, float alpha)
        {
            if (alpha >= 0.99f || !mat.HasProperty("_Mode"))
            {
                return;
            }
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        private static Color ReadColor(Material mat)
        {
            if (UsingScriptablePipeline() && mat.HasProperty("_BaseColor"))
            {
                return mat.GetColor("_BaseColor");
            }
            try
            {
                return mat.color;
            }
            catch (UnityException)
            {
                return Color.white;
            }
        }

        private static Color ReadEmission(Material mat)
        {
            if (mat.HasProperty("_EmissionColor"))
            {
                Color existing = mat.GetColor("_EmissionColor");
                if (existing.r + existing.g + existing.b > 0.001f)
                {
                    return existing;
                }
            }
            return Color.clear;
        }

        private static Texture ReadTexture(Material mat)
        {
            if (UsingScriptablePipeline() && mat.HasProperty("_BaseMap"))
            {
                return mat.GetTexture("_BaseMap");
            }
            try
            {
                return mat.mainTexture;
            }
            catch (UnityException)
            {
                return null;
            }
        }

        private static string Key(Color color, Color emission, Texture tex)
        {
            return ActiveShaderName() + "_"
                + ColorKey(color) + "_" + ColorKey(emission) + "_"
                + (tex != null ? tex.name : "notex");
        }

        private static string ColorKey(Color c)
        {
            return Mathf.RoundToInt(c.r * 255f).ToString("X2")
                + Mathf.RoundToInt(c.g * 255f).ToString("X2")
                + Mathf.RoundToInt(c.b * 255f).ToString("X2")
                + Mathf.RoundToInt(c.a * 255f).ToString("X2");
        }

#if UNITY_EDITOR
        private static string GeneratedPath(string key)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!UnityEditor.AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/Materials", "Generated");
            }
            return GeneratedFolder + "/MF_" + key.GetHashCode().ToString("X8") + ".mat";
        }

        private static void SaveGenerated(string path, Material mat)
        {
            Material existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return;
            }
            UnityEditor.AssetDatabase.CreateAsset(mat, path);
        }
#endif
    }
}
