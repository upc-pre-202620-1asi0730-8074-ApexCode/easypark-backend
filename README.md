# EasyPark Platform — ampliación a 15 tablas y 60 endpoints

**Código base:** backend ASP.NET Core 10 aportado para Sprint 3. Esta ampliación conserva los cinco contextos originales y añade los recursos de Monitoring and Alerts, Notifications, Analytics and Reporting, y refresh tokens.

## Contenido

- 15 tablas de negocio: 8 originales + 7 nuevas; EF Core agrega `__EFMigrationsHistory` para gestionar migraciones.
- 60 operaciones HTTP en 15 controladores: 31 originales + 29 nuevas.
- Una segunda migración EF Core `20261102000000_ExtendedFeatures` que crea las nuevas tablas sin borrar las anteriores.
- JWT, refresh token rotativo con SHA-256 en base de datos y revocación de sesiones.
- CORS, Swagger UI, documento OpenAPI, pruebas xUnit, Docker Compose para MySQL local y Render Blueprint.
- Modelos y datos para alertas, plantillas, notificaciones, métricas de ocupación y reportes.

## IMPORTANTE: estado de verificación

**Esta ampliación se generó sin disponer del SDK .NET 10 ni de un servidor MySQL en el entorno de preparación. No se ha podido ejecutar `dotnet build`, `dotnet test`, aplicar migraciones en MySQL ni comprobar el backend desplegado.** Por tanto no debe considerarse desplegada, probada ni lista para producción. Se entregan scripts para ejecutar estas verificaciones.

**El ZIP es del backend; no contiene una Web Application Vue conectada.** La integración con el frontend del equipo, incluidos los contratos de los nuevos endpoints, requiere pruebas y ajustes. No incluye los tres módulos completos automatizados de principio a fin (por ejemplo alertas y notificaciones por eventos en tiempo real): se proporcionan endpoints y persistencia, no un sistema de distribución en producción.

## Inicio rápido en Windows (PowerShell)

1. Instala .NET 10 SDK, Docker Desktop y Git. Abre PowerShell en esta carpeta `backend`.
2. `Copy-Item .env.example .env`; reemplaza sus tres contraseñas por valores fuertes y **no subas `.env` a GitHub**.
3. `docker compose up --build -d` para iniciar MySQL y la API local. La API realiza automáticamente las dos migraciones.
4. Visita `http://localhost:8080/health/ready` (debe responder `ready`) y `http://localhost:8080/swagger`.
5. Ejecuta `./scripts/verify.ps1` para restaurar paquetes, compilar y ejecutar las pruebas en el equipo con el SDK.
6. Si quieres datos de demostración para pruebas locales, establece `SeedDemoData=true` en `docker-compose.yml` **solo en desarrollo** antes del primer arranque. El usuario de prueba está en `DemoDataSeeder.cs`.
7. Usa `database/verify-schema.sql` desde MySQL Workbench para verificar **15 tablas de negocio**.

El `docker-compose.yml` publica MySQL únicamente en `127.0.0.1:3307` y la API en `127.0.0.1:8080`. En producción usa TLS en MySQL y registra la cadena de conexión y el secreto JWT como variables privadas en Render. Nunca reutilices los datos de demo.

## Endpoints, estructura y despliegue

- `docs/API-ENDPOINTS.md`: listado de 60 operaciones (método, ruta, controlador).
- `docs/DEPLOYMENT.md`: despliegue con Aiven + Render y Firebase para el frontend.
- `database/schema-reference.sql`: DDL de las ocho tablas originales.
- `database/extended-schema-reference.sql`: DDL de siete tablas nuevas.
- `database/complete-schema-reference.sql`: referencia de las 15 tablas.
- **No ejecutes los DDL de referencia** en una base donde se apliquen migraciones EF Core. Son una alternativa de lectura, no el procedimiento recomendado.

## Notas técnicas

La migración original y la nueva están escritas manualmente (sin ModelSnapshot). Antes de desarrollar nuevas migraciones con `dotnet ef`, regenera una línea base de migraciones validada desde el modelo y concilia el historial de bases existentes. El proyecto heredado silencia la advertencia `PendingModelChangesWarning`; no confundas esa configuración con una prueba de correspondencia de esquemas.

Los refresh tokens sí se guardan solo como hash y se rotan tras cada renovación; un JWT de acceso ya emitido sigue siendo válido hasta su expiración. En producción se recomienda reducir la vigencia de JWT de acceso, además de pruebas de seguridad y de concurrencia.
