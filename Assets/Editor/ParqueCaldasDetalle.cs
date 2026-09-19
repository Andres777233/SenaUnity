using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Popayork.Core;
using Popayork.World;

// Pase de detalle del Parque Caldas (Mision1). Aditivo e idempotente:
// limpia su propio grupo "DetallePopayan" + objetos "CornisaDetalle"/"MarcoDetalle_*"
// y lo reconstruye. No mueve gameplay ni marcadores.
// Catedral con campanario (arcos + campanas + cruz + escalinata + reloj),
// Torre del Reloj (molduras + reloj sur + campana + cruz + zócalo),
// cornisas y marcos en las 8 fachadas, 4 calles con aceras,
// Panteón con columnas y frontón, rosa de los vientos y palomas.
public static class ParqueCaldasDetalle
{
    private const string ScenePath = "Assets/Scenes/Mision1.unity";
    private const float SlabTop = 72.0f;

    // Paleta: 0 blanco, 1 teja, 2 gris piedra, 3 verde, 4 verde oscuro,
    // 5 hierro, 6 amarillo, 7 madera.
    private static readonly Color[] Colores = new Color[] {
        new Color(0.96f, 0.94f, 0.89f),
        new Color(0.60f, 0.34f, 0.21f),
        new Color(0.55f, 0.55f, 0.58f),
        new Color(0.24f, 0.48f, 0.20f),
        new Color(0.16f, 0.35f, 0.16f),
        new Color(0.13f, 0.13f, 0.15f),
        new Color(0.91f, 0.73f, 0.23f),
        new Color(0.48f, 0.32f, 0.19f)
    };

