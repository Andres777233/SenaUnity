# AGENTS.md — Popayork — Fuente de verdad en cada sesión

Este archivo es la fuente de verdad en cada sesión. Contiene íntegras y sin resumir las secciones 1 a 7 del mensaje inicial del proyecto. No modificarlas sin orden explícita del usuario. Los cambios y decisiones se registran en `> Decisiones` al final sin alterar las secciones 1-7.

# 1. Visión del juego
- Título: "Popayork". Debajo, subtítulo: "Sena Vs Universitarios".
- Shooter en primera persona con campaña por misiones, estilo low poly.
- Ambientación: Popayán, Cauca (Colombia), "la ciudad blanca": edificios coloniales blancos con tejados de teja. Una marcha estudiantil que se descontrola: caos, explosiones, fuego y humo, empezando por el Parque Caldas.
- Tono: acción caótica con sátira y humor colombiano. Frases de NPC y subtítulos en español colombiano.
- Jugador: aprendiz del SENA con uniforme reconocible, pero con logo e insignias INVENTADOS (sin logos oficiales). Las fuerzas antidisturbios y "SMART" usan insignias ficticias.
- [DECISIÓN NARRATIVA: en la misión 1 SENA y universitarios son aliados; en la misión 2 los universitarios son rivales porque un sector los acusa de haber "vendido el paro" a cambio de un convenio y ahora ven a los aprendices del SENA como esquiroles ]

# 2. Entorno (obligatorio respetar)
- Ubuntu Linux. Nada de rutas de Windows.
- Unity 6.6 (6000.6.2f1). Lenguaje C#.
- Proyecto: /home/andres/Escritorio/SENAUNITY (respeta mayúsculas).
- Ejecutable de Unity: [RUTA].
- Tu conocimiento de Unity 6.6 puede estar desactualizado. Si dudas de una API, dilo en vez de inventarla y anótala en docs/ESTADO.md para verificarla con el compilador. Usa APIs modernas de Unity 6 (FindFirstObjectByType, linearVelocity, etc.).
- Detecta qué sistema de input está activo (Packages/manifest.json y activeInputHandler en ProjectSettings) y usa ese. No lo cambies sin preguntarme.
- Detecta el pipeline de render instalado y úsalo. No lo cambies sin preguntarme.
- Yo cierro el Editor cuando compilas en batch. Si el batch falla por licencia u otro motivo, no insistas más de dos veces: dímelo y yo compilo desde el Editor y te pego los errores de la consola.

# 3. Restricciones técnicas
- No puedes hacer clic en el Editor. Toda escena, prefab y configuración se genera con scripts C# en Assets/Editor/ con menús Popayork/...
- Cada fase incluye un script Popayork/Verify FaseN con comprobaciones automáticas que escriban PASS o FAIL en la consola y en el log.
- Compilación en batch: -batchmode -nographics -quit -projectPath -executeMethod -logFile, y revisa el log.
- Rendimiento: object pooling para proyectiles y enemigos, sin asignaciones de memoria por frame, máximo 30 agentes de IA activos, objetivo 60 FPS en mi equipo.
- Arquitectura: carpetas por dominio (Core, Player, Weapons, Enemies, Missions, UI, Vehicles, World), ScriptableObjects para datos (armas, oleadas, misiones), eventos en vez de referencias directas.
- Todo dato ajustable (velocidades, daño, medidas) va en ScriptableObjects o configuración, no en el código.

# 4. Estándares de calidad (definición de "bien hecho")
- Visual: low poly con flat shading, paleta limitada por escena, niebla y luz cálida de atardecer, humo y fuego con partículas. Posprocesado ligero (bloom y viñeta).
- Game feel: retroceso y sacudida de cámara al disparar, hit markers, partículas y sonido de impacto, indicador direccional de daño, cambios de FOV al correr.
- Sin gore: los enemigos caen o se retiran, sin sangre ni desmembramiento.
- UI: objetivo siempre visible, barra de vida, munición, subtítulos, sensibilidad y FOV ajustables.
- Flujo: checkpoints y reintento de misión en menos de 3 segundos tras morir.
- Cada misión tiene inicio claro, objetivo, progreso visible, victoria y derrota.
- Sin errores ni warnings de código propio en la consola al terminar una fase.

# 5. Reglas de trabajo
- Trabaja por fases. No pases a la siguiente sin cerrar la actual. Si una fase es demasiado grande para una sesión, divídela en sub-pasos con commit por sub-paso.
- Al final de cada fase: compila, ejecuta su Verify, actualiza docs/ESTADO.md con instrucciones de prueba de máximo 5 líneas y haz commit "FaseN".
- Nunca hagas commit de Library/, Temp/ ni Logs/.
- Ninguna acción destructiva (rm, mv de carpetas, borrar escenas) sin preguntarme.
- Si algo es ambiguo, haz UNA pregunta corta o decide y registra en AGENTS.md > Decisiones.
- Nunca declares algo "listo" sin haber verificado que compila. Si no pudiste compilar, dilo explícitamente.

# 6. Fases
1. Base: escenas, GameManager, SceneLoader, guardado, menú, selección de misiones, pausa.
2. Jugador: controlador FPS, vida, 2 armas, HUD, TestArena.
3. Enemigos y aliados: IA policial, aliados estudiantes, oleadas.
4A. Escenario Parque Caldas construido desde Assets/MapaPopayan.
4B. Misión 1 "Empieza el caos": lógica, caos y objetivo Torre del Reloj.
5A. Misión 2, primera mitad: caballos y ruta al Morro de Tulcán.
5B. Misión 2, segunda mitad: defensa del Morro y retirada.
6. Misión 3: descenso en cartón hasta el río.
7. Pulido: audio, optimización, pantallas finales, créditos y build para Linux.

