using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Popayork.Core;
using Popayork.Missions;
using Popayork.Player;

public static class ParqueCaldasBuilder
{
    private const string ScenePath = "Assets/Scenes/Mision1.unity";
    private const string ConfigPath = "Assets/Config/ParqueCaldasConfig.asset";
    private const string VegPath = "Assets/Config/ParqueVegetacion.asset";
    private const string PrefabDir = "Assets/Prefabs/ParqueCaldas";

    // Losa a max MEDIDO (71.7) + margen. Origen XZ = base de la estatua.
    private const float SlabTop = 72.0f;

    private static readonly List<MedidaParque> medidas = new List<MedidaParque>();
    private static Material[] paleta;

    private static void M(string nombre, string valor, ValorFuente fuente, string detalle)
    {
        MedidaParque m = new MedidaParque();
        m.nombre = nombre;
        m.valor = valor;
        m.fuente = fuente;
        m.detalleFuente = detalle;
        medidas.Add(m);
    }

    [MenuItem("Popayork/Construir Parque Caldas")]
    public static void BuildAll()
    {
        medidas.Clear();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildPalette();
        CleanupOldPark();

        Vector3 origen = new Vector3(0f, SlabTop, 0f);
        BuildGround(origen);
        BuildEstatua(origen);
        BuildPila(origen);
        BuildVegetationPrefabs();
        ParqueVegetacion veg = BuildVegetationSO(origen);
        SpawnVegetation(veg);
        BuildFurniture(origen);
        BuildSides(origen);
        Vector3 torreBase = BuildTorre(origen);
        RepositionGameplay(origen, torreBase);

        SaveConfig(origen, torreBase);
        ParqueCaldasDetalle.Aplicar();
        UiLayout.FixSceneUI();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Rebake();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Construir Parque Caldas: PASS — " + medidas.Count + " medidas.");
    }

    // ---------- Paleta (máximo 8 colores base) ----------

    private static void BuildPalette()
    {
        Color[] colores = new Color[] {
            new Color(0.96f, 0.94f, 0.89f), // 0 blanco cálido
            new Color(0.60f, 0.34f, 0.21f), // 1 teja terracota
            new Color(0.55f, 0.55f, 0.58f), // 2 gris piedra
            new Color(0.24f, 0.48f, 0.20f), // 3 verde hoja
            new Color(0.16f, 0.35f, 0.16f), // 4 verde oscuro
            new Color(0.13f, 0.13f, 0.15f), // 5 hierro oscuro
            new Color(0.91f, 0.73f, 0.23f), // 6 amarillo guayacán
            new Color(0.48f, 0.32f, 0.19f)  // 7 madera
        };
        paleta = new Material[colores.Length];
        for (int i = 0; i < colores.Length; i++)
        {
            Material mat = MaterialFactory.New();
            mat.color = colores[i];
            mat.SetFloat("_Glossiness", 0f);
            paleta[i] = mat;
        }
        M("paleta_colores", "8", ValorFuente.Estimado, "diseño: blanco, teja, gris, 2 verdes, hierro, amarillo, madera");
    }

