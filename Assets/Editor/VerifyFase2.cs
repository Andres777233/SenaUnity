using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Popayork.Core;
using Popayork.Player;
using Popayork.UI;
using Popayork.Weapons;
using Popayork.World;

public static class VerifyFase2
{
    private const string ArenaPath = "Assets/Scenes/TestArena.unity";
    private const string GunsFbx = "Assets/Models3D/armas low poly 2/source/Guns.fbx";

    [MenuItem("Popayork/Verify Fase 2")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 2: PASS" : "Verify Fase 2: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyFase2.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Fase 2: PASS" : "Verify Fase 2: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Fase 2: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckArenaRegistered();
        ok &= CheckPlayerInArena();
        ok &= CheckWeaponsFromScriptableObjects();
        ok &= CheckFireAndReload();
        ok &= CheckDamageAndRespawn();
        ok &= CheckNoAllocPerFrame();
        return ok;
    }

    private static bool CheckArenaRegistered()
    {
        bool found = false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == ArenaPath && s.enabled)
            {
                found = true;
                break;
            }
        }
        bool fileExists = System.IO.File.Exists(ArenaPath);
        bool pass = found && fileExists;
        Debug.Log(pass
            ? "Verify Fase 2 [Arena]: PASS — TestArena en Build Settings y archivo existe."
            : "Verify Fase 2 [Arena]: FAIL — TestArena no registrada o sin archivo.");
        return pass;
    }

    private static PlayerController OpenArenaAndGetPlayer()
    {
        EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Single);
        var go = GameObject.Find("Player");
        if (go == null)
        {
            return null;
        }
        return go.GetComponent<PlayerController>();
    }

    private static bool CheckPlayerInArena()
    {
        PlayerController controller = OpenArenaAndGetPlayer();
        bool pass = controller != null
            && controller.GetComponent<PlayerHealth>() != null
            && controller.GetComponent<PlayerWeapons>() != null
            && controller.GetComponent<CharacterController>() != null
            && Object.FindAnyObjectByType<HUD>() != null
            && Object.FindAnyObjectByType<TargetDummy>() != null;
        Debug.Log(pass
            ? "Verify Fase 2 [Player]: PASS — jugador, HUD y dianas instanciados en TestArena."
            : "Verify Fase 2 [Player]: FAIL — falta jugador/HUD/dianas en TestArena.");
        return pass;
    }

    private static bool CheckWeaponsFromScriptableObjects()
    {
        WeaponData msr = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Config/Weapons/Fusil_MSR.asset");
        WeaponData be1 = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Config/Weapons/Subfusil_BE1.asset");
        bool pass = msr != null && be1 != null
            && msr.magazineSize > 0 && be1.magazineSize > 0
            && msr.shotsPerSecond > 0.0f && be1.shotsPerSecond > 0.0f
            && !string.IsNullOrEmpty(msr.modelRootName) && !string.IsNullOrEmpty(be1.modelRootName)
            && FbxHasChild(GunsFbx, msr.modelRootName) && FbxHasChild(GunsFbx, be1.modelRootName);
        Debug.Log(pass
            ? "Verify Fase 2 [WeaponData]: PASS — 2 armas SO válidas con modelos reales (MSR, BE1)."
            : "Verify Fase 2 [WeaponData]: FAIL — faltan SO o modelos en Guns.fbx.");
        return pass;
    }

    private static bool FbxHasChild(string fbxPath, string childName)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbx == null)
        {
            return false;
        }
        Transform[] all = fbx.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == childName)
            {
                return true;
            }
        }
        return false;
    }

    private static Weapon PrepareWeapon(out ProjectilePool pool)
    {
        pool = Object.FindAnyObjectByType<ProjectilePool>();
        var holder = Object.FindAnyObjectByType<PlayerWeapons>();
        Weapon weapon = holder != null ? holder.ActiveWeapon : null;
        if (pool != null)
        {
            pool.Build();
            pool.ReclaimAll();
        }
        if (weapon != null && weapon.Data != null)
        {
            weapon.Setup(weapon.Data, null, pool, null, null);
            weapon.RefillAll();
        }
        return weapon;
    }

    private static bool CheckFireAndReload()
    {
        OpenArenaAndGetPlayer();
        ProjectilePool pool;
        Weapon weapon = PrepareWeapon(out pool);
        if (weapon == null || pool == null)
        {
            Debug.Log("Verify Fase 2 [Fire]: FAIL — sin arma o pool.");
            return false;
        }
        int before = weapon.Magazine;
        Vector3 origin = new Vector3(0f, 1.5f, -20f);
        bool fired = weapon.TryFire(origin, Vector3.forward);
        bool consumed = fired && weapon.Magazine == before - 1;
        weapon.RefillAll();
        pool.ReclaimAll();
        bool restored = weapon.Magazine == weapon.Data.magazineSize;
        bool pass = consumed && restored;
        Debug.Log(pass
            ? "Verify Fase 2 [Fire]: PASS — disparar reduce munición (" + before + "->" + (before - 1) + ") y recargar restaura (" + weapon.Magazine + ")."
            : "Verify Fase 2 [Fire]: FAIL — munición no cuadra (fired=" + fired + " mag=" + weapon.Magazine + ").");
        return pass;
    }

    private static bool CheckDamageAndRespawn()
    {
        PlayerController controller = OpenArenaAndGetPlayer();
        if (controller == null)
        {
            Debug.Log("Verify Fase 2 [Damage]: FAIL — sin jugador.");
            return false;
        }
        var health = controller.GetComponent<PlayerHealth>();
        health.RespawnNow();
        Vector3 expectedSpawn = controller.transform.position;
        float max = health.MaxHealth;
        health.TakeDamage(30.0f, Vector3.forward);
        bool damaged = Mathf.Approximately(health.Health, max - 30.0f);
        health.TakeDamage(10000.0f, Vector3.forward);
        bool died = health.IsDead;
        Vector3 deathSpot = controller.transform.position;
        controller.transform.position = deathSpot + new Vector3(5f, 0f, 5f);
        health.RespawnNow();
        bool respawned = !health.IsDead
            && Mathf.Approximately(health.Health, max)
            && (controller.transform.position - expectedSpawn).sqrMagnitude < 0.0001f;
        bool pass = damaged && died && respawned;
        Debug.Log(pass
            ? "Verify Fase 2 [Damage]: PASS — daño, muerte y reaparición con vida llena."
            : "Verify Fase 2 [Damage]: FAIL — damaged=" + damaged + " died=" + died + " respawned=" + respawned + ".");
        return pass;
    }

    private static bool CheckNoAllocPerFrame()
    {
        PlayerController controller = OpenArenaAndGetPlayer();
        ProjectilePool pool;
        Weapon weapon = PrepareWeapon(out pool);
        if (controller == null || weapon == null)
        {
            Debug.Log("Verify Fase 2 [Alloc]: FAIL — sin jugador o arma.");
            return false;
        }
        // Calienta una vez (JIT/cachés) y luego mide 500 cuadros simulados.
        for (int i = 0; i < 5; i++)
        {
            controller.Simulate(0.016f, 0.5f, 1.0f, true, false, 0.3f, -0.2f, 1.0f);
            weapon.TryFire(Vector3.zero, Vector3.forward);
        }
        pool.ReclaimAll();
        weapon.RefillAll();
        System.GC.Collect();
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 500; i++)
        {
            controller.Simulate(0.016f, 0.5f, 1.0f, true, false, 0.3f, -0.2f, 1.0f);
            weapon.TryFire(Vector3.zero, Vector3.forward);
        }
        long after = System.GC.GetAllocatedBytesForCurrentThread();
        pool.ReclaimAll();
        weapon.RefillAll();
        bool pass = after == before;
        Debug.Log(pass
            ? "Verify Fase 2 [Alloc]: PASS — 0 bytes en 500 cuadros de movimiento+disparo."
            : "Verify Fase 2 [Alloc]: FAIL — " + (after - before) + " bytes asignados en 500 cuadros.");
        return pass;
    }
}
