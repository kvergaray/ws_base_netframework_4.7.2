using log4net;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using WindowsService.Infrastructure.DataAccess;
using WindowsService.Infrastructure.Helpers;
using WindowsService.Services;

namespace WindowsService
{
    public partial class Service1 : ServiceBase
    {
        private const string ServiceNameConfigKey = "ServiceName";
        private const string ServiceDisplayNameConfigKey = "ServiceDisplayName";
        private const string ServiceDescriptionConfigKey = "ServiceDescription";

        //private static readonly ILog LogHelper = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly object _syncRoot = new object();
        private Timer _timer;
        private CancellationTokenSource _cancellation;
        private TaskService _taskService;
        private int _isRunning;

        public Service1()
        {
            InitializeComponent();
            ServiceName = ResolveServiceName();
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = false;
        }

        protected override void OnStart(string[] args)
        {
            StartInternal(args ?? Array.Empty<string>(), false);
        }

        protected override void OnStop()
        {
            StopInternal(false);
        }

        public void RunConsole(string[] args)
        {
            LogHelper.Info("*************** Ejecucion manual (modo consola) ***************");
            EnsureLogDirectory();

            try
            {
                var displayName = ResolveDisplayName();
                var description = ResolveDescription();
                LogHelper.Info($"Identificacion: DisplayName='{displayName}', Description='{description}'.");

                var taskService = BuildTaskService();
                var previewDelay = CalculateNextDelay(out var modeDetail);
                LogHelper.Info($"Modo configurado: {modeDetail}.");
                LogHelper.Info($"Si se ejecuta como servicio, la proxima ejecucion ocurriria en {previewDelay}.");

                taskService.Inicio(CancellationToken.None);
                LogHelper.Info("*************** Ejecucion manual finalizada ***************");
            }
            catch (Exception ex)
            {
                LogHelper.Error("Error durante la ejecucion manual del servicio.", ex);
                throw;
            }
        }

        private void StartInternal(string[] args, bool interactive)
        {
            lock (_syncRoot)
            {
                if (_cancellation != null)
                {
                    LogHelper.Warn("Intento de iniciar el servicio cuando ya esta en ejecucion.");
                    return;
                }

                try
                {
                    var displayName = ResolveDisplayName();
                    var description = ResolveDescription();

                    LogHelper.Info($"*************** Iniciando servicio {ServiceName} {(interactive ? "(modo interactivo)" : string.Empty)} ***************");
                    LogHelper.Info($"Identificacion: DisplayName='{displayName}', Description='{description}'.");

                    EnsureLogDirectory();

                    _cancellation = new CancellationTokenSource();
                    _taskService = BuildTaskService();

                    ScheduleNextRun(logScheduleDetails: true);
                }
                catch (Exception ex)
                {
                    LogHelper.Error("Error al inicializar las dependencias del servicio.", ex);
                    _cancellation?.Dispose();
                    _cancellation = null;
                    _taskService = null;
                    throw;
                }
            }
        }

        private void StopInternal(bool interactive)
        {
            lock (_syncRoot)
            {
                if (_cancellation == null)
                {
                    return;
                }

                LogHelper.Info($"*************** Deteniendo servicio {ServiceName} {(interactive ? "(modo interactivo)" : string.Empty)} ***************");
                _cancellation.Cancel();

                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                _timer?.Dispose();
                _timer = null;

                _cancellation.Dispose();
                _cancellation = null;
                _taskService = null;

                Interlocked.Exchange(ref _isRunning, 0);

                LogHelper.Info($"*************** Servicio {ServiceName} detenido ***************");
            }
        }

        private void ScheduleNextRun(bool logScheduleDetails = false)
        {
            if (_cancellation == null || _cancellation.IsCancellationRequested || _taskService == null)
            {
                return;
            }

            var delay = CalculateNextDelay(out var modeDetail);

            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            if (logScheduleDetails)
            {
                LogHelper.Info($"Modo configurado: {modeDetail}.");
            }

            LogHelper.Info($"Proxima ejecucion programada en {delay} (modo {modeDetail}).");

            if (_timer == null)
            {
                _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            }

            _timer.Change(delay, Timeout.InfiniteTimeSpan);
        }

