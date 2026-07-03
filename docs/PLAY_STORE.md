# Guía completa: publicar Athanor en Google Play

Todo lo técnico ya está preparado en el proyecto. Esta guía cubre lo que hay que
hacer, en orden, marcando qué es automático y qué te toca a vos.

---

## 0. Resumen de lo que ya está listo

| Pieza | Estado |
|---|---|
| Paquete `com.vesperoni.athanor`, versionCode incremental | ✅ automático en cada build |
| Ícono 512×512 (`Assets/Icon/app_icon.png`) | ✅ listo |
| Gráfico de portada 1024×500 (`store/feature_graphic.png`) | ✅ listo |
| Política de privacidad ([PRIVACY.md](../PRIVACY.md)) | ✅ lista (URL pública en GitHub) |
| Build **AAB** firmado (`BuildScript.BuildAab`) | ✅ script listo — solo falta tu keystore |
| Chequeo de actualizaciones por GitHub **desactivado en el build de Play** | ✅ automático (política de Play: nada de auto-updates externos) |
| Textos del listing (abajo, §4) | ✅ listos para pegar |
| Target API 35+ (Unity apunta al SDK 36 instalado) | ✅ |

## 1. Cuenta de desarrollador (una vez, ~25 USD)

1. Entrá en https://play.google.com/console y accedé con tu cuenta de Google.
2. Pagá la tasa única de 25 USD y completá la verificación de identidad
   (DNI + datos; para cuentas personales Google puede pedir verificación con
   un dispositivo y tarda 1–2 días).

## 2. Crear tu clave de firma (una vez, 2 min)

1. Doble click en `tools/crear-keystore.bat` → elegí una contraseña y anotala.
   Se crea `release.keystore` en la raíz del proyecto (ignorado por git).
2. **Copia de seguridad**: guardá `release.keystore` y la contraseña en un
   lugar seguro (Drive personal, gestor de contraseñas). Si se pierden, no se
   puede actualizar la app nunca más.

## 3. Compilar el AAB (cada versión)

En PowerShell, desde la carpeta del proyecto:

```powershell
$env:ATHANOR_KEYSTORE_PASS = "TU_CONTRASEÑA"
$env:ATHANOR_KEY_PASS = "TU_CONTRASEÑA"
& "C:\Program Files\Unity\Hub\Editor\6000.0.78f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\troll\Desktop\alquimia-clicker" -buildTarget Android `
  -executeMethod Athanor.EditorTools.BuildScript.BuildAab -logFile Logs\aab.log
```

Sale `Builds/athanor-vX.Y.Z.aab` firmado y sin el actualizador de GitHub.
(Claude puede ejecutar este paso por vos cuando tengas el keystore.)

## 4. Crear la app en Play Console

**Crear app** → Nombre: `Athanor — Clicker de Alquimia` · Idioma: Español (España) ·
App · Gratis. Después, en el panel:

1. **Ficha de Play Store** (textos listos para pegar):
   - *Descripción corta* (80 máx):
     `Transmuta elementos, descubre la Piedra Filosofal y haz crecer tu laboratorio.`
   - *Descripción completa*:
     ```
     Athanor es un clicker/idle de alquimia con estética minimalista.

     🧪 Toca el matraz para generar los cuatro elementos primordiales
     ⚗️ Combínalos en un árbol de 20 elementos hasta forjar la Piedra Filosofal
     🧙 Contrata 8 ayudantes que producen por ti, incluso sin conexión
     ⭐ 29 logros, 12 objetivos, mejoras de laboratorio y hitos de producción
     🌙 Prestigia con la Gran Obra y renace con Quintaesencia permanente
     ✨ Atrapa el Matraz Dorado para conseguir potenciadores

     Sin anuncios. Sin compras. Sin conexión obligatoria. Tu progreso es tuyo.
     ```
   - Ícono 512: `Assets/Icon/app_icon.png` · Portada 1024×500: `store/feature_graphic.png`
   - Capturas: mínimo 2 del teléfono (hacé capturas del Laboratorio, Elementos y Gran Obra).
2. **Política de privacidad**: pegá la URL
   `https://github.com/SempaiN/athanor/blob/main/PRIVACY.md`
3. **Clasificación de contenido** (cuestionario IARC): categoría Juego → todo
   "No" (sin violencia, sin apuestas, sin datos) → sale PEGI 3.
4. **Seguridad de los datos**: declará "No se recogen datos" y "No se comparten
   datos" (es la verdad: guardado 100 % local y la build de Play no usa red).
5. **Público objetivo**: 13+ (evitás el programa de familias y sus requisitos extra).
6. **Anuncios**: No contiene anuncios.

## 5. Subir y publicar

1. **Producción → Crear versión** → activá **Firma de apps de Play** (recomendado:
   Google custodia la clave final; tu keystore queda como clave de subida).
2. Arrastrá el `.aab` → nombre de la versión se autocompleta → **Guardar**.
3. **Países**: seleccioná todos (o España/LatAm para empezar).
4. **Enviar a revisión**. La primera revisión tarda de 2 a 7 días.
   Las actualizaciones posteriores suelen aprobarse en horas.

## 6. Actualizaciones futuras

Cada nueva versión: subir versionCode (automático con GameVersion), compilar el
AAB (§3) y crear nueva versión de producción en la consola. Los usuarios de Play
actualizan por Play; la distribución por GitHub Releases puede seguir en paralelo
para testeo (esa build sí conserva su propio avisador de versiones).

## Notas de política que ya cumplimos

- **Nada de auto-actualización fuera de Play**: el build AAB desactiva el
  chequeo de GitHub por símbolo de compilación `PLAY_STORE`.
- **Deep links / permisos**: la app solo declara INTERNET (build GitHub) y
  VIBRATE (opcional). La build de Play no usa INTERNET en la práctica.
- **Contenido**: alquimia genérica sin IP de terceros; los assets son propios
  (procedurales + generados con Canva bajo su licencia de contenido).