    [MenuItem("Popayork/Mejorar Parque Caldas")]
    public static void MejorarDesdeMenu()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Backup();
        Aplicar();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Popayork/Mejorar Parque Caldas: PASS.");
    }

    // Llamado por ParqueCaldasBuilder.BuildAll (escena ya abierta, sin salvar aquí).
    public static void Aplicar()
    {
        LimpiarDetalle();
        GameObject root = new GameObject("DetallePopayan");
        DetalleCatedral(root.transform);
        DetalleTorre();
        DetalleFachadas();
        BuildCalles(root.transform);
        DetallePanteon(root.transform);
        BuildRosaVientos(root.transform);
        BuildPalomas(root.transform);
    }

    private static Material Mat(int i)
    {
        return MaterialFactory.Get(Colores[i]);
    }

    private static GameObject Caja(Transform parent, string nombre, Vector3 posMundo, Vector3 escala, int mat, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.position = posMundo;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(mat);
        if (!collider)
        {
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        }
        return go;
    }

    private static GameObject Esfera(Transform parent, string nombre, Vector3 posMundo, float tam, int mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.position = posMundo;
        go.transform.localScale = Vector3.one * tam;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(mat);
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        return go;
    }

    private static GameObject Cilindro(Transform parent, string nombre, Vector3 posMundo, Vector3 escala, int mat, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.position = posMundo;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(mat);
        if (!collider)
        {
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        }
        return go;
    }

    private static void LimpiarDetalle()
    {
        GameObject viejo = GameObject.Find("DetallePopayan");
        if (viejo != null)
        {
            Object.DestroyImmediate(viejo);
        }
        Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < todos.Length; i++)
        {
            string n = todos[i].name;
            if (n == "CornisaDetalle" || n.StartsWith("MarcoDetalle_") || n.StartsWith("Detalle_"))
            {
                Object.DestroyImmediate(todos[i].gameObject);
            }
        }
    }

    private static void Backup()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Backup"))
        {
            AssetDatabase.CreateFolder("Assets/Scenes", "Backup");
        }
        string dest = "Assets/Scenes/Backup/Mision1_detalle_backup.unity";
        int n = 1;
        while (System.IO.File.Exists(dest))
        {
            dest = "Assets/Scenes/Backup/Mision1_detalle_backup" + n + ".unity";
            n++;
        }
        AssetDatabase.CopyAsset(ScenePath, dest);
    }

    // ---------- Catedral (12, -45, frente al norte) ----------

    private static void DetalleCatedral(Transform root)
    {
        GameObject catedral = GameObject.Find("Catedral");
        if (catedral == null)
        {
            Debug.LogWarning("DetallePopayan: sin Catedral, se omite su detalle.");
            return;
        }
        Transform t = catedral.transform;
        Vector3 baseC = t.position;
        // Escalinata de acceso (3 gradas).
        for (int i = 0; i < 3; i++)
        {
            Caja(t, "Detalle_Grada_" + i, baseC + new Vector3(0f, SlabTop + 0.15f + i * 0.3f, 5.2f + i * 0.7f),
                new Vector3(8f - i * 1.2f, 0.3f, 1.4f), 2, false);
        }
        // Arcos del campanario + campanas en cada torre (caras norte y laterales).
        float[] torresX = new float[] { -13f, 13f };
        string[] lados = new string[] { "Izq", "Der" };
        for (int k = 0; k < 2; k++)
        {
            Caja(t, "Detalle_ArcoCamp_" + lados[k], baseC + new Vector3(torresX[k], SlabTop + 13.5f, 3.6f),
                new Vector3(1.8f, 2.8f, 0.25f), 5, false);
            Esfera(t, "Detalle_Campana_" + lados[k], baseC + new Vector3(torresX[k], SlabTop + 13.2f, 3.4f), 1.1f, 7);
            Caja(t, "Detalle_ArcoCampLat_" + lados[k], baseC + new Vector3(torresX[k] + (k == 0 ? -2.6f : 2.6f), SlabTop + 13.5f, 0f),
                new Vector3(0.25f, 2.8f, 1.8f), 5, false);
        }
        // Reloj circular sobre la puerta mayor + cruz en el frontón.
        GameObject reloj = Cilindro(t, "Detalle_RelojCatedral", baseC + new Vector3(0f, SlabTop + 11f, 4.7f),
            new Vector3(2.6f, 0.25f, 2.6f), 0, false);
        reloj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Caja(t, "Detalle_ManecillaCat", baseC + new Vector3(0f, SlabTop + 11f, 4.9f),
            new Vector3(0.2f, 1.1f, 0.1f), 5, false);
        Caja(t, "Detalle_CruzCat_V", baseC + new Vector3(0f, SlabTop + 15.1f, 0f),
            new Vector3(0.3f, 2.2f, 0.3f), 5, false);
        Caja(t, "Detalle_CruzCat_H", baseC + new Vector3(0f, SlabTop + 15.4f, 0f),
            new Vector3(1.4f, 0.3f, 0.3f), 5, false);
    }

    // ---------- Torre del Reloj (-36, -36) ----------

    private static void DetalleTorre()
    {
        GameObject torre = GameObject.Find("TorreDelReloj");
        if (torre == null)
        {
            Debug.LogWarning("DetallePopayan: sin TorreDelReloj, se omite su detalle.");
            return;
        }
        Transform t = torre.transform;
        Vector3 baseT = t.position;
        // Zócalo de piedra.
        Caja(t, "Detalle_ZocaloTorre", baseT + new Vector3(0f, 0.75f, 0f), new Vector3(6.6f, 1.5f, 6.6f), 2, false);
        // Molduras entre cuerpos (anchos 6 / 5.2 / 4.4 a y=7 / 14 / 20).
        float[] anchos = new float[] { 6f, 5.2f, 4.4f };
        float[] alturas = new float[] { 7f, 14f, 20f };
        for (int i = 0; i < 3; i++)
        {
            Caja(t, "Detalle_Moldura_" + (i + 1), baseT + new Vector3(0f, alturas[i], 0f),
                new Vector3(anchos[i] + 0.8f, 0.4f, anchos[i] + 0.8f), 2, false);
        }
        // Reloj cara sur (el builder trae el norte) + campana + cruz del remate.
        GameObject relojS = Cilindro(t, "Detalle_RelojSur", baseT + new Vector3(0f, 20f, -2.7f),
            new Vector3(3.4f, 0.3f, 3.4f), 0, false);
        relojS.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        Caja(t, "Detalle_ManecillaSur", baseT + new Vector3(0f, 20f, -2.95f),
            new Vector3(0.25f, 1.5f, 0.1f), 5, false);
        Esfera(t, "Detalle_CampanaTorre", baseT + new Vector3(0f, 24f, 0f), 1.4f, 5);
        Caja(t, "Detalle_CruzTorre_V", baseT + new Vector3(0f, 30.6f, 0f),
            new Vector3(0.25f, 1.8f, 0.25f), 5, false);
        Caja(t, "Detalle_CruzTorre_H", baseT + new Vector3(0f, 30.9f, 0f),
            new Vector3(1.1f, 0.25f, 0.25f), 5, false);
    }

    // ---------- Cornisas y marcos en las 8 fachadas ----------

    private static void DetalleFachadas()
    {
        string[] costados = new string[] { "Costado_Norte", "Costado_Sur", "Costado_Oriente", "Costado_Occidente" };
        int cornisas = 0;
        int marcos = 0;
        for (int c = 0; c < costados.Length; c++)
        {
            GameObject lado = GameObject.Find(costados[c]);
            if (lado == null)
            {
                continue;
            }
            for (int b = 0; b < lado.transform.childCount; b++)
            {
                Transform bloque = lado.transform.GetChild(b);
                if (!bloque.name.StartsWith("Bloque"))
                {
                    continue;
                }
                Transform muro = bloque.Find("Muro");
                if (muro == null)
                {
                    continue;
                }
                float w = muro.localScale.x;
                float h = muro.localScale.y;
                float d = muro.localScale.z;
                // Cornisa continua arriba del muro (local al bloque).
                GameObject cornisa = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cornisa.name = "CornisaDetalle";
                cornisa.transform.SetParent(bloque, false);
                cornisa.transform.localPosition = new Vector3(0f, SlabTop + h + 0.1f, 0f);
                cornisa.transform.localScale = new Vector3(w + 0.7f, 0.35f, d + 0.7f);
                cornisa.GetComponent<MeshRenderer>().sharedMaterial = Mat(2);
                Object.DestroyImmediate(cornisa.GetComponent<BoxCollider>());
                cornisas++;
                // Marco de madera tras cada ventana alta.
                for (int v = 0; v < bloque.childCount; v++)
                {
                    Transform ventana = bloque.GetChild(v);
                    if (!ventana.name.StartsWith("VentanaAlta_"))
                    {
                        continue;
                    }
                    GameObject marco = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    marco.name = "MarcoDetalle_" + marcos;
                    marco.transform.SetParent(bloque, false);
                    marco.transform.localPosition = ventana.localPosition + new Vector3(0f, 0f, -0.06f);
                    marco.transform.localScale = ventana.localScale + new Vector3(0.35f, 0.35f, -0.1f);
                    marco.GetComponent<MeshRenderer>().sharedMaterial = Mat(7);
                    Object.DestroyImmediate(marco.GetComponent<BoxCollider>());
                    marcos++;
                }
            }
        }
        Debug.Log("DetallePopayan: cornisas=" + cornisas + " marcos=" + marcos + ".");
    }

    // ---------- Vía empedrada perimetral + aceras ----------
    // Las fachadas arrancan en |41| m, así que la calzada de 10 m de la especificación
    // no cabe sin atravesar edificios: se usa vía peatonal de 4 m (36-40) + acera (40-41.5).

    private static void BuildCalles(Transform root)
    {
        GameObject calles = new GameObject("Calles");
        calles.transform.SetParent(root, false);
        float via = 38f;
        float acera = 40.75f;
        float largo = 80f;
        Caja(calles.transform, "Calzada_N", new Vector3(0f, SlabTop + 0.02f, via), new Vector3(largo, 0.08f, 4f), 2, false);
        Caja(calles.transform, "Calzada_S", new Vector3(0f, SlabTop + 0.02f, -via), new Vector3(largo, 0.08f, 4f), 2, false);
        Caja(calles.transform, "Calzada_E", new Vector3(via, SlabTop + 0.02f, 0f), new Vector3(4f, 0.08f, largo), 2, false);
        Caja(calles.transform, "Calzada_O", new Vector3(-via, SlabTop + 0.02f, 0f), new Vector3(4f, 0.08f, largo), 2, false);
        Caja(calles.transform, "Acera_N", new Vector3(0f, SlabTop + 0.04f, acera), new Vector3(largo, 0.1f, 1.5f), 0, false);
        Caja(calles.transform, "Acera_S", new Vector3(0f, SlabTop + 0.04f, -acera), new Vector3(largo, 0.1f, 1.5f), 0, false);
        Caja(calles.transform, "Acera_E", new Vector3(acera, SlabTop + 0.04f, 0f), new Vector3(1.5f, 0.1f, largo), 0, false);
        Caja(calles.transform, "Acera_O", new Vector3(-acera, SlabTop + 0.04f, 0f), new Vector3(1.5f, 0.1f, largo), 0, false);
    }

    // ---------- Panteón de los Próceres (fondo norte) ----------

    private static void DetallePanteon(Transform root)
    {
        GameObject fondo = GameObject.Find("PanteonFondo");
        if (fondo == null)
        {
            Debug.LogWarning("DetallePopayan: sin PanteonFondo, se omite su detalle.");
            return;
        }
        Vector3 baseP = fondo.transform.position;
        float sueloP = baseP.y - 4f;
        GameObject pant = new GameObject("PanteonDetalle");
        pant.transform.SetParent(root, false);
        pant.transform.position = baseP;
        // Columnata blanca al frente (cara sur, hacia la plaza).
        for (int i = 0; i < 6; i++)
        {
            Cilindro(pant.transform, "Columna_" + i, new Vector3(baseP.x - 12.5f + i * 5f, sueloP + 3.5f, baseP.z - 5.5f),
                new Vector3(1.2f, 7f, 1.2f), 0, false);
        }
        // Frontón de teja + escalinata.
        GameObject frontL = Caja(pant.transform, "Fronton_L", new Vector3(baseP.x - 6.5f, sueloP + 8.6f, baseP.z - 5.5f),
            new Vector3(14f, 0.35f, 2.5f), 1, false);
        frontL.transform.rotation *= Quaternion.Euler(0f, 0f, 18f);
        GameObject frontR = Caja(pant.transform, "Fronton_R", new Vector3(baseP.x + 6.5f, sueloP + 8.6f, baseP.z - 5.5f),
            new Vector3(14f, 0.35f, 2.5f), 1, false);
        frontR.transform.rotation *= Quaternion.Euler(0f, 0f, -18f);
        for (int i = 0; i < 2; i++)
        {
            Caja(pant.transform, "GradaPant_" + i, new Vector3(baseP.x, sueloP + 0.15f + i * 0.3f, baseP.z - 6.5f - i * 0.8f),
                new Vector3(30f - i * 2f, 0.3f, 1.6f), 2, false);
        }
    }

    // ---------- Rosa de los vientos (8 puntas blancas) ----------

    private static void BuildRosaVientos(Transform root)
    {
        GameObject rosa = new GameObject("RosaVientos");
        rosa.transform.SetParent(root, false);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            GameObject punta = Caja(rosa.transform, "Punta_" + i,
                new Vector3(Mathf.Cos(a) * 5.5f, SlabTop + 0.07f, Mathf.Sin(a) * 5.5f),
                new Vector3(1.1f, 0.1f, 6.5f), 0, false);
            punta.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
        }
    }

    // ---------- Palomas ----------

    private static void BuildPalomas(Transform root)
    {
        GameObject grupo = new GameObject("Palomas");
        grupo.transform.SetParent(root, false);
        Vector3[] suelo = new Vector3[] {
            new Vector3(6f, 0f, 4f), new Vector3(-7f, 0f, 6f), new Vector3(4f, 0f, -8f),
            new Vector3(-5f, 0f, -5f), new Vector3(10f, 0f, -3f)
        };
        for (int i = 0; i < suelo.Length; i++)
        {
            GameObject p = new GameObject("Paloma_" + i);
            p.transform.SetParent(grupo.transform, false);
            p.transform.position = new Vector3(suelo[i].x, SlabTop + 0.2f, suelo[i].z);
            p.transform.rotation = Quaternion.Euler(0f, i * 72f, 0f);
            Esfera(p.transform, "Cuerpo", p.transform.position, 0.38f, 2);
            Esfera(p.transform, "Cabeza", p.transform.position + new Vector3(0f, 0.25f, 0.22f), 0.2f, 2);
        }
        for (int i = 0; i < 3; i++)
        {
            GameObject p = new GameObject("PalomaVuelo_" + i);
            p.transform.SetParent(grupo.transform, false);
            Esfera(p.transform, "Cuerpo", p.transform.position, 0.38f, 2);
            PalomaVuelo vuelo = p.AddComponent<PalomaVuelo>();
            vuelo.Configurar(new Vector3(0f, SlabTop, 0f), 11f + i * 3f, 7f + i * 1.5f, 0.45f + i * 0.1f, i * 2.1f);
        }
    }
}
