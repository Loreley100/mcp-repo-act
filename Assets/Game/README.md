# Michi Cósmico 🐱✨

Minijuego 2D de un solo nivel, hecho **100% por código**: sprites, sonidos, UI y
escena se generan en runtime, sin ningún asset externo.

## Cómo se juega

Eres **Michi**, una gata espacial atrapada en un campo de meteoritos.

- **Objetivo**: atrapa las **12 estrellas** antes de que se acaben los **45 segundos**.
- **WASD / flechas**: moverse (también joystick de gamepad).
- **SHIFT**: dash para esquivar (1 s de recarga).
- **3 vidas**: cada meteorito quita un corazón y da ~1,7 s de invulnerabilidad.
- **Trampa**: cada estrella que recoges hace que los meteoritos vayan un poco más rápido.
- **R**: reiniciar en cualquier momento tras ganar o perder.

## Detalles técnicos

| Archivo | Responsabilidad |
|---|---|
| `GameManager.cs` | Estados (título/juego/victoria/derrota), construcción del mundo y de la UI, HUD, reinicio sin recargar escena |
| `GameArt.cs` | Texturas y sprites procedurales (gata, estrellas, meteoritos, corazones, paneles…) con antialiasing por supersampling |
| `PlayerController.cs` | Movimiento con aceleración, dash, squash & stretch, parpadeo de invulnerabilidad |
| `Asteroid.cs` | Meteoritos que rebotan en los bordes y aceleran con el progreso |
| `StarPickup.cs` | Estrellas que flotan, giran y pulsan |
| `Starfield.cs` | Fondo: degradado, nebulosas, planeta con anillo y 110 estrellas con parpadeo |
| `Spark.cs` + `CameraShake.cs` | Partículas de explosión y sacudida de cámara con ruido Perlin |
| `Sfx.cs` | Efectos de sonido sintetizados (ondas sinusoidales con envolvente) |

Notas:

- Input: **Input System** (nuevo), con teclado y gamepad.
- Render: URP 2D del proyecto; todos los sprites usan un material unlit.
- La escena `Assets/Scenes/MichiCosmico.unity` solo contiene el objeto del juego;
  el resto se construye al pulsar Play.
