# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 2 cerrada (compila, Verify F2 6/6 PASS, F1 re-PASS, commit "Fase 2").

## Hecho (Fase 2)
- FPS con `CharacterController`: caminar/sprint/salto, mirada con sensibilidad del guardado, FOV 75→85 al correr, colisión con muros (no atraviesa).
- Vida/daño/muerte + reaparición en 1.5s (<3s) en punto de spawn, con refill de armas.
- 2 armas por ScriptableObject con modelos reales de `Guns.fbx`: Fusil MSR (9/s, 22 daño, 30+90) y Subfusil BE1 (12/s, 14 daño, 32+96); proyectiles e impactos con pooling, sonido placeholder procedural.
- Game feel: retroceso + sacudida de cámara, hitmarker (rojo si mata), partículas de impacto, indicador direccional de daño (4 flechas).
- HUD: vida, munición, mira, arma actual, aviso de reaparición. Escena TestArena (6 dianas, coberturas, muros) + prefab `Player`.
- `Popayork/Verify Fase 2`: 6/6 PASS, incluye 0 bytes asignados en 500 cuadros de movimiento+disparo. Cero errores/warnings CS propios.

## Correcciones de modelos por código (Fase 2)
- `MSR` medía 1.71m → escala ×0.439 para 0.75m; `BE1` medía 0.49m → escala ×1.222 para 0.60m. Pivotes recentrados en X/Z por `WeaponViewNormalizer`.

## Falta
- Decisión narrativa misión 2 (`porque ______` en AGENTS.md §1).
- Fases 3-7 según `docs/PLAN.md`.

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

## Instrucciones de prueba
1. Abrir TestArena, Play (clic en juego, WASD + mouse, Shift corre, Espacio salta, 1/2 cambian arma, R recarga, P pausa).
2. Disparar a dianas: caen y se levantan; hitmarker blanco/rojo y chispas en impacto.
3. Dejarse caer al vacío o verificar vida: al morir reaparece en <3s con vida y munición llenas.
4. Opciones: sensibilidad afecta la cámara y persiste entre sesiones.
5. `Popayork/Verify Fase 2` y `Popayork/Verify Fase 1`: todo PASS.
