# Athanor — Clicker de Alquimia 🧪

Juego clicker/idle para Android con temática de alquimia genérica. Estilo flat/minimalista,
portrait, en español. Hecho con Unity 6.3 LTS (6000.3.19f1).

## Estado

**En desarrollo — Etapa 1** (clicker core). Ver plan de etapas abajo.

## Documentación

- [Documento de diseño (GDD)](docs/GDD.md) — paleta, árbol de 20 elementos con recetas,
  fórmulas de generadores y prestigio, logros, arquitectura.
- [Especificación de assets](docs/ASSETS.md) — qué archivos de arte/audio hay que generar,
  con nombre, tamaño y formato exactos.

## Plan de etapas

1. **Clicker core** — matraz clickeable, elementos base, guardado local, primer APK. ⬅️ acá
2. **Generadores idle** — ayudantes comprables, producción por segundo.
3. **Combinaciones** — árbol completo de 20 elementos con descubrimiento progresivo.
4. **Prestigio + laboratorio** — La Gran Obra, fondo que evoluciona con el progreso.
5. **Logros + audio + juice.**
6. **Progreso offline + modos de rendimiento + ajustes.**

Cada etapa termina en un APK instalable publicado como GitHub Release.

## Instalación en el celular

**Link directo a la última versión:**
https://github.com/SempaiN/athanor/releases/latest/download/athanor.apk

1. Abrí ese link en el navegador del celular y descargá el `.apk`.
2. Al abrir el archivo, Android va a pedir permiso para "instalar apps desconocidas" desde el
   navegador → **Permitir** (es un permiso por app, una sola vez).
3. Tocá **Instalar**. Listo. Las versiones nuevas se instalan encima sin perder el guardado.

Todas las versiones: https://github.com/SempaiN/athanor/releases

## Datos técnicos

- Paquete: `com.vesperoni.athanor` · Min SDK: 23 (Android 6.0) · Orientación: portrait
- Guardado: JSON local en `persistentDataPath` (arquitectura lista para nube)
- Localización: es (estructura lista para en) · Monetización: no activa (capa desacoplada)
