# Fase 0 - Frontend: explicación pedagógica

## 1. Qué se construyó

Se creó el esqueleto de la aplicación web (la parte que verán los usuarios en el navegador) para el sistema de tickets digitales e inventario de combustible. Todavía no hace nada "de verdad" — no guarda datos, no valida usuarios reales — pero ya tiene la navegación completa entre las 8 pantallas principales, un menú lateral y un encabezado, y está listo para que el resto del equipo empiece a conectar la lógica real encima de esta base.

## 2. Por qué se construyó así

### Por qué React + Vite

React es la librería que se usa para construir la interfaz por "piezas" (componentes) que se pueden reutilizar: un botón, una tabla, un menú. En vez de escribir una página HTML gigante, se arma la pantalla combinando piezas pequeñas.

Vite es la herramienta que arranca el proyecto, lo compila y lo sirve en el navegador mientras se desarrolla. Se eligió sobre otras opciones (como Create React App, que ya está descontinuada) porque es mucho más rápida: cada cambio en el código se ve reflejado en el navegador casi al instante, sin tener que recargar todo el proyecto.

### Por qué esta estructura de carpetas

El código se organizó separando "qué tipo de cosa es cada archivo", no "para qué pantalla es". Esto es un patrón muy común en proyectos React porque permite que, cuando el proyecto crezca, cualquier persona del equipo sepa dónde buscar algo con solo saber qué tipo de pieza necesita:

- Si busca "cómo se ve un botón reutilizable" → va a `components/`.
- Si busca "qué pasa cuando entro a una URL" → va a `pages/` o `routes/`.
- Si busca "cómo le hablamos a la API" → va a `services/`.

La alternativa habría sido organizar todo por pantalla (una carpeta "Usuarios" con su propio botón, su propia llamada a la API, etc.), pero eso genera código duplicado cuando varias pantallas necesitan lo mismo (por ejemplo, todas necesitan llamar a la API con el token de sesión).

### Por qué este patrón de rutas

Se usó React Router, que es la librería estándar para que una aplicación de una sola página (SPA, ver glosario) simule tener varias "páginas" distintas sin recargar el navegador. Se decidió que el login viva fuera del layout con menú, porque antes de iniciar sesión no tiene sentido mostrarle a alguien un menú con Empleados, Vehículos, etc. — esas pantallas requieren estar autenticado. El resto de las pantallas comparten un mismo "molde" (el layout con sidebar y header) para no repetir ese menú en cada una.

## 3. Recorrido archivo por archivo

### `frontend/vite.config.js`

- **Qué hace:** configura las herramientas que usa el proyecto: le dice a Vite que use React y que use Tailwind CSS (la librería de estilos).
- **Por qué existe:** sin este archivo, Vite no sabría cómo procesar los archivos `.jsx` (React) ni cómo convertir las clases de Tailwind en CSS real.
- **Ejemplo:** cuando escribes `className="bg-blue-500"` en un componente, es este archivo el que hace posible que esa clase se transforme en color de fondo azul.

### `frontend/index.html`

- **Qué hace:** es la única página HTML real de todo el proyecto. Tiene un `<div id="root">` vacío donde React va a "inyectar" toda la aplicación.
- **Por qué existe:** todo navegador necesita un HTML como punto de entrada. En una SPA ese HTML es casi vacío porque el contenido lo genera React con JavaScript.
- **Ejemplo:** si inspeccionas la página en el navegador antes de que cargue React, solo verías ese `<div>` vacío.

### `frontend/src/main.jsx`

- **Qué hace:** es el primer archivo JavaScript que se ejecuta. Toma el componente principal (`App`) y lo "monta" dentro del `<div id="root">` del HTML.
- **Por qué existe:** es el punto de conexión entre el HTML puro y el mundo de React.
- **Ejemplo:** si algún día se quisiera envolver toda la app en un sistema de traducción de idiomas, se agregaría aquí, porque este archivo envuelve a toda la aplicación.

### `frontend/src/App.jsx`

- **Qué hace:** envuelve toda la aplicación en el "sistema de rutas" (`BrowserRouter`) y le entrega el control a `AppRoutes`.
- **Por qué existe:** es el componente raíz. Si no existiera, no habría un lugar único donde arrancar la navegación.
- **Ejemplo:** es tan simple que solo tiene una etiqueta `<BrowserRouter>` envolviendo a `<AppRoutes />`.

### `frontend/src/routes/AppRoutes.jsx`

- **Qué hace:** es el "mapa" de la aplicación: dice qué componente se muestra según la URL que el usuario visite (por ejemplo, `/tickets` muestra `TicketsPage`).
- **Por qué existe:** sin este archivo, cambiar la URL del navegador no cambiaría nada en pantalla — React no sabe automáticamente qué mostrar para cada dirección.
- **Ejemplo:** si un profesor escribe `localhost:5173/dashboard` en el navegador, este archivo es el que decide que ahí va `DashboardPage`, dentro del layout con menú.

