# Athanor — Clicker de Alquimia · Documento de Diseño (GDD)

> **Athanor** = el horno de los alquimistas. Nombre de trabajo del juego (temática de alquimia
> genérica, sin IP de terceros). Plataforma: Android, portrait, Unity 6.3 LTS (6000.3.19f1).

---

## 1. Paleta de colores (flat / minimalista)

Tema oscuro alquímico: fondo profundo violáceo, acentos ámbar/dorado, colores planos sin
degradados ni texturas realistas.

| Rol | Hex | Uso |
|---|---|---|
| Fondo profundo | `#14121F` | Fondo general de pantalla |
| Panel | `#221E33` | Paneles laterales, barras |
| Tarjeta | `#2E2947` | Cards de mejoras, logros, elementos |
| Ámbar primario | `#F2A541` | Botones de acción, highlights, círculo de transmutación |
| Dorado | `#E8C547` | Esencia (moneda), oro, números flotantes |
| Verde alquímico | `#7FB069` | Éxito, "puede comprarse", vida |
| Violeta místico | `#9B72CF` | Prestigio, Quintaesencia, efectos arcanos |
| Rojo fuego | `#E4572E` | Fuego, advertencias |
| Azul agua | `#3D9BB3` | Agua, información |
| Gris aire | `#9FB8C8` | Aire, elementos deshabilitados |
| Marrón tierra | `#8C5A3C` | Tierra, cobre, madera del laboratorio |
| Texto principal | `#F4EFE6` | Crema, sobre fondos oscuros |
| Texto secundario | `#A79FBC` | Subtítulos, descripciones |

Regla: máximo 2 acentos por pantalla además de los colores de elementos. Bordes redondeados
(radio ~16 px a 1080p), sin sombras duras (elevación por cambio de tono, no por sombra).

## 2. Monedas y recursos

- **Elementos** (20): materiales acumulables, ver árbol abajo. Se obtienen por click,
  generadores y combinación.
- **Esencia** (dorado): moneda principal. Se obtiene **transmutando** (vendiendo) elementos a su
  valor. Compra generadores y mejoras.
- **Quintaesencia** (violeta): moneda de prestigio, permanente. Cada punto = **+10% producción
  global** (clicks y generadores).

## 3. Árbol de elementos (20) y recetas

Cada receta consume **10 unidades de cada insumo → produce 1 unidad** del resultado.
Valor en Esencia ≈ ×25 por tier (el bonus de transmutar premia subir de tier).

### Tier 0 — Básicos (por click y generadores)
| Elemento | Color placeholder | Valor (Esencia) |
|---|---|---|
| Tierra | `#8C5A3C` | 1 |
| Agua | `#3D9BB3` | 1 |
| Fuego | `#E4572E` | 1 |
| Aire | `#9FB8C8` | 1 |

### Tier 1 — Compuestos (valor 25)
| Elemento | Receta | Color |
|---|---|---|
| Barro | Tierra + Agua | `#6E4F35` |
| Lava | Tierra + Fuego | `#D9642E` |
| Polvo | Tierra + Aire | `#B59F7E` |
| Vapor | Agua + Fuego | `#B8D8DB` |
| Niebla | Agua + Aire | `#7FA8B8` |
| Energía | Fuego + Aire | `#F2C14E` |

### Tier 2 — Materiales (valor 625)
| Elemento | Receta | Color |
|---|---|---|
| Piedra | Barro + Lava | `#7D7A75` |
| Metal | Lava + Polvo | `#A8AABC` |
| Cristal | Vapor + Niebla | `#BFE3E0` |
| Vida | Barro + Energía | `#7FB069` |

### Tier 3 — Tria Prima (valor 15 625)
| Elemento | Receta | Color | Simbolismo |
|---|---|---|---|
| Sal | Piedra + Cristal | `#E8E4DA` | el cuerpo |
| Mercurio | Metal + Niebla | `#C0C5CE` | el espíritu |
| Azufre | Energía + Vida | `#E3B505` | el alma |

### Tier 4 — Nobles (valor ~390 000)
| Elemento | Receta | Color |
|---|---|---|
| Oro | Azufre + Mercurio | `#E8C547` |
| Éter | Mercurio + Sal | `#9B72CF` |

### Tier 5 — Culminación (valor ~9 800 000)
| Elemento | Receta | Color |
|---|---|---|
| Piedra Filosofal | Oro + Éter | `#D64550` |

**Desbloqueo progresivo:** las recetas de un tier se muestran (con silueta "?") cuando el jugador
posee ambos insumos por primera vez; se descubren al ejecutarlas una vez. Crear la Piedra
Filosofal desbloquea el prestigio (además del umbral de Esencia).

## 4. Click (mecánica core)

- Tocar el matraz/círculo de transmutación central genera **+1 de cada elemento Tier 0
  desbloqueado** (mejorable: "poder de click" ×2, ×4… comprado con Esencia).
- Feedback: pulso de escala del matraz (~1.0→1.08, 120 ms), anillo de partículas planas del
  color del elemento, número flotante `+N` dorado que sube y desvanece (400 ms).
