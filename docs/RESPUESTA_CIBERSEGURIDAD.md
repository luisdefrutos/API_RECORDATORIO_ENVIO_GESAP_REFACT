# Informe de Remediación de Seguridad (Actualizado)

**Fecha:** 16 de Julio de 2026
**Proyecto:** Sistema Recordatorio de Envío GESAP (TÜV SÜD)

Este documento detalla las acciones técnicas aplicadas sobre el código fuente en respuesta a la última auditoría de seguridad (Ronda 4), justificando cómo el sistema cumple con las directrices de Ciberseguridad.

## 1. Puntos Críticos y Altos

### C-01: Secretos en el repositorio y claves no cubiertas (Crítico - 🟡 PENDIENTE DE REUNIÓN)
**Estado Actual:** Se encuentra pendiente de una reunión organizativa con Jefatura/Sistemas. 
**Justificación:** Se ha tomado la decisión estratégica de no alterar los archivos `Web.config` (ni sus variantes de Release) con reemplazos manuales ni automatizados de credenciales en este ciclo. No se realizarán cambios en los secretos hardcodeados hasta acordar un protocolo oficial para inyectar la cadena de conexión de Oracle y las claves de cifrado (`EncryptionKey`, `HmacKey`, `ApiKey`) en Producción (ya sea delegándolo en el servidor, usando variables de entorno, o el WAF). Hasta que se celebre dicha reunión, las claves permanecen en claro en el SVN.

## 2. Puntos Medios y Bajos Solucionados

### M-06: Dependencias con CVEs (Newtonsoft.Json) (Medio - ✅ RESUELTO)
**Acción realizada:** Se ha erradicado por completo la versión vulnerable `12.0.3`.
**Detalle técnico:** 
- Actualizados los paquetes NuGet (`packages.config`) y las referencias de proyecto (`.csproj`) tanto de la Web como de la API a la versión segura **`13.0.3`**.
- Se han ajustado las reglas de redirección de ensamblados (`bindingRedirect`) en los `Web.config` para forzar que cualquier librería de terceros (ej. Swashbuckle) apunte inequívocamente a la versión segura `13.0.0.0`, resolviendo posibles cuellos de botella y vulnerabilidades de deserialización JSON.

### B-05: Excepciones silenciadas (catch vacíos) (Bajo - ✅ RESUELTO)
**Acción realizada:** Refactorización de la gestión de fallos en `LogHelper.cs`.
**Detalle técnico:** Se han eliminado los bloques `catch { }` vacíos que "tragaban" errores críticos si fallaba la escritura de logs en el disco. En su lugar, si ocurre una catástrofe de I/O, se realiza un intento de escritura directo al **Visor de Eventos de Windows** (`System.Diagnostics.EventLog.WriteEntry`) bajo el origen de la Aplicación. Esto asegura que la evidencia de fallos de auditoría nunca se pierda silenciosamente.

## 3. Remediaciones Dinámicas y Arquitectónicas (Ajustes Adicionales)

### M-03 y B-06: Endurecimiento de CSP y CORS (Medio/Bajo - ✅ RESUELTO SUPERIOR)
**Acción realizada:** Implementación dinámica y delegación de seguridad (Zero-Hardcoding y Zero-Wildcards).
**Detalle técnico:** 
- **CORS (API Backend):** Se ha corregido la regresión detectada (M-03). Se ha eliminado el fallback a `"*"` en el código de inicialización de la API. Ahora, si la variable `CorsOrigins` no existe o viene mal informada, el sistema aplica un "Fallback Ultra-Seguro" denegando cualquier origen cruzado (cero asteriscos). Además, se ha configurado la variable de forma estricta (`https://portal-contrataciones.tuv-sud.es`) en lugar de delegar a ciegas en el WAF, cerrando definitivamente la vulnerabilidad.
- **CSP (Frontend Web):** Se ha detectado que una política estática previa podía o bien bloquear a la propia aplicación (Error 0 de conexión cruzada) o bien usar comodines inseguros en `connect-src`. La solución aplicada inyecta la cabecera `Content-Security-Policy` dinámicamente desde `Global.asax.cs`, extrayendo la URL exacta declarada en `ApiBaseUrl` y permitiendo *única y exclusivamente* conexiones AJAX a ese dominio validado. **Adicionalmente**, se ha añadido robustez mediante un bloque `try/catch` al parsear la URL de configuración, garantizando que un error tipográfico en `ApiBaseUrl` no interrumpa el servicio (fail-safe) y se aplique una política restrictiva por defecto. Esto impide por completo exfiltraciones de datos vía XSS.

---
**Conclusión de la Revisión:** 
Excluyendo el acuerdo organizativo de contraseñas en texto claro (C-01), el código fuente supera los hallazgos técnicos del informe. Se han cerrado las excepciones silenciadas, se han mitigado vulnerabilidades por dependencias (CVEs) y se ha configurado una protección de red y de navegador robusta, dinámica y libre de comodines peligrosos.