### `frontend/src/components/Layout.jsx`

- **Qué hace:** es el "molde" visual que envuelve a casi todas las pantallas: pone el menú lateral (Sidebar) y el encabezado (Header) alrededor del contenido de cada página.
- **Por qué existe:** evita repetir el mismo menú y encabezado en las 7 pantallas internas. Se define una sola vez y todas lo heredan.
- **Ejemplo:** cuando el usuario pasa de "Usuarios" a "Vehículos", el menú y el encabezado no "parpadean" ni se vuelven a construir — solo cambia el contenido del centro.

### `frontend/src/components/Sidebar.jsx`

- **Qué hace:** dibuja el menú lateral con los enlaces a las 7 pantallas internas (Dashboard, Usuarios, Empleados, etc.) y resalta en cuál está parado el usuario.
- **Por qué existe:** es la forma en que el usuario se mueve entre pantallas sin tener que escribir URLs a mano.
- **Ejemplo:** al hacer clic en "Empleados", ese enlace cambia de color para indicar "estás aquí", gracias a que React Router sabe cuál es la ruta activa.

### `frontend/src/components/Header.jsx`

- **Qué hace:** dibuja la barra superior con el nombre de la sección y un espacio reservado para el usuario que inició sesión.
- **Por qué existe:** es el lugar natural donde, en fases futuras, se mostrará el nombre real del usuario logueado y un botón de "cerrar sesión".
- **Ejemplo:** hoy dice simplemente "Usuario" con un círculo gris; más adelante ese texto vendrá de los datos reales de sesión.

### `frontend/src/components/PageContainer.jsx`

- **Qué hace:** es una plantilla reutilizable que muestra un título grande y, debajo, o bien el contenido real de la pantalla, o bien un mensaje de "Pantalla en construcción" si todavía no hay nada.
- **Por qué existe:** para no repetir el mismo "título + espacio de contenido" en cada una de las 7 páginas por separado.
- **Ejemplo:** `DashboardPage` es solo una línea de código que le dice a `PageContainer`: "muéstrate con el título Dashboard".

### `frontend/src/pages/*.jsx` (una por pantalla)

- **Qué hace:** cada archivo representa una pantalla completa del sistema: `LoginPage`, `DashboardPage`, `UsuariosPage`, `EmpleadosPage`, `VehiculosPage`, `DepartamentosPage`, `SolicitudesPage`, `TicketsPage`.
- **Por qué existe:** separar cada pantalla en su propio archivo permite que, más adelante, cada una crezca (tablas, formularios, filtros) sin que ese código se mezcle con el de otra pantalla.
- **Ejemplo:** cuando se implemente de verdad la gestión de empleados, todo ese trabajo se hará dentro de `EmpleadosPage.jsx`, sin tocar `VehiculosPage.jsx`.

### `frontend/src/services/api.js`

- **Qué hace:** centraliza cómo la aplicación le habla al backend (el servidor). Arma automáticamente la dirección completa de cada pedido y, si el usuario ya inició sesión, agrega su "credencial" (token) a cada solicitud.
- **Por qué existe:** sin este archivo, cada pantalla tendría que repetir el mismo código para conectarse a la API y adjuntar el token — y si cambia la forma de autenticar, habría que corregirlo en decenas de lugares en vez de uno solo.
- **Ejemplo:** cuando la pantalla de Empleados necesite pedir la lista de empleados, en vez de escribir toda la configuración de la llamada a mano, va a llamar a una función de este archivo pasándole solo `/employees`.

### `frontend/.env.example`

- **Qué hace:** es una plantilla que muestra qué "variables de configuración" necesita el proyecto para funcionar — en este caso, la dirección del backend (`VITE_API_URL`).
- **Por qué existe:** cada persona del equipo (o cada ambiente: desarrollo, pruebas, producción) puede tener el backend en una dirección distinta. En vez de escribir esa dirección fija dentro del código, se lee desde un archivo `.env` que cada quien configura localmente y que nunca se sube al repositorio.
- **Ejemplo:** un compañero que corre el backend en el puerto 5000 copia este archivo a `.env` y no tiene que tocar ni una línea de código para que el frontend le apunte correctamente.

### `frontend/src/mocks/`

- **Qué hace:** carpeta reservada para guardar datos de prueba "falsos" que simulan lo que devolvería la API real.
- **Por qué existe:** el equipo trabaja en paralelo — si el backend todavía no tiene listo un endpoint, el frontend puede simular esa respuesta y seguir avanzando sin quedar bloqueado esperando a otro compañero.
- **Ejemplo:** si el backend aún no expone `/fuel-requests`, se puede crear `solicitudes.mock.js` con una lista de solicitudes inventadas para poder diseñar y probar la pantalla de Solicitudes igual.

