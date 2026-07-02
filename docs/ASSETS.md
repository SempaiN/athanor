# Athanor — Especificación de assets

Todos los assets los genera el usuario y se entregan como archivos. Mientras tanto el juego usa
**placeholders** (formas geométricas planas con los colores del GDD) con los mismos nombres de
archivo, así reemplazar = sobrescribir el archivo, sin tocar código.

## Reglas generales

- Formato: **PNG con transparencia** (32-bit RGBA). Sin bordes blancos ni sombras rasterizadas.
- Estilo: flat, sin degradados complejos, paleta del GDD (§1).
- El contenido debe ocupar ~80% del lienzo, centrado, con margen transparente uniforme.
- Nombres exactos en minúsculas, sin espacios ni acentos (así se cargan solos).
- Carpeta destino en el proyecto: `Assets/Art/<subcarpeta>/`.

## 1. Iconos de elementos — `Assets/Art/Elements/` — 256×256 px

| Archivo | Elemento | Sugerencia visual (flat) |
|---|---|---|
| `el_tierra.png` | Tierra | montículo/rombo marrón `#8C5A3C` |
| `el_agua.png` | Agua | gota `#3D9BB3` |
| `el_fuego.png` | Fuego | llama `#E4572E` |
| `el_aire.png` | Aire | espiral/ráfaga `#9FB8C8` |
| `el_barro.png` | Barro | charco `#6E4F35` |
| `el_lava.png` | Lava | gota chorreante `#D9642E` |
| `el_polvo.png` | Polvo | nube de puntos `#B59F7E` |
| `el_vapor.png` | Vapor | nubecita `#B8D8DB` |
| `el_niebla.png` | Niebla | franjas horizontales `#7FA8B8` |
| `el_energia.png` | Energía | rayo `#F2C14E` |
| `el_piedra.png` | Piedra | roca facetada `#7D7A75` |
| `el_metal.png` | Metal | lingote `#A8AABC` |
| `el_cristal.png` | Cristal | gema `#BFE3E0` |
| `el_vida.png` | Vida | hoja/brote `#7FB069` |
| `el_sal.png` | Sal | cubo blanco `#E8E4DA` |
| `el_mercurio.png` | Mercurio | gota metálica `#C0C5CE` |
| `el_azufre.png` | Azufre | cristal amarillo `#E3B505` |
| `el_oro.png` | Oro | lingote/moneda `#E8C547` |
| `el_eter.png` | Éter | orbe con anillo `#9B72CF` |
| `el_piedrafilosofal.png` | Piedra Filosofal | gema roja radiante `#D64550` |

## 2. Núcleo de la pantalla — `Assets/Art/Core/`

| Archivo | Tamaño | Contenido |
|---|---|---|
| `matraz.png` | 512×512 | Matraz de fondo redondo, silueta flat ámbar |
| `circulo_transmutacion.png` | 1024×1024 | Círculo alquímico genérico (círculos + triángulos), línea dorada sobre transparente |
| `esencia.png` | 128×128 | Icono moneda Esencia (gota/estrella dorada `#E8C547`) |
| `quintaesencia.png` | 128×128 | Icono Quintaesencia (orbe violeta `#9B72CF`) |

## 3. Laboratorio (fondo por capas) — `Assets/Art/Lab/`

Piezas sueltas que se van sumando al fondo (ver GDD §7). Portrait de referencia: 1080×1920.

| Archivo | Tamaño | Contenido |
|---|---|---|
| `lab_mesa.png` | 1080×400 | Mesa de trabajo (franja inferior) |
| `lab_estanteria_izq.png` | 400×700 | Estantería con frascos |
| `lab_estanteria_der.png` | 400×700 | Estantería con libros |
| `lab_alambique.png` | 350×500 | Alambique de vidrio |
| `lab_simbolos_pared.png` | 800×300 | Símbolos geométricos de pared |
| `lab_candelabro.png` | 250×450 | Candelabro |
| `lab_vitral.png` | 1080×350 | Vitral superior (franja) |

## 4. Ayudantes (generadores) — `Assets/Art/Generators/` — 256×256 px

`gen_aprendiz.png`, `gen_alambique.png`, `gen_brasero.png`, `gen_fuelle.png`,
`gen_crisol.png`, `gen_condensador.png`, `gen_athanor.png`, `gen_transmutador.png`
— icono flat de cada ayudante (GDD §5).

## 5. Logros — `Assets/Art/Achievements/` — 128×128 px

Por ahora **uno genérico por categoría** (6 archivos): `ach_clicks.png`, `ach_esencia.png`,
`ach_elementos.png`, `ach_generadores.png`, `ach_prestigio.png`, `ach_especial.png`.
Medalla/escudo flat con el color de la categoría.

## 6. Ícono de la app — `Assets/Art/Icon/`

| Archivo | Tamaño | Notas |
|---|---|---|
| `icon_fg.png` | 432×432 dentro de lienzo 1080×1080 | Capa frontal adaptativa (matraz), transparente alrededor |
| `icon_bg.png` | 1080×1080 | Fondo plano `#14121F` (puede ser color sólido) |
| `icon_legacy.png` | 512×512 | Ícono completo cuadrado (para Play Store más adelante) |

## 7. Audio — `Assets/Audio/`

| Archivo | Formato | Contenido |
|---|---|---|
| `music_lab_loop.ogg` | OGG, loop perfecto, 60–90 s | Ambiente místico calmo |
| `sfx_click.wav` | WAV 44.1 kHz | Burbujeo/tintineo corto (<200 ms) |
| `sfx_compra.wav` | WAV | Confirmación cálida |
| `sfx_logro.wav` | WAV | Campanita ascendente |
| `sfx_prestigio.wav` | WAV | Acorde místico grande |
| `sfx_descubrir.wav` | WAV | "Ajá" arcano (nuevo elemento) |

> Prioridad de entrega sugerida: (1) matraz + 4 elementos base + esencia, (2) elementos T1–T2,
> (3) laboratorio, (4) resto. El juego funciona con placeholders hasta entonces.
