# 🔐 Guía de Cifrado de Configuración (.NET Framework 4.7.2)

Esta guía explica cómo proteger secciones sensibles del archivo `Web.config` (como Connection Strings) utilizando la herramienta nativa de .NET `aspnet_regiis`.

> [!CAUTION]
> **ESTE PROCESO DEBE EJECUTARSE EN EL SERVIDOR DE DESTINO, NUNCA EN LOCAL.**
> El cifrado utiliza una llave RSA almacenada en la Machine Key del sistema operativo del servidor.
> *   **NO CIFRAR EN LOCAL:** Si cifras el archivo en tu PC de desarrollo y lo subes, **no funcionará** en el servidor (Error de descifrado).
> *   **PROCEDIMIENTO CORRECTO:** Sube el `Web.config` **sin cifrar (texto plano)** al servidor, y ejecuta el comando `aspnet_regiis` directamente en la consola del servidor como último paso del despliegue.

---

## 1. Localización de la Herramienta

La utilidad se encuentra en el directorio del Framework .NET:
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe
```

Para usarla, abre una consola de **PowerShell o CMD como Administrador**.

---

## 2. Cifrado por Ruta Física (Opción Recomendada)

Esta opción cifra el archivo basándose en la carpeta física donde se encuentra, sin necesidad de conocer el nombre del sitio en IIS.

### Sintaxis
```powershell
aspnet_regiis.exe -pef "nombre_seccion" "ruta_carpeta"
```
*   `-pef`: **P**rovider **E**ncrypt **F**ile (cifrado por ruta física).
*   `nombre_seccion`: La sección XML a cifrar (`connectionStrings` o `appSettings`).
*   `ruta_carpeta`: La ruta absoluta a la carpeta que contiene el `Web.config` **(sin barra final)**.

### Ejemplo Real — Servidor de Pruebas (SESMADE55003 / Gestion3)

**Cifrar la cadena de conexión de la API:**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\App\gestion3\API_REC_ENV_GESAP"
```

**Cifrar la cadena de conexión de la Web:**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\App\gestion3\WEB_REC_ENV_GESAP"
```

---

## 3. Cifrado por Aplicación IIS (Opción Alternativa)

Si prefieres usar el nombre de la aplicación virtual registrada en IIS (requiere conocer el nombre del sitio padre):

### Sintaxis
```powershell
aspnet_regiis.exe -pe "nombre_seccion" -app "/AliasEnIIS" -site "NombreSitioIIS"
```

### Ejemplo Real — Servidor de Pruebas
```powershell
# Cifrar cadena de conexión de la API via nombre IIS
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pe "connectionStrings" -app "/API_REC_ENV_GESAP" -site "Gestion3"

# Cifrar cadena de conexión de la Web via nombre IIS
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pe "connectionStrings" -app "/WEB_REC_ENV_GESAP" -site "Gestion3"
```

---

## 4. Cómo Descifrar (Revertir)

Si necesitas editar el archivo `Web.config` manualmente en el servidor (por ejemplo, para cambiar la contraseña de Oracle), primero debes descifrarlo.

### Por Ruta Física (`-pdf`)
```powershell
# Descifrar API
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "E:\App\gestion3\API_REC_ENV_GESAP"

# Descifrar Web
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "E:\App\gestion3\WEB_REC_ENV_GESAP"
```

### Por Aplicación IIS (`-pd`)
```powershell
# Descifrar API via nombre IIS
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pd "connectionStrings" -app "/API_REC_ENV_GESAP" -site "Gestion3"

# Descifrar Web via nombre IIS
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pd "connectionStrings" -app "/WEB_REC_ENV_GESAP" -site "Gestion3"
```

> [!TIP]
> Tras editar el `Web.config`, vuelve a ejecutar el comando de cifrado (`-pef` o `-pe`). La aplicación .NET lee los datos descifrados automáticamente en memoria; no es necesario ningún cambio en el código.

---

## 5. Verificación

Una vez cifrado, si abres el `Web.config` con el Bloc de Notas, la sección `connectionStrings` se verá como un bloque ilegible:
```xml
<connectionStrings configProtectionProvider="RsaProtectedConfigurationProvider">
  <EncryptedData>
    <CipherData>
      <CipherValue>aq9s8d7f9s8d7f...</CipherValue>
    </CipherData>
  </EncryptedData>
</connectionStrings>
```
**La aplicación .NET leerá los valores descifrados automáticamente** en memoria sin ningún cambio de código.

---

## 6. Ejemplo Real — Servidor de Producción

> [!IMPORTANT]
> En producción, la API y la Web están en **servidores físicos distintos**. Debes ejecutar `aspnet_regiis` en cada servidor por separado.

**Cifrar la cadena de conexión de la API (servidor DMZ — `ws-dmz.atisae.com`):**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\app\gestion-dmz\API_REC_ENV_GESAP"
```

**Descifrar la cadena de conexión de la API (servidor DMZ):**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "E:\app\gestion-dmz\API_REC_ENV_GESAP"
```

**Cifrar/Descifrar la Web (servidor Portal — `portal-contrataciones.tuv-sud.es`):**
```powershell
# Sustituir [RUTA_FISICA_WEB] por la ruta asignada por el equipo de Sistemas
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "[RUTA_FISICA_WEB_PRODUCCION]"
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "[RUTA_FISICA_WEB_PRODUCCION]"
```

---

*© 2026 TÜV SÜD - Documentación Técnica Confidencial.*
