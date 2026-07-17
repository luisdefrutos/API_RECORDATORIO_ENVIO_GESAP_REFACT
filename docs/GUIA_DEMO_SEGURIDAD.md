# 🛡️ Guía de Demostración: Seguridad y Control de Acceso (v1.6.0)

Este documento resume las nuevas capacidades de seguridad implementadas para la demo técnica. El objetivo es mostrar un sistema proactivo, verificable y fácil de administrar.

---

## 🏗️ 1. Arquitectura de Defensa en Profundidad
Explica que la seguridad no es un "punto único", sino una serie de capas sincronizadas:
- **Capa 1 (Web/Proxy)**: Bloqueo inmediato al detectar la IP. Ahorra recursos al no procesar la petición.
- **Capa 2 (API)**: Verificación redundante. Si alguien saltara el Proxy, el API cortaría la conexión igualmente.
- **Capa 3 (Infraestructura)**: Lógica centralizada en `SecurityService` para consistencia total.

---

## 🛠️ 2. Puntos Clave de la Demo

### A. Configuración Dinámica (El "Botón del Pánico")
- **Dónde**: `Web.config` -> llave `Security_BlackList`.
- **Qué mostrar**: Cómo se pueden añadir IPs separadas por comas.
- **Poder**: Soporte de comodines. `185.45.*` bloquea todo un rango corporativo en segundos sin recompilar.

### B. Diagnóstico Interactivo (Lanzadera Pro - Panel 5)
- **Escenario**: *"Un usuario dice que no puede entrar. ¿Sistemas lo ha bloqueado?"*
- **Acción**: Introduce la IP en el Panel 5 y dale a **"Probar Regla"**.
- **Resultado**: El panel se vuelve rojo (Bloqueada) o verde (Permitida) instantáneamente, mostrando además qué reglas del config están causando el bloqueo.

### C. Suite de Verificación Automática (`SecurityTests.aspx`)
- **Escenario**: *"¿Cómo sabemos que los comodines o las reglas complejas funcionan bien?"*
- **Acción**: Pulsa **"Ejecutar Suite Automática"**.
- **Resultado**: Se abre una página que ejecuta 10 tests de estrés sobre la lógica algorítmica. Ver todos los "✓ PASADO" da total confianza al cliente sobre la robustez del motor.

---

## 🗣️ Guion Sugerido (Elevator Pitch)
> *"Hemos dotado al sistema de un 'Escudo de Red' administrable. No hace falta ser programador para bloquear un ataque IP; basta con actualizar el Web.config. Y lo más importante: hemos incluido herramientas de diagnóstico para que el equipo de soporte pueda verificar bloqueos de forma visual y segura, asegurando que la operativa nunca se detenga por errores de configuración."*

---


> [!TIP]
> El sistema detecta la IP real incluso detrás de balanceadores de carga gracias al soporte de la cabecera `X-Forwarded-For`.
