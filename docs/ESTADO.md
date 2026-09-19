# ESTADO.md — Popayork — Hecho / Falta / Roto

Fecha: 2026-09-19. Fase actual: Parque Caldas EN CURSO (bloqueado: Editor abierto, sin batch).

## UNITY INSTALADO (2026-09-19, en esta máquina)
- Unity 6000.6.2f1 (changeset 770e33f6875c) en `/home/andres/Unity/Hub/Editor/6000.6.2f1`,
  descargado del CDN oficial (4.15 GB, hash de URL verificado). `Unity -version` → 6000.6.2f1 OK.
- Solo módulo Editor (sin IL2CPP/Android). Si el build Linux pide IL2CPP, hay que añadir el módulo.
- CachyOS/Arch sin libxml2.so.2 y sin sudo: compat en `/home/andres/Unity-descargas/compat`
  (symlink a libxml2.so.16). Ejecutar siempre con `LD_LIBRARY_PATH=/home/andres/Unity-descargas/compat`.
- Instalador guardado en `/home/andres/Unity-descargas/Unity-6000.6.2f1.tar.xz` (se puede borrar).
- SIN LICENCIA: generado `/home/andres/Unity-descargas/Unity_v6000.6.2f1.alf` para activación
  manual en license.unity3d.com/manual. Decisión usuario: él compila en su Editor.
- Sistema (2026-09-19, con sudo): `libxml2-legacy` + `git-lfs` instalados por pacman.
  Unity ya corre SIN `LD_LIBRARY_PATH` (el compat queda como respaldo).
- Batch intentado 1 vez: EXIT=198, `'com.unity.editor.headless' was not found`
  (0 entitlements, sin access token). BLOQUEADO por licencia: falta login con Unity ID
  o archivo .ulf. Sin .git en esta copia → LFS irresoluble aquí (solo `git lfs pull`
  donde esté el remoto).
- Probar: 1. `LD_LIBRARY_PATH=.../compat Unity -batchmode -nographics -quit -projectPath ... -logFile ...`
  2. Con licencia: `Popayork/Verify Parque`, `Verify Arte`, `Verify Fase 5A` en PASS.

## ALERTA LFS (2026-09-19, verificado en esta copia)
- `.gitattributes` marca `*.fbx *.zip *.jpeg *.jpg *.blend *.png` como Git LFS.
- En ESTA copia todos son punteros sin resolver (~130 bytes de texto): los 4 `.fbx`,
  los 3 `.blend`, las 30+ texturas (policías, armas, personas, `model.jpg`/`model.jpeg`).
  No hay `.git` ni `git-lfs` aquí, así que no se pueden descargar en esta máquina.
- El mapa nuevo (plaza, catedral, torre, fachadas, calles, palomas) usa 100% primitivas +
  `MaterialFactory` y NO depende de LFS: funciona igual con o sin binarios.
- Lo que SÍ depende de LFS: personajes, policías, armas, `CiudadBase` (model.fbx) yHorse.
  Si en tu Editor ves policías grises/sin textura, armas invisibles o la ciudad ausente,
  es por LFS no resuelto. Resolver: `git lfs pull` (o clonar de nuevo con LFS) en tu máquina
  y verificar que `model.fbx` pese ~36 MB y las texturas KB/MB, no 130 bytes.

## Hecho (Parque Caldas, parcial)
- `docs/ESPECIFICACION_PARQUE_CALDAS.md` creada íntegra + referencia en AGENTS.md (commit "Especificación Parque Caldas").
- `docs/DIAGNOSTICO_MAPA.md`: modelo de 1 malla inseccionable (939716 verts); recomendación = construir por código y usar el modelo de fondo.
- Código ESCRITO sin compilar: `ParqueCaldasBuilder` (`Popayork/Construir Parque Caldas`), `VerifyParque` (`Popayork/Verify Parque`), SO `ParqueCaldasConfig`/`ParqueVegetacion`, `Documentado` en `ValorFuente`.
- Decisión tomada y registrada: estatua en el origen con losa nivelada a 72.0 (suelo real con 13 m de desnivel); torre SW en (-36,-36).

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

## Mejora visual Popayán (2026-09-19, SIN COMPILAR — sin Unity en esta máquina)
- Nuevo `Assets/Scripts/World/PalomaVuelo.cs`: paloma ambiental en círculo (centro/radio/altura/vel), sin allocations.
- Nuevo `Assets/Editor/ParqueCaldasDetalle.cs` (`Popayork/Mejorar Parque Caldas`): pase aditivo e
  idempotente sobre Mision1 (grupo `DetallePopayan`, backup del .unity, nada de gameplay se mueve):
  Catedral con arcos de campanario + 2 campanas + cruz + escalinata + reloj; Torre del Reloj con
  zócalo, 3 molduras, reloj sur, campana y cruz; cornisas + marcos de madera en las 8 fachadas;
  4 calles (10 m) con aceras y línea amarilla; Panteón con 6 columnas + frontón + gradas;
  rosa de los vientos de 8 puntas; 5 palomas + 3 en vuelo. Todo con `MaterialFactory` (paleta de 8).
