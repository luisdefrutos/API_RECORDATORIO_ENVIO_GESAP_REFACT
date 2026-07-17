PROPÓSITO: Generar todas las capas necesarias para un nuevo endpoint de lectura respetando Clean Architecture sin romper dependencias.

CONTEXTO PREVIO: La IA debe revisar `context.md` y leer el archivo de la Entidad base en `RecordatorioEnvio.Domain`.

INSTRUCCIÓN EXACTA PARA LA IA:
Vas a crear un nuevo endpoint en el sistema basándote en la entidad que te voy a proporcionar. Sigue estrictamente este orden y no te saltes pasos:
1. Crea o modifica el DTO de respuesta (`RespDto`) en el proyecto `Application`.
2. Añade la firma del método en la interfaz correspondiente dentro de `Application`.
3. Si requiere acceso a datos, añade la firma del método en el repositorio (en `Domain`) e impleméntalo en `Infrastructure` usando sentencias SQL limpias (ADO.NET puro). No uses Entity Framework.
4. Implementa la lógica de negocio en el Servicio dentro de `Application`. Asegúrate de utilizar el gestor de dependencias e inyectar lo necesario.
5. Crea el endpoint (GET o POST) en el `Controller` pertinente dentro del proyecto `API`, devolviendo siempre un ActionResult adecuado (Ok, BadRequest).
6. Por favor, explícame brevemente los cambios realizados paso a paso. No escribas todo el código de golpe a menos que te confirme que el enfoque es correcto.
