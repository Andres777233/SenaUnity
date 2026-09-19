# PLAN.md — Popayork — Checklist de fases

Fuente de verdad: `AGENTS.md` (secciones 1-7 íntegras). Referencia de assets reales: `docs/INVENTARIO_ASSETS.md`. No inventar assets. No usar `-createProject`. Verificar `ProjectSettings/` con `ls` antes de correr Unity.

## Fase 1 — Base [CERRADA 2026-09-19: compila, Verify 5/5 PASS, commit "Fase 1"]
- [x] Escenas base (MainMenu, Campaign, Mision1/2/3) generadas y en Build Settings
- [x] GameManager + SceneLoader + sistema de guardado
- [x] Menú principal + selección de misiones + pausa (tecla P)
- [x] Script `Assets/Editor/` con menú `Popayork/Construir Proyecto`
- [x] Script `Popayork/Verify Fase 1` con PASS/FAIL en consola y log
- [x] Compilación batch + revisión de log + `docs/ESTADO.md` + commit "Fase 1"

## Fase 2 — Jugador [CERRADA 2026-09-19: compila, Verify F2 6/6 PASS + F1 re-PASS, commit "Fase 2"]
- [x] Controlador FPS + vida + 2 armas (datos en ScriptableObjects)
- [x] HUD (objetivo, vida, munición, subtítulos, sensibilidad/FOV ajustables)
- [x] Escena TestArena jugable
- [x] Game feel base (retroceso, sacudida, hit markers, FOV correr)
- [x] Verify Fase2 + compilación + ESTADO + commit "Fase2"

## Fase 3 — Enemigos y aliados [CERRADA 2026-09-19: compila, Verify F3 6/6 PASS + F1/F2 re-PASS, commit "Fase 3"]
- [x] IA policial (máx. 30 agentes activos, pooling, sin allocs por frame)
- [x] Aliados estudiantes
- [x] Sistema de oleadas (ScriptableObjects)
- [x] Enemigos caen/se retiran, sin gore
- [x] Verify Fase3 + compilación + ESTADO + commit "Fase3"

## Fase 4A — Escenario Parque Caldas desde Assets/MapaPopayan [CERRADA 2026-09-19: compila, Verify 4A 3/3 PASS + F1/F2/F3 re-PASS, commit "Fase 4A"]
- [x] Escena Mision1 con sector del parque (plaza medida por raycast, Torre del Reloj por código)
- [x] ParqueConfig con valores MEDIDO (14) / ESTIMADO (11)
- [x] Prefabs reutilizados (Arbol/Banca/Estatua/Farola) + fachadas con colisión + límites
- [x] Atardecer, niebla, NavMesh (1583 verts, PathComplete), spawns definidos
- [x] Verify Fase4A + compilación + ESTADO + commit "Fase4A"

## Fase 4B — Misión 1 "Empieza el caos" [CERRADA 2026-09-19: compila, Verify 4B 4/4 PASS + F1/F2/F3/4A re-PASS, commit "Fase 4B"]
- [x] Lógica misión: intro/tutorial, objetivo visible, progreso, victoria/derrota + pantallas
- [x] 3 oleadas progresivas a la Torre + 4 aliados; checkpoints por oleada, reintento <3s
- [x] Caos (fuegos/humos/explosiones con sacudida, escombros) y Misión 2 desbloqueada al ganar
- [x] Verify Fase4B + compilación + ESTADO + commit "Fase4B"

## Fase 5A — Misión 2 primera mitad
- [ ] Caballos (ver limitación: `Horse.blend` requiere Blender o exportar a FBX)
- [ ] Ruta al Morro de Tulcán
- [ ] Verify Fase5A + compilación + ESTADO + commit "Fase5A"

## Fase 5B — Misión 2 segunda mitad
- [ ] Defensa del Morro y retirada
- [ ] Verify Fase5B + compilación + ESTADO + commit "Fase5B"

## Fase 6 — Misión 3
- [ ] Descenso en cartón hasta el río
- [ ] Verify Fase6 + compilación + ESTADO + commit "Fase6"

## Fase 7 — Pulido
- [ ] Audio, optimización (pooling, 60 FPS objetivo), pantallas finales, créditos
- [ ] Build para Linux
- [ ] Posprocesado ligero (bloom + viñeta), sin errores/warnings propios
- [ ] Verify Fase7 + compilación + ESTADO + commit "Fase7"
