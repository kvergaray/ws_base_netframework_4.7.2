using System;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace WindowsService
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private const string ServiceNameKey = "ServiceName";
        private const string ServiceDisplayNameKey = "ServiceDisplayName";
        private const string ServiceDescriptionKey = "ServiceDescription";

        public ProjectInstaller()
        {
            var appSettings = LoadAppSettings();

            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = GetRequiredSetting(appSettings, ServiceNameKey),
                DisplayName = GetRequiredSetting(appSettings, ServiceDisplayNameKey),
                Description = GetRequiredSetting(appSettings, ServiceDescriptionKey),
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        private static KeyValueConfigurationCollection LoadAppSettings()
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var configuration = ConfigurationManager.OpenExeConfiguration(assemblyPath);
            return configuration.AppSettings.Settings;
        }

        private static string GetRequiredSetting(KeyValueConfigurationCollection settings, string key)
        {
            var value = settings[key]?.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException($"La clave '{key}' es obligatoria en appSettings.");
            }

            return value.Trim();
        }
    }
}
