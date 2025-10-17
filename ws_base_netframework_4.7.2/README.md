# WindowsService base (.NET Framework 4.7.2)

## Descripcion

Servicio Windows parametrizable para ejecutar tareas programadas contra SQL Server. Permite ejecutarse en modo servicio o en modo consola para depuracion, y registra toda la actividad con log4net en archivos rotativos.

## Caracteristicas clave

- Programacion configurable por `appSettings`: modo `INTERVAL` (cada N minutos) o `DAILY` (hora fija).
- Motor de ejecucion basado en `Timer` y cancelacion con `CancellationToken` para detener el servicio con seguridad.
- Servicio expose un `TaskService` que procesa usuarios pendientes con reintentos controlados por `NumIntentos`.
- Acceso a datos centralizado en `UserRepository`, que ejecuta el procedimiento almacenado `[PRUEBA].[sp_GetUsuarioXIntento]`.
- Registro de eventos con log4net (info y errores separados) en la carpeta `Logs`.
- Instalador (`ProjectInstaller`) que toma nombre, display name y descripcion directamente del `App.config`.

## Requisitos previos

- Windows 10/11 con permisos para instalar servicios.
- .NET Framework 4.7.2 Developer Pack (incluido con Visual Studio 2019+).
- SQL Server con la base de datos y el procedimiento almacenado esperados por `UserRepository`.
- Acceso al `InstallUtil.exe` del framework (normalmente en `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319`).

## Configuracion

### App.config (`<appSettings>`)

| Clave                                                       | Descripcion                                                                                  |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `Mode`                                                      | `INTERVAL` (por defecto) o `DAILY`.                                                          |
| `IntervalMinutes`                                           | Intervalo entre ejecuciones (formato `hh:mm` o minutos enteros) cuando `Mode` es `INTERVAL`. |
| `ScheduledTime`                                             | Hora (hh:mm:ss) usada cuando `Mode` es `DAILY`.                                              |
| `NumIntentos`                                               | Reintentos maximos por usuario en `TaskService`.                                             |
| `ServiceName` / `ServiceDisplayName` / `ServiceDescription` | Identidad del servicio en Windows; los usa tanto `Service1` como `ProjectInstaller`.         |
| `DefaultConnectionName`                                     | Nombre de la cadena de conexion que se tomara de `<connectionStrings>`.                      |
| `testing.mode` / `testing.to`                               | Valores de ejemplo para escenarios de prueba.                                                |

### Conexiones

Defina la cadena activa bajo `<connectionStrings>`. El repositorio toma `DefaultConnectionName` y espera credenciales validas hacia SQL Server.

### Logging

`log4net.config` define dos archivos:

- `Logs\log.txt` para mensajes `DEBUG/INFO`.
- `Logs\logError.txt` para `WARN/ERROR`.

El directorio `Logs` se crea al iniciar el servicio si no existe.

## Restaurar paquetes y compilar

```powershell
# Desde la carpeta raiz del repositorio
nuget restore ws_base_netframework_4.7.2\WindowsService.csproj
msbuild ws_base_netframework_4.7.2\WindowsService.csproj /p:Configuration=Release
```

### Ejecucion manual (modo consola)

```powershell
cd ws_base_netframework_4.7.2\bin\Debug
.\WindowsService.exe
```

El programa detecta el modo interactivo, ejecuta una corrida completa y deja los registros en `Logs`.

### Instalacion como servicio de Windows

Ejecute los siguientes comandos desde una consola con privilegios elevados:

```powershell
cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319
installutil.exe -i "RUTA\AL\PROYECTO\ws_base_netframework_4.7.2\bin\Debug\WindowsService.exe"
# Para desinstalar:
installutil.exe -u "RUTA\AL\PROYECTO\ws_base_netframework_4.7.2\bin\Debug\WindowsService.exe"
```

Reemplace `RUTA\AL\PROYECTO` por la ruta real del ejecutable compilado. Administre el servicio con `services.msc` o mediante `Get-Service`.

## Flujo de ejecucion

1. `Service1` se inicializa, valida configuraciones y levanta `TaskService`.
2. Se calcula el siguiente disparo segun `Mode` y se crea un `Timer`.
3. Cada ciclo invoca `TaskService.Inicio`, que consulta usuarios pendientes (`UserRepository.GetUsersToProcess`) y procesa cada registro respetando el maximo de intentos.
4. Si el servicio se detiene, se cancela el token y se limpian los recursos.

## Estructura relevante

- `Program.cs`: punto de entrada, alterna entre modo servicio y modo consola.
- `Service1.cs`: logica del servicio Windows, programacion y manejo de cancelacion.
- `Services/TaskService.cs`: implementa la logica de negocio con reintentos.
- `Infrastructure/DataAccess/UserRepository.cs`: obtencion de usuarios desde SQL Server.
- `Infrastructure/Helpers/LogHelper.cs`: wrapper de log4net.
- `ProjectInstaller.cs`: instalador para `InstallUtil`.

## Personalizacion

- Reemplace la seccion marcada en `TaskService` con la logica final de procesamiento (por ejemplo, envio de correos).
- Ajuste `NumIntentos`, `IntervalMinutes` o `ScheduledTime` para adecuarse a las ventanas de negocio.
- Actualice las cadenas de conexion y agregue claves adicionales a `App.config` si necesita mas parametros.

## Validacion recomendada

1. Ejecutar en modo consola para verificar logs y conectividad a base de datos.
2. Revisar `Logs\log.txt` y `Logs\logError.txt` para confirmar el resultado.
3. Instalar como servicio en un entorno de pruebas y validar inicios/detenciones desde `services.msc`.
4. Monitorear el consumo de recursos y el comportamiento del `Timer` bajo la carga esperada.
