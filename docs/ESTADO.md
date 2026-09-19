# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 4B cerrada (compila, Verify 4B 4/4 PASS, F1/F2/F3/4A re-PASS, commit "Fase 4B").

## Hecho (Fase 4B)
- Misión 1 "Empieza el caos": intro con título + tutorial, objetivo visible y barra de progreso; victoria (3 oleadas o 240s) / derrota (policía en la Torre) con pantallas y reintento <3s.
- 3 oleadas progresivas (4/6/8 policías) hacia la Torre + 4 aliados defensores; checkpoints por oleada con subtítulo; al ganar se desbloquea Misión 2 en el guardado.
- Caos: 4 fuegos + 4 humos en loop, explosiones cada 12s con boom procedural, sacudida y daño en área, 8 escombros.
- Proyectiles del jugador ya dañan policías (cápsula trigger en agentes; aliados sin fuego amigo) con hitmarker.
- `Popayork/Verify Fase 4B`: 4/4 PASS (arranque, derrota sin defensa, victoria+guardado, caos). Cero errores/warnings CS propios.

## Rendimiento (Fase 4A, vigente)
- Presupuesto estático medido en batch: 125 renderers, 1512418 tris (99% ciudad estática, 1 draw call), 89 colliders, textura ciudad 2048px.
- FPS promedio con render: NO medible en batch (pendiente usuario en Editor con `Popayork/Medir FPS Mision1`).

## Falta
- Decisión narrativa misión 2 (`porque ______` en AGENTS.md §1).
- Fases 5A-7 según `docs/PLAN.md`.

## Roto / Limitaciones conocidas
- Batch en Ubuntu 26.04 exige compat libxml2 (ver AGENTS.md > Decisiones); sin eso el Editor ni arranca.
- `model.zip`/`model.jpeg` originales intactos; se agregaron `source/model.fbx` + `source/model.jpg` (2k). `.blend` x3 no nativos sin Blender.
- Lock stale `Temp/UnityLockfile` tras crash: se borra si no hay Editor abierto (regenerable, no se commitea).
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
1. Mision1 en Play: intro "EMPIEZA EL CAOS", botón ¡A defender la Torre!, objetivo y barra visibles.
2. Frenar 3 oleadas con aliados; explosiones sacuden la cámara; checkpoints por oleada.
3. Victoria desbloquea Misión 2 (ver en Campaña); derrota si un tomba toca la Torre.
4. Reintentar recarga en <3s; P pausa como siempre.
5. `Popayork/Verify Fase 4B` (+ F1/F2/F3/4A): todo PASS.
