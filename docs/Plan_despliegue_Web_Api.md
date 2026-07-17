# 🚀 Guía de Despliegue en IIS (Web y API)

> **Documento único de referencia para todos los despliegues.**
> Basado en el despliegue real del servidor de pruebas (`gestion3-desa.atisae.com`). Al final se detalla qué cambia para **Producción**.

---

## Fase 0: Requisitos Técnicos del Servidor

Antes de empezar, asegúrate de que los servidores destino cumplen estos requisitos:

> [!IMPORTANT]
> En producción, **Web y API deben estar en servidores separados**. Este aislamiento es obligatorio por seguridad: si la Web sufre un ataque, la API y Oracle permanecen inaccesibles directamente desde el exterior.

### 🌐 Servidor Web (Frontend + Proxy)
*   **SO**: Windows Server 2012 R2 o superior.
*   **Servidor Web**: IIS 8.5+.
*   **Runtime**: .NET Framework 4.7.2 Runtime.
*   **Módulos IIS**: ASP.NET 4.7 en modo **Integrado**. El módulo `ExtensionlessUrlHandler` se configura automáticamente desde el `Web.config`.
*   **Contenido en producción**: Solo `index.html`, `favicon.ico`, `images/`, `css/`, `js/`, `Global.asax`, `Web.config` y carpeta `bin/`. **Sin archivos `.aspx`.**

### ⚙️ Servidor API (Backend + Oracle)
*   **SO**: Windows Server 2012 R2 o superior.
*   **Servidor Web**: IIS 8.5+.
*   **Runtime**: .NET Framework 4.7.2 Runtime.
*   **Acceso a Datos**: No requiere cliente Oracle pesado. El paquete `Oracle.ManagedDataAccess` está incluido en la carpeta `bin/`. El servidor debe tener visibilidad de red al puerto **1521** de Oracle.

---


## 🗺️ Tabla de Entornos

| Parámetro | Servidor de Pruebas | Servidor de Producción (API) | Servidor de Producción (Web) |
|---|---|---|---|
| **Servidor IIS** | `SESMADE55003` | Servidor DMZ (consultar a Sistemas) | Servidor portal (consultar a Sistemas) |
| **Sitio IIS** | `Gestion3` | Sitio propio en DMZ | Sitio propio en portal |
| **Pool de aplicaciones** | `Gestion2` (.NET v4.0, Integrado) | Pool dedicado (.NET v4.0, Integrado) | Pool dedicado (.NET v4.0, Integrado) |
| **Ruta física** | API: `E:\App\gestion3\API_REC_ENV_GESAP` | `E:\app\gestion-dmz\API_REC_ENV_GESAP` | Consultar a Sistemas |
| | Web: `E:\App\gestion3\WEB_REC_ENV_GESAP` | — | — |
| **URL pública** | API: `https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api` | `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api` | — |
| | Web: `https://gestion3-desa.atisae.com/WEB_REC_ENV_GESAP/` | — | `https://portal-contrataciones.tuv-sud.es/WEB_REC_ENV_GESAP/` |
| **Swagger y GetEncrypt** | ✅ Activo (`EsDesarrollo=true`) | ❌ Desactivado (`EsDesarrollo=false`) | N/A |
| **debug_launcher.aspx** | ✅ Incluido | N/A | ❌ **Eliminado o renombrado** |
| **SecurityTests.aspx** | ✅ Incluido | N/A | ❌ **Eliminado o renombrado** |
| **Notificación Email** | ✅ Envía correo tras aceptar oferta | ✅ Requiere visibilidad SMTP | N/A |

---

## Fase 1: Compilación y Generación de Entregables (Visual Studio)

### 1.1. Publicar el Backend (API)
1. Abre la solución `RecordatorioEnvioGesap.sln` en Visual Studio.
2. En la barra superior, cambia el modo de compilación de **Debug** a **Release**.
3. Haz clic derecho en el proyecto **RecordatorioEnvio.API** → **Publicar** (Publish).
4. Elige como destino **Carpeta** (Folder) y una ruta local accesible, por ejemplo: `C:\Despliegues\API_GESAP`
5. Pulsa **Publicar**. Visual Studio generará los binarios (`.dll`) en esa ruta.

