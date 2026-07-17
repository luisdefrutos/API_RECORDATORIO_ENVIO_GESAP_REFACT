# Reglas Base para Asistentes de IA (Antigravity IDE)

1. **Fuente de Verdad:** Lee SIEMPRE el archivo `docs/context.md` ubicado en la carpeta docs antes de proponer cambios arquitectónicos o agregar nuevas funcionalidades.
2. **Arquitectura Limpia:** Respeta las reglas de dependencia de Clean Architecture documentadas en `docs/context.md`.
3. **Restricción de Frontend:** Nunca agregues código C# o lógica de servidor en el proyecto `.Web`. Es estrictamente HTML/JS.
4. **Restricción de Backend:** Nunca mezcles sentencias SQL dentro de los Controladores de la API. Todo el SQL debe ir en `RecordatorioEnvio.Infrastructure`.
5. **Prompts:** Para tareas repetitivas o complejas, consulta o guarda las instrucciones en la carpeta `/prompts`.
6. **Idioma:** Mantén los comentarios de código, la documentación técnica y las interacciones de chat en Español, a menos que el usuario indique lo contrario. Las variables y clases se nombran en inglés/español según el contexto existente.
7. **Estilos y Diseño (TÜV SÜD Algorithm Design System):** Al trabajar en el Frontend, es obligatorio respetar los Design Tokens corporativos (ej. `--tuv-blue: #005A9C`). Nunca inventes colores, no uses CSS inline ni rompas la estructura visual. Usa siempre las variables definidas en `:root` de `styles.css`.
   - **Referencia Principal (Algorithm):** https://create.tuvsud.com/latest/t-ue-v-s-ue-d-algorithm-design-system-seamless-solutions-unified-experiences-b79PckjY
   - **Referencia de Marca (Brand Basics MADC):** https://madc.tuvsud.com/guidelines/guide/a3fe0c89-278a-49a6-a394-1693b419f260/page/8b0b71f8-43ec-4fad-ae33-16c95695c6ed
