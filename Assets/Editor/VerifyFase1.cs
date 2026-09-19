using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Popayork.Core;
using Popayork.UI;

public static class VerifyFase1
{
    [MenuItem("Popayork/Verify Fase 1")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 1: PASS" : "Verify Fase 1: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFase1.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 1: PASS" : "Verify Fase 1: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Fase 1: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckBuildSettings();
        ok &= CheckSceneFiles();
        ok &= CheckSaveRoundtrip();
        ok &= CheckOnlyMission1Unlocked();
        ok &= CheckBootInScenes();
        return ok;
    }

    private static bool CheckBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        bool pass = scenes != null;
        if (pass)
        {
            // Las 6 escenas base deben estar presentes y activas (se permiten extras).
            foreach (string expected in GameConfig.AllScenes)
            {
                string path = "Assets/Scenes/" + expected + ".unity";
                bool found = false;
                foreach (var s in scenes)
                {
                    if (s.path == path && s.enabled)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    pass = false;
                    break;
                }
            }
        }
        Debug.Log(pass
            ? "Verify Fase 1 [BuildSettings]: PASS — 6 escenas registradas."
            : "Verify Fase 1 [BuildSettings]: FAIL — se esperaban 6 escenas: MainMenu, Campaign, Mision1, Mision2, Mision3, TestArena.");
        return pass;
    }

    private static bool CheckSceneFiles()
    {
        bool pass = true;
        foreach (string s in GameConfig.AllScenes)
        {
            if (!File.Exists("Assets/Scenes/" + s + ".unity"))
            {
                pass = false;
            }
        }
        Debug.Log(pass
            ? "Verify Fase 1 [SceneFiles]: PASS — existen los 5 .unity."
            : "Verify Fase 1 [SceneFiles]: FAIL — falta algún .unity en Assets/Scenes.");
        return pass;
    }

    private static bool CheckSaveRoundtrip()
    {
        string path = SaveSystem.SavePath;
        string backup = null;
        bool hadBackup = false;
        if (File.Exists(path))
        {
            backup = File.ReadAllText(path);
            hadBackup = true;
        }
        try
        {
            var data = SaveData.NewDefault();
            data.masterVolume = 0.42f;
            data.mouseSensitivity = 2.5f;
            SaveSystem.Save(data);
            if (!File.Exists(path))
            {
                Debug.Log("Verify Fase 1 [Save]: FAIL — el archivo no se escribió.");
                return false;
            }
            SaveData loaded = SaveSystem.LoadFreshWithoutWriting();
            bool pass = loaded != null
                && Mathf.Approximately(loaded.masterVolume, 0.42f)
                && Mathf.Approximately(loaded.mouseSensitivity, 2.5f)
                && loaded.IsUnlocked(GameConfig.Mission1Scene);
            Debug.Log(pass
                ? "Verify Fase 1 [Save]: PASS — guardado escribe y lee bien."
                : "Verify Fase 1 [Save]: FAIL — roundtrip no coincide.");
            return pass;
        }
        finally
        {
            if (hadBackup)
            {
                File.WriteAllText(path, backup);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static bool CheckOnlyMission1Unlocked()
    {
        SaveData fresh = SaveData.NewDefault();
        bool pass = fresh.IsUnlocked(GameConfig.Mission1Scene)
            && !fresh.IsUnlocked(GameConfig.Mission2Scene)
            && !fresh.IsUnlocked(GameConfig.Mission3Scene);
        Debug.Log(pass
            ? "Verify Fase 1 [Unlocks]: PASS — solo Mision1 desbloqueada en guardado nuevo."
            : "Verify Fase 1 [Unlocks]: FAIL — el guardado nuevo no tiene solo Mision1.");
        return pass;
    }

    private static bool CheckBootInScenes()
    {
        bool pass = true;
        foreach (string s in GameConfig.AllScenes)
        {
            string path = "Assets/Scenes/" + s + ".unity";
            if (!File.Exists(path))
            {
                pass = false;
                continue;
            }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool hasManager = Object.FindAnyObjectByType<GameManager>() != null;
            bool hasLoader = Object.FindAnyObjectByType<SceneLoader>() != null;
            bool hasUI = Object.FindAnyObjectByType<MainMenuUI>() != null
                || Object.FindAnyObjectByType<MissionSelectUI>() != null
                || Object.FindAnyObjectByType<PauseMenuUI>() != null;
            if (!hasManager || !hasLoader || !hasUI)
            {
                pass = false;
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }
        Debug.Log(pass
            ? "Verify Fase 1 [Boot+UI]: PASS — GameManager, SceneLoader y UI presentes."
            : "Verify Fase 1 [Boot+UI]: FAIL — falta GameManager/SceneLoader/UI en alguna escena.");
        return pass;
    }
}
