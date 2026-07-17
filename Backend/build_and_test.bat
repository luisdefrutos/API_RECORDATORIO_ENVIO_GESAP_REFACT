@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo   API RECORDATORIO ENVIO GESAP - BUILD ^& TEST (Local CI)
echo ========================================================
echo.

:: Detectar MSBuild y VSTest asumiendo Visual Studio 2017 o superior
set MSBUILD_PATH=
set VSTEST_PATH=

:: Intentar buscar VS 2022, 2019, 2017
for %%v in (2022 2019 2017) do (
    for %%e in (Enterprise Professional Community BuildTools) do (
        if exist "C:\Program Files\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe" (
            set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe"
            set "VSTEST_PATH=C:\Program Files\Microsoft Visual Studio\%%v\%%e\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
            goto compiler_found
        )
        if exist "C:\Program Files (x86)\Microsoft Visual Studio\%%v\%%e\MSBuild\15.0\Bin\MSBuild.exe" (
            set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\%%v\%%e\MSBuild\15.0\Bin\MSBuild.exe"
            set "VSTEST_PATH=C:\Program Files (x86)\Microsoft Visual Studio\%%v\%%e\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
            goto compiler_found
        )
    )
)

:compiler_found
if "%MSBUILD_PATH%"=="" (
    echo [ERROR] No se ha encontrado MSBuild. Asegurate de tener Visual Studio instalado.
    exit /b 1
)

echo [INFO] Restaurando paquetes NuGet...
:: Asumimos que nuget.exe está en el PATH o usamos msbuild /t:restore si es posible
"%MSBUILD_PATH%" "RecordatorioEnvioGesap.sln" /t:Restore /p:Configuration=Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Fallo al restaurar paquetes NuGet.
    exit /b %ERRORLEVEL%
)

echo.
echo [INFO] Compilando solucion (Release)...
"%MSBUILD_PATH%" "RecordatorioEnvioGesap.sln" /p:Configuration=Release /v:m
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] La compilacion fallo.
    exit /b %ERRORLEVEL%
)

echo.
echo [INFO] Ejecutando Tests Unitarios...
"%VSTEST_PATH%" "src\RecordatorioEnvio.Tests.Unit\bin\Release\RecordatorioEnvio.Tests.Unit.dll" /logger:console;verbosity=normal
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================================
    echo   [ERROR] ✖ ALGUNOS TESTS UNITARIOS HAN FALLADO.
    echo           Revisa el log de arriba para ver los motivos.
    echo ========================================================
    exit /b %ERRORLEVEL%
)

echo.
echo =======================================================================
echo   ATENCION: Se estan ejecutando Pruebas de Integracion.
echo   Se usara la conexion definida en App.config de Integracion
echo   (asegurate de que apunta a la BBDD de Test, igual que la API).
echo =======================================================================
echo [INFO] Ejecutando Tests de Integracion contra la Base de Datos...
"%VSTEST_PATH%" "src\RecordatorioEnvio.Tests.Integration\bin\Release\RecordatorioEnvio.Tests.Integration.dll" /logger:console;verbosity=normal 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================================
    echo   [ERROR] ✖ ALGUNOS TESTS DE INTEGRACION HAN FALLADO.
    echo           Revisa el log de arriba para ver los motivos.
    echo           NO HAGAS COMMIT AUN.
    echo ========================================================
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo   [SUCCESS] TODOS LOS TESTS PASARON CORRECTAMENTE.
echo   PUEDES HACER COMMIT EN SVN CON CONFIANZA.
echo ========================================================
pause