- Anti-spam: sin límite de tap, pero las partículas se agrupan por frame (rendimiento).

## 5. Generadores ("Ayudantes") — idle

Producen elementos por segundo. Coste exponencial clásico:

```
coste(n) = costeBase × 1.15^n        (n = cantidad ya comprada)
producción total = prodBase × n × (1 + 0.10 × Quintaesencia) × multiplicadores de logros
```

| Ayudante | Produce | costeBase (Esencia) | prodBase (u/s) |
|---|---|---|---|
| Aprendiz | Tierra | 15 | 0.5 |
| Alambique | Agua | 100 | 2 |
| Brasero | Fuego | 600 | 8 |
| Fuelle | Aire | 3 500 | 20 |
| Crisol | Barro/Lava (alterna) | 20 000 | 6 (T1) |
| Condensador | Vapor/Niebla | 120 000 | 15 (T1) |
| Horno Athanor | Energía | 700 000 | 35 (T1) |
| Transmutador | ejecuta la mejor receta conocida 1×/s | 4 000 000 | 1 receta/s |

(Balance inicial, se ajusta con testing.)

## 6. Prestigio — "La Gran Obra"

- Requisito: haber creado ≥1 Piedra Filosofal **y** ≥1 000 000 de Esencia histórica.
- Al prestigiar: se reinician elementos, Esencia, generadores y mejoras. Se conservan:
  Quintaesencia, logros, elementos *descubiertos* (el árbol queda revelado).
- Fórmula: `quintaesenciaTotal = floor( sqrt(esenciaHistórica / 1e6) )`;
  ganancia del reinicio = total − ya poseída. Cada punto: +10% producción global.
- Primer prestigio ≈ 1–2 h de juego → 1–2 puntos. Cada reinicio posterior debe tardar menos
  en volver al punto anterior (curva compuesta), haciendo el reinicio valioso pero opcional.

## 7. Progresión visual del laboratorio

El fondo de la pantalla principal se puebla por hitos (flat, silhouettes de color plano):

| Hito | Se agrega al fondo |
|---|---|
| Inicio | Mesa vacía + matraz central |
| 1er generador | Estantería izquierda con 3 frascos |
| Descubrir Tier 1 completo | Alambique de vidrio a la derecha |
| 10 generadores | Estantería derecha + libros |
| Descubrir Tria Prima | Símbolos alquímicos en la pared (genéricos: círculos/triángulos) |
| Primer Oro | Candelabros + tono de luz más cálido |
| Piedra Filosofal | Círculo de transmutación completo bajo el matraz |
| Cada prestigio | Vitral superior gana un color |

## 8. Logros (24 al lanzamiento)

Categorías y recompensas (multiplicador permanente de producción salvo indicado):

- **Clicks:** 100 / 1 000 / 10 000 / 100 000 / 1 000 000 → +1% c/u
- **Esencia histórica:** 1e3 / 1e6 / 1e9 / 1e12 → +2% c/u
- **Elementos descubiertos:** 6 / 10 / 15 / 20 → +2% c/u
- **Generadores totales:** 1 / 10 / 50 / 100 → +1% c/u
- **Prestigios:** 1 / 3 / 10 → +5% c/u
- **Especiales:** primer Oro (+500 Esencia ×nivel), primera Piedra Filosofal (+5%),
  jugar 7 días distintos (+2%), volver tras >8 h offline (+2%)

## 9. Progreso offline

- Al cerrar: se guarda `timestampUltimoCierre`.
- Al abrir: `esenciaOffline = producción/s × segundosFuera × 0.5` (50% de eficiencia offline,
  tope 8 h; ampliable con mejoras futuras). Popup: "Mientras estabas fuera…".
- Guardado: JSON en `Application.persistentDataPath/save.json`, autosave cada 30 s y en
  `OnApplicationPause`. Interfaz `ISaveBackend` para migrar a nube más adelante.

## 10. Rendimiento

Dos modos en Ajustes (default: Alto Rendimiento):
- **Alto Rendimiento:** 30 fps target, partículas ×0.3, sin parallax, sin bloom.
- **Máxima Calidad:** 60 fps target, partículas completas, parallax sutil del laboratorio.

Min SDK Android: API 23 (Android 6.0). Assets planos (SVG→PNG chicos, atlas único).

## 11. Arquitectura (resumen)

- **Dominio puro C#** (sin UnityEngine): `GameState`, `Element`, `Recipe`, `Generator`,
  `Achievement`, fórmulas — testeable y portable.
- **Capa Unity:** MonoBehaviours finos (UI, partículas, audio) que leen/mutan el dominio.
- **Servicios desacoplados:** `ISaveBackend` (local hoy, nube mañana), `IMonetization`
  (no-op hoy), `ILocalization` (es hoy, en mañana), `IVersionCheck` (GitHub Releases).
- Strings en tabla de localización (ScriptableObject/JSON), nunca hardcodeados.
