# whisper&Sound · API de reservas fonoaudiológicas

API REST con comunicación en tiempo real para gestionar las horas de atención de una consulta fonoaudiológica. Proyecto desarrollado para la asignatura **Construcción de Software: Desarrollo de APIs** (Analista Programador, Instituto Profesional Virginio Gómez).

## Funcionalidades

- **API REST** con CRUD completo de Clientes y Reservas, persistencia en SQL Server y validaciones con Data Annotations.
- **Autenticación JWT**: login que genera un token y endpoints protegidos con `[Authorize]`.
- **WebSockets con SignalR**: panel en vivo en el que los cambios de estado de una reserva (Pendiente, Confirmada o Cancelada) se guardan en la base de datos y aparecen al instante en todas las pantallas conectadas.
- **Integración REST + WebSockets**: cuando se crea una reserva desde la API, el panel se actualiza solo.
- **Documentación con Swagger**, con el botón Authorize para probar los endpoints protegidos.
- **Cliente web** (HTML, CSS y JavaScript) con diseño propio para la consulta whisper&Sound.

En desarrollo: Server-Sent Events (SSE) y Webhooks.

## Tecnologías

- ASP.NET Core 8 Web API (C#)
- Entity Framework Core + SQL Server (LocalDB)
- SignalR
- JWT Bearer
- Swagger (Swashbuckle)
- HTML, CSS y JavaScript

## Estructura

```
Evaluacion1_LoretoPradel/
├── Controllers/   AuthController, ClientesController, ReservasController
├── Data/          ReservasContext (Entity Framework Core)
├── Hubs/          ReservasHub (SignalR)
├── Migrations/    Migraciones de la base de datos
├── Model/         Cliente, Reserva
└── wwwroot/       Panel web (index.html, css, js)
```

## Cómo ejecutarlo

1. Clonar el repositorio y abrir `Evaluacion1_LoretoPradel.sln` en Visual Studio 2022.
2. Crear la base de datos desde la Consola del Administrador de Paquetes:
   ```
   Update-Database
   ```
3. Ejecutar el proyecto.
   - Panel web: `https://localhost:44302/`
   - Swagger: `https://localhost:44302/swagger`
4. Iniciar sesión con el usuario de prueba: `admin` / `123456`.

> La clave JWT de `appsettings.json` y el usuario de prueba existen solo para la demostración local. En un entorno real irían fuera del código, por ejemplo en variables de entorno o en User Secrets.

## Autora

Loreto Pradel
