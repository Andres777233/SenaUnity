# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Fase 1 cerrada (compila, Verify PASS, commit "Fase 1").

## Hecho (Fase 1)
- Estructura `Assets/Scripts/{Core,Player,Weapons,Enemies,Missions,UI,Vehicles,World}` + `Assets/Scenes` + `Assets/Editor`.
- 5 escenas generadas con `Popayork/Construir Proyecto` y registradas en Build Settings: MainMenu, Campaign, Mision1, Mision2, Mision3.
- GameManager persistente (DontDestroyOnLoad) + SceneLoader con pantalla de carga.
- Guardado JSON en `Application.persistentDataPath/popayork_save.json` (desbloqueadas/completadas, volumen, sensibilidad).
- Menú principal ("Popayork" / "Sena Vs Universitarios", Campaña/Opciones/Salir), selección (solo Misión 1 desbloqueada), pausa con tecla P.
- `Popayork/Verify Fase 1`: 5/5 PASS. Compilación batch EXIT=0, cero errores y cero warnings CS de código propio.
- Detección entorno: input = Old Input Manager (sin cambios), render = Built-in (sin cambios), solo se agregó `com.unity.ugui`.

## Falta
- Decisión narrativa misión 2 (`porque ______` en AGENTS.md §1).
- Fases 2-7 según `docs/PLAN.md`.

## Roto / Limitaciones conocidas
- Batch en Ubuntu 26.04 exige compat libxml2 (ver AGENTS.md > Decisiones); sin eso el Editor ni arranca.
- `.blend` x3 no nativos sin Blender; `model.zip` no importable; archivos >50MB (Police FBX 141M, model.zip 97M, model.jpeg 69M).
- Warnings de importación de los .blend de terceros (ajenos al código propio, se mantienen).

## APIs Unity 6.6 verificadas con compilador
- `FindFirstObjectByType` obsoleto (CS0618) → usar `FindAnyObjectByType`.
- `UnityEventTools.AddFloatPersistentListener` exige argumento float explícito.
- `JsonUtility` sin referencia de módulo en este proyecto → JSON manual en SaveSystem.

## Instrucciones de prueba
1. Abrir el proyecto, escena MainMenu, Play: ver título, subtítulo y 3 botones.
2. Campaña: solo Misión 1 interactuable; Misión 2/3 bloqueadas.
3. Entrar a Misión 1, pulsar P: pausa con Continuar/Opciones/Menú.
4. Cambiar volumen/sensibilidad, salir a menú y reentrar: valores persisten.
5. `Popayork/Verify Fase 1` en Editor: todo PASS.
