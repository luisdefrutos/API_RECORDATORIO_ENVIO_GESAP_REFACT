# Documentación Unificada - API RECORDATORIO ENVIO

---

# Arquitectura TÃ©cnica - RecordatorioEnvio

## 1. Estructura de Proyectos y Dependencias

``![Diagrama](images/ARQUITECTURA_TECNICA_diag_1.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
graph TB
    subgraph "PresentaciÃ³n"
        WEB["RecordatorioEnvio.Web HTML + jQuery + JS"]
        API["RecordatorioEnvio.API Web API Controllers + Swagger"]
    end
    
    subgraph "LÃ³gica de AplicaciÃ³n"
        APP["RecordatorioEnvio.Application Services + DTOs + Interfaces"]
    end
    
    subgraph "Dominio"
        DOM["RecordatorioEnvio.Domain Entities + Business Logic"]
    end
    
    subgraph "Infraestructura"
        INF["RecordatorioEnvio.Infrastructure Repositories + ADO.NET + Encryption + Logging"]
    end
    
    subgraph "Datos Externos"
        ORACLE[("Oracle Database DATOS_PRUEBA")]
        SYSINT["Sistema Interno Backend"]
    end
    
    subgraph "Testing"
        TEST["RecordatorioEnvio.Tests Unit Tests"]
    end
    
    WEB -->|AJAX GET/POST| API
    API --> APP
    APP --> DOM
    APP --> INF
    INF --> ORACLE
    INF --> SYSINT
    TEST -.->|Prueba| API
    TEST -.->|Prueba| APP
    TEST -.->|Prueba| INF
    
    style WEB fill:#e1f5ff
    style API fill:#fff4e1
    style APP fill:#f0e1ff
    style DOM fill:#ffe1e1
    style INF fill:#e1ffe1
    style ORACLE fill:#ffd700
    style SYSINT fill:#ffa500
    style TEST fill:#d3d3d3
`
</details>``

## 2. Dependencias entre Proyectos

``![Diagrama](images/ARQUITECTURA_TECNICA_diag_2.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
graph LR
    API["API"] --> APP["Application"]
    API --> INF["Infrastructure"]
    APP --> DOM["Domain"]
    APP --> INF
    INF --> DOM
    TEST["Tests"] --> API
    TEST --> APP
    TEST --> INF
    WEB["Web"] -.->|HTTP| API
    
    style API fill:#fff4e1
    style APP fill:#f0e1ff
    style DOM fill:#ffe1e1
    style INF fill:#e1ffe1
    style WEB fill:#e1f5ff
    style TEST fill:#d3d3d3
`
</details>``

**Reglas de Dependencia:**
- âœ… **API** puede referenciar â†’ Application, Infrastructure
- âœ… **Application** puede referenciar â†’ Domain, Infrastructure
- âœ… **Infrastructure** puede referenciar â†’ Domain
- âŒ **Domain** NO debe referenciar a nadie (nÃºcleo puro)
- âœ… **Tests** puede referenciar â†’ Todos los proyectos
- âœ… **Web** NO referencia proyectos (solo consume API vÃ­a HTTP)

---

## 3. Flujo de PeticiÃ³n GET (Consulta Inicial)

``![Diagrama](images/ARQUITECTURA_TECNICA_diag_3.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
sequenceDiagram
    participant Email as ðŸ“§ Correo ElectrÃ³nico
    participant User as ðŸ‘¤ Usuario
    participant Browser as ðŸŒ Navegador
    participant Frontend as ðŸ“„ Frontend (index.html)
    participant API as ðŸ”Œ API Controller
    participant Service as âš™ï¸ Application Service
    participant Encrypt as ðŸ” Encryption Service
    participant Repo as ðŸ“Š Repository
    participant DB as ðŸ—„ï¸ Oracle DB
    participant SysInt as ðŸ–¥ï¸ Sistema Interno
    
    Email->>User: Enlace: https://servidor.com/index.html?id=X7k9mP2qR5tY8wZ
    User->>Browser: Click en enlace
    Browser->>Frontend: Carga index.html
    Frontend->>Frontend: Extrae parÃ¡metro 'id' de URL
    
    Frontend->>API: GET /api/datos/{"idEncriptado"}
    Note over API: DatosController.Get(idEncriptado)
    
    API->>Service: GetDatosById(idEncriptado)
    Service->>Encrypt: Decrypt(idEncriptado)
    Encrypt-->>Service: idOriginal = "12345"
    
    alt OpciÃ³n 1: Datos en Oracle
        Service->>Repo: GetById(idOriginal)
        Repo->>DB: SELECT * FROM DATOS_PRUEBA WHERE ID = 12345
        DB-->>Repo: Datos encontrados
        Repo-->>Service: Entity
    else OpciÃ³n 2: Datos en Sistema Interno
        Service->>SysInt: HTTP GET /datos/12345
        SysInt-->>Service: JSON con datos
    end
    
    Service-->>API: DatosDTO
    API-->>Frontend: HTTP 200 OK + JSON
    
    Frontend->>Frontend: Renderiza datos en formulario
    Frontend->>Browser: Muestra formulario con datos
`
</details>``

---

## 4. Flujo de PeticiÃ³n POST (Guardar Datos Modificados)

``![Diagrama](images/ARQUITECTURA_TECNICA_diag_4.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
sequenceDiagram
    participant User as ðŸ‘¤ Usuario
    participant Frontend as ðŸ“„ Frontend (index.html)
    participant API as ðŸ”Œ API Controller
    participant Validator as âœ… Validator
    participant Service as âš™ï¸ Application Service
    participant Repo as ðŸ“Š Repository
    participant DB as ðŸ—„ï¸ Oracle DB
    participant Logger as ðŸ“ LogHelper (Custom)
    
    User->>Frontend: Modifica datos y click "Guardar"
    Frontend->>Frontend: ValidaciÃ³n JavaScript (campos obligatorios)
    
    alt ValidaciÃ³n Frontend OK
        Frontend->>API: POST /api/datos (JSON Plano)
        Note over API: DatosController.Post(datosDTO)
        
        API->>Validator: Validate(datosDTO)
        Validator-->>API: ValidationResult
        
        alt ValidaciÃ³n Backend OK
            API->>Service: UpdateDatos(datosDTO)
            Service->>Repo: Update(entity)
            Repo->>DB: UPDATE DATOS_PRUEBA SET...
            
            alt Update Exitoso
                DB-->>Repo: Rows affected = 1
                Repo-->>Service: Success
                Service->>Logger: LogInfo("Datos actualizados ID: 12345")
                Service-->>API: OperationResult (Success)
                API-->>Frontend: HTTP 200 OK
                Frontend->>User: Muestra mensaje Ã©xito âœ…
            else Error en BD
                DB-->>Repo: Exception
                Repo-->>Service: Error
                Service->>Logger: LogError("Error al actualizar", exception)
                Service-->>API: OperationResult (Error)
                API-->>Frontend: HTTP 500
                Frontend->>User: Muestra mensaje error âŒ
            end
        else ValidaciÃ³n Backend Falla
            API->>Logger: LogWarning("ValidaciÃ³n fallida")
            API-->>Frontend: HTTP 400 Bad Request
            Frontend->>User: Muestra errores de validaciÃ³n âš ï¸
        end
    else ValidaciÃ³n Frontend Falla
        Frontend->>User: Muestra errores en formulario âš ï¸
    end
`
</details>``

---

## 5. Arquitectura de Capas (Clean Architecture Simplificada)

``![Diagrama](images/ARQUITECTURA_TECNICA_diag_5.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
graph TB
    subgraph "Capa de PresentaciÃ³n"
        direction LR
        WEB["Web Frontend HTML/JS/jQuery"]
        API["API REST Controllers"]
    end
    
    subgraph "Capa de AplicaciÃ³n"
        direction LR
        SERV["Services"]
        DTO["DTOs"]
        IFACE["Interfaces"]
    end
    
    subgraph "Capa de Dominio"
        direction LR
        ENT["Entities"]
        LOGIC["Business Logic"]
    end
    
    subgraph "Capa de Infraestructura"
        direction LR
        REPO["Repositories"]
        DATA["Data Access ADO.NET"]
        ENC["Encryption"]
        LOG["Logging"]
    end
    
    WEB --> API
    API --> SERV
    SERV --> DTO
    SERV --> IFACE
    SERV --> ENT
    SERV --> REPO
    REPO --> DATA
    REPO --> ENT
    DATA --> LOG
    SERV --> ENC
    
    style WEB fill:#e1f5ff
    style API fill:#fff4e1
    style SERV fill:#f0e1ff
    style DTO fill:#f0e1ff
    style IFACE fill:#f0e1ff
    style ENT fill:#ffe1e1
    style LOGIC fill:#ffe1e1
    style REPO fill:#e1ffe1
    style DATA fill:#e1ffe1
    style ENC fill:#e1ffe1
    style LOG fill:#e1ffe1
`
</details>``

---

## 6. Componentes Principales

### **Frontend (Web)**
- **index.html**: Formulario principal con datos
- **debug_launcher.aspx**: Consola de administraciÃ³n (solo en Debug)
- **app.js**: LÃ³gica JavaScript (AJAX, validaciones, dirty checking, modal de confirmaciÃ³n)
- **styles.css**: Estilos responsive basados en la intranet corporativa

### **API REST**
- **RecordatorioController**: 
  - `GET /api/recordatorio/{idEncriptado}` â†’ Obtener datos del recordatorio
  - `POST /api/recordatorio` â†’ Guardar datos modificados
- **Swagger y utilidades**: La documentaciÃ³n Swagger y el mÃ©todo de cifrado `GetEncrypt` estÃ¡n controlados por `EsDesarrollo` en `Web.config` (activos en pruebas, desactivados en producciÃ³n).

### **Application Layer**
- **RecordatorioService**: Orquestador de la lÃ³gica de negocio
- **RecordatorioMapper**: Centraliza el mapeo DTO â†” Entity
- **RecordatorioValidator**: Centraliza validaciones (longitud, XSS, formatos)
- **EmailTemplateBuilder**: Genera el HTML del correo de confirmaciÃ³n de oferta (tablas inline CSS compatibles con Outlook)
- **RecordatorioEnvioRespDto**: Objeto de transferencia de datos principal
- **IRecordatorioService**: Interfaz del servicio

### **Domain Layer**
- **RecordatorioEnvioResp**: Entidad principal del dominio
- **IRecordatorioRepository**: Interfaz del repositorio
- **IEmailNotificationService**: Interfaz del servicio de notificaciones por email
- Validaciones de negocio

### **Infrastructure Layer**
- **RecordatorioRepository**: Acceso a datos Oracle (SQL centralizado en constantes)
- **OracleConnectionFactory**: GestiÃ³n de conexiones
- **EncryptionService**: AES-256 encryption/decryption
- **EmailNotificationService**: EnvÃ­o SMTP con logo CID embebido y validaciÃ³n de direcciones
- **LogHelper (Custom)**: Logging a archivo y BD

---

## 7. Flujo de EncriptaciÃ³n/DesencriptaciÃ³n

``![Diagrama](images/ARQUITECTURA_TECNICA_diag_6.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
graph LR
    subgraph "GeneraciÃ³n de Enlace"
        ID1["ID Original: 12345"]
        ENC["Encryption Service AES-256"]
        HASH["ID Encriptado: X7k9mP2qR5tY8wZ"]
        URL["URL Completa: https://.../index.html?id=X7k9mP2qR5tY8wZ"]
    end
    
    subgraph "Procesamiento de PeticiÃ³n"
        URL2["URL Recibida"]
        EXTRACT["Extraer parÃ¡metro 'id'"]
        DEC["Decryption Service AES-256"]
        ID2["ID Original: 12345"]
        QUERY["Query a BD: WHERE ID = 12345"]
    end
    
    ID1 --> ENC
    ENC --> HASH
    HASH --> URL
    
    URL --> URL2
    URL2 --> EXTRACT
    EXTRACT --> DEC
    DEC --> ID2
    ID2 --> QUERY
    
    style ENC fill:#ffd700
    style DEC fill:#ffd700
`
</details>``

**ConfiguraciÃ³n de EncriptaciÃ³n:**
- Algoritmo: **AES-256**
- Clave: Almacenada en `Web.config` (encriptada)
- IV (Initialization Vector): Generado aleatoriamente
- Encoding: **Base64URL** (seguro para URLs)

---

## 8. Manejo de Errores y Logging

``![Diagrama](images/ARQUITECTURA_TECNICA_diag_7.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
graph TD
    START["PeticiÃ³n HTTP"]
    TRY{"Try-Catch"}
    VALID{"ValidaciÃ³n"}
    BIZ{"LÃ³gica Negocio"}
    DB{"Acceso BD"}
    
    START --> TRY
    TRY --> VALID
    
    VALID -->|OK| BIZ
    VALID -->|Error| LOG_WARN["Log WARN (####) HTTP 400"]
    
    BIZ -->|OK| DB
    BIZ -->|Error| LOG_ERROR["Log ERROR (####) HTTP 500"]
    
    DB -->|OK| LOG_INFO["Log INFO HTTP 200"]
    DB -->|Error| LOG_ERROR
    
    LOG_WARN --> RESPONSE_ERROR["Response Error"]
    LOG_ERROR --> RESPONSE_ERROR
    LOG_INFO --> RESPONSE_OK["Response OK"]
    
    style LOG_INFO fill:#90EE90
    style LOG_WARN fill:#FFD700
    style LOG_ERROR fill:#FF6B6B
`
</details>``

**Niveles de Log y destinos (v1.9.0):**

| Nivel | TXT (.txt) | BD Oracle | Separadores `####` en TXT |
|---|---|---|---|
| DEBUG | Si `Audit_LogLevel=DEBUG` | No (salvo config) | No |
| INFO | Si `Audit_LogLevel` â‰¤ INFO | Si `Audit_DbLogLevel` â‰¤ INFO | No |
| WARN | Si `Audit_LogLevel` â‰¤ WARN | Si `Audit_DbLogLevel` â‰¤ WARN | **SÃ­** |
| ERROR | Si `Audit_LogLevel` â‰¤ ERROR | Si `Audit_DbLogLevel` â‰¤ ERROR | **SÃ­** |
| FATAL | Si `Audit_LogLevel` â‰¤ FATAL | Si `Audit_DbLogLevel` â‰¤ FATAL | **SÃ­** |

**Campos enriquecidos en TXT (v1.9.0):**  
Cada entrada incluye: `[CorrID]`, `[IP]`, `[Method]`, `[User]`, `[URL]`, `[Agent]` y opcionalmente `[RID]`.  
Las entradas de tipo WARN/ERROR/FATAL aÃ±aden: `MENSAJE`, `EXCEPTION`, `INNER EX`, `SQL` y `STACKTRACE` en bloque delimitado.


---

## 9. ConfiguraciÃ³n de Seguridad

``![Diagrama](images/ARQUITECTURA_TECNICA_diag_8.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
graph TB
    subgraph "Capas de Seguridad"
    ENC["EncriptaciÃ³n de ID AES-256 (ConstantTime)"]
    BLACK["BlackList (Confianza Red Interna)"]
    VALID["ValidaciÃ³n de Datos Data Annotations (Anti-IDOR)"]
    SQL["PrevenciÃ³n SQL Injection ParÃ¡metros ADO.NET"]
    HEADERS["Strict Security Headers (CSP, HSTS, No-CORS)"]
    HTTPS["HTTPS Only ProducciÃ³n"]
end

REQUEST["HTTP Request"] --> ENC
ENC --> BLACK
BLACK --> VALID
VALID --> SQL
SQL --> CORS
CORS --> HTTPS
    
    style ENC fill:#FFD700
    style VALID fill:#90EE90
    style SQL fill:#FF6B6B
    style CORS fill:#87CEEB
    style HTTPS fill:#DDA0DD
`
</details>``

---

## 10. TecnologÃ­as y LibrerÃ­as

| Componente | TecnologÃ­a | VersiÃ³n |
|------------|-----------|---------|
| Framework | .NET Framework | 4.7.2 |
| API | ASP.NET Web API | 5.2.7 |
| ORM | ADO.NET | Nativo |
| Base de Datos | Oracle | 12c+ |
| Cliente Oracle | Oracle.ManagedDataAccess | 19.x |
| Logging | LogHelper (Custom) | Fichero + Oracle |
| DocumentaciÃ³n API | Swashbuckle (Swagger) | 5.6.x |
| Testing | MSTest / NUnit | Ãšltima compatible |
| Frontend | jQuery | 3.6.x |
| EncriptaciÃ³n | System.Security.Cryptography | Nativo |
| ValidaciÃ³n | Data Annotations | Nativo |

---

## 11. âš™ï¸ Configuraciones de AppSettings (Web.config)

A continuaciÃ³n se detallan las claves de configuraciÃ³n principales necesarias en el `Web.config`:

| Clave | Proyecto | DescripciÃ³n | Pruebas | ProducciÃ³n |
| :--- | :--- | :--- | :--- | :--- |
| `EncryptionKey` | API + Web | Llave maestra para cifrado AES-256 (Base64) | Clave de pruebas | **Nueva clave** |
| `HmacKey` | API + Web | Llave para firma de integridad HMAC-SHA256 | Clave de pruebas | **Nueva clave** |
| `ApiKey` | API + Web | Token de comunicaciÃ³n interna Web â†” API | Clave de pruebas | **Nueva clave** |
| `ApiBaseUrl` | Web | URL base de la API REST (usada por el Proxy) | `https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api` | `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api` |
| `EsDesarrollo` | API | Activa/desactiva la documentaciÃ³n Swagger y el mÃ©todo GetEncrypt | `true` | **`false`** |
| `LogRetentionDays` | API + Web | DÃ­as de retenciÃ³n de logs en disco | `30` | `30` |
| `RateLimit_Limit` | API | Umbral de peticiones permitidas por ventana | `500` | `500` |
| `RateLimit_WindowSeconds` | API | DuraciÃ³n (segundos) de la ventana de limitaciÃ³n | `60` | `60` |
| `Audit_LogLevel` | API + Web | Nivel mÃ­nimo para logs de archivo (`.txt`) | `WARN` | `WARN` |
| `Audit_DbLogLevel` | API | Nivel mÃ­nimo para logs en base de datos Oracle | `ERROR` | `ERROR` |
| `Log_EnableDb` | API | Interruptor maestro BD: `true`=escribe \| `false`=solo TXT | `true` | `true` |
| `Security_BlackList` | API + Web | IPs o rangos prohibidos (separados por coma) | `""` | `""` |
| `Security_BlackList_JsonPath` | API | Ruta al archivo JSON de bloqueos masivos | `~/App_Data/blacklist.json` | `~/App_Data/blacklist.json` |

> [!IMPORTANT]
> **Detalle del parÃ¡metro `EsDesarrollo`**: 
> Cuando este parÃ¡metro se establece en `true` en la API, habilita dos herramientas exclusivas para entornos de desarrollo y pruebas:
> 1. **Swagger UI**: Expone una interfaz grÃ¡fica interactiva (`/swagger`) que permite a los desarrolladores explorar, probar y entender rÃ¡pidamente los endpoints de la API.
> 2. **MÃ©todo `GetEncrypt`**: Habilita un endpoint de utilidad en la API que permite generar de forma rÃ¡pida identificadores cifrados en Base64. Esto es estrictamente necesario para poder generar enlaces de prueba o para utilizar herramientas de diagnÃ³stico como la Lanzadera de Control (`debug_launcher.aspx`), pero representa un riesgo de seguridad si se expone en producciÃ³n. Por ello, **debe desactivarse** (`false`) en el entorno productivo.

> [!NOTE]
> A partir de la versiÃ³n 1.8.0, la gestiÃ³n de parÃ¡metros sensibles y de red de producciÃ³n se automatiza utilizando transformaciones XML de despliegue (`Web.Release.config`), aislando las configuraciones locales de desarrollo.

---

## Resumen de Decisiones ArquitectÃ³nicas

> [!IMPORTANT]
> **Principios Clave:**
> 1. **SeparaciÃ³n de Responsabilidades**: Cada capa tiene un propÃ³sito claro
> 2. **Simplicidad**: Arquitectura comprensible para equipos de desarrollo existentes
> 3. **Mantenibilidad**: CÃ³digo limpio y bien organizado
> 4. **Seguridad**: EncriptaciÃ³n, validaciones, prevenciÃ³n de SQL injection
> 5. **Trazabilidad**: Logging completo de operaciones

> [!NOTE]
> **Adaptabilidad:**
> - DTOs, entidades y tablas son fÃ¡cilmente reemplazables
> - Cadena de conexiÃ³n configurable en Web.config
> - Sistema interno puede ser mockeado para pruebas
# ðŸ“˜ Manual TÃ©cnico - Sistema de GestiÃ³n de Recordatorios de EnvÃ­o

**Cliente:** TÃœV SÃœD  
**VersiÃ³n:** 2.0.0 (Notificaciones Email + EstabilizaciÃ³n)  
**TecnologÃ­a:** .NET Framework 4.7.2 + Web API 2 + Oracle + jQuery

---

## ðŸ—ï¸ 1. Arquitectura del Sistema (Onion Architecture)

El proyecto sigue una **Arquitectura Cebolla (Onion Architecture)**, diseÃ±ada para desacoplar la lÃ³gica de negocio de las preocupaciones de infraestructura. Este enfoque garantiza que el nÃºcleo de la aplicaciÃ³n sea independiente de la base de datos, APIs externas o interfaces de usuario.

### Diagrama de Dependencias (Capa a Capa)

``![Diagrama](images/MANUAL_TECNICO_diag_1.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
graph BT
    subgraph Capa_Externa ["Capa Externa (PresentaciÃ³n / Entrada)"]
        direction BT
        Web["RecordatorioEnvio.Web (Proxy)"]
        API["RecordatorioEnvio.API (REST)"]
        
        subgraph Infraestructura ["Infraestructura / Servicios"]
            direction BT
            Infra["LogHelper, Encryption, OracleRepo"]
            
            subgraph Aplicacion ["AplicaciÃ³n / Negocio"]
                direction BT
                App["RecordatorioService, DTOs"]
                
                subgraph Dominio ["NÃºcleo (Domain)"]
                    direction BT
                    Core["Entidades e Interfaces"]
                end
            end
        end
    end

    Web -.-> App
    API -.-> App
    App -.-> Core
    Infra -- Implementa --> Core
`
</details>``

### Componentes de la SoluciÃ³n
1.  **RecordatorioEnvio.Domain**: Contiene las entidades e interfaces de repositorio (El corazÃ³n del sistema).
2.  **RecordatorioEnvio.Application**: Contiene los DTOs, el `RecordatorioService` (orquestador), `RecordatorioMapper` (mapeo centralizado) y `RecordatorioValidator` (validaciÃ³n centralizada). Gestiona la lÃ³gica de negocio siguiendo el principio de Responsabilidad Ãšnica (SRP).
3.  **RecordatorioEnvio.Infrastructure**: Implementa el acceso a datos (Oracle), el cifrado y el registro de eventos (`LogHelper`).
4.  **RecordatorioEnvio.API**: Expone los servicios vÃ­a REST. Protegido por API Key.
5.  **RecordatorioEnvio.Web**: AplicaciÃ³n MVC que sirve el frontend y actÃºa como Proxy seguro.

---

## 2. ðŸ›¡ï¸ Seguridad

### 2.1 Cifrado de Datos
*   **Algoritmo**: AES-256 CBC + PKCS7.
*   **ValidaciÃ³n**: HMAC-SHA256 para integridad de tokens.
*   **Payload Cifrado**: Todo el trÃ¡fico POST entre Web y API viaja cifrado dentro de una estructura `SecurePayload`.

### 2.2 Control de Acceso (BlackList Híbrida)
*   **Motor**: `SecurityService.cs`.
*   **Fuentes de Datos**: 
    1.  **Web.config**: Clave `Security_BlackList` para reglas rápidas y patrones con comodín (`10.45.*`).
    2.  **JSON Externo**: Archivo `~/App_Data/blacklist.json` para listados extensos de IPs exactas.
*   **Rendimiento**: La carga del JSON está optimizada con un sistema de **Caching en Memoria** que detecta automáticamente si el archivo ha cambiado sin necesidad de reiniciar la aplicación.
*   **Detección**: Bloqueo instantáneo con respuesta `403 Forbidden` y registro en el log de seguridad.

### 2.3 Hardening y Prevención (Post-Auditoría)
*   **Aislamiento de Entorno (C-02, M-05)**: Durante la publicación (`Web.Release.config`), se inyecta la variable `EsDesarrollo=false` y se elimina el atributo `debug="true"`.
*   **Bloqueo de Endpoints de Soporte (C-03, M-02)**: Los endpoints de lectura de logs y depuración (`/Proxy/GetLogs`, `/Proxy/GetWebLogContent`, `/debug/check-ip`, etc.) están restringidos. Si `EsDesarrollo` no es `true`, devuelven `403 Forbidden`.
*   **Anti Path Traversal (C-04)**: El visor de logs utiliza `System.IO.Path.GetFileName()` para neutralizar intentos de escape de directorio (ej. `..\..\Windows\win.ini`).
*   **Prevención XSS (A-03)**: Las plantillas HTML generadas (como los correos electrónicos) sanitizan las entradas dinámicas del usuario mediante `System.Net.WebUtility.HtmlEncode()`.

---

## 3. 🔄 Flujos de Datos

### Diagrama de Secuencia: Guardado Seguro

``![Diagrama](images/MANUAL_TECNICO_diag_2.svg)

<details>
<summary>Ver cÃ³digo fuente del diagrama</summary>

`	ext
sequenceDiagram
    participant JS as Frontend (jQuery)
    participant PR as Proxy (Web)
    participant AP as API (Backend)
    participant BD as Oracle (GESAP)

    JS->>PR: POST /UpdateRecordatorio (JSON Plano)
    Note over PR: Generar CorrelationID
    Note over PR: Cifrado AES-256 (Payload)
    PR->>AP: POST /api/recordatorio (X-Correlation-ID)
    Note over AP: Auth + AuditorÃ­a forense
    AP->>BD: Update + Log AuditorÃ­a
    BD-->>AP: Commit OK
    AP-->>PR: 200 OK
    PR-->>JS: Success Message
`
</details>``

### 3.2 GestiÃ³n AutomÃ¡tica de Estados
A partir de la versiÃ³n **1.7.4**, el sistema automatiza la transiciÃ³n de estados para garantizar que cualquier interacciÃ³n de guardado sea supervisada:
*   **Regla de Negocio**: Al ejecutar el mÃ©todo `SaveAll`, el repositorio fuerza el campo `ID_ESTADO_REC_ENVIO_RESPUESTA = 2` (PENDIENTE REVISIÃ“N).
*   **Impacto**: Se ignora cualquier valor de estado enviado desde el frontend, centralizando la lÃ³gica en la capa de datos para evitar manipulaciones accidentales.
*   **RefactorizaciÃ³n v1.7.5**: Se ha migrado la tabla de notas de `RECORDATORIO_ENVIO_EQ_REV` a `RECORDATORIO_ENVIO_RESP_NOTA` (y la columna ID a `ID_RECORDATORIO_ENV_RESP_NOTA`) para mejorar la consistencia semÃ¡ntica, con mapeo automÃ¡tico de campos descriptivos. AdemÃ¡s, se han eliminado los campos obsoletos (`DESCRIPCION_TIPO_EQUIPO`, `CONDICIONES_ECO_CORRECCION`, `IDENTIFICADOR_REC_ENVIO`) que ya no existen en la tabla principal.

---

## 3.3 Sistema de Notificaciones por Correo ElectrÃ³nico (v2.0.0)

Tras el guardado exitoso de una oferta aceptada, la API envÃ­a automÃ¡ticamente un correo de confirmaciÃ³n al titular. Este flujo se compone de tres elementos:

### Componentes

| Componente | Capa | Responsabilidad |
|---|---|---|
| `EmailTemplateBuilder.cs` | Application | Genera el HTML del correo con todos los datos de la oferta, usando tablas inline CSS compatibles con Outlook |
| `EmailNotificationService.cs` | Infrastructure | EnvÃ­o SMTP con validaciÃ³n de direcciones, soporte multi-destinatario y logo CID embebido |
| `IEmailNotificationService.cs` | Domain | Contrato/interfaz del servicio de notificaciones |

### CaracterÃ­sticas tÃ©cnicas
*   **Logo embebido (CID)**: La imagen del octogono de TÃœV SÃœD se codifica en Base64 y se adjunta como `LinkedResource` con `ContentId = "logotuv"`. Esto evita que Outlook bloquee la imagen por polÃ­ticas de seguridad.
*   **ValidaciÃ³n de destinatarios**: Antes de enviar, se valida cada direcciÃ³n de correo con `System.Net.Mail.MailAddress`. Las direcciones invÃ¡lidas se ignoran y se registran como WARNING.
*   **Multi-destinatario**: Soporta mÃºltiples correos separados por `;` o `,`. Si estÃ¡ configurado el campo `EmailEnvioGir` en `SYS_CONFIGURACION`, se aÃ±ade automÃ¡ticamente como destinatario adicional.
*   **ConfiguraciÃ³n SMTP**: Se obtiene dinÃ¡micamente de la tabla `GESAP.SYS_CONFIGURACION` de Oracle (servidor, puerto, usuario, contraseÃ±a, SSL). No requiere parÃ¡metros en `Web.config`.
*   **Tolerancia a fallos**: Si el envÃ­o del correo falla, los datos ya guardados en BD no se revierten. El sistema devuelve un mensaje de advertencia al usuario indicando que el guardado fue exitoso pero el correo no pudo enviarse.

---

## 4. ðŸ“ Sistema de AuditorÃ­a y Logs

El sistema implementa una arquitectura de trazas dual con paridad de informaciÃ³n entre ficheros TXT y base de datos Oracle:

### 4.1 Ficheros TXT (ambas capas)

*   **Rutas**: `~/App_Data/Logs/LogWeb_yyyyMMdd.txt` (Web) y `~/App_Data/Logs/LogApi_yyyyMMdd.txt` (API).
*   **Enriquecimiento (v1.9.0)**: Cada entrada incluye automÃ¡ticamente `[CorrID]`, `[IP]`, `[Method]`, `[User]`, `[URL]` y `[Agent]`.
*   **Separadores visuales (v1.9.0)**: Los niveles `WARN`, `ERROR` y `FATAL` generan un bloque delimitado con `##########` para distinguirlos a simple vista en el fichero:

```
################################################## ERROR ##################################################
10:30:15 [API][ERROR] [CorrID: A1B2C3D4] [IP: 1.2.3.4] [Method: GET] [User: Anonymous] [URL: /api/recordatorio/xyz] [Agent: Mozilla/5.0...]
MENSAJE  : ERROR GET ID xyz: ORA-00942: table or view does not exist
EXCEPTION: ORA-00942: table or view does not exist
SQL      : SELECT * FROM GESAP.RECORDATORIO_ENVIO WHERE ID = :v_id
STACKTRACE:
   at RecordatorioEnvio.Infrastructure.Repositories.RecordatorioRepository.GetById(...)
##################################################
```

*   **RetenciÃ³n**: Configurable con `LogRetentionDays`. Limpieza automÃ¡tica al escribir cada entrada.

### 4.2 AuditorÃ­a en Base de Datos (Oracle)

*   **Tabla**: `GESAP.RECORDATORIO_ENVIO_LOGS`.
*   **Solo capa API** (la Web nunca escribe en BD directamente).
*   **Campos avanzados**: `LOG_DATE` (SYSDATE), `CORRELATION_ID`, `HTTP_METHOD`, `USER_AGENT`, `RECORD_ID`, `EXCEPTION_MSG` (CLOB), `STACKTRACE` (CLOB), `SQL_QUERY` (CLOB).
*   **Robustez**: Si la BD falla, el error se captura y se escribe en el TXT. La aplicaciÃ³n **nunca cae** por un fallo de logging.

### 4.3 Niveles de Log dinÃ¡micos

JerarquÃ­a: `DEBUG < INFO < WARN < ERROR < FATAL < NONE`.

| ParÃ¡metro | Proyecto | DescripciÃ³n | Valor recomendado |
|---|---|---|---|
| `Audit_LogLevel` | API + Web | Nivel mÃ­nimo para ficheros TXT | `WARN` |
| `Audit_DbLogLevel` | API | Nivel mÃ­nimo para BD Oracle | `ERROR` |
| `Log_EnableDb` | API | Interruptor maestro de BD (`true`/`false`) | `true` |

**Comportamiento de `Log_EnableDb` (v1.9.0):**

| `Log_EnableDb` | `Audit_DbLogLevel` | Resultado |
|---|---|---|
| `true` | `ERROR` | Solo ERROR + FATAL en BD (**producciÃ³n**) |
| `true` | `WARN` | WARN + ERROR + FATAL en BD (debug) |
| `false` | cualquiera | **Nunca escribe en BD** (solo TXT) |
| *(no existe)* | cualquiera | Retrocompatible: funciona como `true` |

### 4.4 Trazabilidad cruzada

El `CorrelationID` (8 caracteres hexÃ¡decimal generado por el Proxy) se propaga vÃ­a cabecera `X-Correlation-ID` a la API. Permite rastrear una transacciÃ³n completa buscando el mismo ID en el `LogWeb_*.txt`, `LogApi_*.txt` y la tabla `RECORDATORIO_ENVIO_LOGS`.


---

## 5. ðŸ› ï¸ Lanzadera Pro: Consola de AuditorÃ­a

*   **Visor de Ficheros**: Lectura completa de archivos `.txt` locales.
*   **Visor de AuditorÃ­a (DB)**: Grid de alto rendimiento con los Ãºltimos 500 registros de auditorÃ­a de Oracle.
*   **DiagnÃ³stico de Seguridad (SecciÃ³n 5)**: Herramienta de "Doble Chequeo" que valida una IP contra las listas negras de la Web y la API simultÃ¡neamente.

---

## 6. âš™ï¸ Configuraciones de AppSettings (Web.config)

A continuaciÃ³n se detallan las claves de configuraciÃ³n principales necesarias en el `Web.config` para la API y el Proxy Web:

| Clave | Proyecto | DescripciÃ³n | Pruebas | ProducciÃ³n |
| :--- | :--- | :--- | :--- | :--- |
| `EncryptionKey` | API + Web | Llave maestra para cifrado AES-256 (Base64) | Clave de pruebas | **Nueva clave** |
| `HmacKey` | API + Web | Llave para firma de integridad HMAC-SHA256 | Clave de pruebas | **Nueva clave** |
| `ApiKey` | API + Web | Token de seguridad corporativo Web â†” API | Clave de pruebas | **Nueva clave** |
| `ApiBaseUrl` | Web | URL base de la API REST (Proxy y debuggers) | `https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api` | `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api` |
| `EsDesarrollo` | API | Activa/desactiva la documentaciÃ³n Swagger y el mÃ©todo GetEncrypt | `true` | **`false`** |
| `LogRetentionDays` | API + Web | DÃ­as de retenciÃ³n de logs en disco | `30` | `30` |
| `RateLimit_Limit` | API | Umbral de peticiones permitidas por ventana | `500` | `500` |
| `RateLimit_WindowSeconds` | API | DuraciÃ³n (segundos) de la ventana de limitaciÃ³n | `60` | `60` |
| `Audit_LogLevel` | API + Web | Nivel mÃ­nimo para logs de archivo `.txt` | `WARN` | `WARN` |
| `Audit_DbLogLevel` | API | Nivel mÃ­nimo para logs en base de datos Oracle | `ERROR` | `ERROR` |
| `Log_EnableDb` | API | Interruptor maestro BD: `true`=escribe \| `false`=solo TXT | `true` | `true` |
| `Security_BlackList` | API + Web | IPs o rangos prohibidos (ej: `1.2.3.4, 10.*`) | `""` | `""` |
| `Security_BlackList_JsonPath` | API | Ruta al archivo JSON de bloqueos masivos | `~/App_Data/blacklist.json` | `~/App_Data/blacklist.json` |

> [!IMPORTANT]
> **Detalle del parÃ¡metro `EsDesarrollo`**: 
> Cuando este parÃ¡metro se establece en `true` en la API, habilita dos herramientas exclusivas para entornos de desarrollo y pruebas:
> 1. **Swagger UI**: Expone una interfaz grÃ¡fica interactiva (`/swagger`) que permite a los desarrolladores explorar, probar y entender rÃ¡pidamente los endpoints de la API.
> 2. **MÃ©todo `GetEncrypt`**: Habilita un endpoint de utilidad en la API que permite generar de forma rÃ¡pida identificadores cifrados en Base64. Esto es estrictamente necesario para poder generar enlaces de prueba o para utilizar herramientas de diagnÃ³stico como la Lanzadera de Control (`debug_launcher.aspx`), pero representa un riesgo de seguridad si se expone en producciÃ³n. Por ello, **debe desactivarse** (`false`) en el entorno productivo.

> [!NOTE]
> **GestiÃ³n de Entornos (TransformaciÃ³n XML):**
> A partir de la versiÃ³n 1.8.0, se implementa el sistema de transformaciones XML (`Web.Release.config` / `Web.Debug.config`). Esto permite que los valores sensibles y las rutas de producciÃ³n (como `ApiBaseUrl`, `ApiKey` y cadenas de conexiÃ³n a base de datos Oracle) se inyecten de forma automÃ¡tica al compilar y publicar en modo **Release**, garantizando que el entorno local (`Web.config` por defecto) permanezca aislado y seguro para el desarrollo cotidiano.

> [!TIP]
> **ProtecciÃ³n en Cascada (SincronizaciÃ³n de BlackList):**
> El sistema permite bloquear IPs en dos niveles para una seguridad total:
> 1.  **Nivel Web**: Bloquea el acceso al portal (interfaz de usuario). Se configura en el `Web.config` del proyecto Web.
> 2.  **Nivel API**: Bloquea el acceso profundo a los datos. Se configura en el `Web.config` de la API.
> 3.  **Transparencia (Multi-Capa)**: Gracias al uso de `X-Forwarded-For` y la reciente integraciÃ³n con cabeceras de **Imperva/Incapsula** (`Incap-Client-IP`), la API es capaz de reconocer la IP real del usuario aunque la peticiÃ³n pase por mÃºltiples proxies o WAFs, permitiendo que las reglas de bloqueo se apliquen correctamente en ambos sitios.

---
Â© 2026 TÃœV SÃœD - DocumentaciÃ³n TÃ©cnica Confidencial.
# ðŸ‘¤ Manual Funcional - Sistema de Recordatorio de EnvÃ­o

**Cliente:** TÃœV SÃœD  
**PropÃ³sito:** GuÃ­a de uso para usuarios de negocio, soporte tÃ©cnico y administradores.

---

## 1. ðŸ“‹ IntroducciÃ³n al Sistema

Este sistema automatiza la recogida de datos y respuestas de clientes tras un recordatorio de envÃ­o. Consta de dos partes principales:
1.  **Formulario del Cliente**: Lo que el cliente rellena.
2.  **Lanzadera de Control (Consola Pro)**: Herramienta interna para gestiÃ³n y auditorÃ­a.

---

## 2. ðŸš€ GuÃ­a de la Lanzadera Pro

### 2.1 Herramientas de Mantenimiento y Seguridad
La "Lanzadera Pro" incluye ahora herramientas avanzadas para el equipo de soporte:
*   **InspecciÃ³n de Datos**: Para ver y corregir estados de Oracle si hubiera un error de sincronizaciÃ³n.
*   **AuditorÃ­a de Logs**: VisualizaciÃ³n directa de errores sin entrar al servidor.
*   **DiagnÃ³stico de Seguridad**: Permite comprobar si una direcciÃ³n IP estÃ¡ bloqueada. Es vital usar esto antes de aÃ±adir reglas de bloqueo generales (con asteriscos) para evitar bloqueos accidentales a usuarios legÃ­timos.

### ðŸŸ¢ Panel 1: Generador de Enlaces
AquÃ­ es donde comienza el trabajo.
1.  **Token / ID**: Inserte el cÃ³digo del registro que desea gestionar.
2.  **BotÃ³n Lanzar**: Genera automÃ¡ticamente la URL segura que se debe enviar al cliente.
3.  **BotÃ³n Abrir**: Abre directamente la vista del cliente en una nueva pestaÃ±a.

### ðŸ” Panel 2: Herramienta de Cifrado
El sistema no utiliza nÃºmeros de ID simples para evitar que alguien adivine otros registros.
*   Introduzca un ID numÃ©rico (ej: `12345`).
*   Pulse **Cifrar**. ObtendrÃ¡ un token largo e indescifrable que es el que se usa en las URLs oficiales.

### ðŸ“ Panel 3: InspecciÃ³n y EdiciÃ³n de Registros
Permite corregir datos sin entrar directamente en la base de datos.
*   **Cargar**: Muestra todos los datos del cliente en un editor (formato JSON).
*   **Guardar**: Si realiza un cambio en el texto, este botÃ³n lo persiste en Oracle de forma segura.

### ðŸ“Š Panel 4: CatÃ¡logo de Estados
Muestra la lista oficial de estados permitidos por el sistema (ej: "PENDIENTE", "TRAMITADO", etc.). Es puramente informativo para consulta rÃ¡pida.

### ðŸ” Panel 5: AuditorÃ­a de Logs (El "SemÃ¡foro")
Control total de lo que ocurre en el servidor:
*   **Logs Oracle (API)**: Muestra errores de base de datos. Si un guardado falla, aquÃ­ verÃ¡ por quÃ©.
*   **Logs Web (Servidor)**: Muestra la actividad del portal web.
*   **Botones de Recarga**: Iconos (`ðŸ”„`) para refrescar la informaciÃ³n al instante.

---

## 3. ðŸ”„ Flujo de Respuesta del Cliente

1.  **RecepciÃ³n**: El cliente recibe un correo con un enlace (URL con ID cifrado).
2.  **ValidaciÃ³n**: Al abrirlo, el sistema carga sus datos actuales de **TÃœV SÃœD**.
3.  **ModificaciÃ³n**: El cliente puede actualizar:
    *   Datos de Emplazamiento.
    *   Datos de FacturaciÃ³n (IBAN, Email).
    *   Notas adicionales por cada equipo.
    *   **SelecciÃ³n Inteligente**: El selector de cabecera permite marcar todos los equipos a la vez, y se actualiza automÃ¡ticamente si se marcan las filas una a una.
4.  **Guardado Seguro y DetecciÃ³n de Cambios**: Al pulsar "Guardar Datos", el sistema realiza una validaciÃ³n inteligente:
    - **ValidaciÃ³n de Formatos**: Comprueba que los emails y el IBAN sean correctos.
    - **Ventana de ConfirmaciÃ³n**: Si hay cambios, se muestra una ventana detallando quÃ© se ha modificado. Cada campo incluye la secciÃ³n a la que pertenece para evitar ambigÃ¼edades (Ej: "NIF (Datos del Titular)", "NIF (Datos del Representante/Gestor)").
    - **Aviso de VariaciÃ³n de Precio**: Si se han modificado equipos o notas, el modal muestra un aviso destacado: *"El precio total inspecciÃ³n puede variar. PÃ³ngase en contacto con el personal de TÃœV SÃœD."*
    - **Sin Cambios**: Si no se ha modificado nada, el sistema preguntarÃ¡ "Â¿Desea confirmar la oferta?" para evitar clics accidentales.
    - **ConfirmaciÃ³n Final**: Solo tras pulsar "Aceptar" en la ventana de cambios se procederÃ¡ con el guardado real en la base de datos.
5.  **NotificaciÃ³n por Correo ElectrÃ³nico**: Tras el guardado exitoso, el sistema envÃ­a automÃ¡ticamente un **correo electrÃ³nico de confirmaciÃ³n** a la direcciÃ³n del titular (y opcionalmente al GIR si estÃ¡ configurado). El correo incluye:
    *   Resumen completo de la oferta aceptada (datos generales, equipos, precios, facturaciÃ³n).
    *   Logo oficial de TÃœV SÃœD embebido (visible sin descargar imÃ¡genes en Outlook).
    *   Notas adicionales si las hubiera.
    *   ValidaciÃ³n automÃ¡tica de las direcciones de correo antes del envÃ­o.

---

## 4. ðŸ’¡ Comportamiento de los Mensajes
Para su tranquilidad, el sistema le avisarÃ¡ de todo lo que ocurra:
*   **Confirmaciones Verdes**: Aparecen en un modal central con animaciÃ³n. El mensaje indica si el correo se ha enviado correctamente (ej: *"Datos guardados correctamente y notificaciÃ³n enviada a correo1@test.com y correo2@test.com"*). Se cierra automÃ¡ticamente a los 5 segundos.
*   **Confirmaciones con Advertencia**: Si los datos se guardaron pero el correo no pudo enviarse (por error SMTP o direcciÃ³n invÃ¡lida), el modal verde incluirÃ¡ un aviso: *"Datos guardados correctamente (Advertencia: No se pudo enviar el correo de notificaciÃ³n)"*.
*   **Avisos Rojos (Errores)**: No se quitan solos. Si el error es en un dato (como el IBAN), la marca roja se quitarÃ¡ en cuanto usted empiece a escribir en ese cuadro para corregirlo. Si el error es de conexiÃ³n, deberÃ¡ intentar guardar de nuevo o pulsar F5.

## ðŸ†˜ ResoluciÃ³n de Problemas Comunes

| SÃ­ntoma | Causa Probable | SoluciÃ³n |
| :--- | :--- | :--- |
| **"Error al conectar con la API"** | El servicio backend no estÃ¡ arrancado. | Verifique que ambos proyectos (Web y API) estÃ©n en ejecuciÃ³n en Visual Studio. |
| **"ID InvÃ¡lido"** | El token de la URL ha sido manipulado o estÃ¡ incompleto. | Genere un nuevo enlace desde el Panel 1 de la Lanzadera. |
| **"Formato de IBAN incorrecto"** | Se han introducido espacios o el cÃ³digo de paÃ­s no es espaÃ±ol (ES). | Introduzca los 24 caracteres seguidos, empezando por ES. |
| **No se guardan los cambios** | Posible pÃ©rdida de conexiÃ³n con Oracle. | Revise el Panel 5 (Logs) para ver el mensaje de error tÃ©cnico. |
| **"Too Many Requests" (Error 429)** | Se ha superado el lÃ­mite de peticiones de seguridad para esa IP. | Espere 60 segundos antes de volver a intentar la operaciÃ³n. |

---
Â© 2026 TÃœV SÃœD - Manual de Usuario Final.
# ðŸš€ GuÃ­a de Despliegue en IIS (Web y API)

> **Documento Ãºnico de referencia para todos los despliegues.**
> Basado en el despliegue real del servidor de pruebas (`gestion3-desa.atisae.com`). Al final se detalla quÃ© cambia para **ProducciÃ³n**.

---

## Fase 0: Requisitos TÃ©cnicos del Servidor

Antes de empezar, asegÃºrate de que los servidores destino cumplen estos requisitos:

> [!IMPORTANT]
> En producciÃ³n, **Web y API deben estar en servidores separados**. Este aislamiento es obligatorio por seguridad: si la Web sufre un ataque, la API y Oracle permanecen inaccesibles directamente desde el exterior.

### ðŸŒ Servidor Web (Frontend + Proxy)
*   **SO**: Windows Server 2012 R2 o superior.
*   **Servidor Web**: IIS 8.5+.
*   **Runtime**: .NET Framework 4.7.2 Runtime.
*   **MÃ³dulos IIS**: ASP.NET 4.7 en modo **Integrado**. El mÃ³dulo `ExtensionlessUrlHandler` se configura automÃ¡ticamente desde el `Web.config`.
*   **Contenido en producciÃ³n**: Solo `index.html`, `favicon.ico`, `images/`, `css/`, `js/`, `Global.asax`, `Web.config` y carpeta `bin/`. **Sin archivos `.aspx`.**

### âš™ï¸ Servidor API (Backend + Oracle)
*   **SO**: Windows Server 2012 R2 o superior.
*   **Servidor Web**: IIS 8.5+.
*   **Runtime**: .NET Framework 4.7.2 Runtime.
*   **Acceso a Datos**: No requiere cliente Oracle pesado. El paquete `Oracle.ManagedDataAccess` estÃ¡ incluido en la carpeta `bin/`. El servidor debe tener visibilidad de red al puerto **1521** de Oracle.

---


## ðŸ—ºï¸ Tabla de Entornos

| ParÃ¡metro | Servidor de Pruebas | Servidor de ProducciÃ³n (API) | Servidor de ProducciÃ³n (Web) |
|---|---|---|---|
| **Servidor IIS** | `SESMADE55003` | Servidor DMZ (consultar a Sistemas) | Servidor portal (consultar a Sistemas) |
| **Sitio IIS** | `Gestion3` | Sitio propio en DMZ | Sitio propio en portal |
| **Pool de aplicaciones** | `Gestion2` (.NET v4.0, Integrado) | Pool dedicado (.NET v4.0, Integrado) | Pool dedicado (.NET v4.0, Integrado) |
| **Ruta fÃ­sica** | API: `E:\App\gestion3\API_REC_ENV_GESAP` | `E:\app\gestion-dmz\API_REC_ENV_GESAP` | Consultar a Sistemas |
| | Web: `E:\App\gestion3\WEB_REC_ENV_GESAP` | â€” | â€” |
| **URL pÃºblica** | API: `https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api` | `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api` | â€” |
| | Web: `https://gestion3-desa.atisae.com/WEB_REC_ENV_GESAP/` | â€” | `https://portal-contrataciones.tuv-sud.es/WEB_REC_ENV_GESAP/` |
| **Swagger y GetEncrypt** | âœ… Activo (`EsDesarrollo=true`) | âŒ Desactivado (`EsDesarrollo=false`) | N/A |
| **debug_launcher.aspx** | âœ… Incluido | N/A | âŒ **Eliminado o renombrado** |
| **SecurityTests.aspx** | âœ… Incluido | N/A | âŒ **Eliminado o renombrado** |
| **NotificaciÃ³n Email** | âœ… EnvÃ­a correo tras aceptar oferta | âœ… Requiere visibilidad SMTP | N/A |

---

## Fase 1: CompilaciÃ³n y GeneraciÃ³n de Entregables (Visual Studio)

### 1.1. Publicar el Backend (API)
1. Abre la soluciÃ³n `RecordatorioEnvioGesap.sln` en Visual Studio.
2. En la barra superior, cambia el modo de compilaciÃ³n de **Debug** a **Release**.
3. Haz clic derecho en el proyecto **RecordatorioEnvio.API** â†’ **Publicar** (Publish).
4. Elige como destino **Carpeta** (Folder) y una ruta local accesible, por ejemplo: `C:\Despliegues\API_GESAP`
5. Pulsa **Publicar**. Visual Studio generarÃ¡ los binarios (`.dll`) en esa ruta.

### 1.2. Publicar el Frontend (Web)
1. Haz clic derecho en el proyecto **RecordatorioEnvio.Web** â†’ **Publicar**.
2. Elige **Carpeta** y como ruta: `C:\Despliegues\WEB_GESAP`
3. Pulsa **Publicar**.

> [!TIP]
> Una vez terminada esta fase puedes cerrar Visual Studio. Solo trabajaremos con las dos carpetas generadas.

---

## Fase 2: ConfiguraciÃ³n de Web.config (Valores Reales del Servidor de Pruebas)

> [!WARNING]
> El `Web.config` que genera la publicaciÃ³n contiene los valores del entorno local de desarrollo. **Deben sustituirse** por los valores reales del servidor antes de copiar los archivos.

### 2.1. Web.config de la API (`C:\Despliegues\API_GESAP\Web.config`)

Este es el estado real con el que estÃ¡ desplegado en el servidor de pruebas:

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
  <!-- AuditorÃ­a: Niveles (DEBUG, INFO, WARN, ERROR, FATAL, NONE)              -->
  <!-- Audit_LogLevel   â†’ nivel mÃ­nimo para fichero TXT                        -->
  <!-- Audit_DbLogLevel â†’ nivel mÃ­nimo para BD Oracle                          -->
  <!-- Log_EnableDb     â†’ interruptor maestro BD: true=escribe | false=solo TXT -->
  <add key="Audit_LogLevel"    value="WARN"  />
  <add key="Audit_DbLogLevel"  value="ERROR" />
  <add key="Log_EnableDb"      value="true"  />
  <add key="Security_BlackList" value="" />
  <add key="Security_BlackList_JsonPath" value="~/App_Data/blacklist.json" />
  <!-- PRUEBAS: Swagger y debug activo. En producciÃ³n cambiar a false -->
  <add key="EsDesarrollo" value="true" />
</appSettings>
```

> [!IMPORTANT]
> **Aviso de Seguridad (`EsDesarrollo`)**: 
> Durante el despliegue es vital verificar este parÃ¡metro. Cuando se establece en `true` (entorno de pruebas), habilita:
> 1. **Swagger UI**: Permite explorar y probar los endpoints visualmente.
> 2. **MÃ©todo `GetEncrypt`**: Endpoint de utilidad para generar identificadores cifrados en Base64 (usado por la Lanzadera de Control).
> 
> En producciÃ³n, este valor **DEBE ser `false`** para evitar exponer la documentaciÃ³n interna y, sobre todo, para prevenir que un tercero pueda generar identificadores cifrados arbitrarios.

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
  <!-- AuditorÃ­a: Niveles (DEBUG, INFO, WARN, ERROR, FATAL, NONE)         -->
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
> `EncryptionKey`, `HmacKey` y `ApiKey` deben ser **idÃ©nticas** en la API y en la Web. Si no coinciden, el sistema rechazarÃ¡ todas las peticiones.

---

## Fase 3: Copiar archivos al Servidor IIS

### 3.1. Servidor de Pruebas (un solo servidor)

1. ConÃ©ctate al servidor **`SESMADE55003`** (acceso RDP o red compartida).
2. Copia la carpeta publicada de la API a:
   ```
   E:\App\gestion3\API_REC_ENV_GESAP\
   ```
3. Copia la carpeta publicada de la Web a:
   ```
   E:\App\gestion3\WEB_REC_ENV_GESAP\
   ```
4. Si existÃ­an archivos anteriores, sobrescribe todo. Si es un despliegue limpio desde cero, borra primero la carpeta destino.

### 3.2. ProducciÃ³n (servidores separados)

> [!CAUTION]
> En producciÃ³n, **la API y la Web estÃ¡n en servidores fÃ­sicos distintos**. Debes conectarte a cada uno por separado.

**Servidor de la API (DMZ):**
1. ConÃ©ctate al servidor DMZ donde estÃ¡ publicada `https://ws-dmz.atisae.com`.
2. Copia la carpeta publicada de la API a:
   ```
   E:\app\gestion-dmz\API_REC_ENV_GESAP\
   ```

**Servidor de la Web (Portal):**
1. ConÃ©ctate al servidor del portal donde estÃ¡ publicada `https://portal-contrataciones.tuv-sud.es`.
2. Copia la carpeta publicada de la Web a la ruta fÃ­sica configurada por Sistemas (consultar al equipo de infraestructura).

> [!NOTE]
> **Archivo `images/`:** La carpeta `images/` con el logo SVG de TÃœV SÃœD no se copia automÃ¡ticamente por Visual Studio si no estÃ¡ incluida en el proyecto. CÃ³piala manualmente junto al `index.html`.

---

## Fase 4: Crear las Aplicaciones en IIS (Solo en el primer despliegue)

Abre el **Administrador de IIS** en el servidor `SESMADE55003`.

```
SESMADE55003
  â””â”€ Sitios
       â””â”€ Gestion3          â† Sitio padre
            â”œâ”€ API_REC_ENV_GESAP   â† AplicaciÃ³n (icono globo ðŸŒ)
            â””â”€ WEB_REC_ENV_GESAP   â† AplicaciÃ³n (icono globo ðŸŒ)
```

**Para la API** (si no existe ya):
1. Clic derecho sobre **Gestion3** â†’ **Agregar aplicaciÃ³n...**
2. Alias: `API_REC_ENV_GESAP`
3. Ruta fÃ­sica: `E:\App\gestion3\API_REC_ENV_GESAP`
4. Pool: `Gestion2` (**.NET v4.0, Modo Integrado**)
5. Aceptar.

**Para la Web** (si no existe ya):
1. Clic derecho sobre **Gestion3** â†’ **Agregar aplicaciÃ³n...**
2. Alias: `WEB_REC_ENV_GESAP`
3. Ruta fÃ­sica: `E:\App\gestion3\WEB_REC_ENV_GESAP`
4. Pool: `Gestion2`
5. Aceptar.

> [!IMPORTANT]
> Verifica que el icono de ambas aplicaciones es un **globo terrÃ¡queo** (ðŸŒ). Si es solo una carpeta amarilla, haz clic derecho â†’ **Convertir en aplicaciÃ³n**. Sin esto el enrutamiento MVC no funciona y verÃ¡s errores 404 en las rutas `/Proxy/...`.

---

## Fase 5: Permisos de Escritura para Logs

La aplicaciÃ³n necesita permisos de escritura para generar los archivos de auditorÃ­a en `App_Data/Logs`.

**Para la API:**
1. Ve a `E:\App\gestion3\API_REC_ENV_GESAP\App_Data`
2. Clic derecho â†’ **Propiedades** â†’ **Seguridad** â†’ **Editar** â†’ **Agregar**
3. Escribe `IIS_IUSRS`, pulsa "Comprobar nombres" y acepta.
4. Marca el permiso **Modificar** y acepta.

**Para la Web:** Repite los mismos pasos sobre `E:\App\gestion3\WEB_REC_ENV_GESAP\App_Data`.

> [!TIP]
> Si la carpeta `App_Data\Logs` no existe, no hace falta crearla manualmente. La propia aplicaciÃ³n la crearÃ¡ al arrancar si tiene permiso de Modificar sobre `App_Data`.

---

## Fase 6: Cifrado de la Cadena de ConexiÃ³n (aspnet_regiis)

> [!CAUTION]
> **Este comando se ejecuta SIEMPRE en el servidor de destino, nunca en local.** El cifrado utiliza la Machine Key del sistema operativo del servidor. Un `Web.config` cifrado en local no funcionarÃ¡ en el servidor (y viceversa).

Abre una consola de **CMD o PowerShell como Administrador** en el servidor `SESMADE55003`.

### 6.1. Cifrar la cadena de conexiÃ³n (Servidor de Pruebas)

> **Este comando se ejecuta solo en la Api ya que solo este web.config tendra cadena de conexiÃ³n de base de datos.Este comando se ejecutara con cmd o PowerShell como Administrador en el servidor,porque el cifrado usa una llave secreta que estÃ¡ "escondida" en el hardware/Windows de tu PC (la Machine Key). Con lo cual este comando es a nivel de mÃ¡quina y si lo "ofuscas" en local y lo subes a mano al servidor Â¡La aplicaciÃ³n explotarÃ¡ y darÃ¡ un Error 500!. 

**Para la API:**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\App\gestion3\API_REC_ENV_GESAP"
```



### 6.2. Descifrar (si necesitas editar el Web.config manualmente)

Si necesitas modificar la cadena de conexiÃ³n, primero descÃ­frala con uno de estos comandos:

**Por ruta fÃ­sica** (mÃ¡s directo, no requiere saber el nombre del sitio IIS):
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "E:\App\gestion3\API_REC_ENV_GESAP"
```

**Por aplicaciÃ³n IIS** (requiere saber el nombre del sitio padre):
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pd "connectionStrings" -app "/API_REC_ENV_GESAP" -site "Gestion3"
```

> [!NOTE]
> Tras editar el `Web.config`, vuelve a ejecutar el comando de **cifrado** (`-pef`). La aplicaciÃ³n .NET lee los datos descifrados automÃ¡ticamente en memoria; no es necesario ningÃºn cambio de cÃ³digo.

---

## Fase 7: Pruebas de VerificaciÃ³n

### 7.1. Verificar que la API responde
Abre un navegador y navega a:
```
https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api/recordatorio
```
âœ… Correcto: Devuelve un error `401 Unauthorized` o `403 Forbidden` (la API estÃ¡ viva y protegida por ApiKey).  
âŒ Error: Si da `404`, revisa que la aplicaciÃ³n IIS estÃ¡ creada correctamente (Fase 4).

### 7.2. Verificar que el Swagger estÃ¡ activo (solo en pruebas)
```
https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/swagger
```
âœ… Correcto: Muestra la interfaz de documentaciÃ³n Swagger.

### 7.3. Verificar la Lanzadera (solo en pruebas)
```
https://gestion3-desa.atisae.com/WEB_REC_ENV_GESAP/debug_launcher.aspx
```
âœ… Correcto: Muestra la consola de administraciÃ³n con acceso a cifrado y logs.

### 7.4. Verificar el flujo completo de un recordatorio
Desde la Lanzadera, selecciona un ID de Oracle, copia la URL generada y pÃ©gala en una pestaÃ±a nueva. El formulario debe cargar con los datos del registro de prueba.

---

## Fase 8: âš ï¸ Diferencias para el Despliegue en ProducciÃ³n

> [!CAUTION]
> En producciÃ³n, la **API** se despliega en `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/` y la **Web** en `https://portal-contrataciones.tuv-sud.es/WEB_REC_ENV_GESAP/`. Son **servidores fÃ­sicos distintos**. Este aislamiento es intencional por seguridad: si un servidor se ve comprometido, el otro permanece aislado.

### 8.1. Cambios obligatorios en el Web.config de la API (producciÃ³n)

```xml
<!-- Cambiar a false para desactivar completamente Swagger y herramientas de desarrollo -->
<add key="EsDesarrollo" value="false" />

<!-- Nuevas claves generadas especÃ­ficamente para producciÃ³n (nunca reutilizar las de pruebas) -->
<add key="EncryptionKey" value="[NUEVA_CLAVE_PRODUCCION_BASE64]" />
<add key="HmacKey"       value="[NUEVA_CLAVE_PRODUCCION_BASE64]" />
<add key="ApiKey"        value="[NUEVO_API_KEY_PRODUCCION]" />

<!-- Cadena de conexiÃ³n apuntando a Oracle de producciÃ³n -->
<add name="OracleConnection" connectionString="[CADENA_PRODUCCION]" ... />
```

### 8.2. Cambios obligatorios en el Web.config de la Web (producciÃ³n)

```xml
<!-- URL pÃºblica de la API de producciÃ³n (servidor DMZ) -->
<add key="ApiBaseUrl" value="https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api" />

<!-- Las mismas claves nuevas de producciÃ³n que en la API -->
<add key="EncryptionKey" value="[NUEVA_CLAVE_PRODUCCION_BASE64]" />
<add key="HmacKey"       value="[NUEVA_CLAVE_PRODUCCION_BASE64]" />
<add key="ApiKey"        value="[NUEVO_API_KEY_PRODUCCION]" />
```

### 8.3. Archivos que NO deben existir en producciÃ³n

> [!CAUTION]
> La eliminaciÃ³n o inutilizaciÃ³n de estos archivos es **obligatoria** antes de cualquier pentest o auditorÃ­a de seguridad. Su presencia expone el sistema a ataques directos sobre herramientas administrativas internas.

| Archivo | Motivo de eliminaciÃ³n | Alternativa |
|---|---|---|
| `debug_launcher.aspx` | Consola completa de administraciÃ³n. Expone cifrado, logs y auditorÃ­a de Oracle. | Renombrar a `debug_launcher.aspx_bak` |
| `SecurityTests.aspx` | Suite de pruebas de seguridad. Revela detalles de la arquitectura interna. | Renombrar a `SecurityTests.aspx_bak` |
| `App_Data/Logs/*.txt` | No subir logs de desarrollo/pruebas al servidor de producciÃ³n. | Borrar contenido |

> [!TIP]
> **Renombrar vs. Eliminar:** Si se opta por renombrar la extensiÃ³n (ej. `.aspx` â†’ `.aspx_bak`), IIS no procesarÃ¡ el archivo como pÃ¡gina activa, pero se conserva en el servidor para poder restaurarlo rÃ¡pidamente en caso de necesidad diagnÃ³stica puntual. Tras la intervenciÃ³n, volver a renombrar a `.aspx_bak`.

**En producciÃ³n, la Web solo debe contener:**
```
WEB_REC_ENV_GESAP/  (en portal-contrataciones.tuv-sud.es)
â”œâ”€â”€ index.html          â† La Ãºnica pÃ¡gina pÃºblica
â”œâ”€â”€ favicon.ico
â”œâ”€â”€ Global.asax
â”œâ”€â”€ Web.config          â† Con ApiBaseUrl apuntando a ws-dmz.atisae.com
â”œâ”€â”€ images/             â† Logo TÃœV SÃœD
â”œâ”€â”€ css/
â”œâ”€â”€ js/
â”‚   â””â”€â”€ app.js
â”œâ”€â”€ App_Data/           â† VacÃ­o (solo para logs en runtime)
â””â”€â”€ bin/                â† DLLs compiladas
```

### 8.4. ConfiguraciÃ³n del cifrado de cadenas en producciÃ³n

Ejecuta los mismos comandos `aspnet_regiis -pef` pero con las rutas fÃ­sicas reales del servidor de producciÃ³n:

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
> A partir de la versiÃ³n 2.0.0, la API envÃ­a correos electrÃ³nicos de confirmaciÃ³n al titular tras aceptar una oferta. Para que esto funcione en producciÃ³n:
> - El servidor de la API (DMZ) debe tener **visibilidad de red al servidor SMTP corporativo** configurado en la tabla `GESAP.SYS_CONFIGURACION` de Oracle.
> - La configuraciÃ³n SMTP (host, puerto, usuario, contraseÃ±a, SSL) se obtiene **dinÃ¡micamente de la base de datos**, no del `Web.config`.
> - El logo de TÃœV SÃœD se embebe directamente en el correo (tecnologÃ­a CID/Base64), por lo que **no requiere acceso a URLs externas** para mostrarse correctamente en Outlook.

---

## ðŸ“‹ Checklist de Despliegue

### Servidor de Pruebas (`gestion3-desa.atisae.com`)
- [ ] CÃ³digo compilado en modo **Release**
- [ ] Web.config de API con valores de Oracle de pruebas
- [ ] Web.config de Web con `ApiBaseUrl` â†’ `https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/api`
- [ ] `EsDesarrollo=true` en el Web.config de la API
- [ ] Aplicaciones IIS creadas con icono de globo (no carpeta amarilla)
- [ ] Pool `Gestion2` asignado a ambas aplicaciones
- [ ] Permisos `IIS_IUSRS` + Modificar sobre `App_Data`
- [ ] `connectionStrings` cifrada con `aspnet_regiis -pef`
- [ ] Swagger accesible en `https://gestion3-desa.atisae.com/API_REC_ENV_GESAP/swagger`
- [ ] Lanzadera accesible en `https://gestion3-desa.atisae.com/WEB_REC_ENV_GESAP/debug_launcher.aspx`
- [ ] EnvÃ­o de correo de prueba verificado (comprobar SMTP)

### ProducciÃ³n â€” Servidor API (`ws-dmz.atisae.com`)
- [ ] CÃ³digo compilado en modo **Release**
- [ ] Web.config con `EsDesarrollo=false`
- [ ] Nuevas claves `EncryptionKey`, `HmacKey` y `ApiKey` generadas
- [ ] `connectionStrings` cifrada con `aspnet_regiis -pef` en `E:\app\gestion-dmz\API_REC_ENV_GESAP`
- [ ] Permisos `IIS_IUSRS` + Modificar sobre `App_Data`
- [ ] Visibilidad de red al servidor SMTP corporativo (para envÃ­o de emails)
- [ ] API responde en `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api/recordatorio` (401/403 = OK)

### ProducciÃ³n â€” Servidor Web (`portal-contrataciones.tuv-sud.es`)
- [ ] CÃ³digo compilado en modo **Release**
- [ ] Web.config con `ApiBaseUrl` â†’ `https://ws-dmz.atisae.com/API_REC_ENV_GESAP/api`
- [ ] Claves `EncryptionKey`, `HmacKey`, `ApiKey` **idÃ©nticas** a las de la API
- [ ] `debug_launcher.aspx` **eliminado o renombrado** a `.aspx_bak`
- [ ] `SecurityTests.aspx` **eliminado o renombrado** a `.aspx_bak`
- [ ] Permisos `IIS_IUSRS` + Modificar sobre `App_Data`
- [ ] Formulario accesible en `https://portal-contrataciones.tuv-sud.es/WEB_REC_ENV_GESAP/index.html?id=[TOKEN]`
- [ ] Pentest ejecutado sobre el entorno de producciÃ³n antes del go-live

---

*Para mÃ¡s detalles sobre el cifrado de configuraciÃ³n, ver [GUIA_CIFRADO_CONFIG.md](GUIA_CIFRADO_CONFIG.md).*
# ðŸ” GuÃ­a de Cifrado de ConfiguraciÃ³n (.NET Framework 4.7.2)

Esta guÃ­a explica cÃ³mo proteger secciones sensibles del archivo `Web.config` (como Connection Strings) utilizando la herramienta nativa de .NET `aspnet_regiis`.

> [!CAUTION]
> **ESTE PROCESO DEBE EJECUTARSE EN EL SERVIDOR DE DESTINO, NUNCA EN LOCAL.**
> El cifrado utiliza una llave RSA almacenada en la Machine Key del sistema operativo del servidor.
> *   **NO CIFRAR EN LOCAL:** Si cifras el archivo en tu PC de desarrollo y lo subes, **no funcionarÃ¡** en el servidor (Error de descifrado).
> *   **PROCEDIMIENTO CORRECTO:** Sube el `Web.config` **sin cifrar (texto plano)** al servidor, y ejecuta el comando `aspnet_regiis` directamente en la consola del servidor como Ãºltimo paso del despliegue.

---

## 1. LocalizaciÃ³n de la Herramienta

La utilidad se encuentra en el directorio del Framework .NET:
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe
```

Para usarla, abre una consola de **PowerShell o CMD como Administrador**.

---

## 2. Cifrado por Ruta FÃ­sica (OpciÃ³n Recomendada)

Esta opciÃ³n cifra el archivo basÃ¡ndose en la carpeta fÃ­sica donde se encuentra, sin necesidad de conocer el nombre del sitio en IIS.

### Sintaxis
```powershell
aspnet_regiis.exe -pef "nombre_seccion" "ruta_carpeta"
```
*   `-pef`: **P**rovider **E**ncrypt **F**ile (cifrado por ruta fÃ­sica).
*   `nombre_seccion`: La secciÃ³n XML a cifrar (`connectionStrings` o `appSettings`).
*   `ruta_carpeta`: La ruta absoluta a la carpeta que contiene el `Web.config` **(sin barra final)**.

### Ejemplo Real â€” Servidor de Pruebas (SESMADE55003 / Gestion3)

**Cifrar la cadena de conexiÃ³n de la API:**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\App\gestion3\API_REC_ENV_GESAP"
```

**Cifrar la cadena de conexiÃ³n de la Web:**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\App\gestion3\WEB_REC_ENV_GESAP"
```

---

## 3. Cifrado por AplicaciÃ³n IIS (OpciÃ³n Alternativa)

Si prefieres usar el nombre de la aplicaciÃ³n virtual registrada en IIS (requiere conocer el nombre del sitio padre):

### Sintaxis
```powershell
aspnet_regiis.exe -pe "nombre_seccion" -app "/AliasEnIIS" -site "NombreSitioIIS"
```

### Ejemplo Real â€” Servidor de Pruebas
```powershell
# Cifrar cadena de conexiÃ³n de la API via nombre IIS
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pe "connectionStrings" -app "/API_REC_ENV_GESAP" -site "Gestion3"

# Cifrar cadena de conexiÃ³n de la Web via nombre IIS
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pe "connectionStrings" -app "/WEB_REC_ENV_GESAP" -site "Gestion3"
```

---

## 4. CÃ³mo Descifrar (Revertir)

Si necesitas editar el archivo `Web.config` manualmente en el servidor (por ejemplo, para cambiar la contraseÃ±a de Oracle), primero debes descifrarlo.

### Por Ruta FÃ­sica (`-pdf`)
```powershell
# Descifrar API
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "E:\App\gestion3\API_REC_ENV_GESAP"

# Descifrar Web
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "E:\App\gestion3\WEB_REC_ENV_GESAP"
```

### Por AplicaciÃ³n IIS (`-pd`)
```powershell
# Descifrar API via nombre IIS
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pd "connectionStrings" -app "/API_REC_ENV_GESAP" -site "Gestion3"

# Descifrar Web via nombre IIS
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pd "connectionStrings" -app "/WEB_REC_ENV_GESAP" -site "Gestion3"
```

> [!TIP]
> Tras editar el `Web.config`, vuelve a ejecutar el comando de cifrado (`-pef` o `-pe`). La aplicaciÃ³n .NET lee los datos descifrados automÃ¡ticamente en memoria; no es necesario ningÃºn cambio en el cÃ³digo.

---

## 5. VerificaciÃ³n

Una vez cifrado, si abres el `Web.config` con el Bloc de Notas, la secciÃ³n `connectionStrings` se verÃ¡ como un bloque ilegible:
```xml
<connectionStrings configProtectionProvider="RsaProtectedConfigurationProvider">
  <EncryptedData>
    <CipherData>
      <CipherValue>aq9s8d7f9s8d7f...</CipherValue>
    </CipherData>
  </EncryptedData>
</connectionStrings>
```
**La aplicaciÃ³n .NET leerÃ¡ los valores descifrados automÃ¡ticamente** en memoria sin ningÃºn cambio de cÃ³digo.

---

## 6. Ejemplo Real â€” Servidor de ProducciÃ³n

> [!IMPORTANT]
> En producciÃ³n, la API y la Web estÃ¡n en **servidores fÃ­sicos distintos**. Debes ejecutar `aspnet_regiis` en cada servidor por separado.

**Cifrar la cadena de conexiÃ³n de la API (servidor DMZ â€” `ws-dmz.atisae.com`):**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "E:\app\gestion-dmz\API_REC_ENV_GESAP"
```

**Descifrar la cadena de conexiÃ³n de la API (servidor DMZ):**
```powershell
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "E:\app\gestion-dmz\API_REC_ENV_GESAP"
```

**Cifrar/Descifrar la Web (servidor Portal â€” `portal-contrataciones.tuv-sud.es`):**
```powershell
# Sustituir [RUTA_FISICA_WEB] por la ruta asignada por el equipo de Sistemas
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" "[RUTA_FISICA_WEB_PRODUCCION]"
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pdf "connectionStrings" "[RUTA_FISICA_WEB_PRODUCCION]"
```

---

*Â© 2026 TÃœV SÃœD - DocumentaciÃ³n TÃ©cnica Confidencial.*
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
# ðŸ—ºï¸ GuÃ­a de Lectura de DocumentaciÃ³n

Para una correcta transiciÃ³n a producciÃ³n y entendimiento del sistema, se recomienda seguir este orden de lectura segÃºn tu perfil:

---

### ðŸš€ Si vas a hacer un despliegue (nuevo o actualizaciÃ³n)

#### 1. [GuÃ­a de Despliegue en IIS](Plan_despliegue_Web_Api.md) â† **Empieza aquÃ­**
*   **PropÃ³sito**: Documento Ãºnico con todo el proceso: requisitos del servidor, datos reales del servidor de pruebas (`gestion3-desa.atisae.com`), Web.config, IIS, permisos, cifrado, hardening de seguridad y checklist.
*   **QuÃ© buscar**: Tabla de entornos, Fase 8 (diferencias para producciÃ³n), archivos a eliminar antes de un pentest.

#### 2. [GuÃ­a de Cifrado de ConfiguraciÃ³n](GUIA_CIFRADO_CONFIG.md)
*   **PropÃ³sito**: Proteger las credenciales en el servidor con `aspnet_regiis`.
*   **QuÃ© buscar**: Comandos exactos con los paths reales del servidor de pruebas y cÃ³mo descifrar para modificar.

---

### ðŸ“˜ Si eres desarrollador nuevo en el proyecto

#### 1. [Manual TÃ©cnico](MANUAL_TECNICO.md)
*   **PropÃ³sito**: Documento de referencia de arquitectura.
*   **QuÃ© buscar**: Arquitectura Cebolla (Onion), flujos de cifrado AES-256, HMAC, sistema de auditorÃ­a forense.

#### 2. [Arquitectura TÃ©cnica](ARQUITECTURA_TECNICA.md)
*   **PropÃ³sito**: Diagramas de alto nivel y tabla de configuraciÃ³n.
*   **QuÃ© buscar**: Diagramas de flujo, tabla de AppSettings por entorno (pruebas vs producciÃ³n).

#### 3. [Manual Funcional](MANUAL_FUNCIONAL.md)
*   **PropÃ³sito**: GuÃ­a de uso para usuarios finales.
*   **QuÃ© buscar**: Flujo de trabajo del formulario de respuesta, estados de los recordatorios.

---

### ðŸ›¡ï¸ Si vas a hacer un pentest o auditorÃ­a de seguridad

#### 1. [GuÃ­a Demo Seguridad](GUIA_DEMO_SEGURIDAD.md)
*   **PropÃ³sito**: Verificar que el sistema bloquea IPs correctamente.
*   **QuÃ© buscar**: Paso a paso para simular una intrusiÃ³n y validar la respuesta del servidor.

#### 2. [GuÃ­a de Despliegue en IIS](Plan_despliegue_Web_Api.md) (SecciÃ³n Hardening / Fase 8)
*   **QuÃ© buscar**: Lista de archivos que DEBEN estar eliminados antes del pentest (`debug_launcher.aspx`, `SecurityTests.aspx`, Swagger desactivado).

---

### ðŸ“„ Documento de referencia completo

#### [DocumentaciÃ³n Unificada](Documentacion_Unificada_API_RECORDATORIO.md)
*   **PropÃ³sito**: Todo el contenido en un Ãºnico documento, listo para imprimir o enviar.

---

*Â© 2026 TÃœV SÃœD - DocumentaciÃ³n TÃ©cnica Confidencial.*
