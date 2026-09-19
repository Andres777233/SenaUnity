# DIAGNOSTICO_ARTE — Popayork (BLOQUE 0, solo lectura)

Fecha: 2026-09-19. Proyecto real: `/home/andres/Escritorio/SenaUnity-master`
(AGENTS.md §2 dice `/home/andres/Escritorio/SENAUNITY`, ruta inexistente; el proyecto con
`ProjectSettings/` y `Assets/` está en `SenaUnity-master`). Editor verificado cerrado
(`pgrep -l -i unity` vacío). Sin cambios de código en este bloque.

## a) Pipeline de render activo

- `Packages/manifest.json`: solo `com.unity.ai.navigation 2.0.14`,
  `com.unity.modules.ai`, `com.unity.modules.particlesystem`, `com.unity.ugui`.
  NO hay URP ni HDRP ni PostProcessing.
- `ProjectSettings/GraphicsSettings.asset`: `m_CustomRenderPipeline: {fileID: 0}`
  (sin asset de pipeline asignado) → **Built-in Render Pipeline**.
- `ProjectSettings/QualitySettings.asset`: todos los niveles con
  `customRenderPipeline: {fileID: 0}` → Built-in en todas las calidades.
- Conclusión: **NO instalar URP** (regla del bloque). El shader correcto en
  código es `"Standard"`. `MaterialFactory` debe usar `Standard` en built-in
  (y `Universal Render Pipeline/Lit` solo si algún día hay pipeline activo).

## b) Shaders usados por mis scripts

- `Shader.Find("Standard")` en 2 scripts runtime:
  `Assets/Scripts/Weapons/ImpactPool.cs:57`, `Assets/Scripts/Weapons/ProjectilePool.cs:36`.
- `new Material(Shader.Find("Standard"))` en builders (todos correctos para built-in):
  `Fase4ABuilder.cs:219` (`Mat`), `ParqueCaldasBuilder.cs:80,271,445`,
  `Fase2Builder.cs:354,361,368,420`, `Fase3Builder.cs:356,366`,
  `Fase4BBuilder.cs:270,496,566`, `Fase5ABuilder.cs:386`, `Fase5BBuilder.cs:219,239,324`,
  `Fase6Builder.cs:99,243`. Total ≈ 20 sitios. `CreateAsset` solo crea
  ScriptableObjects (configs/waves), nunca materiales.
- NO existe `Assets/Materials/` (ni `Generated/` ni `Backup/`): cero `.mat` en
  `Assets/` (búsqueda `**/*.mat` = 0). Los materiales viven solo como
  `sharedMaterial` dentro de prefabs/escenas generados por script.
- Hipótesis del magenta (a confirmar con Verify Arte en edit-mode): en built-in
  `Standard` existe, así que el magenta de árboles/bancas/postes NO viene de mis
  `Mat()` salvo que `Shader.Find` devolviera null por strippeo; el sospechoso real
  son los **materiales importados de los FBX** (`materialImportMode: 2` en
  `model.fbx.meta`, `Guns.fbx.meta`, `temp_export.fbx`) y los `.blend` no
  importables (p. ej. `Horse.blend` ya declarado inservible): si traen shaders
  Blender/glTF (`Principled`, `Autodesk Interactive`) Unity los muestra magenta.
  DUDA API (anotada para compilador): el significado exacto de
  `materialImportMode: 2` no se adivina; el Verify lo medirá por
  `renderer.sharedMaterial.shader == null || !shader.isSupported`.

## c) Materiales incompatibles en Models3D / MapaPopayan

- Conteo estático exacto hoy: **0 `.mat`** en `Assets/Models3D` y
  `Assets/MapaPopayan` (no hay ficheros que contar; verificado por glob).
- Los materiales problemáticos son los **embebidos en la importación FBX** y los
  generados por script dentro de prefabs/escenas, que solo se ven con el Editor:
  el `Popayork/Reparar Materiales` + `Verify Arte` (Bloque 1) los recorrerán vía
  `Renderer.sharedMaterial` en las 3 escenas de misión y los convertirán a
  `Standard` conservando textura/color, con backup en `Assets/Materials/Backup`.
- Candidatos: `personas modelos1/source/temp_export.fbx` (Man01/Woman01),
  `Policias/source/*`, `armas low poly 2/source/Guns.fbx` (MSR/BE1),
  `MapaPopayan/source/model.fbx` (1 material + `model.jpg`).

## d) NavMesh por escena y cálculo de spawns

- Superficies: `Fase4ABuilder.BakeNavMesh` crea `NavMeshBake` con
  `NavMeshSurface`, `agentTypeID = 0` (valores por defecto del agente),
  `collectObjects = CollectObjects.Volume` (centro `sueloY+2`, tamaño
  `170x12x170`), `useGeometry = PhysicsColliders`, y prueba ruta A→B con
  `NavMesh.CalculatePath`. Mision3 usa `Children` (el `Volume` bakeaba vacío).
  Cobertura declarada: solo la plaza (≈80×80) + prueba de ruta; árboles/edificios
  no se excluyen explícitamente (los props primitivos generan colliders que el
  Volume sí recoge → riesgo de bake ruidoso).
