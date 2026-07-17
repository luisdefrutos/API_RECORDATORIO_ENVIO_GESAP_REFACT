# Plan de Documentación y Entrega Estable

Este plan prioriza la creación de documentación de alta calidad, visual y detallada para asegurar que el proyecto sea comprensible para perfiles técnicos de sistemas existentes y para la dirección.

## User Review Required

> [!IMPORTANT]
> **Formato PDF**: Dado que no puedo generar un archivo `.pdf` directamente, generaré archivos `.html` con estilos CSS optimizados para que puedas usar la opción "Imprimir en PDF" del navegador. ¿Te parece bien este método?
> **Diagramas**: Usaré Mermaid para los diagramas en Markdown. Para los PDF/HTML, incluiré versiones de imagen o renderizadas.

## Propuestas de Cambio

### Documentación del Proyecto

#### [NEW] [SecurePayload.cs](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Application/DTOs/SecurePayload.cs)
*   DTO simple para transportar datos cifrados entre el Proxy y la API.

#### [MODIFY] [ProxyController.cs](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Web/Controllers/ProxyController.cs)
*   Implementar cifrado del JSON antes de enviarlo a la API.
*   Uso de `EncryptionService` para generar el token seguro del body completo.

#### [MODIFY] [RecordatorioController.cs](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.API/Controllers/RecordatorioController.cs)
*   Actualizar el método `Post` para recibir `SecurePayload`.
*   Desencriptar y deserializar el DTO original.

#### [MODIFY] [MANUAL_TECNICO.md](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/docs/MANUAL_TECNICO.md) (y HTML)
*   Reflejar el nuevo flujo de seguridad "Payload Encryption" en los diagramas y secciones de seguridad.

#### [NEW] [MANUAL_FUNCIONAL.md](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/docs/MANUAL_FUNCIONAL.md)
*   **Guía de la Lanzadera**: Cómo usar cada panel (Cifrado, Inspección, Catálogo, Logs).
*   **Flujo de Negocio**: Diagrama del proceso de recordatorios desde el envío hasta la respuesta.
#### [MODIFY] [app.js](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Web/js/app.js)
*   Implement a robust synchronization function between the head checkbox and details rows.
*   Ensure synchronization triggers after dynamic data load.
*   Add event listeners for both header and row changes.

## Phase 14: IP BlackList & Security Diagnostic

### [Component Name]

#### [NEW] [SecurityService.cs](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Infrastructure/Security/SecurityService.cs)
*   Clase para encapsular la lógica de detección de IPs bloqueadas (soporta exactitud y comodines `*`).

#### [MODIFY] [SecurityAttributes.cs](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Api/Filters/SecurityAttributes.cs)
*   Integrar `SecurityService` en `SecurityMonitoringAttribute` para lanzar 403 Forbidden si la IP está en la BlackList del `Web.config` de la API.

#### [MODIFY] [ProxyController.cs](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Web/Controllers/ProxyController.cs)
*   Implementar chequeo de IP al inicio de cada acción para bloquear antes de llamar a la API.

#### [MODIFY] [debug_launcher.aspx](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Web/debug_launcher.aspx)
*   Añadir **Panel 6: Diagnóstico de Seguridad**. Herramienta interactiva para que soporte técnico pruebe si una IP coincide con las reglas del `Web.config` sin riesgo.

#### [MODIFY] [Web.config](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Api/Web.config) y [Web.config](file:///C:/ProyectosRepositorio/API_RECORDATORIO_ENVIO/src/RecordatorioEnvio.Web/web.config)
*   Añadir la clave `<add key="Security_BlackList" value="" />` con comentarios explicativos.

## Plan de Verificación
1. **Unitaria**: Probar `SecurityService` con casos de éxito, fallo y comodines (1.2.*, etc).
2. **Funcional**: Usar el Panel 6 de la Lanzadera para validar reglas complejas.
3. **Integración**: Bloquear mi propia IP local y confirmar que el sistema arroja Error 403.