- `ParqueCaldasBuilder.BuildAll` ahora llama al detalle antes de guardar.
- `VerifyParque` suma `[Detalle]`: 22 objetos obligatorios + cornisas≥8, marcos≥20, palomas≥8.
- Decisión: detalle sin colliders (adosado a muros existentes) para no alterar el bake NavMesh.
- Sintaxis revisada por conteo (archivos nuevos OK; `ParqueCaldasBuilder` conserva su estructura,
  solo +1 línea). Probar en Editor: 1. `Popayork/Construir Parque Caldas` (o `Mejorar Parque Caldas`).
  2. `Popayork/Verify Parque` debe dar PASS. 3. Abrir Mision1 y revisar la plaza en Game view.
  4. Compilar batch y pegar errores si hay.

## BLOQUES ARTE 0-5 (2026-09-19, SIN COMPILAR — sin Unity en esta máquina)
- Entorno: proyecto real en `SenaUnity-master` (AGENTS.md §2 dice `SENAUNITY`, no existe).
  No hay ejecutable Unity ni repo git aquí: NO se compiló, NO se ejecutó Verify Arte,
  NO hay commits. El usuario debe compilar/verificar en su Editor (líneas de prueba abajo).
- BLOQUE 0: `docs/DIAGNOSTICO_ARTE.md` creado (pipeline built-in sin URP; Standard correcto;
  0 `.mat`; spawns sin SamplePosition; scalers sin configurar; CiudadBase = 1 malla 1709×1378 m).
- BLOQUE 1: `MaterialFactory` (`Scripts/Core`) único creador de materiales (Standard/URP Lit,
  flat sin brillo, caché en `Assets/Materials/Generated`); 10 archivos migrados, cero
  `Shader.Find`/`new Material()` fuera; menús `Popayork/Reparar Materiales` (backup en
  `Assets/Materials/Backup`, no toca .unity) y `Popayork/Verify Arte` (higiene + materiales).
- BLOQUE 2: spawns con `SamplePosition` (r=4) + `Warp`; `Activate` devuelve bool con warning
  del punto; `WaveManager`/M1/M2/M3 pasan nombre del punto; menú `Popayork/Hornear NavMesh`
  (backup en `Assets/Scenes/Backup`, agentTypeID 0, valida triangulación+spawns+rutas);
  Verify Arte simula oleada completa por escena. PENDIENTE: excluir árboles/edificios del
  bake por capas (requiere retaguear props + re-hornea; no se hizo para no cambiar cobertura).
- BLOQUE 3: `UiLayout.FixSceneUI()` (scaler 1920×1080 match 0.5; intro con LayoutGroup
  título/historia/botón; objetivo arriba + progreso debajo sin solape; HUD por anclas;
  pausa/resultados igual); llamado desde los 8 builders antes de guardar + menú
  `Popayork/Arreglar UI`; Verify Arte chequea scalers y solapes a 1920×1080/1280×720/2560×1440.
- BLOQUE 4 (solo diagnóstico, nada modificado): las formas grises retorcidas son la
  `CiudadBase` (instancia de `Assets/MapaPopayan/source/model.fbx`, malla única ~1.5M tris,
  1709×1378 m, textura 2048px estirada) vista de cerca junto a la plaza de 80×80.
  RECOMENDADO: excluirla de la vista cercana (dejarla solo como silueta lejana de fondo y
  tapar laterales con las fachadas por código); a medio plazo reexportar el sector
  recortado desde Blender. Alternativas descartadas: reescalar (escala 1u=1m validada).
- BLOQUE 5: menú `Popayork/Pasada Visual` (sol cálido con sombras suaves, skybox procedural
  atardecer built-in, niebla, auditoría paleta ≤8, fuego/humo ×1.6 y 36/s); posproceso
  bloom/viñeta NO aplicado (exige RP/paquetes). FPS antes/después: PENDIENTE medición usuario.
- DUDAS API anotadas: `materialImportMode: 2` (medido por Verify, no asumido); props del
  skybox procedural con `HasProperty` por seguridad; `NavMesh.GetSettingsByID` existe.
- Cómo probar (Editor, 5 líneas): 1. Abrir proyecto y `Popayork/Reparar Materiales`.
  2. `Popayork/Hornear NavMesh`. 3. `Popayork/Arreglar UI` por escena si no se reconstruye.
  4. `Popayork/Pasada Visual`. 5. `Popayork/Verify Arte` debe dar PASS; medir FPS y avisar.