### 1.2. Publicar el Frontend (Web)
1. Haz clic derecho en el proyecto **RecordatorioEnvio.Web** → **Publicar**.
2. Elige **Carpeta** y como ruta: `C:\Despliegues\WEB_GESAP`
3. Pulsa **Publicar**.

> [!TIP]
> Una vez terminada esta fase puedes cerrar Visual Studio. Solo trabajaremos con las dos carpetas generadas.

---

## Fase 2: Configuración de Web.config (Valores Reales del Servidor de Pruebas)

> [!WARNING]
> El `Web.config` que genera la publicación contiene los valores del entorno local de desarrollo. **Deben sustituirse** por los valores reales del servidor antes de copiar los archivos.

### 2.1. Web.config de la API (`C:\Despliegues\API_GESAP\Web.config`)

Este es el estado real con el que está desplegado en el servidor de pruebas:

```xml
<connectionStrings>
  <add name="OracleConnection"
    connectionString="Data Source=(DESCRIPTION=(SDU=65535)(ADDRESS=(PROTOCOL=TCP)(HOST=10.108.206.7)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ORCLDV.snprivate.vcnatisae.oraclevcn.com)));User Id=GESAP;Password=GesAp.-20;"
    providerName="Oracle.ManagedDataAccess.Client" />
</connectionStrings>
<appSettings>
  <add key="EncryptionKey" value="gx9azkSylw1eOPpBKbx0AtlPGKYj7XswwYpl9AxyPb4=" />
  <add key="HmacKey"       value="Eq80RniQq83vASNFV4m83vARIjM0VWZ3iJmqu8zd7v8=" />
  <add key="ApiKey"        value="f3a1c2d4e5b67890abcdef1234567890abcdefabcdefabcdefabcdefabcdef" />
  <add key="LogRetentionDays"     value="30" />
  <add key="RateLimit_Limit"      value="500" />
  <add key="RateLimit_WindowSeconds" value="60" />
  <!-- Auditoría: Niveles (DEBUG, INFO, WARN, ERROR, FATAL, NONE)              -->
  <!-- Audit_LogLevel   → nivel mínimo para fichero TXT                        -->
  <!-- Audit_DbLogLevel → nivel mínimo para BD Oracle                          -->
  <!-- Log_EnableDb     → interruptor maestro BD: true=escribe | false=solo TXT -->
  <add key="Audit_LogLevel"    value="WARN"  />
  <add key="Audit_DbLogLevel"  value="ERROR" />
  <add key="Log_EnableDb"      value="true"  />
  <add key="Security_BlackList" value="" />
  <add key="Security_BlackList_JsonPath" value="~/App_Data/blacklist.json" />
  <!-- PRUEBAS: Swagger y debug activo. En producción cambiar a false -->
  <add key="EsDesarrollo" value="true" />
</appSettings>
```

> [!IMPORTANT]
> **Aviso de Seguridad (`EsDesarrollo`)**: 
> Durante el despliegue es vital verificar este parámetro. Cuando se establece en `true` (entorno de pruebas), habilita:
> 1. **Swagger UI**: Permite explorar y probar los endpoints visualmente.
> 2. **Método `GetEncrypt`**: Endpoint de utilidad para generar identificadores cifrados en Base64 (usado por la Lanzadera de Control).
> 
> En producción, este valor **DEBE ser `false`** para evitar exponer la documentación interna y, sobre todo, para prevenir que un tercero pueda generar identificadores cifrados arbitrarios.

### 2.2. Web.config de la Web (`C:\Despliegues\WEB_GESAP\Web.config`)

