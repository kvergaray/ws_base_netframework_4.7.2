using System;
using System.Threading;
using WindowsService.Infrastructure.DataAccess;
using WindowsService.Infrastructure.Helpers;

namespace WindowsService.Services
{
    public class TaskService
    {
        private readonly UserRepository _usuarioRepo;
        private readonly int _maxIntentos;

        public TaskService(UserRepository usuarioRepo, int maxIntentos)
        {
            _usuarioRepo = usuarioRepo ?? throw new ArgumentNullException(nameof(usuarioRepo));
            if (maxIntentos <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIntentos), "El numero maximo de intentos debe ser mayor que cero.");
            }

            _maxIntentos = maxIntentos;
        }

        public void Inicio(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogHelper.Warn("Cancelacion solicitada antes de iniciar el procesamiento de usuarios.");
                return;
            }

            var usuarioNotif = _usuarioRepo.GetUsersToProcess(_maxIntentos);

            if (usuarioNotif == null || usuarioNotif.Count <= 0)
            {
                LogHelper.Info("No hay poblacion a procesar.");
                return;
            }

            var total = usuarioNotif.Count;
            LogHelper.Info($"Iniciando procesamiento de {total} notificacion(es). Intentos maximos: {_maxIntentos}");

            foreach (var un in usuarioNotif)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogHelper.Warn("Cancelacion solicitada. Se detiene el procesamiento de usuarios restantes.");
                    break;
                }

                if (un == null)
                {
                    LogHelper.Warn("Elemento nulo en la coleccion usuarioNotif. Se omite.");
                    continue;
                }

                LogHelper.Info($"******** {un.nombres} con usuario: {un.usuario} (id: {un.idUsuario}) ********");

                if (string.IsNullOrWhiteSpace(un.email))
                {
                    LogHelper.Error($"Procesamiento a {un.usuario}: correo vacio o nulo. Se omite.");
                    continue;
                }

                var intento = 0;
                var enviado = false;

                while (intento < _maxIntentos && !enviado && !cancellationToken.IsCancellationRequested)
                {
                    intento++;
                    try
                    {
                        LogHelper.Info($"Notificacion {un.usuario}: intento {intento} de {_maxIntentos}.");

                        // Aqui va tu logica de envio
                        //List<string> arrAttachmentPath = new List<string>();
                        //enviado = MessagingClient.SendMail("",
                        //    _msgConfig.From, _msgConfig.To, _msgConfig.Cc, _msgConfig.Bcc, _msgConfig.Asunto, arrAttachmentPath.ToArray());

                        LogHelper.Info($"Procesamiento a {un.usuario}: OK en {intento} intento(s).");
                        enviado = true;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"Procesamiento a {un.idUsuario}: error en intento {intento}.", ex);

                        if (intento >= _maxIntentos)
                        {
                            LogHelper.Error($"Procesamiento a {un.usuario}: agoto reintentos ({_maxIntentos}). Marcado como fallido.");
                        }
                    }
                }

                if (!enviado && cancellationToken.IsCancellationRequested)
                {
                    LogHelper.Warn($"Procesamiento a {un.usuario}: cancelado por solicitud del servicio.");
                }
            }
        }

    }
}
