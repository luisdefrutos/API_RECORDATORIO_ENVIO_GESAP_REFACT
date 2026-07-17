# Plan de Refactorización: React + .NET (Sandbox PoC)

Este documento es la guía paso a paso para construir la Prueba de Concepto (PoC) de la aplicación de Recordatorios, migrando a una arquitectura moderna separada (Frontend en React y Backend en .NET MVC) dentro de un entorno aislado (Sandbox) controlado por Git/GitHub.

---

## FASE 1: Preparación del Entorno Aislado

1. **Crear la carpeta raíz**: Crea una nueva carpeta en tu equipo totalmente fuera del control de SVN (ej. `C:\Project\API_RECORDATORIO_ENVIO_GESAP_REFACT`).
2. **Estructura Interna**: Dentro de esa carpeta, crea dos subcarpetas:
   - `\Backend`
   - `\Frontend`

---

## FASE 2: Configuración del Backend (.NET)

El objetivo es trasladar la lógica actual de C# y prepararla para aceptar peticiones desde un puerto externo (CORS).

1. **Copiar el Código Base**: 
   Copia el contenido de tu antiguo proyecto `API_RECORDATORIO_ENVIO_GESAP` (excluyendo la carpeta `RecordatorioEnvio.Web` y todas las carpetas ocultas de SVN) dentro de la nueva carpeta `\Backend`.
2. **Instalar soporte CORS**:
   Abre la solución en Visual Studio. Ve al Administrador de Paquetes Nuget e instala `Microsoft.AspNet.WebApi.Cors`.
3. **Habilitar CORS**:
   En el archivo de configuración de inicio de la API (ej. `WebApiConfig.cs` o `Global.asax.cs`), añade la política para permitir que React se comunique con la API en local:
   ```csharp
   // Permite peticiones desde el puerto de Vite (React)
   var cors = new EnableCorsAttribute("http://localhost:5173", "*", "*");
   config.EnableCors(cors);
   ```

---

## FASE 3: Configuración del Frontend (React + Vite)

Construiremos el nuevo frontal desde cero utilizando Vite, el estándar actual de la industria.

1. **Inicializar Proyecto React**:
   Abre una terminal en la carpeta `\Frontend` y ejecuta:
   ```bash
   npm create vite@latest . -- --template react-ts
   ```
2. **Configurar Acceso a TÜV SÜD (NPM)**:
   Crea un archivo `.npmrc` en la carpeta `\Frontend` con tu Token de Azure (PAT):
   ```text
   registry=https://registry.npmjs.org/
   always-auth=true
   @tuvsud:registry=https://pkgs.dev.azure.com/tuvsud01/_packaging/design-system/npm/registry/
   //pkgs.dev.azure.com/tuvsud01/_packaging/design-system/npm/registry/:_authToken=AQUÍ_TU_TOKEN_PAT
   ```
3. **Instalar Dependencias**:
   Ejecuta la instalación de las librerías base y las corporativas:
   ```bash
   npm install
   npm install @tuvsud/design-system-react @material-symbols/svg-400
   ```
4. **¡Ciberseguridad!**: Borra inmediatamente el archivo `.npmrc` para no comprometer tu Token PAT.

---

## FASE 4: Control de Versiones (Git y GitHub)

Desconectaremos el proyecto del antiguo SVN y lo protegeremos en Git.

1. **Ignorar Basura (Gitignore)**:
   En la raíz (`Gesap_React_PoC`), crea un archivo `.gitignore` para evitar subir binarios y librerías pesadas:
   ```text
   # .NET
   [Bb]in/
   [Oo]bj/
   .vs/

   # Node / React
   node_modules/
   dist/
   .npmrc
   ```
2. **Inicializar Git**:
   Abre una terminal en la raíz y ejecuta:
   ```bash
   git init
   git add .
   git commit -m "Initial Sandbox setup: .NET Backend and React Frontend"
   ```
3. **Subir a GitHub**:
   Ve a GitHub, crea un repositorio vacío, copia su URL y ejecuta:
   ```bash
   git remote add origin <URL_DE_TU_REPOSITORIO>
   git push -u origin main
   ```

---

## FASE 5: Ejecución en Local y Desarrollo

A partir de este momento, el entorno está listo para programar. 

1. **Arrancar el Backend**: Inicia tu API desde Visual Studio (IIS Express). Apunta el puerto que te asigne (ej. `localhost:44321`).
2. **Arrancar el Frontend**: Abre una terminal en `\Frontend` y ejecuta `npm run dev`. Tu aplicación de React estará viva en `http://localhost:5173`.
3. **Desarrollo (Refactorización)**: Ya podemos empezar a programar los componentes `.tsx` (formularios, botones, modales) e integrarlos con las llamadas `fetch()` hacia tu puerto de IIS Express.
