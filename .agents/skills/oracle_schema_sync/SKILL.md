---
name: oracle_schema_sync
description: Skill para sincronizar la base de datos Oracle. Ejecutar siempre que el usuario pida comprobar la base de datos, sincronizar esquema, buscar tablas nuevas, o reflejar cambios de Oracle en la API C#.
---

# Lector de Esquemas de Oracle

Esta skill te permite como IA leer directamente la base de datos Oracle del proyecto para poder actualizar las clases C# (`Entity`, `DTO`, `Mapper`, `Repository`) automáticamente.

## Instrucciones de Uso

1. **Revisión de Credenciales**
   - Comprueba si existe el archivo `tools/OracleSchemaReader/App.config`.
   - Si **NO** existe, pide amablemente al usuario que te facilite la cadena de conexión (o Host, Puerto, Service Name, Usuario y Contraseña) diciendo: *"Para conectarme a Oracle necesito configurar tu acceso local. Por favor, dame las credenciales para la conexión (estás en local, no se subirán a SVN)."*
   - Cuando el usuario te las dé, copia el contenido de `tools/OracleSchemaReader/App.config.template`, reemplaza los valores de `YOUR_..._HERE` y guárdalo como `tools/OracleSchemaReader/App.config`.

2. **Compilación**
   - Antes de ejecutar, debes asegurarte de que el ejecutable esté compilado.
   - Ejecuta el siguiente comando (asumiendo que MSBuild está disponible, como en el script de CI):
     ```powershell
     & "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" tools/OracleSchemaReader/OracleSchemaReader.csproj /p:Configuration=Release
     ```
     *(Nota: si MSBuild no está en esa ruta, busca la correcta en `C:\Program Files (x86)\Microsoft Visual Studio\` o `C:\Program Files\Microsoft Visual Studio\`)*.

3. **Ejecución y Extracción de Datos**
   - Si el usuario ha preguntado de forma genérica (ej. *"¿qué tablas hay?"* o *"búscame cambios"* sin especificar tabla):
     Ejecuta: `tools\OracleSchemaReader\bin\Release\OracleSchemaReader.exe`
     Esto devolverá un JSON con la lista de tablas. Muestra las tablas al usuario y pregúntale cuál quiere analizar.
   - Si el usuario pide sincronizar o analizar una tabla en concreto (ej. *"sincroniza RECORDATORIO_ENVIO_RESP"*):
     Ejecuta: `tools\OracleSchemaReader\bin\Release\OracleSchemaReader.exe [NOMBRE_TABLA]`
     Esto devolverá un JSON con las columnas, tipos de datos, nulabilidad y PK.

4. **Traducción de Tipos a C#**
   Cuando recibas el JSON del esquema, aplica estas reglas mentales para generar/actualizar el código C#:
   - `NUMBER` sin precisión o precisión > 10 => `long` (o `long?` si es nullable)
   - `NUMBER` con precisión <= 10 => `int` (o `int?` si es nullable)
   - `VARCHAR2`, `NVARCHAR2`, `CHAR` => `string`
   - `DATE`, `TIMESTAMP` => `DateTime` (o `DateTime?` si es nullable)
   - Si el campo es nuevo, actualiza la `Entity` (`src/RecordatorioEnvio.Domain/Entities/`), el `DTO` (`src/RecordatorioEnvio.Application/DTOs/`), el `Mapper` y el `Repository` (`src/RecordatorioEnvio.Infrastructure/Repositories/`).
   - Sigue siempre las reglas de Arquitectura Limpia del proyecto (no poner código SQL en controladores, etc.).
