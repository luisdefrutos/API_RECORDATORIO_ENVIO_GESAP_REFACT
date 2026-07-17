# Sistema de Gestión de Prompts (/prompts)

Esta carpeta sirve como un repositorio de **flujos de trabajo, instrucciones complejas o comandos útiles** que hemos depurado y queremos reutilizar en el futuro con herramientas de Inteligencia Artificial.

## ¿Cómo utilizar esta carpeta?

### 1. Guardar un nuevo Prompt
Cuando interactúes con la IA y consigas un prompt (instrucción) complejo que dé muy buenos resultados (por ejemplo, para generar un test unitario muy concreto o refactorizar código), pídele a tu IA:
> *"Guarda el prompt que acabamos de usar en la carpeta `/prompts` con el nombre `02_generar_test_unitario.md`"*

### 2. Recuperar y Ejecutar un Prompt
Cuando tú o un compañero necesitéis aplicar un conocimiento del pasado, simplemente abrid un nuevo chat con la IA y escribid:
> *"Lee el prompt `01_crear_nuevo_endpoint.md` de la carpeta `/prompts` y ejecútalo basándote en la entidad `Estadisticas`."*

## Plantilla recomendada para un Prompt
Un buen archivo de prompt (para guardar aquí) debería tener esta estructura:
- **Propósito:** ¿Para qué sirve?
- **Contexto que necesita la IA:** Archivos que debe leer antes de empezar.
- **Instrucción Exacta (Prompt):** El texto literal que se le pasará a la IA.