```xml
  <add key="webpages:Version" value="3.0.0.0" />
  <add key="webpages:Enabled" value="false" />
  <!-- URL real de la API en el servidor de pruebas -->
  <add key="ApiBaseUrl" value="https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api" />
  <add key="EncryptionKey" value="gx9azkSylw1eOPpBKbx0AtlPGKYj7XswwYpl9AxyPb4=" />
  <add key="HmacKey"       value="Eq80RniQq83vASNFV4m83vARIjM0VWZ3iJmqu8zd7v8=" />
  <add key="ApiKey"        value="f3a1c2d4e5b67890abcdef1234567890abcdefabcdefabcdefabcdefabcdef" />
  <add key="LogRetentionDays"  value="30" />
  <!-- Auditoría: Niveles (DEBUG, INFO, WARN, ERROR, FATAL, NONE)         -->
  <!-- La Web solo escribe en TXT. No tiene Log_EnableDb ni Audit_DbLogLevel -->
  <add key="Audit_LogLevel"    value="WARN" />
  <add key="Security_BlackList" value="" />
</appSettings>
<system.web>
  <compilation targetFramework="4.7.2" />
  <httpRuntime targetFramework="4.7.2" />
</system.web>
<system.webServer>
  <modules runAllManagedModulesForAllRequests="true">
    <remove name="UrlRoutingModule-4.0" />
    <add name="UrlRoutingModule-4.0" type="System.Web.Routing.UrlRoutingModule" preCondition="" />
  </modules>
  <handlers>
    <remove name="ExtensionlessUrlHandler-Integrated-4.0" />
    <remove name="OPTIONSVerbHandler" />
    <remove name="TRACEVerbHandler" />
    <add name="ExtensionlessUrlHandler-Integrated-4.0" path="*." verb="*"
         type="System.Web.Handlers.TransferRequestHandler"
         preCondition="integratedMode,runtimeVersionv4.0" />
  </handlers>
</system.webServer>
```

> [!IMPORTANT]
> `EncryptionKey`, `HmacKey` y `ApiKey` deben ser **idénticas** en la API y en la Web. Si no coinciden, el sistema rechazará todas las peticiones.

### 2.3. Configuración de Seguridad Obligatoria (Pentest / IIS)

Para evitar fugas de información y pasar con éxito auditorías de seguridad (Pentest), es **obligatorio** que tanto el `Web.config` de la API como el de la Web incluyan los siguientes nodos dentro de `<system.web>` y `<system.webServer>`:

**Dentro de `<system.web>`:**
```xml
  <!-- [SEGURIDAD] Ocultar versión de ASP.NET -->
  <httpRuntime targetFramework="4.7.2" enableVersionHeader="false" />
  <!-- [SEGURIDAD] Evitar Pantallazo Amarillo (YSOD) con detalles del servidor -->
  <customErrors mode="On" />
```

**Dentro de `<system.webServer>`:**
```xml
  <!-- [SEGURIDAD] Cabeceras HTTP de Seguridad -->
  <httpProtocol>
    <customHeaders>
      <!-- Ocultar versión de IIS/ASP.NET -->
      <remove name="X-Powered-By" />
      <!-- Protección Anti-Clickjacking -->
      <add name="X-Frame-Options" value="SAMEORIGIN" />
      <!-- Protección Anti MIME-Sniffing -->
      <add name="X-Content-Type-Options" value="nosniff" />
      <!-- Forzar HTTPS estricto (HSTS) -->
      <add name="Strict-Transport-Security" value="max-age=31536000; includeSubDomains" />
      <!-- Filtro Anti-XSS nativo -->
      <add name="X-XSS-Protection" value="1; mode=block" />
    </customHeaders>
  </httpProtocol>
```

---

## Fase 3: Copiar archivos al Servidor IIS

### 3.1. Servidor de Pruebas (un solo servidor)

1. Conéctate al servidor **`SESMADE55003`** (acceso RDP o red compartida).
2. Copia la carpeta publicada de la API a:
   ```
   E:\App\gestion3\API_REC_ENV_GESAP\
   ```
3. Copia la carpeta publicada de la Web a:
   ```
   E:\App\gestion3\WEB_REC_ENV_GESAP\
   ```
4. Si existían archivos anteriores, sobrescribe todo. Si es un despliegue limpio desde cero, borra primero la carpeta destino.

### 3.2. Producción (servidores separados)

> [!CAUTION]
> En producción, **la API y la Web están en servidores físicos distintos**. Debes conectarte a cada uno por separado.

**Servidor de la API (DMZ):**
1. Conéctate al servidor DMZ donde está publicada `https://ws-dmz.atisae.com`.
2. Copia la carpeta publicada de la API a:
   ```
   E:\app\gestion-dmz\API_REC_ENV_GESAP\
   ```

**Servidor de la Web (Portal):**
1. Conéctate al servidor del portal donde está publicada `https://portal-contrataciones.tuv-sud.es`.
2. Copia la carpeta publicada de la Web a la ruta física configurada por Sistemas (consultar al equipo de infraestructura).

