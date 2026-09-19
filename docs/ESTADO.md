# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 4A cerrada (compila, Verify 4A 3/3 PASS, F1/F2/F3 re-PASS, commit "Fase 4A").

## Hecho (Fase 4A)
- Mision1 con Parque Caldas: ciudad `model.fbx` como base visual (1 malla, 939716 verts) + plaza de 60m cuyo centro/altura se midieron por raycast (31×31) sobre el modelo.
- Torre del Reloj low poly por código (28m ESTIMADO) como objetivo; `ParqueConfig` con MEDIDO=14 / ESTIMADO=11 (nada "exacto" inventado).
- Prefabs reutilizados: 24 árboles, 10 bancas, estatua, 8 farolas + 4 fachadas coloniales con colisión y muros límite anticaída.
- Atardecer (sol 18°, niebla 0.004) y NavMesh de la zona (1583 verts, ruta spawn→torre `PathComplete`); spawns de jugador/aliados/oleadas definidos (sin lógica aún).
- `Popayork/Verify Fase 4A`: 3/3 PASS. Cero errores/warnings CS propios. `AudioListener` agregado a cámaras de jugador (Mision1/TestArena).

## Rendimiento (Fase 4A)
- Presupuesto estático medido en batch: 125 renderers, 1512418 tris (99% ciudad estática, 1 draw call), 89 colliders, textura ciudad 2048px.
- FPS promedio con render: NO medible en batch (`-nographics` no renderiza; el run play-mode en batch se cuelga/segfaulta al salir: 2 intentos, no insistir). Pendiente que el usuario lo mida en el Editor con `Popayork/Medir FPS Mision1` (600 cuadros, loguea promedio).

## Falta
- Decisión narrativa misión 2 (`porque ______` en AGENTS.md §1).
- Fases 4B-7 según `docs/PLAN.md`.
- Integración pendiente (Fase 4B): proyectiles del jugador aún solo dañan dianas, no agentes; oleadas aún no cableadas en Mision1.

## Roto / Limitaciones conocidas
- Batch en Ubuntu 26.04 exige compat libxml2 (ver AGENTS.md > Decisiones); sin eso el Editor ni arranca.
- `model.zip`/`model.jpeg` originales intactos; se agregaron `source/model.fbx` + `source/model.jpg` (2k). `.blend` x3 no nativos sin Blender.
- Lock stale `Temp/UnityLockfile` tras crash del perf: se borra si no hay Editor abierto (regenerable, no se commitea).
- Warnings de importación de los .blend de terceros (ajenos al código propio, se mantienen).

## APIs Unity 6.6 verificadas con compilador
- `FindFirstObjectByType` obsoleto (CS0618) → usar `FindAnyObjectByType`.
- `UnityEventTools.AddFloatPersistentListener` exige argumento float explícito.
- `JsonUtility` sin referencia de módulo en este proyecto → JSON manual en SaveSystem.
- `ParticleSystem` exige paquete `com.unity.modules.particlesystem` (agregado).
- En edit-mode `Awake` no corre sin `ExecuteAlways` → init perezoso en componentes testeados.
- `PrefabUtility.SaveAsPrefabAsset` (API vigente para guardar prefabs desde Editor).
- AI Navigation 2.0.14: `NavMeshCollectGeometry`, `BuildNavMesh()` void, `MedQualityObstacleAvoidance`, `FindObjectsByType` sin sort mode.
- NavMesh bake en 2 pasadas (escena recién abierta); `NavMesh.CalculateTriangulation` funciona en edit-mode para diagnosticar bakes vacíos.

## Instrucciones de prueba
1. Abrir Mision1, Play: aparecer en la plaza, caminar entre bancas/árboles sin atravesar fachadas ni caer del mapa.
2. Ver la Torre del Reloj blanca con reloj y techo de teja al nororiente de la plaza.
3. Atardecer + niebla ligera sobre la ciudad blanca de fondo.
4. `Popayork/Verify Fase 4A` (+ F1/F2/F3): todo PASS.
5. `Popayork/Medir FPS Mision1` en el Editor y anotar el promedio aquí.
