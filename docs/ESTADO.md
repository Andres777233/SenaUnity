# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 7 cerrada — CAMPAÑA COMPLETA (Verify Final PASS + 8 fases re-PASS, build Linux arranca, commit "Fase 7").

## Hecho (Fase 7)
- Audio 100% procedural marcado PLACEHOLDER: música menú/misión, pasos, clic, boom, viento (`AudioManager` persistente por escena).
- Optimización: sombras OFF en ciudad (1.5M tris), 60 FPS objetivo (`PerfBoot`), calidad Alta/Baja en Opciones (persistida).
- Créditos en menú; pantallas de victoria/derrota en las 3 misiones (verificadas); `ProductName=Popayork` por script.
- Build Linux `Builds/Linux/Popayork.x86_64` (169MB) con `Popayork/Compilar Build`; arranque headless limpio (exit 0, sin excepciones).
- `Popayork/Verify Final`: M1→M2→M3 en orden con desbloqueos encadenados (`Campania_Completa`). Cero errores/warnings CS propios.

## Bugs conocidos (recorrido completo de campaña en batch)
- CORREGIDOS: `AudioClip` stream+SetData (log del build); enemigos/caos actuando en intros (freeze `timeScale`); `AudioListener` faltante en MainMenu.
- ABIERTOS (no críticos): clic UI sin sonido; Uni rival usa frases de Uni leal; caballo invulnerable (diseño); test [Choque] 1 flaky entre runs; altura del punto de salida M2 aproximada.
- PENDIENTE USUARIO (no medible headless): FPS real con GPU, QA visual/sonoro, sensibilidad de juego real.

## Rendimiento
- Mision1: 125 renderers, 1512418 tris. Mision3: 259 renderers, 83602 tris. Verifies de GC: 0 bytes (disparo, IA×30).
- FPS con render: pendiente de medición en Editor por el usuario.

## Falta
- Nada obligatorio. Opcional: bloom/viñeta (requiere RP, no instalado por decisión), sonidos no-procedurales, QA visual.

## Roto / Limitaciones conocidas
- Batch en Ubuntu 26.04 exige compat libxml2 (ver AGENTS.md > Decisiones).
- `model.zip`/`model.jpeg`/`Horse.blend` originales intactos. `.blend` restantes no nativos.
- Shutdown del batch a veces segfaultea tras el PASS (teardown, no juego).
- `Builds/` no se commitea (gitignore).

## APIs Unity 6.6 verificadas con compilador
- `FindFirstObjectByType` obsoleto (CS0618) → `FindAnyObjectByType`; `FindObjectsByType` sin sort mode.
- `UnityEventTools.AddFloatPersistentListener` exige float; `RemovePersistentListener(target, index)` existe.
- `JsonUtility` sin módulo → JSON manual; `ParticleSystem` exige `com.unity.modules.particlesystem`.
- En edit-mode `Awake` no corre sin `ExecuteAlways` → init perezoso.
- `PrefabUtility.SaveAsPrefabAsset`; AI Navigation: `NavMeshCollectGeometry`, `BuildNavMesh()` void, `MedQualityObstacleAvoidance`.
- Bake en 2 pasadas; `Volume` falló en Mision3 y `Children` sí; `CalculateTriangulation` diagnostica en edit-mode.
- `AudioClip.Create(stream:true)` + `SetData` = error (usar `false`); `PlayerSettings.productName` por script; `BuildPipeline.BuildPlayer` Linux64 OK.

## Cómo jugar (build)
1. Ejecutar `Builds/Linux/Popayork.x86_64` (o abrir el proyecto en el Editor).
2. Campaña: Misión 1 → Misión 2 (caballo con E) → Misión 3 (cartón); P pausa siempre.
3. Opciones: volumen, sensibilidad, calidad Alta/Baja; créditos en el menú.
4. `Popayork/Verify Final` (+ fases 1-6): todo PASS.
5. `Popayork/Compilar Build` regenera el build Linux.