> [!NOTE]
> **Archivo `images/`:** La carpeta `images/` con el logo SVG de TÜV SÜD no se copia automáticamente por Visual Studio si no está incluida en el proyecto. Cópiala manualmente junto al `index.html`.

---

## Fase 4: Crear las Aplicaciones en IIS (Solo en el primer despliegue)

Abre el **Administrador de IIS** en el servidor `SESMADE55003`.

```
SESMADE55003
  └─ Sitios
       └─ Gestion3          ← Sitio padre
            ├─ API_REC_ENV_GESAP   ← Aplicación (icono globo 🌐)
            └─ WEB_REC_ENV_GESAP   ← Aplicación (icono globo 🌐)
```

**Para la API** (si no existe ya):
1. Clic derecho sobre **Gestion3** → **Agregar aplicación...**
2. Alias: `API_REC_ENV_GESAP`
3. Ruta física: `E:\App\gestion3\API_REC_ENV_GESAP`
4. Pool: `Gestion2` (**.NET v4.0, Modo Integrado**)
5. Aceptar.

**Para la Web** (si no existe ya):
1. Clic derecho sobre **Gestion3** → **Agregar aplicación...**
2. Alias: `WEB_REC_ENV_GESAP`
3. Ruta física: `E:\App\gestion3\WEB_REC_ENV_GESAP`
4. Pool: `Gestion2`
5. Aceptar.

> [!IMPORTANT]
> Verifica que el icono de ambas aplicaciones es un **globo terráqueo** (🌐). Si es solo una carpeta amarilla, haz clic derecho → **Convertir en aplicación**. Sin esto el enrutamiento MVC no funciona y verás errores 404 en las rutas `/Proxy/...`.

---

## Fase 5: Permisos de Escritura para Logs

La aplicación necesita permisos de escritura para generar los archivos de auditoría en `App_Data/Logs`.

**Para la API:**
1. Ve a `E:\App\gestion3\API_REC_ENV_GESAP\App_Data`
2. Clic derecho → **Propiedades** → **Seguridad** → **Editar** → **Agregar**
3. Escribe `IIS_IUSRS`, pulsa "Comprobar nombres" y acepta.
4. Marca el permiso **Modificar** y acepta.

**Para la Web:** Repite los mismos pasos sobre `E:\App\gestion3\WEB_REC_ENV_GESAP\App_Data`.

> [!TIP]
> Si la carpeta `App_Data\Logs` no existe, no hace falta crearla manualmente. La propia aplicación la creará al arrancar si tiene permiso de Modificar sobre `App_Data`.

---

## Fase 6: Cifrado de la Cadena de Conexión (aspnet_regiis)

> [!CAUTION]
> **Este comando se ejecuta SIEMPRE en el servidor de destino, nunca en local.** El cifrado utiliza la Machine Key del sistema operativo del servidor. Un `Web.config` cifrado en local no funcionará en el servidor (y viceversa).

Abre una consola de **CMD o PowerShell como Administrador** en el servidor `SESMADE55003`.

### 6.1. Cifrar la cadena de conexión (Servidor de Pruebas)

> **Este comando se ejecuta solo en la Api ya que solo este web.config tendra cadena de conexión de base de datos.Este comando se ejecutara con cmd o PowerShell como Administrador en el servidor,porque el cifrado usa una llave secreta que está "escondida" en el hardware/Windows de tu PC (la Machine Key). Con lo cual este comando es a nivel de máquina y si lo "ofuscas" en local y lo subes a mano al servidor ¡La aplicación explotará y dará un Error 500!. 

**Para la API:**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\App\gestion3\API_REC_ENV_GESAP"
```



### 6.2. Descifrar (si necesitas editar el Web.config manualmente)

Si necesitas modificar la cadena de conexión, primero descífrala con uno de estos comandos:

**Por ruta física** (más directo, no requiere saber el nombre del sitio IIS):
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "E:\App\gestion3\API_REC_ENV_GESAP"
```