# 7. Formato de tus respuestas
Al terminar cada fase: (a) qué hiciste, (b) cómo lo pruebo, (c) qué falta o está roto. Máximo 5 líneas cada uno.

---

# Decisiones
- 2026-09-19 (Fase 1): ejecutable Unity = `/home/andres/Unity/Hub/Editor/6000.6.2f1/Editor/Unity` (resuelve `[RUTA]` de §2).
- 2026-09-19 (Fase 1): batch requiere `LD_LIBRARY_PATH=/tmp/opencode/unity-compat` con symlink `libxml2.so.2 -> /usr/lib/x86_64-linux-gnu/libxml2.so.16` (Ubuntu 26.04 solo trae libxml2.so.16).
- 2026-09-19 (Fase 1): agregado `com.unity.ugui 2.0.0` (resolvió 2.6.0) a `Packages/manifest.json` porque venía vacío y sin él no existe `UnityEngine.UI`. Input sin cambiar (Old Input Manager vía `KeyCode`) y render sin cambiar (Built-in, sin RP).
- 2026-09-19 (Fase 1): tecla de pausa = `P` (no usa Escape para no chocar con liberar cursor del Editor).
- 2026-09-19 (Fase 1): usar `FindAnyObjectByType` en vez de `FindFirstObjectByType` (obsoleto CS0618 en 6.6).
- 2026-09-19 (Fase 1): `SaveSystem` usa JSON manual propio (el módulo JSONSerialize no quedaba referenciado por Assembly-CSharp en este proyecto).
- 2026-09-19 (Fase 2): agregado `com.unity.modules.particlesystem 1.0.0` (módulo built-in; sin él no existe `ParticleSystem`). Armas elegidas de `Guns.fbx`: `MSR` (Fusil MSR) y `BE1` (Subfusil BE1).
- 2026-09-19 (Fase 2): en edit-mode `Awake` no corre (sin `ExecuteAlways`); los componentes usan init perezoso (`EnsureParts`) para que el Verify pueda probarlos sin Play mode.
- 2026-09-19 (Fase 3): `com.unity.ai.navigation` NO venía instalado; verificado en registry (`latest 2.0.14`, sin adivinar) y agregado `2.0.14` + `com.unity.modules.ai 1.0.0` (built-in). Modelos: policías `Police idle1/walk1_gameasset`, aliados `Man01` (SENA) y `Woman01` (Uni).
- 2026-09-19 (Fase 3): API real de AI Navigation 2.0.14 (leída del paquete, no adivinada): `useGeometry` es `NavMeshCollectGeometry`, `BuildNavMesh()` devuelve `void`, `ObstacleAvoidanceType.MedQualityObstacleAvoidance`; `FindObjectsByType` sin `FindObjectsSortMode` (obsoleto).
- 2026-09-19 (Fase 3): el registro del pool en memoria no se guarda en el `.unity`; `AgentPool.Discover()` re-registra al cargar y los datos de agentes van serializados (`[SerializeField]`).
- 2026-09-19 (Fase 4A): `model.zip` descompimido (solo lectura; originales intactos): `model.fbx` (36MB, 1 malla "Model", 939716 verts) + `model.jpg` reducido a 2048px con PIL (original 16384px/66MB) en `Assets/MapaPopayan/source/`. La ciudad es una sola malla: "sector del parque" = zona jugable acotada con muros.
- 2026-09-19 (Fase 4A): el bake NavMesh sobre colisionadores recién creados sale vacío; se bacea en 2ª pasada con la escena recién abierta (verificado empíricamente).
- 2026-09-19 (Fase 4A): `AudioListener` agregado a la cámara del jugador (faltaba; avisaba en Play). Parche aplicado a prefab-vía-escenas Mision1 y TestArena.
- 2026-09-19 (Fase 4B): proyectiles del jugador dañan policías (`AgentHealth` + cápsula trigger en agentes; aliados excluidos de fuego amigo). Victoria de Mision1 guarda directo con `SaveSystem` (funciona en edit-mode) y refresca `GameManager` en play.
- 2026-09-19 (Fase 5A): decisión narrativa misión 2 completada en §1 (universitarios rivales por acusación de "vender el paro"); nuevo `AgentData.hostile` (policías y Uni rivales = true) en vez de filtrar por facción.
- 2026-09-19 (Fase 5A): `Horse.blend` inservible (exportador FBX de Blender 5.0 roto + script Unity incompatible); malla real extraída con Blender a `HorseMesh.json` (952 verts, 3 materiales) y reconstruida por código en el builder.
- 2026-09-19 (Fase 5B): SMART como `AgentData` (facción Police, anillo casi negro, `Police idle2`); `WaveEntry.variant` selecciona modelo en oleadas; defensa con 2º WaveManager y victoria por eliminación o retirada a cartones.
- 2026-09-19 (Fase 6): NavMesh `Volume` bakea vacío en Mision3 pero `Children` (pista emparentada) sí (379 verts); el shutdown del batch a veces segfaultea tras el PASS (inestabilidad del batch, no del juego).

# Anexos operativos verificados
- Proyecto existe en /home/andres/Escritorio/SENAUNITY. Verificado con `ls`: existe `ProjectSettings/` (contenía solo `ProjectVersion.txt` el 2026-09-19) y `Assets/` con `MapaPopayan/` + `Models3D/`.
- Unity 6.6 verificado: `ProjectSettings/ProjectVersion.txt` = `m_EditorVersion: 6000.6.2f1`.
- Referencia de assets reales: `docs/INVENTARIO_ASSETS.md` (39 archivos). No inventar assets.
- Pendiente: ruta real del ejecutable de Unity (en sección 2 figura `[RUTA]` sin definir).
- Pendiente: decisión narrativa misión 2 (`porque ______`).
