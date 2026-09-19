# Inventario Assets — SENAUNITY

Origen: `~/Escritorio/Models 3d.` y `~/Escritorio/MapaPopayan.` (copia exacta, sin reorganizar, sin .meta).
Destino: `Assets/Models3D/` y `Assets/MapaPopayan/`. Total: 39 archivos.

## Armas (3)
- Assets/Models3D/armas low poly/source/Sketchfab_2023_04_25_06_31_32.blend | .blend | 1.4M | ⚠️ NO nativo (requiere Blender instalado o exportar a FBX)
- Assets/Models3D/armas low poly 2/source/Guns.fbx | .fbx | 633K | OK nativo
- Assets/Models3D/armas low poly 2/textures/GradientCG.png | .png | 16K | OK nativo

## Policías (29)
- Assets/Models3D/Policias/source/Posed People by JJ - Police vol1 with HQ.fbx | .fbx | 141M | OK nativo (⚠️ >50MB, ver nota git)
- Assets/Models3D/Policias/textures/Arrested guy_gameasset_BaseColor.png | .png | 3.3M | OK
- Assets/Models3D/Policias/textures/Arrested guy_gameasset_Normal.png | .png | 3.5M | OK
- Assets/Models3D/Policias/textures/Black guy what_gameasset_BaseColor.png | .png | 3.6M | OK
- Assets/Models3D/Policias/textures/Black guy what_gameasset_Normal.png | .png | 3.4M | OK
- Assets/Models3D/Policias/textures/Detective_gameasset_BaseColor.png | .png | 4.4M | OK
- Assets/Models3D/Policias/textures/Detective_gameasset_Normal.png | .png | 4.3M | OK
- Assets/Models3D/Policias/textures/Gang member_gameasset_BaseColor.png | .png | 3.8M | OK
- Assets/Models3D/Policias/textures/Gang member_gameasset_Normal.png | .png | 3.9M | OK
- Assets/Models3D/Policias/textures/Police idle1_gameasset_BaseColor.png | .png | 3.4M | OK
- Assets/Models3D/Policias/textures/Police idle1_gameasset_Normal.png | .png | 3.8M | OK
- Assets/Models3D/Policias/textures/Police idle2_gameasset_BaseColor.png | .png | 3.8M | OK
- Assets/Models3D/Policias/textures/Police idle2_gameasset_Normal.png | .png | 4.1M | OK
- Assets/Models3D/Policias/textures/Police idle3_gameasset_BaseColor.png | .png | 3.6M | OK
- Assets/Models3D/Policias/textures/Police idle3_gameasset_Normal.png | .png | 4.1M | OK
- Assets/Models3D/Policias/textures/Police idle4_gameasset_BaseColor.png | .png | 3.7M | OK
- Assets/Models3D/Policias/textures/Police idle4_gameasset_Normal.png | .png | 3.8M | OK
- Assets/Models3D/Policias/textures/Police idle5_gameasset_BaseColor.png | .png | 3.3M | OK
- Assets/Models3D/Policias/textures/Police idle5_gameasset_Normal.png | .png | 3.7M | OK
- Assets/Models3D/Policias/textures/Police kneeling aiming_gameasset_BaseColor.png | .png | 4.0M | OK
- Assets/Models3D/Policias/textures/Police kneeling aiming_gameasset_Normal.png | .png | 4.4M | OK
- Assets/Models3D/Policias/textures/Police walk1_gameasset_BaseColor.png | .png | 3.5M | OK
- Assets/Models3D/Policias/textures/Police walk1_gameasset_Normal.png | .png | 3.8M | OK
- Assets/Models3D/Policias/textures/Police walk2_gameasset_BaseColor.png | .png | 3.5M | OK
- Assets/Models3D/Policias/textures/Police walk2_gameasset_Normal.png | .png | 3.6M | OK
- Assets/Models3D/Policias/textures/Police walk3_gameasset_BaseColor.png | .png | 3.4M | OK
- Assets/Models3D/Policias/textures/Police walk3_gameasset_Normal.png | .png | 3.7M | OK
- Assets/Models3D/Policias/textures/Police walk4_gameasset_BaseColor.png | .png | 3.5M | OK
- Assets/Models3D/Policias/textures/Police walk4_gameasset_Normal.png | .png | 3.8M | OK

## Personas (4)
- Assets/Models3D/personas modelos1/source/temp_export.fbx | .fbx | 271K | OK nativo
- Assets/Models3D/personas modelos1/textures/BlueGrid_studioochi.com.jpeg | .jpeg | 430K | OK nativo
- Assets/Models3D/Personajes Low poly/source/Low Poly SuperHero.blend | .blend | 427K | ⚠️ NO nativo (requiere Blender o exportar a FBX)
- Assets/Models3D/personas modelos1/textures/internal_ground_ao_texture.jpeg | .jpeg | 45K | OK nativo

## Caballo (1)
- Assets/Models3D/Caballo low poly/source/Horse.blend | .blend | 629K | ⚠️ NO nativo (requiere Blender o exportar a FBX)

## Mapa (2) — peso total: 166M
- Assets/MapaPopayan/source/model.zip | .zip | 97M | ⚠️ NO importable (contiene: model.fbx 36MB + model.jpg 66MB; hay que descomprimir y usar el FBX) + ⚠️ >50MB
- Assets/MapaPopayan/textures/model.jpeg | .jpeg | 69M | OK nativo (⚠️ >50MB, textura muy pesada, conviene comprimir)

## Otros (0)
- Ninguno.

## Formatos problemáticos
- `.blend` x3 (Horse, SuperHero, Sketchfab armas): Unity solo lo importa si Blender está instalado; si no, exportar a FBX desde Blender. No se instaló nada.
- `.zip` x1 (model.zip): Unity no lo importa; descomprimir manual y usar model.fbx + model.jpg.
- No hay `.glb/.gltf` en este lote; si aparecen luego haría falta paquete glTF (ej. glTFast) — no instalado sin tu permiso.
- Archivos >50MB x3: Police FBX 141M, model.zip 97M, model.jpeg 69M (aviso previo al commit según pedido).
