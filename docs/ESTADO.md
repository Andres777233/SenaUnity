# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 6 cerrada (compila, Verify 6 4/4 PASS, resto re-PASS, commit "Fase 6").

## Hecho (Fase 6)
- Mision3: ladera de ~600m con curvas generada por script (90 puntos, barandas, 24 pinos) + 3 rampas con salto + 8 rocas que frenan/dañan/giran sin matar.
- Cartón con inercia: acelera por pendiente (verificado 4.6 m/s en 3s), dirección, freno, topes, checkpoints con respawn y FOV 75→90 + vibración + estelas de viento.
- 3 policías persiguen al inicio (los dejas atrás); río con agua animada + espumas; llegada guarda Mision3 (+ `Campania_Completa` si están las 3).
- `Popayork/Verify Fase 6`: 4/4 PASS (pendiente, choque, río+guardado, stats 259 renderers/83k tris). Cero errores/warnings CS propios.

## Rendimiento (vigente)
- Mision1: 125 renderers, 1512418 tris, 89 colliders (batch). Mision3: 259 renderers, 83602 tris. FPS con render pendiente (usuario en Editor).

## Falta
- Fase 7 según `docs/PLAN.md` (audio, optimización, pantallas finales, créditos, build Linux).

## Roto / Limitaciones conocidas
- Batch en Ubuntu 26.04 exige compat libxml2 (ver AGENTS.md > Decisiones); sin eso el Editor ni arranca.
- `model.zip`/`model.jpeg`/`Horse.blend` originales intactos. `.blend` restantes no nativos (exportador Blender 5.0 roto).
- Shutdown del batch a veces segfaultea DESPUÉS del PASS (teardown del Editor, no del juego); un run colgó con lock stale (se borra si no hay Editor abierto).
- Test [Choque] mostró 1 flaky FAIL entre runs (física edit-mode); pasó en las demás (2/3). Vigilar.
- Warnings de importación de los .blend de terceros (ajenos al código propio, se mantienen).

## APIs Unity 6.6 verificadas con compilador
- `FindFirstObjectByType` obsoleto (CS0618) → usar `FindAnyObjectByType`.
- `UnityEventTools.AddFloatPersistentListener` exige argumento float explícito.
- `JsonUtility` sin referencia de módulo en este proyecto → JSON manual en SaveSystem.
- `ParticleSystem` exige paquete `com.unity.modules.particlesystem` (agregado).
- En edit-mode `Awake` no corre sin `ExecuteAlways` → init perezoso en componentes testeados.
- `PrefabUtility.SaveAsPrefabAsset` (API vigente para guardar prefabs desde Editor).
- AI Navigation 2.0.14: `NavMeshCollectGeometry`, `BuildNavMesh()` void, `MedQualityObstacleAvoidance`, `FindObjectsByType` sin sort mode.
- NavMesh bake en 2 pasadas (escena recién abierta); modo `Volume` falló en Mision3 y `Children` sí funcionó.
- `NavMesh.CalculateTriangulation` funciona en edit-mode para diagnosticar bakes vacíos.

## Instrucciones de prueba
1. Mision3 en Play: ¡Nos tiramos! A/D dirección, S freno, rampas y rocas en la pista.
2. FOV que sube con la velocidad + vibración; checkpoints y 3 tombos que se quedan atrás.
3. Caer al río: resultados y Mision3 guardada; P pausa como siempre.
4. Sin muerte injusta: los golpes solo frenan/dañan y reapareces en puerta.
5. `Popayork/Verify Fase 6` (+ resto): todo PASS.
