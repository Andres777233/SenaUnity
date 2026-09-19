using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Popayork.Core;

// Popayork/Reparar Materiales (BLOQUE 1).
// Convierte al shader activo (vía MaterialFactory) todo material con shader
// inválido o magenta bajo Assets/Models3D y Assets/MapaPopayan, más los
// materiales de los prefabs generados (árboles, bancas, farolas, etc.).
// Copia de seguridad en Assets/Materials/Backup antes de cada cambio.
// NO toca archivos .unity: las escenas se regeneran con los builders.
public static class RepararMateriales
{
    private const string BackupFolder = "Assets/Materials/Backup";

    private static readonly string[] MaterialFolders = new string[]
    {
        "Assets/Models3D",
        "Assets/MapaPopayan"
    };

    private const string PrefabFolder = "Assets/Prefabs";

    [MenuItem("Popayork/Reparar Materiales")]
    public static void RunFromMenu()
    {
        int fixedMats, fixedPrefabs;
        Run(out fixedMats, out fixedPrefabs);
        Debug.Log("Reparar Materiales: materiales=" + fixedMats + " prefabs=" + fixedPrefabs + ". Backup en " + BackupFolder + ".");
    }

    public static void Run(out int fixedMaterials, out int fixedPrefabs)
    {
        fixedMaterials = 0;
        fixedPrefabs = 0;
        EnsureFolder("Assets/Materials");
        EnsureFolder(BackupFolder);

        // 1) Materiales importados (.mat y sub-assets de FBX) bajo Models3D/MapaPopayan.
        string[] guids = AssetDatabase.FindAssets("t:Material", MaterialFolders);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || !MaterialFactory.NeedsRepair(mat))
            {
                continue;
            }
            BackupAsset(path);
            if (MaterialFactory.RepairInPlace(mat))
            {
                EditorUtility.SetDirty(mat);
                fixedMaterials++;
                Debug.Log("Reparar Materiales: " + path + " -> " + MaterialFactory.ActiveShaderName() + ".");
            }
        }

        // 2) Prefabs generados por script (props de escena): repara sus
        // sharedMaterials inválidos con backup del prefab completo.
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { PrefabFolder });
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject contents = null;
            bool dirty = false;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Reparar Materiales: no se pudo abrir " + path + ": " + e.Message);
                continue;
            }
            Renderer[] renderers = contents.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] shared = renderers[r].sharedMaterials;
                bool changed = false;
                for (int m = 0; m < shared.Length; m++)
                {
                    if (shared[m] != null && MaterialFactory.NeedsRepair(shared[m]))
                    {
                        if (!dirty)
                        {
                            BackupAsset(path);
                            dirty = true;
                        }
                        MaterialFactory.RepairInPlace(shared[m]);
                        EditorUtility.SetDirty(shared[m]);
                        changed = true;
                        fixedMaterials++;
                    }
                }
                if (changed)
                {
                    renderers[r].sharedMaterials = shared;
                }
            }
            if (dirty)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                fixedPrefabs++;
                Debug.Log("Reparar Materiales: prefab " + path + " reparado.");
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        int slash = path.LastIndexOf('/');
        string parent = slash > 0 ? path.Substring(0, slash) : "Assets";
        string name = slash > 0 ? path.Substring(slash + 1) : path;
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void BackupAsset(string path)
    {
        string file = System.IO.Path.GetFileNameWithoutExtension(path);
        string ext = System.IO.Path.GetExtension(path);
        string dest = BackupFolder + "/" + file + "_backup" + ext;
        int n = 1;
        while (System.IO.File.Exists(FullPath(dest)))
        {
            dest = BackupFolder + "/" + file + "_backup" + n + ext;
            n++;
        }
        string error = AssetDatabase.CopyAsset(path, dest);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning("Reparar Materiales: backup falló para " + path + ": " + error);
        }
    }

    private static string FullPath(string assetPath)
    {
        return System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), assetPath);
    }
}