## PASADA QA (2026-09-19, 4 subagentes solo-lectura + verificación propia, SIN COMPILAR)
- CORREGIDOS (verificado cada uno en código antes de tocar):
  P0 compilación: `ParqueCaldasBuilder` llave sobrante (SpawnPrefab fuera de clase);
  `VerifyParque` `FindObjectsByType` sin `Object.`.
  Detalle propio: Panteón flotante a y≈151 (ahora relativo a su base); backup sin carpeta;
  calles d=46 atravesaban fachadas (ahora vía peatonal 36-40 + acera, desviación ESTIMADO).
  Gameplay: balas solo dañaban Police → ahora `IsHostile` (M2 era inmune a tiros);
  `AITickScheduler`/`AgentPool.GetActive` guarda de índice (IndexOutOfRange en combate);
  BtnReintentar M2 a `StartRoute` → `Retry`; HUD inexistente en misiones → `UiLayout`
  crea HUD en MissionCanvas + VerifyArte lo exige (HealthFill/AmmoText/Crosshair).
  Re-ejecución: `RemoveAllListeners` en 4B/5A/6/7; MissionCanvas y SubtitleCanvas se
  recrean en 5A/6. Null-checks: WaveManager/Mission1Controller/Mission2Controller/PlayerWeapons/
  ImpactPool; respawn con `WaitForSecondsRealtime` (pausa ya no lo congela).
- BACKLOG QA verificado, NO corregido (ordenado): P sobre Resultados reactiva IA;
  M3 sin estado Derrota; M1 victoria por timer aunque kills=0; M2 compass tras defensa;
  M2 `SaveArrival` contamina completadas; `AllScenes` sin TestArena; Cardboard `Find` por frame;
  SaveSystem sin try/catch; duplicados por re-ejecución en Agents/NavMeshBake/Covers/Trinchera
  (5A/5B/6/4B); pausa sin botones en 5A/6; progreso BG ausente en 5A/6; retreatThreshold=8 vs
  oleada de 10 (victoria por eliminación inalcanzable en defensa M2); verifies 4A/Parque usan
  configs distintas; VerifyParque umbrales al límite.
- Probar: 1. Batch compilar (los P0 eran errores de compilación reales).
  2. `Verify Parque` + `Verify Arte` PASS. 3. M2: disparar a universitarios rivales.
  4. Morir en pausa y reintentar M2. 5. Reconstruir 5A/6 dos veces y jugar.

## MEJORAS sobre backlog QA (2026-09-19, SIN COMPILAR — sin Unity en esta máquina)
- Pausa: `P` se ignora con intro o resultados visibles (`MissionUI.IntroVisible` nuevo).
- M3: estado `Defeat` + derrota al morir descendiendo (antes imposible perder).
- M1: victoria por timer exige `kills>0` (no se gana escondido sin pelear).
- M2: compass apunta a `exitPoint` ("Salida") en retirada; `SaveArrival` ya no inventa
  `"Mision2_Ruta"`; `retreatThreshold` 8→12 (con 10 hostiles en oleada 3 la victoria por
  eliminación era inalcanzable; decisión registrada).
- `GameConfig.AllScenes` incluye `TestArena` (entra al build siempre).
- `SaveSystem` con try/catch (disco/corrupto → defaults + warning).
- `CardboardController` busca vientos cada 2 s, no cada frame.
- Builders 5A/6: pausa con Continuar/Menú funcionales; `ProgressBG` en barras de objetivo;
  sin duplicados al re-ejecutar (Agents, CoverM2_, NavCamp/Fight/Morro, Trinchera_, Carton_,
  Mission/SubtitleCanvas); 4B limpia hijos de Agents.
- Probar: 1. Batch compilar. 2. M3 morir en el descenso → derrota + reintento.
  3. M2 defensa oleada 3: verificar que se puede ganar eliminando. 4. P en intro/resultados
  no pausa. 5. Pausa en M2/M3 con botones. 6. Reconstruir 5A dos veces sin duplicados.

## VERIFICACIÓN PROFUNDA (2026-09-19, 2 subagentes + correcciones, SIN COMPILAR)
- Confirmado OK: HUD en misiones (firmas, nombres, orden de dibujo), pausa/intro/resultado,
  M3 derrota, kills de M1, compass, dedup (nada usa lo destruido), TestArena en build,
  flujo de munición/vida inicial, eventos sin fuga, Boot en 6 escenas, cadena de desbloqueos.
- Corregidos de esta ronda: `VerifyFase5A` exigía `"Mision2_Ruta"` (roto por mi fix; ahora
  verifica llegada SIN marcador falso); `SceneLoader` fallback restaura `timeScale=1`;
  `MissionSelect` con progreso real "X/3" y bloqueo sin GameManager; `RefillAll` re-emite
  el arma activa (HUD mostraba la última); subtítulos con `unscaledDeltaTime` (no se pegan
  tras resultado); textos "5 escenas"→6 en `VerifyFase1`.
- Riesgo conocido documentado: 2 `PauseMenuUI` en escena pelearían por `timeScale`
  (los builders crean uno por escena; no duplicar a mano).
- Leves sin tocar: `SaveLoaded` solo en Awake/Reload (funciona por persistencia + Refresh);
  créditos/calidad no existen sin correr Fase 7 (esperado).
- Probar: 1. Batch compilar. 2. `Verify Fase 5A` PASS (checkpoints). 3. Morir → respawn
  muestra munición del arma activa. 4. Subtítulo visible al ganar → se oculta solo.
