using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Popayork.Missions;

public static class VerifyParque
{
    private const string ScenePath = "Assets/Scenes/Mision1.unity";
    private const string ConfigPath = "Assets/Config/ParqueCaldasConfig.asset";

    [MenuItem("Popayork/Verify Parque")]
    public static void RunFromMenu()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Parque: PASS" : "Verify Parque: FAIL");
    }

    // Punto de entrada para batch: -executeMethod VerifyParque.RunBatch
    public static void RunBatch()
    {
        bool ok = RunAll();
        Debug.Log(ok ? "Verify Parque: PASS" : "Verify Parque: FAIL");
        if (!ok)
        {
            Debug.LogError("Verify Parque: FAIL");
        }
    }

    private static bool RunAll()
    {
        bool ok = true;
        ok &= CheckCostados();
        ok &= CheckEstatua();
        ok &= CheckTorre();
        ok &= CheckMagenta();
        ok &= CheckConfig();
        ok &= CheckDetalle();
        ok &= CheckStaticStats();
        return ok;
    }

    private static void OpenPark()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static bool CheckCostados()
    {
        OpenPark();
        string[] lados = new string[] { "Costado_Norte", "Costado_Sur", "Costado_Oriente", "Costado_Occidente" };
        bool pass = true;
        for (int i = 0; i < lados.Length; i++)
        {
            if (GameObject.Find(lados[i]) == null)
            {
                pass = false;
            }
        }
        Debug.Log(pass
            ? "Verify Parque [Costados]: PASS — 4 costados con sus nombres."
            : "Verify Parque [Costados]: FAIL — falta algún costado.");
        return pass;
    }

    private static bool CheckEstatua()
    {
        OpenPark();
        GameObject est = GameObject.Find("EstatuaCaldas");
        bool pass = est != null
            && Mathf.Abs(est.transform.position.x) < 0.5f
            && Mathf.Abs(est.transform.position.z) < 0.5f;
        Debug.Log(pass
            ? "Verify Parque [Estatua]: PASS — estatua en el origen."
            : "Verify Parque [Estatua]: FAIL — fuera del origen o ausente.");
        return pass;
    }

    private static bool CheckTorre()
    {
        OpenPark();
        GameObject torre = GameObject.Find("TorreDelReloj");
        GameObject objetivo = GameObject.Find("Objetivo_Torre");
        GameObject spawn = GameObject.Find("Spawn_Oleada_0");
        if (torre == null || objetivo == null || spawn == null)
        {
            Debug.Log("Verify Parque [Torre]: FAIL — sin torre, objetivo o spawn.");
            return false;
        }
        Vector3 t = torre.transform.position;
        bool suroeste = t.x < -20f && t.x > -55f && t.z < -20f && t.z > -55f;
        var path = new NavMeshPath();
        bool ruta = NavMesh.CalculatePath(spawn.transform.position, objetivo.transform.position, NavMesh.AllAreas, path)
            && (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial);
        bool pass = suroeste && ruta;
        Debug.Log(pass
            ? "Verify Parque [Torre]: PASS — suroccidente (" + t.x.ToString("F0") + "," + t.z.ToString("F0") + ") y alcanzable (" + path.status + ")."
            : "Verify Parque [Torre]: FAIL — suroeste=" + suroeste + " ruta=" + ruta + ".");
        return pass;
    }

    private static bool CheckMagenta()
    {
        OpenPark();
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        Color magenta = new Color(1f, 0f, 1f, 1f);
        int revisados = 0;
        bool pass = true;
        for (int i = 0; i < roots.Length && pass; i++)
        {
            Renderer[] rs = roots[i].GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < rs.Length && pass; j++)
            {
                Material[] mats = rs[j].sharedMaterials;
                for (int k = 0; k < mats.Length; k++)
                {
                    if (mats[k] == null)
                    {
                        continue;
                    }
                    revisados++;
                    if (mats[k].color == magenta)
                    {
                        pass = false;
                    }
                }
            }
        }
        Debug.Log(pass
            ? "Verify Parque [Magenta]: PASS — " + revisados + " materiales, ninguno magenta."
            : "Verify Parque [Magenta]: FAIL — hay magenta (material perdido).");
        return pass;
    }

    private static bool CheckConfig()
    {
        ParqueCaldasConfig config = AssetDatabase.LoadAssetAtPath<ParqueCaldasConfig>(ConfigPath);
        if (config == null)
        {
            Debug.Log("Verify Parque [Config]: FAIL — sin ParqueCaldasConfig.");
            return false;
        }
        int doc = config.Contar(ValorFuente.Documentado);
        int med = config.Contar(ValorFuente.Medido);
        int est = config.Contar(ValorFuente.Estimado);
        bool pass = doc >= 3 && est >= 3;
        Debug.Log(pass
            ? "Verify Parque [Config]: PASS — DOCUMENTADO=" + doc + " MEDIDO=" + med + " ESTIMADO=" + est + "."
            : "Verify Parque [Config]: FAIL — doc=" + doc + " med=" + med + " est=" + est + ".");
        return pass;
    }

    private static bool CheckDetalle()
    {
        OpenPark();
        string[] obligatorios = new string[] {
            "Detalle_ArcoCamp_Izq", "Detalle_ArcoCamp_Der",
            "Detalle_Campana_Izq", "Detalle_Campana_Der",
            "Detalle_CruzCat_V", "Detalle_RelojCatedral",
            "Detalle_Moldura_1", "Detalle_Moldura_2", "Detalle_Moldura_3",
            "Detalle_RelojSur", "Detalle_CampanaTorre", "Detalle_CruzTorre_V",
            "Detalle_ZocaloTorre", "Calzada_N", "Calzada_S", "Calzada_E", "Calzada_O",
            "Acera_N", "Acera_S", "Acera_E", "Acera_O", "RosaVientos"
        };
        int faltan = 0;
        for (int i = 0; i < obligatorios.Length; i++)
        {
            if (GameObject.Find(obligatorios[i]) == null)
            {
                faltan++;
            }
        }
        Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        int cornisas = 0;
        int marcos = 0;
        int palomas = 0;
        for (int i = 0; i < todos.Length; i++)
        {
            string n = todos[i].name;
            if (n == "CornisaDetalle")
            {
                cornisas++;
            }
            else if (n.StartsWith("MarcoDetalle_"))
            {
                marcos++;
            }
            else if (n.StartsWith("Paloma_") || n.StartsWith("PalomaVuelo_"))
            {
                palomas++;
            }
        }
        bool pass = faltan == 0 && cornisas >= 8 && marcos >= 20 && palomas >= 8;
        Debug.Log(pass
            ? "Verify Parque [Detalle]: PASS — catedral, torre, calles, cornisas=" + cornisas + " marcos=" + marcos + " palomas=" + palomas + "."
            : "Verify Parque [Detalle]: FAIL — faltan=" + faltan + " cornisas=" + cornisas + " marcos=" + marcos + " palomas=" + palomas + ".");
        return pass;
    }

    private static bool CheckStaticStats()
    {
        OpenPark();
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        int renderers = 0;
        long tris = 0;
        for (int i = 0; i < roots.Length; i++)
        {
            Renderer[] rs = roots[i].GetComponentsInChildren<Renderer>(true);
            renderers += rs.Length;
            MeshFilter[] fs = roots[i].GetComponentsInChildren<MeshFilter>(true);
            for (int j = 0; j < fs.Length; j++)
            {
                if (fs[j].sharedMesh != null)
                {
                    tris += fs[j].sharedMesh.triangles.Length / 3;
                }
            }
        }
        Debug.Log("Verify Parque [Stats]: renderers=" + renderers + " tris=" + tris + " (FPS con render: medir en Editor).");
        return renderers > 0 && tris > 0;
    }
}
