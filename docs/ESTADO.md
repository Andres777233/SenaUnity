# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 5B cerrada (compila, Verify 5B 4/4 PASS, resto re-PASS, commit "Fase 5B").

## Hecho (Fase 5B)
- Defensa del Morro: 3 oleadas escaladas (4/5/10) con policía + SMART Táctico ficticio (`Police idle2`, anillo casi negro, 140 vida); `WaveEntry.variant` compone modelos por oleada.
- Retirada: con ≥8 hostiles activos suena el aviso y se activa la salida a los cartones; llegar completa Mision2 y desbloquea Mision3 en el guardado.
- Derrota a las 3 caídas con pantalla y reintento (<3s); victoria también por eliminar todo.
- Trincheras, cartones y 2º WaveManager en Mision2 + rebake NavMesh (303 verts).
- `Popayork/Verify Fase 5B`: 4/4 PASS (escala, retirada, salida+guardado, derrota). Cero errores/warnings CS propios.

## Rendimiento (vigente)
- Mision1: 125 renderers, 1512418 tris, 89 colliders (batch). FPS con render pendiente (usuario en Editor con `Popayork/Medir FPS Mision1`).

## Falta
- Fases 6-7 según `docs/PLAN.md`.

## Roto / Limitaciones conocidas
- Batch en Ubuntu 26.04 exige compat libxml2 (ver AGENTS.md > Decisiones); sin eso el Editor ni arranca.
- `model.zip`/`model.jpeg`/`Horse.blend` originales intactos. `.blend` restantes no nativos (exportador Blender 5.0 roto).
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
- Malla del camino con winding invertido no bakea (solo reversos); verificar normales hacia arriba.

## Instrucciones de prueba
1. Mision2: llegar al Morro (5A), defender 3 oleadas con aliados en trincheras.
2. Con 8+ enemigos llega el aviso: correr a los cartones (flecha/brújula) para ganar y desbloquear Mision3.
3. Morir 3 veces = derrota con reintento; P pausa como siempre.
4. SMART de negro con más vida y daño en la oleada final.
5. `Popayork/Verify Fase 5B` (+ resto): todo PASS.
