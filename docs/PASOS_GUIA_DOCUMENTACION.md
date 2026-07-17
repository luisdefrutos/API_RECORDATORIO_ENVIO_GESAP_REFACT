# 🗺️ Guía de Lectura de Documentación

Para una correcta transición a producción y entendimiento del sistema, se recomienda seguir este orden de lectura según tu perfil:

---

### 🚀 Si vas a hacer un despliegue (nuevo o actualización)

#### 1. [Guía de Despliegue en IIS](Plan_despliegue_Web_Api.md) ← **Empieza aquí**
*   **Propósito**: Documento único con todo el proceso: requisitos del servidor, datos reales del servidor de pruebas (`gestion3-desa.atisae.com`), Web.config, IIS, permisos, cifrado, hardening de seguridad y checklist.
*   **Qué buscar**: Tabla de entornos, Fase 8 (diferencias para producción), archivos a eliminar antes de un pentest.

#### 2. [Guía de Cifrado de Configuración](GUIA_CIFRADO_CONFIG.md)
*   **Propósito**: Proteger las credenciales en el servidor con `aspnet_regiis`.
*   **Qué buscar**: Comandos exactos con los paths reales del servidor de pruebas y cómo descifrar para modificar.

---

### 📘 Si eres desarrollador nuevo en el proyecto

#### 1. [Manual Técnico](MANUAL_TECNICO.md)
*   **Propósito**: Documento de referencia de arquitectura.
*   **Qué buscar**: Arquitectura Cebolla (Onion), flujos de cifrado AES-256, HMAC, sistema de auditoría forense.

#### 2. [Arquitectura Técnica](ARQUITECTURA_TECNICA.md)
*   **Propósito**: Diagramas de alto nivel y tabla de configuración.
*   **Qué buscar**: Diagramas de flujo, tabla de AppSettings por entorno (pruebas vs producción).

#### 3. [Manual Funcional](MANUAL_FUNCIONAL.md)
*   **Propósito**: Guía de uso para usuarios finales.
*   **Qué buscar**: Flujo de trabajo del formulario de respuesta, estados de los recordatorios.

---

### 🛡️ Si vas a hacer un pentest o auditoría de seguridad

#### 1. [Guía Demo Seguridad](GUIA_DEMO_SEGURIDAD.md)
*   **Propósito**: Verificar que el sistema bloquea IPs correctamente.
*   **Qué buscar**: Paso a paso para simular una intrusión y validar la respuesta del servidor.

#### 2. [Guía de Despliegue en IIS](Plan_despliegue_Web_Api.md) (Sección Hardening / Fase 8)
*   **Qué buscar**: Lista de archivos que DEBEN estar eliminados antes del pentest (`debug_launcher.aspx`, `SecurityTests.aspx`, Swagger desactivado).

---

### 📄 Documento de referencia completo

#### [Documentación Unificada](Documentacion_Unificada_API_RECORDATORIO.md)
*   **Propósito**: Todo el contenido en un único documento, listo para imprimir o enviar.

---

*© 2026 TÜV SÜD - Documentación Técnica Confidencial.*
