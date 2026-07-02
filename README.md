# Athanor — Clicker de Alquimia 🧪

Juego clicker/idle para Android con temática de alquimia. Flat design con ilustraciones,
portrait, en español. Unity 6 (6000.0.78f1). **Estado: completo y jugable (v1.8+).**

## Características

- **Clicker core**: matraz Erlenmeyer animado (respira, burbujea) que genera los 4 elementos base.
- **Árbol de 20 elementos** en 6 tiers con descubrimiento progresivo estilo Little Alchemy:
  de Tierra/Agua/Fuego/Aire a la Tria Prima (Sal, Mercurio, Azufre), el Oro, el Éter y la
  **Piedra Filosofal**.
- **8 ayudantes idle** con hitos ×2 (10/25/50/100/200) y compra ×1/×10/Máx.
- **10 mejoras de laboratorio**: poder de click, producción, offline… y automatización
  de late-game (Sifón de esencia, Alambique perpetuo).
- **El Matraz Dorado**: aparece aleatoriamente; buffs de Frenesí (×7 producción),
  Fiebre de transmutación (×7 click) o Fortuna alquímica (esencia instantánea).
- **Prestigio "La Gran Obra"**: Quintaesencia permanente (+10 % por punto), con panel ilustrado.
- **29 logros** con bonus permanentes + **12 objetivos encadenados** que guían sin tutorial.
- **Progreso offline** (mejorable hasta 24 h / 75 %), guardado local con autosave.
- **Identidad visual**: fondo de laboratorio ilustrado y arte de la Gran Obra (Canva),
  20 iconos por elemento horneados con volumen, ícono de app propio.
- **Audio procedural**: ambiente con progresión de acordes en loop + SFX sintetizados,
  volúmenes separados y vibración opcional.
- **Ajustes**: calidad gráfica, sonido, estadísticas completas, borrado seguro.
- **Actualizador in-app**: avisa cuando hay versión nueva en GitHub Releases.

## Instalación en el celular

**Link directo a la última versión:**
https://github.com/SempaiN/athanor/releases/latest/download/athanor.apk

1. Abrí ese link en el navegador del celular y descargá el `.apk`.
2. Android pedirá permitir "instalar apps desconocidas" → **Permitir** (una sola vez).
3. **Instalar**. Las versiones nuevas se instalan encima sin perder el guardado —
   y desde la v0.1.2 el propio juego avisa cuando hay actualización.

Todas las versiones: https://github.com/SempaiN/athanor/releases

## Desarrollo

- Documentación de diseño: [GDD](docs/GDD.md) · [Especificación de assets](docs/ASSETS.md)
- **Suite de tests de dominio** (~110 verificaciones + simulación de 2 h de juego):
  `Unity.exe -batchmode -executeMethod Athanor.EditorTools.SelfTest.Run`
- **Build de APK**: `Unity.exe -batchmode -buildTarget Android -executeMethod Athanor.EditorTools.BuildScript.BuildApk`
- **Regenerar assets horneados**: `-executeMethod Athanor.EditorTools.AssetBaker.Run`
- Arquitectura: dominio C# puro testeable (`Assets/Scripts/Domain`), capa Unity fina,
  UI 100 % construida por código, assets PNG con fallback procedural
  (reemplazá cualquier PNG de `Assets/Resources/Art/**` y listo).

## Datos técnicos

- Paquete: `com.vesperoni.athanor` · Min SDK: 23 (Android 6.0) · Portrait · IL2CPP ARM64+ARMv7
- Guardado: JSON en `persistentDataPath` (interfaz lista para nube)
- Localización: es (estructura lista para en) · Monetización: no activa (capa desacoplada)
