# RGB HFR Analyzer

<p align="center"><img src="docs/logo-horizontal.png" alt="RGB HFR Analyzer" width="520"></p>

**Descubre qué nitidez tiene realmente cada canal de color. HFR por canal (R, G, B, RGB) y autoenfoque sobre el canal que importa, para cámaras en color, como plugin de [N.I.N.A.](https://nighttime-imaging.eu/)**

Para filtros de banda estrecha y dual band, acromáticos, refractores ED/APO, reflectores y SCT por igual.

🇬🇧 [Read in English](README.md)

> **Beta.** Probado por su autor, pero todavía no por otros usuarios ni con una gama amplia de equipos. Los avisos de errores y comentarios son muy bienvenidos (ver [Issues](../../issues)).

![Panel de análisis](docs/screenshot.png)

## Índice

1. [Por qué existe este plugin](#por-qué-existe-este-plugin)
2. [Características](#características)
3. [Requisitos e instalación](#requisitos-e-instalación)
4. [Cómo funciona](#cómo-funciona)
5. [Guía de uso](#guía-de-uso)
6. [Cómo leer los datos del análisis](#cómo-leer-los-datos-del-análisis)
7. [Avisos automáticos](#avisos-automáticos)
8. [Limitaciones](#limitaciones)
9. [Compilar desde el código fuente](#compilar-desde-el-código-fuente)
10. [Créditos, licencia y uso de IA](#créditos-licencia-y-uso-de-ia)

## Por qué existe este plugin

El HFR que muestra NINA, y que usa su autoenfoque, es **un único número global**: mezcla los tres canales de color de una imagen en color (OSC). Pero una imagen en color son en realidad tres imágenes, y no siempre están igual de nítidas. Ese HFR global no te dice **qué canal está mejor enfocado**, y el autoenfoque converge a un compromiso sobre el que no puedes influir.

RGB HFR Analyzer te da esa información dentro del flujo normal de NINA, **reutilizando el propio debayer, el detector de estrellas y el motor de autoenfoque de NINA**, para que los números sean comparables con los que ya muestra el programa. Es útil con cualquier telescopio:

- **Filtros de banda estrecha y dual/tri band (Ha, Ha+OIII, SII...).** La señal solo llega a algunos canales. Con un filtro Ha el canal útil es el rojo, así que ese es el que conviene enfocar; con Ha+OIII puedes comparar el rojo con el verde+azul. Enfocar sobre el canal que lleva tu señal, y no sobre un HFR global diluido por canales que casi no reciben luz, da mejor foco donde importa.
- **Refractores acromáticos.** La aberración cromática longitudinal pone el mejor foco del rojo, del verde y del azul en posiciones distintas del enfocador. Puedes verla, medirla y elegir qué color favorecer. (Con cámara monocroma se enfoca cada filtro por separado; una cámara OSC solo tiene una posición por foto.)
- **Refractores ED/APO.** Comprueba cuánto residuo de color queda y si te afecta en tu fotografía.
- **Reflectores, SCT y sistemas corregidos.** Comprueba que no se ha colado color por filtros IR/UV, reductores o aplanadores, y usa el mapa de zonas, la inclinación y la curvatura de campo para afinar tu tren óptico.
- **Curiosidad.** Es una forma sencilla de aprender cómo se comporta de verdad tu telescopio en cada longitud de onda.

**Lo que no hace.** Mide; no modifica tus imágenes, y no se escribe nada en cabeceras FITS ni en archivos. Tampoco elimina la aberración cromática: en las pruebas del autor con un refractor acromático, enfocar cada canal por separado y recombinarlos después **no** la eliminó. Lo que mejor funcionó fue enfocar el canal **R**: los tres canales quedaron bien enfocados, y mucho mejor que en el compromiso del HFR global de NINA. Tu telescopio y tu cámara pueden comportarse de otra forma, y justo por eso es útil ver cada canal.

## Características

| Función | Dónde | Descripción |
|---|---|---|
| **Análisis por canal** | Panel *HFR RGB* | HFR y estrellas detectadas de R, G, B y RGB en cada imagen (guardada o no). |
| **Visor del canal** | Panel *HFR RGB* | Imagen del canal elegido con zoom, arrastre y círculos opcionales sobre las estrellas. |
| **Mapa de zonas** | Panel *HFR RGB* | Rejilla opcional de 3×3 con el HFR de cada zona. |
| **Estadísticas emergentes** | Panel *HFR RGB* | HFR medio y mediano, σ, error de la media, inclinación y curvatura, al pasar el ratón por la imagen. |
| **Avisos** | Panel *HFR RGB* | Solo cuando se detecta algo importante. Ocultos por defecto. |
| **Mezcla de canales** | Panel *HFR RGB* | Analiza varios canales a la vez (promedio), por ejemplo R+B para el enfoque intermedio. |
| **Autoenfoque por canal** | Panel *RGB Autoenfoque* | Autoenfoque de NINA (misma curva y gráfica) midiendo el canal elegido, o el promedio de dos. |
| **Tabla de mejores posiciones** | Panel *RGB Autoenfoque* | Mejor posición del enfocador de cada canal, con un botón **Ir** para moverse a ella. |
| **Usar en todos los autoenfoques** | Panel *RGB Autoenfoque* | Hace que cualquier autoenfoque de NINA (incluido el secuenciador) mida el canal elegido. |
| **Instrucción de secuenciador** | Secuenciador avanzado | *Channel Autofocus*. |
| **Disparador de secuenciador** | Secuenciador avanzado | *Channel Change and Refocus*, con un plan por tramos. |
| **Patrón de nombre de archivo** | Opciones > Imagen | `$$FOCUSCHANNEL$$`: canal del último autoenfoque, para ordenar las fotos por canal de enfoque. |
| **Idiomas** | Opciones > Plugins | Inglés y español; por defecto sigue el idioma de NINA. |

## Requisitos e instalación

**Requisitos**

- N.I.N.A. **3.2** o superior (desarrollado y probado con 3.2.0.9001).
- Una **cámara en color con matriz Bayer** (OSC). Con cámaras monocromas el panel avisa de que la imagen no es de color.
- Windows 10/11 x64 (el que requiere NINA).

**Instalación**

- *Desde el gestor de plugins de NINA* (cuando esté publicado): abre **Plugins > Disponible**, busca *RGB HFR Analyzer* y pulsa **Instalar**. Mientras esté en beta, puede aparecer solo en el canal beta.
- *A mano*: cierra NINA, descarga el `.zip` de la página de [Releases](../../releases), extrae la DLL en una carpeta dentro de `%localappdata%\NINA\Plugins\3.0.0\` y abre NINA.

**Dónde están los paneles.** En la pestaña de captura, en la barra superior: **Info** → *HFR RGB* (análisis) y **Herramientas** → *RGB Autoenfoque*. Los paneles se pueden anclar, mover o dejar flotantes como cualquier otro panel de NINA.

## Cómo funciona

### Análisis de cada imagen

Cada vez que NINA prepara una imagen para mostrarla (se guarde o no), el plugin la analiza en segundo plano, sin retrasar la captura ni el guardado:

1. **Debayer** con el propio debayer de NINA para obtener los canales R, G y B a resolución completa.
2. **Un canal, una imagen:** cada canal se trata como una imagen monocroma.
3. **Stretch** con los ajustes de estiramiento de tu perfil de NINA.
4. **Detección** con el **detector de estrellas de NINA**, con la sensibilidad, la reducción de ruido y los recortes de tu perfil.
5. **Resultado:** HFR medio, desviación, número de estrellas y lista de estrellas de ese canal.

El canal **RGB** sigue el camino de la imagen principal de NINA (imagen debayerizada en color y detección sobre ella), por lo que su HFR y su número de estrellas coinciden con los de NINA. Cada canal detecta **sus propias estrellas**, así que su número puede diferir entre canales (el azul suele tener menos señal). El HFR se expresa en **píxeles del sensor completo**, igual que en NINA.

### Mezcla de canales

Con **Mezclar** activado puedes seleccionar dos canales (por ejemplo, R+G): se promedian y el resultado pasa por el mismo proceso. Es un promedio simple, no tu procesado real: sirve para comparar combinaciones, no para predecir el resultado final.

### Autoenfoque por canal

El plugin **no** incluye un motor de autoenfoque propio. Aporta un **detector de estrellas** (*RGB HFR Analyzer (active channel)*) que mide el canal elegido, y el autoenfoque de NINA lo usa como usaría el suyo. Se mantienen la curva en V, el ajuste, los pasos, los intentos, la gráfica y todos los ajustes de Opciones > Autoenfoque.

El detector solo actúa **durante un autoenfoque**. En todo lo demás, incluido el HFR que NINA muestra de cada imagen, se comporta como el detector de NINA sobre la imagen completa.

Dos formas de usarlo:

- **Desde el panel *RGB Autoenfoque*:** el plugin selecciona su detector mientras dura el autoenfoque y después restaura el que había.
- **Modo global:** con *Usar este canal en todos los autoenfoques de NINA*, el detector del plugin queda seleccionado y cualquier autoenfoque de NINA (manual, secuenciador heredado o avanzado, disparadores por tiempo, temperatura o HFR) mide el canal elegido.

## Guía de uso

### Panel *HFR RGB* (análisis)

**Fila superior.** A la izquierda, los botones de zoom (acercar, alejar, ajustar a la ventana, 1:1). A la derecha, los botones **R**, **G**, **B** y **RGB**, cada uno con su HFR. RGB viene seleccionado por defecto. Al pasar el ratón por un botón se ve cuántas estrellas se detectaron en ese canal.

**Segunda fila (botones on/off, todos apagados por defecto).**

- **Avisos:** muestra el recuadro rojo de avisos cuando se detecta algo importante; el botón indica cuántos hay (por ejemplo, *Avisos (2)*).
- **Zonas:** superpone una rejilla 3×3 con el HFR de cada zona, coloreada de azul (más nítida que la mediana global) a rojo (menos nítida). Al pasar el ratón por una zona aparecen sus estadísticas.
- **Estrellas:** dibuja un círculo sobre cada estrella detectada en el canal mostrado.
- **Mezclar:** permite seleccionar varios canales a la vez. Apagado, cada botón elige un solo canal. El botón **RGB** siempre marca los tres.

**Estadísticas.** Al pasar el ratón por la imagen se ven la fecha y hora de la captura, el canal mostrado, HFR medio y mediano, σ, número de estrellas, σ relativa, error de la media, rango de zonas, inclinación, curvatura y un resumen de los cuatro canales.

**Visor.** Rueda del ratón para el zoom (centrado en el cursor), arrastrar para moverse, o los botones de zoom. La imagen es una versión reducida (máximo 3000 px de ancho) y ya estirada, por lo que a zoom alto se ve más blanda que en NINA.

### Panel *RGB Autoenfoque*

1. Elige el **canal a enfocar**: R, G, B, RGB (como el autoenfoque normal de NINA) o R+G, G+B, R+B (promedio de dos canales, para enfocar entre ellos).
2. Pulsa **Iniciar autoenfoque** (cámara y enfocador conectados). Puedes **Cancelar** en cualquier momento.
3. La gráfica es la propia de NINA. La tabla de la derecha guarda la posición, el HFR y la hora del último autoenfoque de cada canal, con un botón **Ir** para mover el enfocador a esa posición.
4. Opcionalmente, marca **Usar este canal en todos los autoenfoques de NINA (incluido el secuenciador)**.

El canal elegido se recuerda entre sesiones.

### Patrón de nombre de archivo

En **Opciones > Imagen > Patrón de nombre de archivo** aparece `$$FOCUSCHANNEL$$` (categoría *RGB HFR Analyzer*). Contiene el canal del último autoenfoque hecho con este plugin (`R`, `G`, `B`, `RGB`, `RG`, `GB` o `RB`). Junto con el `$$FOCUSERPOSITION$$` propio de NINA puedes ordenar las fotos por enfoque, por ejemplo:

`$$DATE$$\$$TARGETNAME$$_$$FOCUSCHANNEL$$_$$FILTER$$_$$FOCUSERPOSITION$$_$$EXPOSURETIME$$s_$$FRAMENR$$`

Si haces un autoenfoque de NINA sin el plugin, el patrón conserva el canal anterior.

### Secuenciador

**Secuenciador avanzado**, categoría **RGB HFR Analyzer**:

- **Channel Autofocus:** ejecuta la instrucción de autoenfoque de NINA midiendo el canal elegido en su desplegable.
- **Channel Change and Refocus:** un disparador con un **plan por tramos**. Cada tramo indica un canal y cuántas exposiciones LIGHT dura. Al terminar un tramo, antes de la siguiente exposición, cambia al canal del tramo siguiente y ejecuta el autoenfoque sobre él. **+** y **−** añaden o quitan tramos; **Repetir el plan** vuelve al primero tras el último; **Enfocar al empezar** hace un autoenfoque del primer canal antes de la primera exposición.

**Secuenciador heredado.** Los plugins no pueden modificar sus pantallas, así que su bloque de autoenfoque no tiene selector de canal. Usa el **modo global** del panel *RGB Autoenfoque*: todos sus autoenfoques medirán el canal elegido.

### Idioma

Por defecto la interfaz sigue el idioma de NINA (español, y en cualquier otro caso inglés). Puedes forzar uno en **Opciones > Plugins**; reinicia NINA para aplicarlo. Los nombres de las instrucciones del secuenciador están siempre en inglés.

## Cómo leer los datos del análisis

| Dato | Qué es | Cómo interpretarlo |
|---|---|---|
| **HFR** | Tamaño típico de las estrellas, en píxeles. | Menor es más nítido. Se compara entre canales de la misma foto. |
| **Estrellas** | Estrellas que supera el detector en ese canal. | Un canal más nítido suele detectar más. Pocas estrellas → medidas poco fiables. |
| **Mediana** | Valor central del HFR de las estrellas. | Es menos sensible a estrellas anómalas (dobles, falsas detecciones) que la media. |
| **σ** | Dispersión del HFR entre estrellas. | Cuánto varía el tamaño de unas estrellas a otras. |
| **σ relativa** | σ dividida por el HFR medio. | Permite comparar la dispersión entre canales. |
| **Error de la media** | σ / √N. | Incertidumbre del HFR medio. Una diferencia entre canales es fiable si supera unas 3 veces su error. |
| **Zonas (mín–máx)** | HFR de la mejor y la peor zona de la rejilla 3×3. | Δ por debajo de ~10 %: campo bastante uniforme. |
| **Inclinación** | Diferencia de HFR entre lados (derecha−izquierda, abajo−arriba). | Cercana a 0 es buena. Si es grande y se repite entre fotos, revisa que el sensor esté perpendicular al eje óptico. |
| **Curvatura** | HFR de las esquinas menos el del centro. | Positiva: las esquinas están peor (curvatura de campo, coma). Habitual en refractores sin aplanador. |

Notas:

- Las celdas del mapa usan la **mediana** de cada zona (cada zona tiene pocas estrellas). Los botones de canal muestran la **media**, que es la que calcula NINA, por lo que pueden diferir ligeramente. El cuadro de cada zona muestra ambos valores.
- Las zonas con menos de 5 estrellas no se calculan; para la inclinación y la curvatura se exigen al menos 10 estrellas por grupo.
- Todo sale de **una sola imagen**: el viento, el guiado o la turbulencia pueden agrandar las estrellas puntualmente. Un problema real se repite en varias fotos seguidas.
- Un canal con mayor HFR puede ser el mejor enfocado, según el filtro y la exposición: el enfoque hay que juzgarlo a ojo y con el HFR de los botones, no solo por los números.

## Avisos automáticos

El recuadro rojo aparece **solo** si se cumple alguna de estas condiciones (en cualquier canal analizado). Los umbrales son orientativos:

| Aviso | Condición |
|---|---|
| **Inclinación** | Diferencia entre lados de más del 10 % del HFR y estadísticamente real (más de 3 veces su error). Si se repite en varios canales, sugiere causa mecánica. |
| **Curvatura de campo marcada** | Esquinas más de un 20 % peores que el centro, y estadísticamente real. |
| **Centro peor que las esquinas** | Diferencia de más del 10 % a favor de las esquinas; poco habitual. |
| **Pocas estrellas** | Menos de 100 estrellas utilizables en algún canal. |
| **Mucha dispersión** | σ relativa de más del 35 %. |

No se avisa de que unos canales estén mejor enfocados que otros: con un acromático es lo normal y es lo que el plugin ayuda a estudiar.

## Limitaciones

- **Solo cámaras en color (Bayer).** Con cámaras monocromas no hay canales que separar.
- **Tiempo de análisis.** Cada imagen requiere varias detecciones (R, G, B y RGB) en segundo plano: varios segundos con imágenes de 24 MP. Si llega otra imagen mientras se analiza la anterior, se omite.
- **Memoria.** Se conservan en memoria los canales R, G y B de la última imagen (unos 150 MB con 24 MP) para calcular mezclas bajo demanda.
- **El autoenfoque tarda algo más**, porque hay que extraer y estirar el canal antes de detectar.
- **El canal azul suele tener menos señal:** su curva de enfoque puede salir más ruidosa y quizá convenga más exposición en el autoenfoque.
- **Detector seleccionado en el perfil.** NINA guarda el detector seleccionado en el perfil. Si NINA se cierra a mitad de un autoenfoque lanzado desde el panel, podría quedar seleccionado el detector del plugin; se corrige en *Opciones > Imagen > Detector de estrellas*.
- **El foco se desplaza con la temperatura.** Las posiciones guardadas por canal pueden quedar obsoletas si la temperatura cambia mucho, y la tabla no se conserva al cerrar NINA.
- **Disparador de secuenciador.** Cuenta las exposiciones LIGHT del historial de imágenes: si el historial se vacía, el plan vuelve a empezar. Si un autoenfoque falla, el plan no avanza y lo reintenta antes de la siguiente exposición.
- **Versiones de NINA.** El plugin usa la API de NINA 3.x; futuras versiones pueden requerir ajustes.
- **Los comentarios del código** están mayoritariamente en español.

## Compilar desde el código fuente

1. Instala **Visual Studio 2022 o posterior** con la carga de trabajo de escritorio de .NET.
2. Clona el repositorio y abre la solución (`RGBfocus.slnx`; el proyecto conserva su nombre original).
3. El proyecto usa el paquete NuGet `NINA.Plugin`, que se restaura solo.
4. Cierra NINA y compila (**Recompilar solución**). La compilación copia el plugin a la carpeta de plugins de NINA.
5. Abre NINA y comprueba en su log (`%localappdata%\NINA\Logs`) que aparece `Successfully loaded plugin RGB HFR Analyzer`.

Los avisos `NU1701` sobre `ToastNotifications` y `VVVV.FreeImage` son normales: son dependencias de NINA compiladas para .NET Framework. Para publicar una versión, ver [docs/RELEASING.md](docs/RELEASING.md).

## Créditos, licencia y uso de IA

- Plugin creado por **Antonio Aguado Rovira**.
- Desarrollado sobre la API de plugins de **N.I.N.A. – Nighttime Imaging 'N' Astronomy**, de código abierto bajo licencia MPL 2.0. El plugin reutiliza el detector de estrellas, el debayer y el motor de autoenfoque de NINA, y contiene una pequeña función adaptada del código de NINA (desplazamiento del patrón Bayer según los *offsets* de la cámara).
- **Uso de IA:** este plugin se ha desarrollado con ayuda sustancial de un asistente de IA (Claude, de Anthropic), que escribió la mayor parte del código siguiendo las indicaciones del autor. El autor lo ha revisado, lo prueba con su equipo y lo mantiene.
- Licencia: **MPL 2.0** (ver `LICENSE.txt`).

N.I.N.A. es un proyecto independiente de sus autores y colaboradores; este plugin no está afiliado ni respaldado oficialmente por ellos.