    private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, int mat, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = paleta[mat];
        if (!collider)
        {
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        }
        return go;
    }

    private static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 scale, int mat, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = paleta[mat];
        if (!collider)
        {
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        }
        return go;
    }

    private static GameObject Ball(Transform parent, string name, Vector3 pos, float size, int mat, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<MeshRenderer>().sharedMaterial = paleta[mat];
        if (!collider)
        {
            Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        }
        return go;
    }

    // ---------- Limpieza (solo objetos generados por builders) ----------

    private static void CleanupOldPark()
    {
        GameObject objetivo = GameObject.Find("Objetivo_Torre");
        if (objetivo != null)
        {
            objetivo.transform.SetParent(null, true);
        }
        string[] exactos = new string[] {
            "PlazaCaldas", "SueloEntorno", "TorreDelReloj",
            "EstatuaProcer", "Arbol", "Banca", "Farola", "SolAtardecer",
            "EstatuaCaldas", "PilaAgua",
            "Costado_Norte", "Costado_Sur", "Costado_Oriente", "Costado_Occidente", "PanteonFondo",
            "BancaHierro", "FarolPlaza", "Jardinera", "Papelera", "Seto",
            "Objetivo_Torre_Plaza",
            "Arbol_Araucaria", "Arbol_Guayacan", "Arbol_Corcho", "Arbol_FlorMayo", "Arbol_Mango" };
        string[] prefijos = new string[] { "Fachada_", "Limite_", "CoverPoint_" };
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                continue;
            }
            string n = all[i].name;
            bool borrar = false;
            for (int e = 0; e < exactos.Length && !borrar; e++)
            {
                if (n == exactos[e])
                {
                    borrar = true;
                }
            }
            for (int p = 0; p < prefijos.Length && !borrar; p++)
            {
                if (n.StartsWith(prefijos[p]))
                {
                    borrar = true;
                }
            }
            if (borrar && all[i].gameObject != null)
            {
                Object.DestroyImmediate(all[i].gameObject);
            }
        }
    }

    // ---------- Suelo ----------

    private static void BuildGround(Vector3 origen)
    {
        GameObject suelo = new GameObject("SueloPlaza");
        suelo.transform.position = origen;
        Box(suelo.transform, "LosaBase", new Vector3(0f, SlabTop - 0.5f, 0f), new Vector3(130f, 1f, 130f), 2, true);
        // Senderos blancos: cruz + anillo r=12 + 4 radiales.
        Box(suelo.transform, "SenderoNS", new Vector3(0f, SlabTop + 0.06f, 0f), new Vector3(3f, 0.12f, 80f), 0, false);
        Box(suelo.transform, "SenderoEO", new Vector3(0f, SlabTop + 0.06f, 0f), new Vector3(80f, 0.12f, 3f), 0, false);
        BuildRing(suelo.transform, 12f, 3f);
        for (int i = 0; i < 4; i++)
        {
            float a = 45f + i * 90f;
            float rad = a * Mathf.Deg2Rad;
            GameObject s = Box(suelo.transform, "Radial_" + i, new Vector3(Mathf.Cos(rad) * 24f, SlabTop + 0.06f, Mathf.Sin(rad) * 24f), new Vector3(3f, 0.12f, 26f), 0, false);
            s.transform.rotation = Quaternion.Euler(0f, -a + 90f, 0f);
        }
        // Zonas verdes en cuadrantes.
        for (int i = 0; i < 4; i++)
        {
            float sx = (i % 2 == 0 ? 1f : -1f) * 22f;
            float sz = (i < 2 ? 1f : -1f) * 22f;
            Box(suelo.transform, "ZonaVerde_" + i, new Vector3(sx, SlabTop + 0.05f, sz), new Vector3(26f, 0.1f, 26f), 3, false);
        }
        M("plaza_lado_m", "80", ValorFuente.Estimado, "especificación: plaza cuadrada entre fachadas");
        M("calles_ancho_m", "10", ValorFuente.Estimado, "especificación");
        M("senderos_ancho_m", "3", ValorFuente.Estimado, "especificación");
        M("anillo_radio_m", "12", ValorFuente.Estimado, "especificación: sendero circular");
        M("losa_top_m", SlabTop.ToString("F1"), ValorFuente.Estimado, "diseño sobre max MEDIDO 71.7 en el origen");
    }

    private static void BuildRing(Transform parent, float radius, float width)
    {
        int seg = 24;
        Vector3[] verts = new Vector3[(seg + 1) * 2];
        List<int> tris = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            verts[i * 2] = new Vector3(c * (radius - width / 2f), SlabTop + 0.06f, s * (radius - width / 2f));
            verts[i * 2 + 1] = new Vector3(c * (radius + width / 2f), SlabTop + 0.06f, s * (radius + width / 2f));
            if (i < seg)
            {
                int b = i * 2;
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
            }
        }
        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        GameObject go = new GameObject("Anillo");
        go.transform.SetParent(parent, false);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = paleta[0];
    }

    // ---------- Estatua y pila ----------

    private static void BuildEstatua(Vector3 origen)
    {
        GameObject est = new GameObject("EstatuaCaldas");
        est.transform.position = origen;
        Box(est.transform, "Pedestal", new Vector3(0f, SlabTop + 1.5f, 0f), new Vector3(2f, 3f, 2f), 2, true);
        Box(est.transform, "PlacaFirma", new Vector3(0f, SlabTop + 1.6f, 1.02f), new Vector3(1f, 0.5f, 0.06f), 5, false);
        Box(est.transform, "PlacaPlanta", new Vector3(1.02f, SlabTop + 1.2f, 0f), new Vector3(0.06f, 0.5f, 1f), 5, false);
        Box(est.transform, "Octante", new Vector3(-0.8f, SlabTop + 3.2f, 0.6f), new Vector3(0.3f, 0.5f, 0.15f), 5, false);
        // Figura ~3 m en hierro oscuro.
        Box(est.transform, "Piernas", new Vector3(0f, SlabTop + 3.7f, 0f), new Vector3(0.7f, 1.4f, 0.5f), 5, false);
        Box(est.transform, "Torso", new Vector3(0f, SlabTop + 4.8f, 0f), new Vector3(0.9f, 1.1f, 0.6f), 5, false);
        Ball(est.transform, "Cabeza", new Vector3(0f, SlabTop + 5.6f, 0f), 0.55f, 5, false);
        GameObject brazo = Box(est.transform, "Brazo", new Vector3(0.7f, SlabTop + 5f, 0.2f), new Vector3(0.9f, 0.25f, 0.25f), 5, false);
        brazo.transform.rotation = Quaternion.Euler(0f, 0f, -30f);
        M("estatua_origen", "(0, 72, 0)", ValorFuente.Documentado, "especificación: origen en la base de la estatua");
        M("estatua_medidas_m", "pedestal 3 + figura 3", ValorFuente.Estimado, "especificación: unos 3 m cada uno");
        M("estatua_autor", "Verlet 1910", ValorFuente.Documentado, "especificación");
    }

    private static void BuildPila(Vector3 origen)
    {
        GameObject pila = new GameObject("PilaAgua");
        pila.transform.position = new Vector3(12f, SlabTop, -8f);
        Cyl(pila.transform, "Vaso", new Vector3(0f, 0.45f, 0f), new Vector3(5f, 0.9f, 5f), 2, true);
        GameObject agua = Cyl(pila.transform, "Agua", new Vector3(0f, 0.8f, 0f), new Vector3(4.4f, 0.15f, 4.4f), 2, false);
        Material aguaMat = MaterialFactory.New();
        aguaMat.color = new Color(0.55f, 0.55f, 0.58f, 0.6f);
        agua.GetComponent<MeshRenderer>().sharedMaterial = aguaMat;
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f;
            Box(pila.transform, "Asiento_" + i, new Vector3(Mathf.Cos(a) * 4.5f, 0.25f, Mathf.Sin(a) * 4.5f), new Vector3(1.2f, 0.5f, 0.6f), 2, true);
        }
        M("pila_posicion", "(12, -8) relativo al origen", ValorFuente.Estimado, "especificación: posición exacta ESTIMADO");
    }

    // ---------- Vegetación (5 tipos + setos, por ScriptableObject) ----------

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

    private static void BuildVegetationPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            AssetDatabase.CreateFolder("Assets/Prefabs", "ParqueCaldas");
        }
        // Araucaria: tronco alto + 3 conos escalonados (20-30 m).
        GameObject ara = new GameObject("Arbol_Araucaria");
        Cyl(ara.transform, "Tronco", new Vector3(0f, 6f, 0f), new Vector3(0.8f, 12f, 0.8f), 7, true);
        Cyl(ara.transform, "Cono1", new Vector3(0f, 13f, 0f), new Vector3(7f, 4f, 7f), 4, false);
        Cyl(ara.transform, "Cono2", new Vector3(0f, 17f, 0f), new Vector3(5f, 4f, 5f), 4, false);
        Cyl(ara.transform, "Cono3", new Vector3(0f, 21f, 0f), new Vector3(3f, 4f, 3f), 4, false);
        SavePrefab(ara, "Arbol_Araucaria");
        Object.DestroyImmediate(ara);
        // Guayacán: copa redondeada con puntos amarillos.
        GameObject gua = new GameObject("Arbol_Guayacan");
        Cyl(gua.transform, "Tronco", new Vector3(0f, 2f, 0f), new Vector3(0.7f, 4f, 0.7f), 7, true);
        Ball(gua.transform, "Copa", new Vector3(0f, 5.5f, 0f), 5f, 3, false);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            Ball(gua.transform, "Flor_" + i, new Vector3(Mathf.Cos(a) * 2.2f, 6.2f, Mathf.Sin(a) * 2.2f), 0.5f, 6, false);
        }
        SavePrefab(gua, "Arbol_Guayacan");
        Object.DestroyImmediate(gua);
        // Corcho: copa ancha irregular (3 esferas aplanadas).
        GameObject cor = new GameObject("Arbol_Corcho");
        Cyl(cor.transform, "Tronco", new Vector3(0f, 2.5f, 0f), new Vector3(0.9f, 5f, 0.9f), 7, true);
        Ball(cor.transform, "CopaA", new Vector3(-1.5f, 6f, 0f), 4f, 4, false);
        Ball(cor.transform, "CopaB", new Vector3(1.5f, 6.5f, 0.5f), 3.4f, 3, false);
        Ball(cor.transform, "CopaC", new Vector3(0f, 5.5f, -1.5f), 3f, 4, false);
        SavePrefab(cor, "Arbol_Corcho");
        Object.DestroyImmediate(cor);
        // Flor de mayo: copa ancha clara.
        GameObject flo = new GameObject("Arbol_FlorMayo");
        Cyl(flo.transform, "Tronco", new Vector3(0f, 2f, 0f), new Vector3(0.6f, 4f, 0.6f), 7, true);
        Ball(flo.transform, "CopaA", new Vector3(-1.2f, 5f, 0f), 3.6f, 3, false);
        Ball(flo.transform, "CopaB", new Vector3(1.2f, 5.4f, 0f), 3.2f, 3, false);
        Ball(flo.transform, "Flor", new Vector3(0f, 6.4f, 0f), 1f, 6, false);
        SavePrefab(flo, "Arbol_FlorMayo");
        Object.DestroyImmediate(flo);
        // Mango: copa densa baja (madroños = versión 0.7, arrayán = arbusto).
        GameObject man = new GameObject("Arbol_Mango");
        Cyl(man.transform, "Tronco", new Vector3(0f, 1.2f, 0f), new Vector3(0.7f, 2.4f, 0.7f), 7, true);
        Ball(man.transform, "Copa", new Vector3(0f, 3.2f, 0f), 4.4f, 4, false);
        SavePrefab(man, "Arbol_Mango");
        Object.DestroyImmediate(man);
        // Seto: caja verde baja.
        GameObject seto = new GameObject("Seto");
        Box(seto.transform, "Mata", Vector3.zero, new Vector3(2f, 0.8f, 1f), 4, false);
        SavePrefab(seto, "Seto");
        Object.DestroyImmediate(seto);
        M("vegetacion_especies", "araucaria, guayacán, corcho, flor de mayo, mango/madroño/arrayán", ValorFuente.Documentado, "especificación");
        M("vegetacion_formas", "conos apilados, copa+flores, copas irregulares, copa densa, cajas", ValorFuente.Estimado, "especificación: formas ESTIMADO");
    }

    private static ParqueVegetacion BuildVegetationSO(Vector3 origen)
    {
        List<ArbolPuesto> lista = new List<ArbolPuesto>();
        AddArbol(lista, ArbolTipo.Araucaria, new Vector3(26f, 0f, 26f), 15f, 1f);
        AddArbol(lista, ArbolTipo.Araucaria, new Vector3(-27f, 0f, -24f), 140f, 1.15f);
        AddArbol(lista, ArbolTipo.Guayacan, new Vector3(12f, 0f, 4f), 30f, 1f);
        AddArbol(lista, ArbolTipo.Guayacan, new Vector3(-5f, 0f, -12f), 200f, 0.9f);
        AddArbol(lista, ArbolTipo.Guayacan, new Vector3(-11f, 0f, 7f), 320f, 1.1f);
        AddArbol(lista, ArbolTipo.Corcho, new Vector3(19f, 0f, -19f), 75f, 1f);
        AddArbol(lista, ArbolTipo.Corcho, new Vector3(-19f, 0f, 19f), 250f, 1.05f);
        AddArbol(lista, ArbolTipo.FlorDeMayo, new Vector3(7f, 0f, -20f), 100f, 1f);
        AddArbol(lista, ArbolTipo.FlorDeMayo, new Vector3(-7f, 0f, 21f), 280f, 0.95f);
        AddArbol(lista, ArbolTipo.Mango, new Vector3(-21f, 0f, 14f), 160f, 1f);
        AddArbol(lista, ArbolTipo.Mango, new Vector3(22f, 0f, -14f), 45f, 0.7f);
        AddArbol(lista, ArbolTipo.Mango, new Vector3(16f, 0f, 23f), 300f, 0.7f);
        AddArbol(lista, ArbolTipo.Mango, new Vector3(-15f, 0f, -22f), 190f, 0.5f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            AddArbol(lista, ArbolTipo.Seto, new Vector3(Mathf.Cos(a) * 17f, 0f, Mathf.Sin(a) * 17f), -a * Mathf.Rad2Deg, 1f + (i % 3) * 0.2f);
        }
        ParqueVegetacion veg = AssetDatabase.LoadAssetAtPath<ParqueVegetacion>(VegPath);
        if (veg == null)
        {
            veg = ScriptableObject.CreateInstance<ParqueVegetacion>();
            AssetDatabase.CreateAsset(veg, VegPath);
        }
        veg.arboles = lista.ToArray();
        EditorUtility.SetDirty(veg);
        M("vegetacion_posiciones", lista.Count + " puestos", ValorFuente.Estimado, "diseño determinista guardado en el SO");
        return veg;
    }

    private static void AddArbol(List<ArbolPuesto> lista, ArbolTipo tipo, Vector3 offsetXZ, float rot, float escala)
    {
        ArbolPuesto p = new ArbolPuesto();
        p.tipo = tipo;
        p.posicion = new Vector3(offsetXZ.x, SlabTop, offsetXZ.z);
        p.rotacionY = rot;
        p.escala = escala;
        lista.Add(p);
    }

    private static void SpawnVegetation(ParqueVegetacion veg)
    {
        string[] nombres = new string[] { "Arbol_Araucaria", "Arbol_Guayacan", "Arbol_Corcho", "Arbol_FlorMayo", "Arbol_Mango", "Seto" };
        GameObject flora = new GameObject("Flora");
        for (int i = 0; i < veg.arboles.Length; i++)
        {
            ArbolPuesto p = veg.arboles[i];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + nombres[(int)p.tipo] + ".prefab");
            if (prefab == null)
            {
                continue;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(flora.transform, false);
            go.transform.position = p.posicion;
            go.transform.rotation = Quaternion.Euler(0f, p.rotacionY, 0f);
            go.transform.localScale = Vector3.one * p.escala;
        }
    }

    // ---------- Mobiliario ----------

    private static void BuildFurniture(Vector3 origen)
    {
        // Banca de hierro forjado + listones de madera (cobertura).
        GameObject banca = new GameObject("BancaHierro");
        Box(banca.transform, "Asiento", new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.1f, 0.55f), 7, true);
        Box(banca.transform, "Respaldo", new Vector3(0f, 0.85f, -0.28f), new Vector3(1.8f, 0.5f, 0.1f), 7, false);
        Box(banca.transform, "PataL", new Vector3(-0.75f, 0.2f, 0f), new Vector3(0.12f, 0.4f, 0.5f), 5, false);
        Box(banca.transform, "PataR", new Vector3(0.75f, 0.2f, 0f), new Vector3(0.12f, 0.4f, 0.5f), 5, false);
        GameObject bancaPrefab = SavePrefab(banca, "BancaHierro");
        Object.DestroyImmediate(banca);
        GameObject mob = new GameObject("Mobiliario");
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f + Mathf.PI / 4f;
            SpawnPrefab(bancaPrefab, mob.transform, new Vector3(Mathf.Cos(a) * 8f, SlabTop, Mathf.Sin(a) * 8f), -a * Mathf.Rad2Deg + 180f);
        }
        for (int i = 0; i < 8; i++)
        {
            float t = -30f + i * 8.5f;
            SpawnPrefab(bancaPrefab, mob.transform, new Vector3(t, SlabTop, 16f), 180f);
        }
        // Faroles de hierro con luz amarilla.
        GameObject farol = new GameObject("FarolPlaza");
        Cyl(farol.transform, "Poste", new Vector3(0f, 2.5f, 0f), new Vector3(0.25f, 5f, 0.25f), 5, true);
        GameObject lampara = Ball(farol.transform, "Lampara", new Vector3(0f, 5.2f, 0f), 0.55f, 6, false);
        Material glow = MaterialFactory.New();
        glow.color = paleta[6].color;
        glow.EnableKeyword("_EMISSION");
        glow.SetColor("_EmissionColor", paleta[6].color);
        lampara.GetComponent<MeshRenderer>().sharedMaterial = glow;
        GameObject farolPrefab = SavePrefab(farol, "FarolPlaza");
        Object.DestroyImmediate(farol);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f + Mathf.PI / 8f;
            SpawnPrefab(farolPrefab, mob.transform, new Vector3(Mathf.Cos(a) * 20f, SlabTop, Mathf.Sin(a) * 20f), 0f);
        }
        // Jardineras de piedra (cobertura) y papeleras.
        GameObject jard = new GameObject("Jardinera");
        Box(jard.transform, "Muro", new Vector3(0f, 0.4f, 0f), new Vector3(2f, 0.8f, 1f), 2, true);
        Box(jard.transform, "Tierra", new Vector3(0f, 0.85f, 0f), new Vector3(1.8f, 0.2f, 0.8f), 4, false);
        GameObject jardPrefab = SavePrefab(jard, "Jardinera");
        Object.DestroyImmediate(jard);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            SpawnPrefab(jardPrefab, mob.transform, new Vector3(Mathf.Cos(a) * 26f, SlabTop, Mathf.Sin(a) * 26f), -a * Mathf.Rad2Deg + 90f);
        }
        GameObject papel = new GameObject("Papelera");
        Cyl(papel.transform, "Cesto", new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.6f), 5, false);
        GameObject papelPrefab = SavePrefab(papel, "Papelera");
        Object.DestroyImmediate(papel);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f + 0.3f;
            SpawnPrefab(papelPrefab, mob.transform, new Vector3(Mathf.Cos(a) * 14.5f, SlabTop, Mathf.Sin(a) * 14.5f), 0f);
        }
        M("bancas_cantidad", "12", ValorFuente.Estimado, "especificación: cantidad ESTIMADO; sirven de cobertura");
        M("faroles_cantidad", "8", ValorFuente.Estimado, "especificación: ESTIMADO");
        M("mobiliario_extra", "8 jardineras (cobertura) + 6 papeleras", ValorFuente.Estimado, "diseño");
    }

    // ---------- Costados ----------

    // Fachada modular: parámetros ancho, alto y módulos. Mira hacia la plaza.
    private static void FachadaModular(Transform parent, Vector3 center, float facingY, float width, float height, int modulos, bool portada)
    {
        GameObject f = new GameObject("Bloque");
        f.transform.SetParent(parent, false);
        f.transform.position = center;
        f.transform.rotation = Quaternion.Euler(0f, facingY, 0f);
        float depth = 8f;
        Box(f.transform, "Muro", new Vector3(0f, SlabTop + height / 2f, 0f), new Vector3(width, height, depth), 0, true);
        Box(f.transform, "Zocalo", new Vector3(0f, SlabTop + 0.6f, depth / 2f + 0.05f), new Vector3(width, 1.2f, 0.2f), 2, false);
        float x0 = -width / 2f + width / modulos / 2f;
        for (int i = 0; i < modulos; i++)
        {
            float x = x0 + i * (width / modulos);
            bool esPuerta = portada && i == modulos / 2;
            if (esPuerta)
            {
                Box(f.transform, "Portada_" + i, new Vector3(x, SlabTop + 2f, depth / 2f + 0.1f), new Vector3(2.4f, 4f, 0.3f), 7, false);
            }
            else
            {
                Box(f.transform, "Puerta_" + i, new Vector3(x, SlabTop + 1.5f, depth / 2f + 0.08f), new Vector3(1.4f, 3f, 0.2f), 7, false);
            }
            Box(f.transform, "VentanaAlta_" + i, new Vector3(x, SlabTop + height - 2.2f, depth / 2f + 0.08f), new Vector3(1.2f, 1.8f, 0.2f), 5, false);
            Box(f.transform, "Balcon_" + i, new Vector3(x, SlabTop + height - 3.4f, depth / 2f + 0.5f), new Vector3(1.8f, 0.15f, 1f), 5, false);
        }
        Box(f.transform, "Alero", new Vector3(0f, SlabTop + height + 0.3f, depth / 2f + 0.4f), new Vector3(width + 1f, 0.4f, 1.2f), 7, false);
        GameObject tejadoL = Box(f.transform, "TejadoL", new Vector3(0f, SlabTop + height + 1.6f, -depth / 4f), new Vector3(width + 1f, 0.3f, depth / 2f + 1f), 1, false);
        tejadoL.transform.rotation *= Quaternion.Euler(25f, 0f, 0f);
        GameObject tejadoR = Box(f.transform, "TejadoR", new Vector3(0f, SlabTop + height + 1.6f, depth / 4f), new Vector3(width + 1f, 0.3f, depth / 2f + 1f), 1, false);
        tejadoR.transform.rotation *= Quaternion.Euler(-25f, 0f, 0f);
    }

    private static void BuildSides(Vector3 origen)
    {
        float d = 45f;
        GameObject norte = new GameObject("Costado_Norte");
        FachadaModular(norte.transform, new Vector3(-12f, 0f, d), 180f, 52f, 9f, 7, true);
        FachadaModular(norte.transform, new Vector3(28f, 0f, d), 180f, 28f, 8f, 4, false);
        GameObject panteon = Box(norte.transform, "PanteonFondo", new Vector3(0f, SlabTop + 4f, d + 28f), new Vector3(40f, 8f, 10f), 0, true);
        panteon.name = "PanteonFondo";

        GameObject sur = new GameObject("Costado_Sur");
        // Catedral monumental + Palacio Arzobispal.
        GameObject catedral = new GameObject("Catedral");
        catedral.transform.SetParent(sur.transform, false);
        catedral.transform.position = new Vector3(12f, 0f, -d);
        Box(catedral.transform, "Frente", new Vector3(0f, SlabTop + 7f, 0f), new Vector3(30f, 14f, 9f), 0, true);
        Box(catedral.transform, "TorreIzq", new Vector3(-13f, SlabTop + 9f, 0f), new Vector3(5f, 18f, 7f), 0, true);
        Box(catedral.transform, "TorreDer", new Vector3(13f, SlabTop + 9f, 0f), new Vector3(5f, 18f, 7f), 0, true);
        Box(catedral.transform, "PuertaMayor", new Vector3(0f, SlabTop + 2.5f, 4.6f), new Vector3(4f, 5f, 0.3f), 7, false);
        Box(catedral.transform, "Roseton", new Vector3(0f, SlabTop + 10f, 4.6f), new Vector3(3f, 3f, 0.3f), 5, false);
        FachadaModular(sur.transform, new Vector3(-16f, 0f, -d), 0f, 24f, 8f, 4, false);

        GameObject oriente = new GameObject("Costado_Oriente");
        FachadaModular(oriente.transform, new Vector3(d, 0f, 8f), -90f, 44f, 9f, 6, true);
        FachadaModular(oriente.transform, new Vector3(d, 0f, -22f), -90f, 20f, 8f, 3, false);
        FachadaModular(oriente.transform, new Vector3(d, 0f, -36f), -90f, 12f, 8f, 2, false);

        GameObject occidente = new GameObject("Costado_Occidente");
        FachadaModular(occidente.transform, new Vector3(-d, 0f, 10f), 90f, 30f, 8f, 4, false);
        FachadaModular(occidente.transform, new Vector3(-d, 0f, -16f), 90f, 24f, 8f, 3, false);
        Box(occidente.transform, "AvisoValdez", new Vector3(-d + 4.2f, SlabTop + 5f, -34f), new Vector3(0.3f, 1.2f, 8f), 7, false);
        M("costados", "Norte(Gobernación+Bogotá) Sur(Catedral+Palacio) Oriente(Alcaldía+bancos) Occidente(Cámara+Agrario+Valdez)", ValorFuente.Documentado, "especificación");
        M("fachadas_medidas_m", "8-10 alto, anchos por costado", ValorFuente.Estimado, "especificación: 1-2 pisos");
        M("catedral_palacio", "1940 junto a Catedral", ValorFuente.Documentado, "especificación parcial");
        M("panteon_fondo", "10 m del parque, sin interior", ValorFuente.Documentado, "especificación");
    }

    // ---------- Torre del Reloj (esquina suroccidental) ----------

    private static Mesh PyramidMesh(float half, float height)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half),
            new Vector3(half, 0f, half), new Vector3(-half, 0f, half),
            new Vector3(0f, height, 0f) };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3, 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0 };
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Vector3 BuildTorre(Vector3 origen)
    {
        Vector3 basePos = new Vector3(-36f, SlabTop, -36f);
        GameObject torre = new GameObject("TorreDelReloj");
        torre.transform.position = basePos;
        float[] anchos = new float[] { 6f, 5.2f, 4.4f, 3.6f };
        float[] altos = new float[] { 7f, 7f, 6f, 6f };
        float y = 0f;
        for (int i = 0; i < 4; i++)
        {
            Box(torre.transform, "Cuerpo_" + (i + 1), new Vector3(0f, y + altos[i] / 2f, 0f), new Vector3(anchos[i], altos[i], anchos[i]), 0, true);
            y += altos[i];
        }
        GameObject esfera = Cyl(torre.transform, "Reloj", new Vector3(0f, 20f, 2.7f), new Vector3(3.4f, 0.3f, 3.4f), 0, false);
        esfera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Box(torre.transform, "Manecilla", new Vector3(0f, 20f, 2.95f), new Vector3(0.25f, 1.5f, 0.1f), 5, false);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Box(torre.transform, "Arco_" + i, new Vector3(Mathf.Cos(a) * 1.9f, 24f, Mathf.Sin(a) * 1.9f), new Vector3(1.2f, 2.4f, 0.3f), 5, false);
        }
        GameObject remate = new GameObject("Remate");
        remate.transform.SetParent(torre.transform, false);
        remate.transform.localPosition = new Vector3(0f, 26f, 0f);
        MeshFilter mf = remate.AddComponent<MeshFilter>();
        mf.sharedMesh = PyramidMesh(2.6f, 3.5f);
        MeshRenderer mr = remate.AddComponent<MeshRenderer>();
        mr.sharedMaterial = paleta[1];
        GameObject objetivo = GameObject.Find("Objetivo_Torre");
        if (objetivo == null)
        {
            objetivo = new GameObject("Objetivo_Torre");
        }
        objetivo.transform.SetParent(torre.transform, false);
        objetivo.transform.localPosition = new Vector3(0f, 1f, 4f);
        M("torre_historia", "barroca 1673-1682, reloj inglés 1737, 'la Nariz de Popayán'", ValorFuente.Documentado, "especificación");
        M("torre_disenio", "base 6m, 4 cuerpos, 26m + remate", ValorFuente.Estimado, "especificación: 25-30 m");
        M("torre_posicion", "esquina suroccidental (-36,-36)", ValorFuente.Documentado, "especificación");
        return objetivo.transform.position;
    }

    // ---------- Gameplay ----------

    private static void SetMarker(string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        go.transform.position = pos;
    }

    private static void RepositionGameplay(Vector3 origen, Vector3 torrePos)
    {
        Vector3 spawnJ = new Vector3(5f, SlabTop + 0.5f, -8f);
        SetMarker("Spawn_Jugador", spawnJ);
        SetMarker("Spawn_Aliado_0", new Vector3(1f, SlabTop + 0.5f, -9f));
        SetMarker("Spawn_Aliado_1", new Vector3(9f, SlabTop + 0.5f, -9f));
        SetMarker("Spawn_Oleada_0", new Vector3(0f, SlabTop + 0.5f, 50f));
        SetMarker("Spawn_Oleada_1", new Vector3(50f, SlabTop + 0.5f, 0f));
        SetMarker("Spawn_Oleada_2", new Vector3(-50f, SlabTop + 0.5f, 10f));
        Vector3[] covers = new Vector3[] {
            new Vector3(-20f, SlabTop + 0.5f, -20f), new Vector3(-28f, SlabTop + 0.5f, -10f),
            new Vector3(-10f, SlabTop + 0.5f, -28f), new Vector3(0f, SlabTop + 0.5f, -32f) };
        for (int i = 0; i < covers.Length; i++)
        {
            SetMarker("CoverM1_" + i, covers[i]);
        }
        // Muros límite alrededor de la plaza.
        float half = 65f;
        string[] limNames = new string[] { "Limite_N", "Limite_S", "Limite_E", "Limite_W" };
        Vector3[] limPos = new Vector3[] {
            new Vector3(0f, SlabTop + 8f, -half), new Vector3(0f, SlabTop + 8f, half),
            new Vector3(half, SlabTop + 8f, 0f), new Vector3(-half, SlabTop + 8f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            GameObject w = GameObject.Find(limNames[i]);
            if (w == null)
            {
                w = new GameObject(limNames[i]);
                w.AddComponent<BoxCollider>();
            }
            w.transform.position = limPos[i];
            BoxCollider box = w.GetComponent<BoxCollider>();
            box.size = i < 2 ? new Vector3(half * 2f, 30f, 2f) : new Vector3(2f, 30f, half * 2f);
        }
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = spawnJ;
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.SetSpawn(spawnJ, 180f);
            }
        }
        M("spawns", "jugador+aliados junto a estatua; policía norte y oriente; objetivo base de la Torre", ValorFuente.Documentado, "especificación: uso en el juego");
    }

    // ---------- Config y NavMesh ----------

    private static void SaveConfig(Vector3 origen, Vector3 torrePos)
    {
        ParqueCaldasConfig config = AssetDatabase.LoadAssetAtPath<ParqueCaldasConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<ParqueCaldasConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }
        config.medidas = medidas.ToArray();
        config.estatuaPos = origen;
        config.torrePos = torrePos;
        config.spawnJugador = new Vector3(5f, SlabTop + 0.5f, -8f);
        config.spawnAliados = new Vector3[] {
            new Vector3(1f, SlabTop + 0.5f, -9f), new Vector3(9f, SlabTop + 0.5f, -9f) };
        config.spawnOleadas = new Vector3[] {
            new Vector3(0f, SlabTop + 0.5f, 50f), new Vector3(50f, SlabTop + 0.5f, 0f),
            new Vector3(-50f, SlabTop + 0.5f, 10f) };
        config.objetivoTorre = torrePos;
        EditorUtility.SetDirty(config);
        int doc = 0;
        int med = 0;
        int est = 0;
        for (int i = 0; i < medidas.Count; i++)
        {
            if (medidas[i].fuente == ValorFuente.Documentado)
            {
                doc++;
            }
            else if (medidas[i].fuente == ValorFuente.Medido)
            {
                med++;
            }
            else
            {
                est++;
            }
        }
        Debug.Log("Parque Caldas config: DOCUMENTADO=" + doc + " MEDIDO=" + med + " ESTIMADO=" + est + ".");
    }

    private static void Rebake()
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
        else
        {
            NavMeshSurface[] extras = bakeGo.GetComponentsInChildren<NavMeshSurface>();
            for (int i = 0; i < extras.Length; i++)
            {
                if (extras[i] != surface)
                {
                    Object.DestroyImmediate(extras[i]);
                }
            }
        }
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.center = new Vector3(0f, SlabTop + 2f, 0f);
        surface.size = new Vector3(180f, 14f, 180f);
        surface.defaultArea = 0;
        surface.BuildNavMesh();
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Debug.Log("Parque Caldas NavMesh: rebake verts=" + tri.vertices.Length + ".");
    }

    private static void SpawnPrefab(GameObject prefab, Transform parent, Vector3 pos, float rotY)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
    }
}
