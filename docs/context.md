# Contexto del Proyecto: API Recordatorio de Envío (Fuente de Verdad)

## 1. Visión General
Este proyecto es la modernización de un sistema monolítico legacy hacia una arquitectura limpia y desacoplada (Clean Architecture). El objetivo es gestionar las respuestas y recordatorios de clientes de manera segura, escalable y mantenible.

## 2. Stack Tecnológico
*   **Backend:** C# / .NET Framework 4.7.2
*   **Web API:** ASP.NET Web API 2
*   **Base de Datos:** Oracle 12c (o superior) mediante ADO.NET (`Oracle.ManagedDataAccess`)
*   **Frontend:** HTML5, CSS, JavaScript puro y jQuery (comunicación AJAX)
*   **Testing:** MSTest / NUnit (Pruebas Unitarias)

## 3. Arquitectura (Clean Architecture)
El proyecto está estrictamente dividido en capas con reglas de dependencia unidireccionales (hacia el centro):
1.  **`RecordatorioEnvio.Domain` (Núcleo):** Entidades puras y reglas de negocio. No depende de NADA.
2.  **`RecordatorioEnvio.Application`:** Lógica de orquestación, Casos de Uso, DTOs y `EmailTemplateBuilder` (generación de correos HTML). Depende de `Domain`.
3.  **`RecordatorioEnvio.Infrastructure`:** Implementación técnica (acceso a Oracle, encriptación AES-256, envío de emails SMTP, Logs). Depende de `Domain`.
4.  **`RecordatorioEnvio.API`:** Controladores REST. Depende de `Application` e `Infrastructure`.
5.  **`RecordatorioEnvio.Web`:** Frontend estático. No hay código C#, solo HTML/JS que consume la API.

## 4. Convenciones de Código y Reglas Estrictas
*   **Separación de UI y Lógica:** El frontend JAMÁS debe tener lógica de negocio o acceso a datos. Todo pasa por la API.
*   **Seguridad y Cifrado:** Todos los IDs que viajan a la web van cifrados mediante AES-256. El frontend nunca ve un ID real de BD.
*   **Inyección de Dependencias:** Utilizar el contenedor IoC configurado en la API para inyectar servicios e interfaces (ej. `IRecordatorioService`).
*   **Sin ORM complejo:** Las consultas a Oracle se realizan mediante ADO.NET tradicional por requerimientos de rendimiento y compatibilidad legacy, pero encapsuladas en Repositorios.
*   **Respeto a los Estilos (CSS y Diseño):** Cualquier modificación en el Frontend debe heredar y respetar estrictamente las clases y estilos CSS existentes. No se deben introducir estilos en línea ni romper la consistencia visual y de diseño (UX/UI) del proyecto original.

## 5. Decisiones de Diseño para la IA (AI Guidelines)
Si eres una IA modificando este código, ten en cuenta:
*   **Al crear un endpoint:** Define primero el DTO en `Application`, crea el contrato en la interfaz del servicio, impleméntalo, y finalmente crea el endpoint en el `Controller` correspondiente en `API`.
*   **Al modificar la Base de Datos:** Modifica las consultas SQL en la capa `Infrastructure` y actualiza la entidad en `Domain`.
*   **Al testear:** Crea mocks de las interfaces de `Infrastructure` para testear la lógica de `Application` de manera aislada.
*   **Prompts Reutilizables:** Revisa la carpeta `/prompts` para ver si existe un flujo predefinido antes de realizar tareas complejas (ej. `01_crear_nuevo_endpoint.md`).
*   **Lectura de Base de Datos (Tooling):** NO intentes adivinar el esquema de la base de datos Oracle ni pidas al usuario que ejecute consultas. Tienes a tu disposición la herramienta de consola `tools/OracleSchemaReader/bin/Release/OracleSchemaReader.exe` (explicada en la Skill `.agents/skills/oracle_schema_sync/SKILL.md`). Utilízala siempre para descubrir tablas y columnas nuevas. Si el binario falla porque falta el archivo `App.config`, pide al usuario que lo cree usando la plantilla.
