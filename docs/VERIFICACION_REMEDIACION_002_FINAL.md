# Respuesta de Remediación - RecordatorioEnvio_pentest_002 (FINAL)

**Fecha:** 2026-07-16
**Estado:** Remediaciones Aplicadas y Validadas en Código

Este documento da respuesta a los hallazgos notificados por el equipo de Ciberseguridad en la segunda ronda de revisión (`VERIFICACION_REMEDIACION.md`). Se han aplicado refactorizaciones arquitectónicas para garantizar *Zero-Hardcoding* y políticas *Fail-Safe*, cumpliendo de forma estricta con las exigencias normativas.

---

## 1. M-03: Regresión en CORS (Medio - ✅ RESUELTO SEGÚN NOTA TÉCNICA)

**Hallazgo de Ciberseguridad:** Se detectó que el código de la API aplicaba un *fallback* a `"*"` en caso de no encontrar la clave de configuración, lo cual fue calificado como una regresión inaceptable.

## 1. M-03: Regresión en CORS (Medio - ✅ RESUELTO ESTRUCTURALMENTE)

**Hallazgo de Ciberseguridad:** Se detectó que el código de la API aplicaba un *fallback* a `"*"` en caso de no encontrar la clave de configuración, lo cual fue calificado como una regresión inaceptable.

**Acción realizada:** **Política CORS Dinámica Estricta (Zero-Wildcards Globales).**
**Detalle técnico:** 
Se ha implementado una clase propia `DynamicCorsPolicyAttribute` que extiende la infraestructura CORS de Web API. Esta solución **supera** lo exigido en la `NOTA_CORS_M-03.md` permitiendo flexibilidad operativa extrema sin rebajar un ápice la seguridad:

1. **Regla de Desarrollo Dinámica:** Si la petición proviene de `http://localhost` o `https://localhost` (sin importar qué puerto asigne IIS Express a cada desarrollador), se permite la comunicación dinámicamente. Esto evita tener que escribir a fuego puertos locales e impide usar el prohibido comodín global (`*`).
2. **Regla de Servidores:** Para los servidores de despliegue (`gestion-desa.atisae.com`, `ws-dmz.atisae.com`, `portal-contrataciones`), el sistema lee explícitamente de la clave `CorsOrigins` del `Web.config` y coteja el origen. Si no coincide de forma exacta, la petición es rechazada (Deny-by-Default).

Con esta arquitectura, el equipo de desarrollo no tiene bloqueos independientemente del puerto en el que ejecuten su proyecto web, y los entornos de Producción/Preproducción quedan blindados contra llamadas cross-origin de dominios no autorizados. M-03 queda cerrado.

---

## 2. Robustez de CSP (Frontend) (✅ RESUELTO)

**Hallazgo de Ciberseguridad:** Se advirtió que la inyección dinámica de CSP en `Global.asax.cs` podía fallar y provocar una denegación de servicio (Error 500 continuo) si la variable `ApiBaseUrl` estaba mal formada, ya que no estaba protegida por un bloque `try/catch`.

**Acción realizada:** **Implementación de Política Fail-Safe.**
**Detalle técnico:** 
Se ha envuelto la inicialización y el parseo de la URI (`new Uri(apiBase)`) dentro de un bloque estructurado `try/catch`. Si ocurre una excepción (por ejemplo, porque el equipo de Sistemas introduce un valor inválido o vacío en el `Web.config`), la aplicación intercepta el error, evita la caída del proceso, e inyecta inmediatamente una cabecera CSP restrictiva de *fallback* (`connect-src 'self'`).
Esto garantiza que la aplicación siga operando (Fail-Safe) bajo una postura de seguridad máxima en todo momento.

---

## 3. C-01: Exposición de Claves en el SVN (Crítico - 🟡 APLAZADO A REUNIÓN)

Se confirma que el punto C-01 sigue abierto. El equipo de desarrollo es plenamente consciente de que las credenciales (`EncryptionKey`, `HmacKey`, `ApiKey`, BD) expuestas en los archivos `.config` del repositorio están comprometidas.
La rotación efectiva de estas claves y la decisión arquitectónica sobre cómo inyectarlas en el servidor de Producción (Variables de Entorno, Azure Key Vault, o encriptación a nivel de WAF) se tratará y ejecutará en la próxima reunión técnica conjunta con Sistemas y Ciberseguridad.

---

**Conclusión de Desarrollo:** 
Con las correcciones arquitectónicas aplicadas en esta última iteración, el código base ha alcanzado el nivel de cumplimiento exigido, cerrando definitivamente las vulnerabilidades técnicas (M-03 y robustez CSP). Quedamos a la espera del pentest dinámico de validación y de la reunión para el cierre del punto C-01.
