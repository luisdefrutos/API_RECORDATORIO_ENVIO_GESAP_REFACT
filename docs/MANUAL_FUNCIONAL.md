# 👤 Manual Funcional - Sistema de Recordatorio de Envío

**Cliente:** TÜV SÜD  
**Propósito:** Guía de uso para usuarios de negocio, soporte técnico y administradores.

---

## 1. 📋 Introducción al Sistema

Este sistema automatiza la recogida de datos y respuestas de clientes tras un recordatorio de envío. Consta de dos partes principales:
1.  **Formulario del Cliente**: Lo que el cliente rellena.
2.  **Lanzadera de Control (Consola Pro)**: Herramienta interna para gestión y auditoría.

---

## 2. 🚀 Guía de la Lanzadera Pro

### 2.1 Herramientas de Mantenimiento y Seguridad
La "Lanzadera Pro" incluye ahora herramientas avanzadas para el equipo de soporte:
*   **Inspección de Datos**: Para ver y corregir estados de Oracle si hubiera un error de sincronización.
*   **Auditoría de Logs**: Visualización directa de errores sin entrar al servidor.
*   **Diagnóstico de Seguridad**: Permite comprobar si una dirección IP está bloqueada. Es vital usar esto antes de añadir reglas de bloqueo generales (con asteriscos) para evitar bloqueos accidentales a usuarios legítimos.

### 🟢 Panel 1: Generador de Enlaces
Aquí es donde comienza el trabajo.
1.  **Token / ID**: Inserte el código del registro que desea gestionar.
2.  **Botón Lanzar**: Genera automáticamente la URL segura que se debe enviar al cliente.
3.  **Botón Abrir**: Abre directamente la vista del cliente en una nueva pestaña.

### 🔐 Panel 2: Herramienta de Cifrado
El sistema no utiliza números de ID simples para evitar que alguien adivine otros registros.
*   Introduzca un ID numérico (ej: `12345`).
*   Pulse **Cifrar**. Obtendrá un token largo e indescifrable que es el que se usa en las URLs oficiales.

### 📝 Panel 3: Inspección y Edición de Registros
Permite corregir datos sin entrar directamente en la base de datos.
*   **Cargar**: Muestra todos los datos del cliente en un editor (formato JSON).
*   **Guardar**: Si realiza un cambio en el texto, este botón lo persiste en Oracle de forma segura.

### 📊 Panel 4: Catálogo de Estados
Muestra la lista oficial de estados permitidos por el sistema (ej: "PENDIENTE", "TRAMITADO", etc.). Es puramente informativo para consulta rápida.

### 🔍 Panel 5: Auditoría de Logs (El "Semáforo")
Control total de lo que ocurre en el servidor:
*   **Logs Oracle (API)**: Muestra errores de base de datos. Si un guardado falla, aquí verá por qué.
*   **Logs Web (Servidor)**: Muestra la actividad del portal web.
*   **Botones de Recarga**: Iconos (`🔄`) para refrescar la información al instante.

---

## 3. 🔄 Flujo de Respuesta del Cliente

1.  **Recepción**: El cliente recibe un correo con un enlace (URL con ID cifrado).
2.  **Validación**: Al abrirlo, el sistema carga sus datos actuales de **TÜV SÜD**.
3.  **Modificación**: El cliente puede actualizar:
    *   Datos de Emplazamiento.
    *   Datos de Facturación (IBAN, Email).
    *   Notas adicionales por cada equipo.
    *   **Selección Inteligente**: El selector de cabecera permite marcar todos los equipos a la vez, y se actualiza automáticamente si se marcan las filas una a una.
4.  **Guardado Seguro y Detección de Cambios**: Al pulsar "Guardar Datos", el sistema realiza una validación inteligente:
    - **Validación de Formatos**: Comprueba que los emails y el IBAN sean correctos.
    - **Ventana de Confirmación**: Si hay cambios, se muestra una ventana detallando qué se ha modificado. Cada campo incluye la sección a la que pertenece para evitar ambigüedades (Ej: "NIF (Datos del Titular)", "NIF (Datos del Representante/Gestor)").
    - **Aviso de Variación de Precio**: Si se han modificado equipos o notas, el modal muestra un aviso destacado: *"El precio total inspección puede variar. Póngase en contacto con el personal de TÜV SÜD."*
    - **Sin Cambios**: Si no se ha modificado nada, el sistema preguntará "¿Desea confirmar la oferta?" para evitar clics accidentales.
    - **Confirmación Final**: Solo tras pulsar "Aceptar" en la ventana de cambios se procederá con el guardado real en la base de datos.
5.  **Notificación por Correo Electrónico**: Tras el guardado exitoso, el sistema envía automáticamente un **correo electrónico de confirmación** a la dirección del titular (y opcionalmente al GIR si está configurado). El correo incluye:
    *   Resumen completo de la oferta aceptada (datos generales, equipos, precios, facturación).
    *   Logo oficial de TÜV SÜD embebido (visible sin descargar imágenes en Outlook).
    *   Notas adicionales si las hubiera.
    *   Validación automática de las direcciones de correo antes del envío.

---

## 4. 💡 Comportamiento de los Mensajes
Para su tranquilidad, el sistema le avisará de todo lo que ocurra:
*   **Confirmaciones Verdes**: Aparecen en un modal central con animación. El mensaje indica si el correo se ha enviado correctamente (ej: *"Datos guardados correctamente y notificación enviada a correo1@test.com y correo2@test.com"*). Se cierra automáticamente a los 5 segundos.
*   **Confirmaciones con Advertencia**: Si los datos se guardaron pero el correo no pudo enviarse (por error SMTP o dirección inválida), el modal verde incluirá un aviso: *"Datos guardados correctamente (Advertencia: No se pudo enviar el correo de notificación)"*.
*   **Avisos Rojos (Errores)**: No se quitan solos. Si el error es en un dato (como el IBAN), la marca roja se quitará en cuanto usted empiece a escribir en ese cuadro para corregirlo. Si el error es de conexión, deberá intentar guardar de nuevo o pulsar F5.

## 🆘 Resolución de Problemas Comunes

| Síntoma | Causa Probable | Solución |
| :--- | :--- | :--- |
| **"Error al conectar con la API"** | El servicio backend no está arrancado. | Verifique que ambos proyectos (Web y API) estén en ejecución en Visual Studio. |
| **"ID Inválido"** | El token de la URL ha sido manipulado o está incompleto. | Genere un nuevo enlace desde el Panel 1 de la Lanzadera. |
| **"Formato de IBAN incorrecto"** | Se han introducido espacios o el código de país no es español (ES). | Introduzca los 24 caracteres seguidos, empezando por ES. |
| **No se guardan los cambios** | Posible pérdida de conexión con Oracle. | Revise el Panel 5 (Logs) para ver el mensaje de error técnico. |
| **"Too Many Requests" (Error 429)** | Se ha superado el límite de peticiones de seguridad para esa IP. | Espere 60 segundos antes de volver a intentar la operación. |

---
© 2026 TÜV SÜD - Manual de Usuario Final.
