# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 3 cerrada (compila, Verify F3 6/6 PASS, F1/F2 re-PASS, commit "Fase 3").

## Hecho (Fase 3)
- NavMesh: paquete `com.unity.ai.navigation 2.0.14` (verificado en registry) + `com.unity.modules.ai`; bake por script (`Popayork/Construir Fase 3`) en TestArena, ruta `PathComplete`.
- IA policial con FSM (Avanzar/Buscar cobertura/Atacar/Retirarse con poca vida) sobre NavMeshAgent; modelos `Police idle1/walk1_gameasset`.
- Aliados SENA (`Man01`, chaleco naranja) y universitarios (`Woman01`, chaleco celeste) que siguen objetivo y pelean; enemigos con anillo rojo. Chalecos/anillos por código.
- Oleadas por ScriptableObject (`Oleada_Prueba` 6, `Oleada_Maxima` 30); tope 30 activos, scheduler escalonado (1/3 por cuadro).
- Caída sin gore (caen y se retiran); pooling de 30 agentes; frases colombianas por facción en subtítulos.
- `Popayork/Verify Fase 3`: 6/6 PASS (NavMesh, oleada 6/6, avance 42.2m→5.7m, muerte, 0 bytes con 30 agentes en 300 ticks, subtítulos). Cero warnings CS propios.

## Correcciones de modelos por código (Fase 3)
- `Police idle1` 1.53m→×1.141, `Police walk1` 1.38m→×1.270, `Man01`/`Woman01` 0.59m→×2.979 (objetivo 1.75m); pivotes recentrados. `kneeling aiming` disponible para Fase 4.

## Falta
- Decisión narrativa misión 2 (`porque ______` en AGENTS.md §1).
- Fases 4A-7 según `docs/PLAN.md`.
- Integración pendiente (Fase 4B): proyectiles del jugador aún solo dañan dianas, no agentes.

## Roto / Limitaciones conocidas
- Batch en Ubuntu 26.04 exige compat libxml2 (ver AGENTS.md > Decisiones); sin eso el Editor ni arranca.
- `.blend` x3 no nativos sin Blender; `model.zip` no importable; archivos >50MB (Police FBX 141M, model.zip 97M, model.jpeg 69M).
- Warnings de importación de los .blend de terceros (ajenos al código propio, se mantienen).

## APIs Unity 6.6 verificadas con compilador
- `FindFirstObjectByType` obsoleto (CS0618) → usar `FindAnyObjectByType`.
- `UnityEventTools.AddFloatPersistentListener` exige argumento float explícito.
- `JsonUtility` sin referencia de módulo en este proyecto → JSON manual en SaveSystem.
- `ParticleSystem` exige paquete `com.unity.modules.particlesystem` (agregado).
- En edit-mode `Awake` no corre sin `ExecuteAlways` → init perezoso en componentes testeados.
- `PrefabUtility.SaveAsPrefabAsset` (API vigente para guardar prefabs desde Editor).
- AI Navigation 2.0.14: `NavMeshCollectGeometry`, `BuildNavMesh()` void, `MedQualityObstacleAvoidance`, `FindObjectsByType` sin sort mode.

## Instrucciones de prueba
1. TestArena en Play: oleada manual con `WaveManager.StartWave` (o desde Consola) y ver policías avanzar/atacar/retirarse.
2. Aliados SENA (naranja) y Uni (celestes) pelean contra policías; los caídos se retiran sin gore.
3. Subtítulos inferiores muestran frases ("¡Quieto ahí, parce!", "¡Aguante, parceros!").
4. Con 30 agentes el juego mantiene fluidez (IA escalonada 1/3 por cuadro).
5. `Popayork/Verify Fase 3` (+ F1/F2): todo PASS.