### `frontend/src/hooks/`

- **Qué hace:** carpeta reservada para "hooks" personalizados, que son funciones de React que empaquetan lógica reutilizable (por ejemplo, "saber si el usuario está logueado").
- **Por qué existe:** todavía no hay lógica que compartir entre pantallas, así que está vacía a propósito — se llenará cuando aparezca esa necesidad (por ejemplo, en la fase de autenticación).
- **Ejemplo futuro:** un hook `useAuth()` que cualquier pantalla pueda usar para preguntar "¿hay un usuario conectado ahora mismo?".

## 4. Preguntas que podrían hacerme y cómo responderlas

**¿Por qué React y no Angular o Vue?**
Fue una decisión del equipo basada en que es la tecnología más conocida por quien lidera esa parte y la que tiene más recursos de apoyo disponibles; las tres son válidas para este tipo de proyecto.

**¿Por qué el login está "fuera" del resto de las pantallas?**
Porque el menú lateral y el encabezado son para usuarios ya autenticados. Mostrar ese menú antes de iniciar sesión no tendría sentido y podría dar la impresión de que se puede navegar sin loguearse.

**¿Qué pasa si dos usuarios entran al sistema al mismo tiempo?**
El frontend por sí solo no resuelve eso: cada usuario tiene su propia sesión en su propio navegador. Quien realmente coordina que no haya conflictos de datos (por ejemplo, dos personas modificando el mismo inventario a la vez) es el backend, que en esta fase todavía no está conectado.

**¿Esto ya funciona con datos reales?**
No. Es exclusivamente la "cáscara" visual y de navegación. No hay conexión real a una base de datos ni autenticación real todavía; eso llega en fases posteriores, cuando el backend esté listo y se conecte usando `services/api.js`.

**¿Por qué separaron los archivos en carpetas por tipo y no por pantalla?**
Porque varias pantallas comparten piezas (el menú, el encabezado, la forma de llamar a la API), y organizarlo por tipo evita duplicar ese código y facilita que cualquiera del equipo sepa dónde buscar algo.

**¿Qué es Tailwind CSS y por qué no escribieron CSS "normal"?**
Tailwind es una forma de escribir estilos usando clases cortas directamente en el HTML/JSX (por ejemplo `p-6` para espaciado), en vez de escribir un archivo CSS aparte para cada componente. Acelera el desarrollo porque no hay que inventar nombres de clases ni saltar entre archivos.

**¿Qué pasa si el backend cambia una URL de la API?**
Como todas las llamadas pasan por `services/api.js`, en la mayoría de los casos alcanza con ajustar la variable de entorno o el punto específico donde se define esa ruta, sin tener que revisar cada pantalla una por una.

**¿Por qué usaron una variable de entorno para la URL del backend en vez de escribirla directo en el código?**
Porque la dirección del backend cambia según dónde se ejecute el proyecto (la computadora de un compañero, un servidor de pruebas, producción). Si estuviera escrita fija en el código, habría que editar y volver a compilar el proyecto cada vez que cambiara de ambiente.

## 5. Términos clave usados

- **SPA (Single Page Application):** aplicación web que carga un solo documento HTML y luego cambia lo que se ve en pantalla usando JavaScript, sin recargar la página completa cada vez que el usuario navega.
- **Componente:** una pieza reutilizable de interfaz (un botón, un menú, una pantalla completa) escrita como una función de React.
- **Ruta (route):** la asociación entre una URL (por ejemplo `/tickets`) y el componente que debe mostrarse cuando el usuario visita esa dirección.
- **Layout:** un componente que define una estructura visual compartida (como el menú y el encabezado) dentro de la cual se insertan otras pantallas.
- **Props:** la forma en que un componente de React recibe datos desde quien lo usa, similar a los parámetros de una función.
- **Hook:** una función especial de React (siempre empieza con `use`) que permite reutilizar lógica, como manejar un estado o compartir información entre componentes.
- **Token (JWT):** una especie de "credencial" que el servidor entrega al usuario cuando inicia sesión, y que el frontend debe reenviar en cada pedido para demostrar que sigue autenticado.
- **Variable de entorno:** un valor de configuración (como una URL) que vive fuera del código fuente y puede cambiar según el ambiente donde se ejecuta el proyecto, sin tener que modificar el código.
- **Mock:** un dato o respuesta "falsa" que simula lo que devolvería un sistema real, usado para poder seguir trabajando sin depender de que esa otra parte ya exista.
- **Build:** el proceso de convertir el código fuente (JSX, Tailwind, etc.) en los archivos finales (HTML, CSS, JS) que un navegador puede entender y ejecutar.