**Por aplicación IIS** (requiere saber el nombre del sitio padre):
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pd "connectionStrings" -app "/API_REC_ENV_GESAP" -site "Gestion3"
```

> [!NOTE]
> Tras editar el `Web.config`, vuelve a ejecutar el comando de **cifrado** (`-pef`). La aplicación .NET lee los datos descifrados automáticamente en memoria; no es necesario ningún cambio de código.

---

## Fase 7: Pruebas de Verificación

### 7.1. Verificar que la API responde
Abre un navegador y navega a:
```
https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api/recordatorio
```
✅ Correcto: Devuelve un error `401 Unauthorized` o `403 Forbidden` (la API está viva y protegida por ApiKey).  
❌ Error: Si da `404`, revisa que la aplicación IIS está creada correctamente (Fase 4).

### 7.2. Verificar que el Swagger está activo (solo en pruebas)
```
https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/swagger
```
✅ Correcto: Muestra la interfaz de documentación Swagger.

### 7.3. Verificar la Lanzadera (solo en pruebas)
```
https://gestion3-desa.atisae.com/WEB_REC_ENV_GESAP/debug_launcher.aspx
```
✅ Correcto: Muestra la consola de administración con acceso a cifrado y logs.

### 7.4. Verificar el flujo completo de un recordatorio
Desde la Lanzadera, selecciona un ID de Oracle, copia la URL generada y pégala en una pestaña nueva. El formulario debe cargar con los datos del registro de prueba.

---

## Fase 8: ⚠️ Diferencias para el Despliegue en Producción

> [!CAUTION]
> En producción, la **API** se despliega en `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/` y la **Web** en `https://portal-contrataciones.tuv-sud.es/WEB_REC_ENV_GESAP/`. Son **servidores físicos distintos**. Este aislamiento es intencional por seguridad: si un servidor se ve comprometido, el otro permanece aislado.

### 8.1. Cambios obligatorios en el Web.config de la API (producción)

```xml
<!-- Cambiar a false para desactivar completamente Swagger y herramientas de desarrollo -->
<add key="EsDesarrollo" value="false" />

<!-- Nuevas claves generadas específicamente para producción (nunca reutilizar las de pruebas) -->
<add key="EncryptionKey" value="[NUEVA_CLAVE_PRODUCCION_BASE64]" />
<add key="HmacKey"       value="[NUEVA_CLAVE_PRODUCCION_BASE64]" />
<add key="ApiKey"        value="[NUEVO_API_KEY_PRODUCCION]" />

<!-- Cadena de conexión apuntando a Oracle de producción -->
<add name="OracleConnection" connectionString="[CADENA_PRODUCCION]" ... />
```

### 8.2. Cambios obligatorios en el Web.config de la Web (producción)

```xml
<!-- URL pública de la API de producción (servidor DMZ) -->
<add key="ApiBaseUrl" value="https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api" />

<!-- Las mismas claves nuevas de producción que en la API -->
<add key="EncryptionKey" value="[NUEVA_CLAVE_PRODUCCION_BASE64]" />
<add key="HmacKey"       value="[NUEVA_CLAVE_PRODUCCION_BASE64]" />
<add key="ApiKey"        value="[NUEVO_API_KEY_PRODUCCION]" />
```

### 8.3. Archivos que NO deben existir en producción

> [!CAUTION]
> La eliminación o inutilización de estos archivos es **obligatoria** antes de cualquier pentest o auditoría de seguridad. Su presencia expone el sistema a ataques directos sobre herramientas administrativas internas.

| Archivo | Motivo de eliminación | Alternativa |
|---|---|---|
| `debug_launcher.aspx` | Consola completa de administración. Expone cifrado, logs y auditoría de Oracle. | Renombrar a `debug_launcher.aspx_bak` |
| `SecurityTests.aspx` | Suite de pruebas de seguridad. Revela detalles de la arquitectura interna. | Renombrar a `SecurityTests.aspx_bak` |
| `App_Data/Logs/*.txt` | No subir logs de desarrollo/pruebas al servidor de producción. | Borrar contenido |

> [!TIP]
> **Renombrar vs. Eliminar:** Si se opta por renombrar la extensión (ej. `.aspx` → `.aspx_bak`), IIS no procesará el archivo como página activa, pero se conserva en el servidor para poder restaurarlo rápidamente en caso de necesidad diagnóstica puntual. Tras la intervención, volver a renombrar a `.aspx_bak`.

