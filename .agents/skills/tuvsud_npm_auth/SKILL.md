---
name: tuvsud_npm_auth
description: Instrucciones para configurar .npmrc y descargar la librería Algorithm Design System desde el repositorio privado de Azure DevOps de TÜV SÜD.
---

# TUV SUD NPM Auth Skill

Esta habilidad (skill) te enseña cómo configurar el entorno para descargar paquetes privados de TÜV SÜD (como `@tuvsud/design-system`) utilizando npm.

## Prerrequisitos
El usuario debe proporcionarte un **PAT (Personal Access Token)** válido de Azure DevOps con permisos de `Packaging (Read)`.

## Pasos para la ejecución

1. **Crear el archivo `.npmrc`**:
   En la raíz del proyecto, debes crear un archivo llamado `.npmrc` con el siguiente contenido, reemplazando `${YOUR_PAT_TOKEN}` por el token proporcionado por el usuario:

   ```text
   registry=https://registry.npmjs.org/
   always-auth=true
   
   @tuvsud:registry=https://pkgs.dev.azure.com/tuvsud01/_packaging/design-system/npm/registry/
   //pkgs.dev.azure.com/tuvsud01/_packaging/design-system/npm/registry/:_authToken=${YOUR_PAT_TOKEN}
   ```

2. **Instalar el paquete**:
   Ejecuta el comando para instalar la librería y los iconos:
   ```bash
   npm install @tuvsud/design-system @material-symbols/svg-400
   ```

3. **Adaptación para Proyectos Vanilla (Sin Bundler)**:
   Si estás trabajando en un proyecto HTML/JS Vanilla (como `RecordatorioEnvio.Web`), **no puedes importar desde `node_modules` directamente en el HTML de forma limpia**.
   Debes copiar los archivos compilados que provea el paquete desde `node_modules/@tuvsud/design-system/dist/...` hacia la carpeta pública de assets del proyecto (por ejemplo `css/vendor/` o `js/vendor/`).
   
   Normalmente, para Web Components Vanilla, se copia el bundle JS y el CSS y se incluyen en el `index.html`:
   ```html
   <link rel="stylesheet" href="css/vendor/bundle-light.css">
   <script type="module" src="js/vendor/tuvsud-components.esm.js"></script>
   ```

## Advertencias de Seguridad
- NUNCA subas el archivo `.npmrc` al control de versiones (SVN/Git). Si lo creas, asegúrate de borrarlo.