        private void OnTimerElapsed(object state)
        {
            var tokenSource = _cancellation;
            if (tokenSource == null || tokenSource.IsCancellationRequested)
            {
                return;
            }

            if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            {
                LogHelper.Warn("La tarea previa aun se encuentra en ejecucion; se omite este ciclo.");
                return;
            }

            try
            {
                LogHelper.Info("Inicio de ciclo de trabajo.");
                _taskService?.Inicio(tokenSource.Token);
                LogHelper.Info("Ciclo de trabajo finalizado.");
            }
            catch (Exception ex)
            {
                LogHelper.Error("Error durante la ejecucion del ciclo de trabajo.", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);

                if (_cancellation != null && !_cancellation.IsCancellationRequested)
                {
                    ScheduleNextRun();
                }
            }
        }

        private TimeSpan CalculateNextDelay()
        {
            return CalculateNextDelay(out _);
        }

        private TimeSpan CalculateNextDelay(out string modeDetail)
        {
            var mode = GetModeSetting();
            var now = DateTime.Now;

            switch (mode)
            {
                case "DAILY":
                    var scheduledValue = ConfigurationManager.AppSettings["ScheduledTime"];
                    if (!TimeSpan.TryParse(scheduledValue, out var scheduledTime))
                    {
                        throw new ConfigurationErrorsException("El valor de 'ScheduledTime' no es valido para el modo DAILY.");
                    }

                    var nextRun = now.Date.Add(scheduledTime);
                    if (nextRun <= now)
                    {
                        nextRun = nextRun.AddDays(1);
                    }

                    modeDetail = $"DAILY a las {scheduledTime:hh\\:mm\\:ss}";
                    return nextRun - now;

                case "INTERVAL":
                default:
                    var intervalSetting = ConfigurationManager.AppSettings["IntervalMinutes"];
                    if (!TryParseInterval(intervalSetting, out var interval) || interval <= TimeSpan.Zero)
                    {
                        throw new ConfigurationErrorsException("La clave 'IntervalMinutes' debe contener un intervalo valido mayor a cero para el modo INTERVAL.");
                    }

                    modeDetail = $"INTERVAL cada {interval:c}";
                    return interval;
            }
        }

        private static bool TryParseInterval(string value, out TimeSpan interval)
        {
            interval = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (TimeSpan.TryParse(value, out interval))
            {
                return true;
            }

            if (int.TryParse(value, out var minutes) && minutes > 0)
            {
                interval = TimeSpan.FromMinutes(minutes);
                return true;
            }

            return false;
        }

        private static string GetModeSetting()
        {
            var mode = GetRequiredSetting("Mode");
            return mode.ToUpperInvariant();
        }

        private static string ResolveServiceName()
        {
            return GetRequiredSetting(ServiceNameConfigKey);
        }

        private static void EnsureLogDirectory()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory ?? AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return;
            }

            var logDirectory = Path.Combine(basePath, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        private TaskService BuildTaskService()
        {
            var connectionName = ResolveConnectionName();
            var repository = UserRepository.FromConfig(connectionName);
            var maxAttempts = ResolveMaxAttempts();
            return new TaskService(repository, maxAttempts);
        }

        private string ResolveConnectionName()
        {
            return GetRequiredSetting("DefaultConnectionName");
        }

        private int ResolveMaxAttempts()
        {
            var rawValue = GetRequiredSetting("NumIntentos");
            if (!int.TryParse(rawValue, out var parsed) || parsed <= 0)
            {
                throw new ConfigurationErrorsException("La clave 'NumIntentos' debe ser un entero mayor a cero.");
            }

            return parsed;
        }

        private string ResolveDisplayName()
        {
            return GetRequiredSetting(ServiceDisplayNameConfigKey);
        }

        private string ResolveDescription()
        {
            return GetRequiredSetting(ServiceDescriptionConfigKey);
        }

        private static string GetRequiredSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException($"La clave '{key}' es obligatoria en appSettings.");
            }

            return value.Trim();
        }
    }
}
