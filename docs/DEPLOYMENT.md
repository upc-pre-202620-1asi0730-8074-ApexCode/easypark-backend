# Guía de despliegue para EasyPark

## Local con Docker

Copiar `.env.example` a `.env`, asignar claves seguras y ejecutar `docker compose up --build -d` desde `backend/`. La API espera al estado saludable de MySQL y corre migraciones al iniciar. Comprobar `http://localhost:8080/health/ready` y `http://localhost:8080/swagger`.

## MySQL remoto (Aiven u otro proveedor)

Crear una base MySQL 8 con TLS, un usuario de aplicación con permisos para crear tablas y ejecutar migraciones, y restringir las IP si el proveedor lo permite. Configurar **fuera de Git** la variable privada `ConnectionStrings__DefaultConnection`, por ejemplo `Server=<host>;Port=<port>;Database=easypark;User=<user>;Password=<secret>;SslMode=Required;`.

En MySQL Workbench, ejecutar `database/verify-schema.sql` para comprobar que existan las 15 tablas y el historial de migraciones. La tabla `__EFMigrationsHistory` es adicional.

## Render (backend)

Crear un servicio Docker apuntando a la carpeta `backend` y su `Dockerfile` o utilizar el Blueprint `render.yaml`. Definir secretos de entorno `ConnectionStrings__DefaultConnection`, `TokenSettings__Secret` (mínimo 32 caracteres, aleatorio y distinto del secreto de desarrollo) y `Cors__AllowedOrigins` con la URL HTTPS exacta de Firebase Hosting. `SeedDemoData=false`, `Database__Migrate=true`, `ASPNETCORE_ENVIRONMENT=Production`. Verificar `/health/ready` y `/openapi/v1.json`.

## Firebase Hosting (frontend Vue)

Este paquete no trae frontend. En el repositorio `easypark-webapp`, configurar `VITE_EASYPARK_API_URL=https://<tu-render>/api/v1`, ejecutar `npm ci && npm run build`, después desplegar con Firebase CLI según el proyecto del equipo. Validar que el navegador realmente consulte Render y no `localhost:3000` o json-server.

**Advertencia de contratos:** la Web App actual de Vue requiere adaptar reservas y accesos a comandos del servidor, y verificar los assemblers de métricas y reportes. La presencia de 60 rutas en el backend no sustituye esa integración.

## Pruebas antes de entregar

- `dotnet build EasyPark.Platform.sln -c Release`
- `dotnet test EasyPark.Platform.sln -c Release`
- Login, refresh, revocación, permisos de conductor vs operador.
- Crear instalaciones, espacios, reservas y registrar entradas/salidas con persistencia.
- Crear y consultar alertas, plantillas, notificaciones y métricas; descargar/consultar reportes.
- Simular dos reservas simultáneas sobre el mismo espacio y comprobar que solo una tenga éxito.
- Verificar CORS desde la URL real de Firebase y ausencia de contraseñas o tokens en logs.
- Verificar `/health/ready`, `/swagger`, `/openapi/v1.json` y la lista de 15 tablas MySQL.
