# StockMaster

Descripción del proyecto

StockMaster es una aplicación web diseñada para la gestión integral de inventario dentro de una organización. El sistema permite administrar productos, categorías, proveedores, roles, usuarios y movimientos de inventario (entradas y salidas) mediante un panel administrativo moderno y accesible desde navegador web.

El sistema incluye autenticación segura mediante JWT implementada en una API desarrollada en .NET, y un frontend construido en Vue 3 que consume los endpoints REST de la API. Además, permite registrar movimientos de inventario con motivos asociados, controlar cantidades disponibles y generar trazabilidad de las operaciones realizadas por los usuarios.

Tecnologías utilizadas

Backend
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* JWT (JSON Web Token) para autenticación
* BCrypt para encriptación de contraseñas
* Swagger para documentación y pruebas de endpoints

Frontend
* Vue 3 + Vite
* Vue Router
* Axios / Fetch API
* Bootstrap 5
* Diseño responsivo personalizado

Instrucciones para correr la aplicación

Backend (.NET API)

1. Abre el proyecto en Visual Studio.
2. Verifica que el archivo appsettings.json tenga configurada correctamente la cadena de conexión (DefaultConnection).
3. Asegúrate de que la base de datos exista y esté actualizada.
4. Ejecuta la API desde Visual Studio presionando F5, o desde terminal: dotnet run
5. Verifica que Swagger esté funcionando en: https://localhost:7198/swagger

Frontend (Vue)

1. Entra a la carpeta del frontend (por ejemplo): cd StockFront
2.Instala las dependencias: npm install
3. Levanta el servidor de desarrollo: npm run dev
4. Abre el navegador en la URL que indique Vite (normalmente): http://localhost:5173
