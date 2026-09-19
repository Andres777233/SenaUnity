# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 5A cerrada (compila, Verify 5A 4/4 PASS, F1/F2/F3/4A/4B re-PASS, commit "Fase 5A").

## Hecho (Fase 5A)
- Decisión narrativa misión 2 completada en AGENTS.md §1 (universitarios rivales por acusación de "vender el paro"); `AgentData.hostile` nuevo (policías y Uni rival = true).
- Caballo con modelo real: `Horse.blend` inservible en Blender 5.0/Unity (exportador roto) → malla extraída a `HorseMesh.json` (952 verts, 3 materiales) y reconstruida por código (medido 9.27m → escala ×0.237 para 2.2m). Montar/desmontar con E, trote 6 / galope 11, espera donde te bajas, cámara FP en la silla.
- Mision2: ruta parque→Morro de 447m (Morro medido: cima (520, 78, 355)), camino + barandas + 12 baldosas de colisión + campamento + pirámide del Morro; 3 volúmenes NavMesh; Mision2 registrada en Build Settings.
- 4 checkpoints en orden (2 a caballo, 1 emboscada policía+Uni rival, llegada) con brújula al Morro y progreso; llegada guarda `Mision2_Ruta`.
- `Popayork/Verify Fase 5A`: 4/4 PASS. Cero errores/warnings CS propios.

## Rendimiento (vigente)
- Mision1: 125 renderers, 1512418 tris, 89 colliders (batch). FPS con render pendiente (usuario en Editor con `Popayork/Medir FPS Mision1`).

## Falta
- Fases 5B-7 según `docs/PLAN.md`.

## Roto / Limitaciones conocidas
- Batch en Ubuntu 26.04 exige compat libxml2 (ver AGENTS.md > Decisiones); sin eso el Editor ni arranca.
- `model.zip`/`model.jpeg`/`Horse.blend` originales intactos. `.blend` restantes no nativos (exportador Blender 5.0 roto para FBX/OBJ: solo gltf/fbx mínimo sirven).
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
1. Mision2 en Play: intro, E para montar el caballo junto al campamento, WASD + Shift (galope) por el camino.
2. Seguir la flecha naranja (distancia al Morro); E desmonta y el caballo espera para remontar.
3. Emboscada en el cruce: desmontar y pelear con tombos y Uni rivales (anillo rojo).
4. Llegar a la cima: mensaje de llegada; P pausa como siempre.
5. `Popayork/Verify Fase 5A` (+ resto): todo PASS.
