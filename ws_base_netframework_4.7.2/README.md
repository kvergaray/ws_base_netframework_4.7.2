# WindowsService base (.NET Framework 4.7.2)

## Descripcion

Servicio Windows parametrizable para ejecutar tareas programadas contra distintos motores de base de datos. Puede operar como servicio o en modo consola para depuracion y registra toda la actividad con log4net en archivos rotativos.

## Caracteristicas clave

- Programacion configurable por `appSettings`: modo `INTERVAL` (cada N minutos) o `DAILY` (hora fija).
- Motor de ejecucion basado en `Timer` y cancelacion con `CancellationToken` para detener el servicio con seguridad.
- Pipeline asincrono (`TaskService.InicioAsync`) con reintentos controlados por `NumIntentos` y cancelacion cooperativa.
- Acceso a datos abstraido por `IUserRepository` con implementaciones de ejemplo para SQL Server, Oracle, MySQL y PostgreSQL (carpeta `Infrastructure/DataAccess`).
- Utilidades de mapeo (`DataReaderHelper`) para leer valores nulos o tipados desde `IDataReader` sin excepciones.
- Registro de eventos con log4net (info y errores separados) en la carpeta `Logs`.
- Instalador (`ProjectInstaller`) que toma nombre, display name y descripcion directamente del `App.config`.

## Requisitos previos

- Windows 10/11 con permisos para instalar servicios.
- .NET Framework 4.7.2 Developer Pack (incluido con Visual Studio 2019+).
- Acceso al motor de base de datos que se vaya a utilizar (SQL Server, Oracle, MySQL o PostgreSQL).
- Acceso al `InstallUtil.exe` del framework (normalmente en `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319`).
- Drivers/paquetes nativos cuando aplique (por ejemplo, cliente Oracle o drivers ODBC si la politica de la empresa lo requiere).

## Base de datos de ejemplo

- En la raiz del repositorio se encuentra `script.sql`, que crea la base de datos usada durante las pruebas con SQL Server.
- Para otros motores utilice el script como referencia para crear un objeto compatible (parametro que recibe el numero de intentos y devuelve la poblacion pendiente).
- Ajuste los nombres de servidor, credenciales y base de datos segun su entorno antes de ejecutarlo.

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

Declare tantas conexiones como motores necesite en `<connectionStrings>`. Se incluyen plantillas para:

- `BD_SQL` (SQL Server, `System.Data.SqlClient`)
- `BD_ORACLE_TNS` / `BD_ORACLE_EZ` (Oracle Managed Data Access)
- `BD_MYSQL` (MySqlConnector)
- `BD_POSTGRES` (Npgsql)

Recomendaciones:

1. Ajuste cadenas y credenciales segun el entorno.
2. Elija una cadena activa y renombrela a `BD1` **o** modifique la implementacion de repositorio que utilice para apuntar al nombre deseado.
3. Asegurese de que `DefaultConnectionName` coincida con la cadena seleccionada si decide inyectarla mediante configuracion.

### Seleccionar el repositorio de usuarios

Las implementaciones de `IUserRepository` se encuentran en:

- `Infrastructure/DataAccess/Sql/UserRepositorySql.cs`
- `Infrastructure/DataAccess/MySql/UserRepositoryMySql.cs`
- `Infrastructure/DataAccess/Oracle/UserRepositoryOracle.cs`
- `Infrastructure/DataAccess/Postgresql/UserRepositoryPostgres.cs`

Por defecto `TaskService` crea `UserRepositorySql`. Para cambiar de motor, sustituya la asignacion de `_usuarioRepo` en `Services/TaskService.cs` por la implementacion correspondiente o adapte la clase para inyectarla desde configuracion/DI. Todas exponen el metodo asincrono `GetUsersToProcessAsync` y comparten el mapeo de `DataReaderHelper`.

### Logging

`log4net.config` define dos archivos:

- `Logs\log.txt` para mensajes `DEBUG/INFO`.
- `Logs\logError.txt` para `WARN/ERROR`.

El directorio `Logs` se crea al iniciar el servicio si no existe.

## Primeros pasos

```powershell
# Restaurar paquetes NuGet
nuget restore ws_base_netframework_4.7.2\WindowsService.csproj

# Compilar (Debug o Release segun necesidad)
msbuild ws_base_netframework_4.7.2\WindowsService.csproj /p:Configuration=Release
```

## Ejecucion manual (modo consola)

```powershell
cd ws_base_netframework_4.7.2\bin\Debug
.\WindowsService.exe
```

El programa detecta el modo interactivo, ejecuta una corrida completa y deja los registros en `Logs`.

## Instalacion como servicio de Windows

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
3. Cada ciclo invoca `TaskService.InicioAsync`, que consulta usuarios pendientes (`IUserRepository.GetUsersToProcessAsync`) y procesa cada registro respetando el maximo de intentos.
4. Si el servicio se detiene, se cancela el token y se limpian los recursos.

## Estructura relevante

- `Program.cs`: punto de entrada, alterna entre modo servicio y modo consola.
- `Service1.cs`: logica del servicio Windows, programacion y manejo de cancelacion.
- `Services/TaskService.cs`: implementa la logica de negocio con reintentos y seleccion de repositorio de usuarios.
- `Infrastructure/DataAccess/*`: implementaciones de `IUserRepository` por motor de base de datos y helpers.
- `Infrastructure/Helpers/LogHelper.cs`: wrapper de log4net.
- `ProjectInstaller.cs`: instalador para `InstallUtil`.

## Personalizacion

- Reemplace la seccion marcada en `TaskService` con la logica final de procesamiento (por ejemplo, enviar notificaciones reales).
- Ajuste `NumIntentos`, `IntervalMinutes` o `ScheduledTime` para adecuarse a las ventanas de negocio.
- Modifique la implementacion de `IUserRepository` o agregue otras para motores adicionales/queries especificas.
- Actualice las cadenas de conexion y agregue claves adicionales a `App.config` si necesita mas parametros.

## Validacion recomendada

1. Ejecutar en modo consola para verificar logs y conectividad a base de datos.
2. Revisar `Logs\log.txt` y `Logs\logError.txt` para confirmar el resultado.
3. Instalar como servicio en un entorno de pruebas y validar inicios/detenciones desde `services.msc`.
4. Monitorear el consumo de recursos y el comportamiento del `Timer` bajo la carga esperada.
5. Validar la obtencion de usuarios con cada motor configurado (al menos una corrida de smoke test por proveedor).