**En producción, la Web solo debe contener:**
```
WEB_REC_ENV_GESAP/  (en portal-contrataciones.tuv-sud.es)
├── index.html          ← La única página pública
├── favicon.ico
├── Global.asax
├── Web.config          ← Con ApiBaseUrl apuntando a ws-dmz.atisae.com
├── images/             ← Logo TÜV SÜD
├── css/
├── js/
│   └── app.js
├── App_Data/           ← Vacío (solo para logs en runtime)
└── bin/                ← DLLs compiladas
```

### 8.4. Configuración del cifrado de cadenas en producción

Ejecuta los mismos comandos `aspnet_regiis -pef` pero con las rutas físicas reales del servidor de producción:

**En el servidor de la API (DMZ):**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\app\gestion-dmz\API_REC_ENV_GESAP"
```

**En el servidor de la Web (Portal):**
```powershell
# Sustituir [RUTA_FISICA_WEB] por la ruta real asignada por Sistemas
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "[RUTA_FISICA_WEB_PRODUCCION]"
```

### 8.5. Requisitos de red para notificaciones por email

> [!IMPORTANT]
> A partir de la versión 2.0.0, la API envía correos electrónicos de confirmación al titular tras aceptar una oferta. Para que esto funcione en producción:
> - El servidor de la API (DMZ) debe tener **visibilidad de red al servidor SMTP corporativo** configurado en la tabla `GESAP.SYS_CONFIGURACION` de Oracle.
> - La configuración SMTP (host, puerto, usuario, contraseña, SSL) se obtiene **dinámicamente de la base de datos**, no del `Web.config`.
> - El logo de TÜV SÜD se embebe directamente en el correo (tecnología CID/Base64), por lo que **no requiere acceso a URLs externas** para mostrarse correctamente en Outlook.

---

## 📋 Checklist de Despliegue

### Servidor de Pruebas (`gestion3-desa.atisae.com`)
- [ ] Código compilado en modo **Release**
- [ ] Web.config de API con valores de Oracle de pruebas
- [ ] Web.config de Web con `ApiBaseUrl` → `https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api`
- [ ] `EsDesarrollo=true` en el Web.config de la API
- [ ] Aplicaciones IIS creadas con icono de globo (no carpeta amarilla)
- [ ] Pool `Gestion2` asignado a ambas aplicaciones
- [ ] Permisos `IIS_IUSRS` + Modificar sobre `App_Data`
- [ ] `connectionStrings` cifrada con `aspnet_regiis -pef`
- [ ] Swagger accesible en `https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/swagger`
- [ ] Lanzadera accesible en `https://gestion3-desa.atisae.com/WEB_REC_ENV_GESAP/debug_launcher.aspx`
- [ ] Envío de correo de prueba verificado (comprobar SMTP)

### Producción — Servidor API (`ws-dmz.atisae.com`)
- [ ] Código compilado en modo **Release**
- [ ] Web.config con `EsDesarrollo=false`
- [ ] Nuevas claves `EncryptionKey`, `HmacKey` y `ApiKey` generadas
- [ ] `connectionStrings` cifrada con `aspnet_regiis -pef` en `E:\app\gestion-dmz\API_REC_ENV_GESAP`
- [ ] Permisos `IIS_IUSRS` + Modificar sobre `App_Data`
- [ ] Visibilidad de red al servidor SMTP corporativo (para envío de emails)
- [ ] API responde en `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api/recordatorio` (401/403 = OK)

### Producción — Servidor Web (`portal-contrataciones.tuv-sud.es`)
- [ ] Código compilado en modo **Release**
- [ ] Web.config con `ApiBaseUrl` → `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api`
- [ ] Claves `EncryptionKey`, `HmacKey`, `ApiKey` **idénticas** a las de la API
- [ ] `debug_launcher.aspx` **eliminado o renombrado** a `.aspx_bak`
- [ ] `SecurityTests.aspx` **eliminado o renombrado** a `.aspx_bak`
- [ ] Permisos `IIS_IUSRS` + Modificar sobre `App_Data`
- [ ] Formulario accesible en `https://portal-contrataciones.tuv-sud.es/WEB_REC_ENV_GESAP/index.html?id=[TOKEN]`
- [ ] Pentest ejecutado sobre el entorno de producción antes del go-live

---

*Para más detalles sobre el cifrado de configuración, ver [GUIA_CIFRADO_CONFIG.md](GUIA_CIFRADO_CONFIG.md).*
