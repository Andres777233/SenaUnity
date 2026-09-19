# DIAGNÓSTICO — Modelo de Assets/MapaPopayan

Fecha: 2026-09-19. El modelo NO se borra ni se modifica (solo lectura; originales intactos).

## Medidas del modelo (MEDIDO por script Python sobre el FBX binario)
- Archivo: `source/model.fbx`, 36169936 bytes (36MB). Original `model.zip` (97MB) intacto.
- Contenido: UNA sola malla llamada `Model`, 752113 vértices (939716 según Unity, que duplica por UV/normales), ~1500000 triángulos, 1 material, 1 textura.
- Bounds (ejes FBX, Z arriba): X −854.8…854.8, Y −688.8…688.8, Z −50.7…90.2 → ciudad de ~1709×1378 m y 141 m de desnivel.
- Escala: 1 unidad = 1 metro (verificado: mediana de altura en el sector del parque 4A = 77.4 en FBX = 77.46 medido en Unity).
- Mapeo de ejes verificado empíricamente: Unity(x, z) = FBX(x, −y).
- Textura: `source/model.jpg` 2048px (1.2MB) reducido con PIL del original de 16384px/66MB (`textures/model.jpeg` intacto).

## Suelo en el origen (base de la estatua según especificación)
- Radio 15 m autour de FBX(0,0) = Unity(0,0): 454 vértices, min 59.1 / mediana 66.7 / max 71.7 (13 m de desnivel: zona construida o ladera, NO explanada plana).
- Radio 40 m: min 57.3 / mediana 65.8 / max 72.6.
- Conclusión: en el origen no hay una plaza plana en el modelo; la plataforma de la plaza debe nivelarse por código (losa a max+0.3 ≈ 72.0, MEDIDO max, losa ESTIMADA).

## Recomendación
Se acepta la decisión de partida: construir la plaza de 80×80 y sus cuatro costados POR CÓDIGO según `docs/ESPECIFICACION_PARQUE_CALDAS.md`, y usar el modelo solo como referencia de escala (ya validada) y silueta lejana de fondo (ya está como `CiudadBase` en Mision1).
Razones con datos:
1. El modelo es una sola malla inseccionable: no se puede extraer "solo el parque" sin reescribir el FBX.
2. Un MeshCollider de 1.5M tris es caro de cocinar y frágil (aviso Fast Midphase ya visto); losas y cajas dan colisión exacta y barata.
3. El NavMesh sobre geometría de fotogrametría sale ruidoso; sobre losas planas sale limpio (1583 verts en Fase 4A).
4. Las dimensiones de la especificación (80×80, calles 10 m, senderos 3 m) son ESTIMADO y mandan sobre el modelo.
