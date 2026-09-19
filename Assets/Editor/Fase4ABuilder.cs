using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Popayork.Core;
using Popayork.Missions;
using Popayork.Player;
using Popayork.UI;
using Popayork.Weapons;

public static class Fase4ABuilder
{
    private const string CityFbx = "Assets/MapaPopayan/source/model.fbx";
    private const string ScenePath = "Assets/Scenes/Mision1.unity";
    private const string ConfigPath = "Assets/Config/ParqueConfig.asset";
    private const string PrefabDir = "Assets/Prefabs/Parque";

    private static readonly List<MedidaParque> medidas = new List<MedidaParque>();

    private static void M(string nombre, string valor, ValorFuente fuente, string detalle)
    {
        MedidaParque m = new MedidaParque();
        m.nombre = nombre;
        m.valor = valor;
        m.fuente = fuente;
        m.detalleFuente = detalle;
        medidas.Add(m);
    }

    [MenuItem("Popayork/Construir Fase 4A")]
    public static void BuildAll()
    {
        medidas.Clear();
        EnsureFolders();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureBoot();
        EnsureEventSystem();

        GameObject ciudad = BuildCityBase();
        Vector3 centro;
        float lado;
        float sueloY;
        FindParkSector(ciudad, out centro, out lado, out sueloY);

        BuildPlaza(centro, lado, sueloY);
        Vector3 torrePos = BuildTorre(centro, lado, sueloY);
        int arboles = BuildVegetacion(centro, lado, sueloY);
        int bancas = BuildBancas(centro, lado, sueloY);
        BuildEstatua(centro, sueloY);
        int farolas = BuildFarolas(centro, lado, sueloY);
        BuildFachadas(centro, lado, sueloY);
        BuildBoundaries(centro, sueloY);
        BuildSky();

        Vector3 spawnJugador = centro + new Vector3(0f, 0.5f, -lado * 0.42f);
        Vector3[] spawnAliados = new Vector3[] {
            spawnJugador + new Vector3(-3f, 0f, -1f),
            spawnJugador + new Vector3(3f, 0f, -1f) };
        Vector3[] spawnOleadas = new Vector3[] {
            centro + new Vector3(0f, 0.5f, lado * 0.48f),
            centro + new Vector3(-lado * 0.48f, 0.5f, 0f),
            centro + new Vector3(lado * 0.48f, 0.5f, 0f) };
        BuildMarkers(spawnJugador, spawnAliados, spawnOleadas, torrePos);
        BuildPlayer(spawnJugador);
        AddPauseMenu();
        UiLayout.FixSceneUI();

        SaveConfig(centro, lado, sueloY, torrePos, spawnJugador, spawnAliados, spawnOleadas, arboles, bancas, farolas);
        EditorSceneManager.SaveScene(scene, ScenePath);
        // El bake va en segunda pasada con la escena recién abierta: el bake sobre
        // colisionadores recién creados en la misma sesión sale vacío.
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        bool bakeOk = BakeNavMesh(centro, sueloY);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AppendBakeMeasure(bakeOk);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Fase 4A: PASS — Parque Caldas en Mision1 (" + medidas.Count + " medidas).");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            AssetDatabase.CreateFolder("Assets/Prefabs", "Parque");
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

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    // ---------- Ciudad base ----------

    private static GameObject BuildCityBase()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CityFbx);
        if (fbx == null)
        {
            Debug.LogError("Fase4A: no se encontró " + CityFbx);
            return new GameObject("CiudadBase");
        }
        GameObject ciudad = Object.Instantiate(fbx);
        ciudad.name = "CiudadBase";
        Renderer[] renderers = ciudad.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        Vector3 size = bounds.size;
        M("ciudad_ancho_m", size.x.ToString("F1"), ValorFuente.Medido, "Renderer.bounds del modelo model.fbx");
        M("ciudad_largo_m", size.z.ToString("F1"), ValorFuente.Medido, "Renderer.bounds del modelo model.fbx");
        M("ciudad_alto_m", size.y.ToString("F1"), ValorFuente.Medido, "Renderer.bounds del modelo model.fbx");
        MeshFilter[] filters = ciudad.GetComponentsInChildren<MeshFilter>();
        long verts = 0;
        long tris = 0;
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh != null)
            {
                verts += filters[i].sharedMesh.vertexCount;
                tris += filters[i].sharedMesh.triangles.Length / 3;
            }
        }
        M("ciudad_vertices", verts.ToString(), ValorFuente.Medido, "sharedMesh.vertexCount del modelo");
        M("ciudad_triangulos", tris.ToString(), ValorFuente.Medido, "sharedMesh.triangles del modelo");
        return ciudad;
    }

    // Busca por script el sector plano más grande cerca del centro (la plaza del parque).
    private static void FindParkSector(GameObject ciudad, out Vector3 centro, out float lado, out float sueloY)
    {
        MeshFilter[] filters = ciudad.GetComponentsInChildren<MeshFilter>();
        MeshCollider probe = ciudad.AddComponent<MeshCollider>();
        int bestCount = 0;
        Vector3 best = Vector3.zero;
        float bestY = 0f;
        for (int gx = -15; gx <= 15; gx++)
        {
            for (int gz = -15; gz <= 15; gz++)
            {
                Vector3 p = new Vector3(gx * 20f, 500f, gz * 20f);
                RaycastHit hit;
                if (!Physics.Raycast(p, Vector3.down, out hit, 2000f))
                {
                    continue;
                }
                int flat = 0;
                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oz = -1; oz <= 1; oz++)
                    {
                        RaycastHit h2;
                        if (Physics.Raycast(p + new Vector3(ox * 12f, 0f, oz * 12f), Vector3.down, out h2, 2000f)
                            && Mathf.Abs(h2.point.y - hit.point.y) < 1.5f)
                        {
                            flat++;
                        }
                    }
                }
                if (flat > bestCount)
                {
                    bestCount = flat;
                    best = hit.point;
                    bestY = hit.point.y;
                }
            }
        }
        Object.DestroyImmediate(probe);
        centro = new Vector3(best.x, bestY, best.z);
        lado = Mathf.Clamp(bestCount * 8f, 60f, 120f);
        sueloY = bestY;
        M("plaza_centro", centro.ToString("F1"), ValorFuente.Medido, "raycast 31x31 sobre el modelo, sector plano mayor");
        M("plaza_lado_m", lado.ToString("F1"), ValorFuente.Medido, "celdas planas contiguas x4m");
        M("suelo_altura_m", sueloY.ToString("F2"), ValorFuente.Medido, "raycast vertical sobre el modelo");
        M("textura_modelo", "model.jpg 2048px (original 16384px/66MB reducido con PIL)", ValorFuente.Medido, "Assets/MapaPopayan/source/model.jpg");
        // Filtros no usados aquí (medición por raycast); se evita warning de variable sin uso.
        if (filters.Length == 0)
        {
            Debug.LogWarning("Fase4A: la ciudad no trae mallas visibles.");
        }
    }

    // ---------- Construcción ----------

    private static Material Mat(Color color, float emission)
    {
        Material mat = MaterialFactory.New();
        mat.color = color;
        if (emission > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }
        return mat;
    }

    private static void BuildPlaza(Vector3 centro, float lado, float sueloY)
    {
        GameObject plaza = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plaza.name = "PlazaCaldas";
        plaza.transform.position = new Vector3(centro.x, sueloY - 0.25f, centro.z);
        plaza.transform.localScale = new Vector3(lado, 0.5f, lado);
        plaza.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.78f, 0.74f, 0.68f), 0f);
        M("plaza_losa_m", lado.ToString("F1") + "x" + lado.ToString("F1"), ValorFuente.Estimado, "diseño: cubre el sector medido");

        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "SueloEntorno";
        suelo.transform.position = new Vector3(centro.x, sueloY - 0.05f, centro.z);
        suelo.transform.localScale = new Vector3(40f, 1f, 40f);
        suelo.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.55f, 0.53f, 0.5f), 0f);
    }

    private static Vector3 BuildTorre(Vector3 centro, float lado, float sueloY)
    {
        Vector3 basePos = new Vector3(centro.x + lado * 0.32f, sueloY, centro.z + lado * 0.32f);
        GameObject torre = new GameObject("TorreDelReloj");
        torre.transform.position = basePos;
        Material blanco = Mat(new Color(0.94f, 0.93f, 0.9f), 0f);
        Material teja = Mat(new Color(0.55f, 0.25f, 0.15f), 0f);
        Material oscuro = Mat(new Color(0.1f, 0.1f, 0.12f), 0f);

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cuerpo.name = "Cuerpo";
        cuerpo.transform.SetParent(torre.transform, false);
        cuerpo.transform.localPosition = new Vector3(0f, 12f, 0f);
        cuerpo.transform.localScale = new Vector3(6f, 24f, 6f);
        cuerpo.GetComponent<MeshRenderer>().sharedMaterial = blanco;

        GameObject reloj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        reloj.name = "Reloj";
        reloj.transform.SetParent(torre.transform, false);
        reloj.transform.localPosition = new Vector3(0f, 20f, 3.05f);
        reloj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        reloj.transform.localScale = new Vector3(4f, 0.3f, 4f);
        reloj.GetComponent<MeshRenderer>().sharedMaterial = Mat(Color.white, 0.2f);
        Object.DestroyImmediate(reloj.GetComponent<CapsuleCollider>());

        GameObject manecilla = GameObject.CreatePrimitive(PrimitiveType.Cube);
        manecilla.name = "Manecilla";
        manecilla.transform.SetParent(torre.transform, false);
        manecilla.transform.localPosition = new Vector3(0f, 20f, 3.25f);
        manecilla.transform.localScale = new Vector3(0.25f, 1.6f, 0.1f);
        manecilla.GetComponent<MeshRenderer>().sharedMaterial = oscuro;
        Object.DestroyImmediate(manecilla.GetComponent<BoxCollider>());

        GameObject techo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        techo.name = "TechoTeja";
        techo.transform.SetParent(torre.transform, false);
        techo.transform.localPosition = new Vector3(0f, 26f, 0f);
        techo.transform.localScale = new Vector3(8f, 4f, 8f);
        var techoMesh = techo.GetComponent<MeshFilter>();
        if (techoMesh != null)
        {
            // Tejado piramidal low poly: cono de 4 lados.
            GameObject cono = new GameObject("Piramide");
            cono.transform.SetParent(torre.transform, false);
            cono.transform.localPosition = new Vector3(0f, 26f, 0f);
            MeshFilter mf = cono.AddComponent<MeshFilter>();
            mf.sharedMesh = BuildPyramidMesh(4.5f, 4f);
            MeshRenderer mr = cono.AddComponent<MeshRenderer>();
            mr.sharedMaterial = teja;
            Object.DestroyImmediate(techo);
        }
        else
        {
            techo.GetComponent<MeshRenderer>().sharedMaterial = teja;
        }

        GameObject arco = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arco.name = "Campanario";
        arco.transform.SetParent(torre.transform, false);
        arco.transform.localPosition = new Vector3(0f, 16f, 3.05f);
        arco.transform.localScale = new Vector3(2f, 3f, 0.3f);
        arco.GetComponent<MeshRenderer>().sharedMaterial = oscuro;
        Object.DestroyImmediate(arco.GetComponent<BoxCollider>());

        GameObject objetivo = new GameObject("Objetivo_Torre");
        objetivo.transform.SetParent(torre.transform, false);
        objetivo.transform.localPosition = new Vector3(0f, 1f, 4f);

        M("torre_alto_m", "28", ValorFuente.Estimado, "diseño low poly (referencia real pendiente de consulta web)");
        M("torre_base_m", "6x6", ValorFuente.Estimado, "diseño low poly");
        return objetivo.transform.position;
    }

    private static Mesh BuildPyramidMesh(float half, float height)
    {
        Mesh mesh = new Mesh();
        Vector3[] v = new Vector3[] {
            new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half),
            new Vector3(half, 0f, half), new Vector3(-half, 0f, half),
            new Vector3(0f, height, 0f) };
        mesh.vertices = v;
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3, 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0 };
        mesh.RecalculateNormals();
        return mesh;
    }

    private static GameObject SavePrefab(GameObject go, string name)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;
        }
        return PrefabUtility.SaveAsPrefabAsset(go, path);
    }

    private static int BuildVegetacion(Vector3 centro, float lado, float sueloY)
    {
        GameObject proto = new GameObject("ArbolProto");
        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tronco.name = "Tronco";
        tronco.transform.SetParent(proto.transform, false);
        tronco.transform.localPosition = new Vector3(0f, 1f, 0f);
        tronco.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
        tronco.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.4f, 0.28f, 0.18f), 0f);
        GameObject copa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        copa.name = "Copa";
        copa.transform.SetParent(proto.transform, false);
        copa.transform.localPosition = new Vector3(0f, 3.4f, 0f);
        copa.transform.localScale = new Vector3(2.4f, 2.6f, 2.4f);
        var copaFilter = copa.GetComponent<MeshFilter>();
        // Copa cónica low poly: reduzco segmentos del cilindro a 7.
        Mesh cono = new Mesh();
        int seg = 7;
        Vector3[] verts = new Vector3[seg * 2 + 2];
        for (int i = 0; i < seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            verts[i] = new Vector3(Mathf.Cos(a) * 1.2f, 0f, Mathf.Sin(a) * 1.2f);
            verts[seg + i] = new Vector3(Mathf.Cos(a) * 1.2f, 2.4f, Mathf.Sin(a) * 1.2f);
        }
        verts[seg * 2] = new Vector3(0f, 0f, 0f);
        verts[seg * 2 + 1] = new Vector3(0f, 3.6f, 0f);
        List<int> tris = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            int nx = (i + 1) % seg;
            tris.Add(seg * 2); tris.Add(nx); tris.Add(i);
            tris.Add(seg + i); tris.Add(seg + nx); tris.Add(seg * 2 + 1);
            tris.Add(i); tris.Add(nx); tris.Add(seg + nx);
            tris.Add(i); tris.Add(seg + nx); tris.Add(seg + i);
        }
        cono.vertices = verts;
        cono.triangles = tris.ToArray();
        cono.RecalculateNormals();
        copaFilter.sharedMesh = cono;
        copa.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.2f, 0.5f, 0.25f), 0f);
        Object.DestroyImmediate(copa.GetComponent<CapsuleCollider>());
        GameObject prefab = SavePrefab(proto, "Arbol");
        Object.DestroyImmediate(proto);

        int n = 0;
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24f;
            float r = lado * 0.5f + 6f + (i % 3) * 4f;
            Vector3 p = centro + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            p.y = sueloY;
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tree.transform.position = p;
            tree.transform.rotation = Quaternion.Euler(0f, i * 37f, 0f);
            float s = 0.9f + (i % 4) * 0.15f;
            tree.transform.localScale = Vector3.one * s;
            n++;
        }
        M("arboles_cantidad", n.ToString(), ValorFuente.Medido, "instancias del prefab Arbol generadas por script");
        M("arbol_alto_m", "4.5", ValorFuente.Estimado, "diseño low poly del prefab");
        return n;
    }

    private static int BuildBancas(Vector3 centro, float lado, float sueloY)
    {
        GameObject proto = new GameObject("BancaProto");
        Material madera = Mat(new Color(0.45f, 0.3f, 0.18f), 0f);
        GameObject asiento = GameObject.CreatePrimitive(PrimitiveType.Cube);
        asiento.transform.SetParent(proto.transform, false);
        asiento.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        asiento.transform.localScale = new Vector3(1.8f, 0.1f, 0.5f);
        asiento.GetComponent<MeshRenderer>().sharedMaterial = madera;
        GameObject respaldo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        respaldo.transform.SetParent(proto.transform, false);
        respaldo.transform.localPosition = new Vector3(0f, 0.85f, -0.25f);
        respaldo.transform.localScale = new Vector3(1.8f, 0.5f, 0.1f);
        respaldo.GetComponent<MeshRenderer>().sharedMaterial = madera;
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject pata = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pata.transform.SetParent(proto.transform, false);
            pata.transform.localPosition = new Vector3(i * 0.75f, 0.2f, 0f);
            pata.transform.localScale = new Vector3(0.1f, 0.4f, 0.45f);
            pata.GetComponent<MeshRenderer>().sharedMaterial = madera;
        }
        GameObject prefab = SavePrefab(proto, "Banca");
        Object.DestroyImmediate(proto);

        int n = 0;
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            float r = lado * 0.28f;
            Vector3 p = centro + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            p.y = sueloY;
            GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            b.transform.position = p;
            b.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
            n++;
        }
        M("bancas_cantidad", n.ToString(), ValorFuente.Medido, "instancias del prefab Banca generadas por script");
        M("banca_largo_m", "1.8", ValorFuente.Estimado, "diseño low poly del prefab");
        return n;
    }

    private static void BuildEstatua(Vector3 centro, float sueloY)
    {
        GameObject estatua = new GameObject("EstatuaProcer");
        estatua.transform.position = new Vector3(centro.x, sueloY, centro.z);
        Material piedra = Mat(new Color(0.6f, 0.6f, 0.58f), 0f);
        GameObject base1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        base1.transform.SetParent(estatua.transform, false);
        base1.transform.localPosition = new Vector3(0f, 1f, 0f);
        base1.transform.localScale = new Vector3(2.4f, 2f, 2.4f);
        base1.GetComponent<MeshRenderer>().sharedMaterial = piedra;
        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cuerpo.transform.SetParent(estatua.transform, false);
        cuerpo.transform.localPosition = new Vector3(0f, 3.4f, 0f);
        cuerpo.transform.localScale = new Vector3(1f, 1.6f, 1f);
        cuerpo.GetComponent<MeshRenderer>().sharedMaterial = piedra;
        Object.DestroyImmediate(cuerpo.GetComponent<CapsuleCollider>());
        GameObject cabeza = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cabeza.transform.SetParent(estatua.transform, false);
        cabeza.transform.localPosition = new Vector3(0f, 4.9f, 0f);
        cabeza.transform.localScale = Vector3.one * 0.55f;
        cabeza.GetComponent<MeshRenderer>().sharedMaterial = piedra;
        Object.DestroyImmediate(cabeza.GetComponent<SphereCollider>());
        SavePrefab(estatua, "Estatua");
        M("estatua_alto_m", "5.2", ValorFuente.Estimado, "diseño low poly del prefab");
    }

    private static int BuildFarolas(Vector3 centro, float lado, float sueloY)
    {
        GameObject proto = new GameObject("FarolaProto");
        Material hierro = Mat(new Color(0.12f, 0.12f, 0.14f), 0f);
        GameObject poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poste.transform.SetParent(proto.transform, false);
        poste.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        poste.transform.localScale = new Vector3(0.25f, 5f, 0.25f);
        poste.GetComponent<MeshRenderer>().sharedMaterial = hierro;
        GameObject lampara = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampara.transform.SetParent(proto.transform, false);
        lampara.transform.localPosition = new Vector3(0f, 5.2f, 0f);
        lampara.transform.localScale = Vector3.one * 0.5f;
        lampara.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(1f, 0.85f, 0.6f), 0.8f);
        Object.DestroyImmediate(lampara.GetComponent<SphereCollider>());
        GameObject prefab = SavePrefab(proto, "Farola");
        Object.DestroyImmediate(proto);

        int n = 0;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f + Mathf.PI / 8f;
            float r = lado * 0.4f;
            Vector3 p = centro + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            p.y = sueloY;
            GameObject f = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            f.transform.position = p;
            n++;
        }
        M("farolas_cantidad", n.ToString(), ValorFuente.Medido, "instancias del prefab Farola generadas por script");
        M("farola_alto_m", "5.2", ValorFuente.Estimado, "diseño low poly del prefab");
        return n;
    }

    private static void BuildFachadas(Vector3 centro, float lado, float sueloY)
    {
        Material blanco = Mat(new Color(0.93f, 0.92f, 0.88f), 0f);
        Material teja = Mat(new Color(0.5f, 0.24f, 0.14f), 0f);
        float d = lado * 0.5f + 14f;
        Vector3[] centros = new Vector3[] {
            centro + new Vector3(0f, 0f, -d), centro + new Vector3(0f, 0f, d),
            centro + new Vector3(-d, 0f, 0f), centro + new Vector3(d, 0f, 0f) };
        for (int i = 0; i < centros.Length; i++)
        {
            GameObject f = new GameObject("Fachada_" + i);
            bool horizontal = i < 2;
            f.transform.position = new Vector3(centros[i].x, sueloY, centros[i].z);
            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cuerpo.name = "Cuerpo";
            cuerpo.transform.SetParent(f.transform, false);
            cuerpo.transform.localPosition = new Vector3(0f, 4f, 0f);
            cuerpo.transform.localScale = horizontal ? new Vector3(60f, 8f, 10f) : new Vector3(10f, 8f, 60f);
            cuerpo.GetComponent<MeshRenderer>().sharedMaterial = blanco;
            GameObject techo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            techo.name = "Techo";
            techo.transform.SetParent(f.transform, false);
            techo.transform.localPosition = new Vector3(0f, 8.6f, 0f);
            techo.transform.localRotation = Quaternion.Euler(0f, 0f, horizontal ? 0f : 90f);
            techo.transform.localScale = new Vector3(62f, 0.6f, 12f);
            techo.GetComponent<MeshRenderer>().sharedMaterial = teja;
        }
        M("fachadas_cantidad", "4", ValorFuente.Medido, "manzanas coloniales generadas por script con colisión");
        M("fachada_alto_m", "8", ValorFuente.Estimado, "diseño: 2 pisos coloniales");
    }

    private static void BuildBoundaries(Vector3 centro, float sueloY)
    {
        float half = 75f;
        Vector3[] pos = new Vector3[] {
            centro + new Vector3(0f, 0f, -half), centro + new Vector3(0f, 0f, half),
            centro + new Vector3(-half, 0f, 0f), centro + new Vector3(half, 0f, 0f) };
        for (int i = 0; i < pos.Length; i++)
        {
            GameObject w = new GameObject("Limite_" + i);
            w.transform.position = new Vector3(pos[i].x, sueloY + 5f, pos[i].z);
            BoxCollider box = w.AddComponent<BoxCollider>();
            box.size = i < 2 ? new Vector3(half * 2f, 20f, 2f) : new Vector3(2f, 20f, half * 2f);
        }
        M("limite_medio_lado_m", half.ToString("F0"), ValorFuente.Estimado, "diseño: muros invisibles anticaída");
    }

    private static void BuildSky()
    {
        GameObject sun = new GameObject("SolAtardecer");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.75f, 0.55f);
        light.intensity = 1.1f;
        sun.transform.rotation = Quaternion.Euler(18f, -115f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.55f, 0.52f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.96f, 0.72f, 0.55f);
        RenderSettings.fogDensity = 0.004f;
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.98f, 0.7f, 0.5f);
        }
        M("niebla_densidad", "0.004", ValorFuente.Estimado, "diseño: niebla ligera cálida");
        M("sol_elevacion_grados", "18", ValorFuente.Estimado, "diseño: atardecer");
    }

    // ---------- Actores y sistemas ----------

    private static void BuildMarkers(Vector3 spawnJugador, Vector3[] aliados, Vector3[] oleadas, Vector3 torre)
    {
        CreateMarker("Spawn_Jugador", spawnJugador);
        for (int i = 0; i < aliados.Length; i++)
        {
            CreateMarker("Spawn_Aliado_" + i, aliados[i]);
        }
        for (int i = 0; i < oleadas.Length; i++)
        {
            CreateMarker("Spawn_Oleada_" + i, oleadas[i]);
        }
        CreateMarker("Objetivo_Torre_Plaza", torre);
    }

    private static void CreateMarker(string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        go.transform.position = pos;
    }

    private static void BuildPlayer(Vector3 spawn)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("Fase4A: sin prefab de jugador.");
            return;
        }
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player";
        }
        player.transform.position = spawn;
        player.transform.rotation = Quaternion.identity;
        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.SetSpawn(spawn, 0f);
        }
        EnsureAudioListener(player);
        PatchTestArenaListener();
        GameObject pools = GameObject.Find("CombatPools");
        if (pools == null)
        {
            pools = new GameObject("CombatPools");
            pools.AddComponent<ProjectilePool>();
            pools.AddComponent<ImpactPool>();
        }
    }

    private static void EnsureAudioListener(GameObject player)
    {
        if (player == null)
        {
            return;
        }
        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam != null && cam.GetComponent<AudioListener>() == null)
        {
            cam.gameObject.AddComponent<AudioListener>();
        }
    }

    private static void PatchTestArenaListener()
    {
        Scene arena = EditorSceneManager.OpenScene("Assets/Scenes/TestArena.unity", OpenSceneMode.Additive);
        GameObject[] roots = arena.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "Player")
            {
                EnsureAudioListener(roots[i]);
            }
        }
        EditorSceneManager.SaveScene(arena, "Assets/Scenes/TestArena.unity");
        EditorSceneManager.CloseScene(arena, true);
    }

    private static void AddPauseMenu()
    {
        if (Object.FindAnyObjectByType<PauseMenuUI>() != null)
        {
            return;
        }
        GameObject canvasGo = new GameObject("PauseCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        var ui = canvasGo.AddComponent<PauseMenuUI>();
        GameObject panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvasGo.transform, false);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 420f);
        ui.Bind(panel, null, null, null);
        panel.SetActive(false);
    }

    private static bool BakeNavMesh(Vector3 centro, float sueloY)
    {
        GameObject bakeGo = GameObject.Find("NavMeshBake");
        if (bakeGo == null)
        {
            bakeGo = new GameObject("NavMeshBake");
        }
        NavMeshSurface surface = bakeGo.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = bakeGo.AddComponent<NavMeshSurface>();
        }
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.center = new Vector3(centro.x, sueloY + 2f, centro.z);
        surface.size = new Vector3(170f, 12f, 170f);
        surface.defaultArea = 0;
        surface.BuildNavMesh();
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Debug.Log("Fase4A NavMesh: triangulacion verts=" + tri.vertices.Length + " indices=" + tri.indices.Length + ".");
        Vector3 a = new Vector3(centro.x, sueloY + 0.3f, centro.z - 40f);
        Vector3 b = new Vector3(centro.x + 19f, sueloY + 0.3f, centro.z + 19f);
        var path = new NavMeshPath();
        bool ok = NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path)
            && (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial);
        Debug.Log(ok ? "Fase4A NavMesh: PASS — bake de la zona del parque." : "Fase4A NavMesh: FAIL — sin ruta.");
        return ok;
    }

    private static void AppendBakeMeasure(bool ok)
    {
        ParqueConfig config = AssetDatabase.LoadAssetAtPath<ParqueConfig>(ConfigPath);
        if (config == null)
        {
            return;
        }
        MedidaParque m = new MedidaParque();
        m.nombre = "navmesh_bake_ok";
        m.valor = ok ? "true" : "false";
        m.fuente = ValorFuente.Medido;
        m.detalleFuente = "ruta de prueba tras BuildNavMesh por script";
        List<MedidaParque> lista = new List<MedidaParque>(config.medidas);
        // Evita duplicados entre re-ejecuciones.
        for (int i = lista.Count - 1; i >= 0; i--)
        {
            if (lista[i].nombre == "navmesh_bake_ok")
            {
                lista.RemoveAt(i);
            }
        }
        lista.Add(m);
        config.medidas = lista.ToArray();
        EditorUtility.SetDirty(config);
    }

    private static void SaveConfig(Vector3 centro, float lado, float sueloY, Vector3 torre, Vector3 spawnJugador, Vector3[] aliados, Vector3[] oleadas, int arboles, int bancas, int farolas)
    {
        ParqueConfig config = AssetDatabase.LoadAssetAtPath<ParqueConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<ParqueConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }
        config.medidas = medidas.ToArray();
        config.centroPlaza = centro;
        config.ladoPlaza = lado;
        config.alturaSuelo = sueloY;
        config.posicionTorre = torre;
        config.spawnJugador = spawnJugador;
        config.spawnAliados = aliados;
        config.spawnOleadas = oleadas;
        config.objetivoTorre = torre;
        EditorUtility.SetDirty(config);
        int med = 0;
        int est = 0;
        for (int i = 0; i < medidas.Count; i++)
        {
            if (medidas[i].fuente == ValorFuente.Medido)
            {
                med++;
            }
            else
            {
                est++;
            }
        }
        Debug.Log("Fase4A config: MEDIDO=" + med + " ESTIMADO=" + est + ".");
    }
}