- Spawns: posiciones fijas calculadas en el builder (`spawnJugador`,
  `spawnAliados`, `spawnOleadas` en `ParqueConfig`; markers `Spawn_Oleada_*` en
  `Fase4B`), consumidas tal cual por `WaveManager.SpawnOne`
  (`WaveManager.cs:128-137`: `spot.position` directo) → `AgentPool.SpawnFiltered`
  (`AgentPool.cs:72-87`: sin `SamplePosition`, sin `Warp` validado) →
  `AgentBrain.Activate` (`AgentBrain.cs:126-153`: `SetActive(true)` y luego
  `movement.Place(position)`) → `AgentMovement.Place` (`AgentMovement.cs:55-69`):
  solo hace `Warp` si `isOnNavMesh`, si no teletransporta fuera de malla; el
  `SetDestination` posterior falla con **"Failed to create agent because it is not
  close enough to the NavMesh"**. Ningún spawn usa `NavMesh.SamplePosition` hoy.
- Agentes: pool de 40 slots, tope 30 activos (`WaveManager.MaxActiveAgents`);
  `Discover()` re-registra al cargar (el `.unity` no guarda el registro).

## e) Construcción de cada UI

- Patrón común en todos los builders (`ProjectBuilder.CreateCanvas`,
  `Fase2/Fase4B/Fase5A/Fase6.BuildMissionUI`, subtítulos, pausa): `new GameObject`
  + `Canvas (ScreenSpaceOverlay)` + **`AddComponent<CanvasScaler>()` sin
  configurar** (defaults = Constant Pixel Size, NO "Scale With Screen Size"
  1920×1080 match 0.5) + `GraphicRaycaster`. Ningún Canvas cumple el estándar
  del Bloque 3 hoy.
- Intro Mision1 (`Fase4BBuilder.cs:313-318,403-424`): `IntroPanel` 640×480
  centrado; `IntroTitle` ("EMPIEZA EL CAOS" 52pt) e `IntroBody` (historia +
  controles 20pt) usan **los mismos anchors 0.1–0.9 full-panel** → el título pisa
  el texto de controles. Botón `BtnVamos` anclado a `(0.5, 0.15)` con tamaño fijo
  340×56 (píxeles fijos, se rompe en otras resoluciones).
- HUD juego (`Fase4BBuilder.cs:333-356` + `Fase2Builder` HUD): `ObjectiveBar`
  anclado arriba full-width con offsets fijos (120/-80/-120/-10);
  `ObjectiveText`, `ProgressBG` y `ProgressFill` son hijos del mismo `objBar`
  **sin Layout Group**: la barra verde de progreso comparte el rect del texto
  "Defiende la Torre del Reloj" y lo tapa. Vida/abajo-izq, munición/abajo-der y
  mira/centro se posicionan con píxeles fijos (mismo patrón en `Fase6Builder`
  `Panel/Label/Btn/Bar` con `sizeDelta` fijos 680×520, 340×56, etc.).
- Menú/selección/pausa/resultados/victoria/derrota (`ProjectBuilder`,
  `Fase7Builder`, cada `BuildMissionUI`): mismo Canvas sin scaler configurado y
  `GetLabel/GetButton` con tamaños fijos; hay que aplicarles la misma revisión
  (scaler 1920×1080 match 0.5, anclas + Layout Groups, sin píxeles fijos).

## Problema 4 (adelanto, sin tocar nada)

- `model.fbx`: UNA malla "Model" (939716 verts Unity, ~1.5M tris), bounds
  ≈ 1709×1378 m con 141 m de desnivel (`docs/DIAGNOSTICO_MAPA.md`, medido en
  `Fase4ABuilder.cs:133-158`). `Fase4A` lo instancia entero como `CiudadBase` de
  fondo + construye la plaza 80×80 por código encima. Las "formas grises grandes
  y retorcidas a los lados de la plaza" encajan con **fragmentos de esa malla
  única vistos de cerca** (edificios/lejanía a escala ciudad junto a la plaza,
  con la textura `model.jpg` de 2048px estirada en 1.7 km). Detalle y
  recomendación en Bloque 4 (ESTADO.md); no se borra ni modifica nada.

## Problema 5 (base para la pasada visual)

- Luz: sol direccional + `RenderSettings.ambientMode = Flat`,
  `ambientLight (0.62,0.55,0.52)`, `fog = true`, `fogColor (0.96,0.72,0.55)`,
  `fogDensity 0.004` (`Fase4ABuilder.cs:558-567`). Sin skybox de atardecer
  dedicado ni Volume de posprocesado (bloom/viñeta requerirían RP, no instalado
  por decisión Fase 7). Paleta por escena no auditada (límite: 8 colores base).
  Partículas fuego/humo vía `ChaosManager` + `ImpactPool` (revisar visibilidad a
  distancia en Bloque 5).
