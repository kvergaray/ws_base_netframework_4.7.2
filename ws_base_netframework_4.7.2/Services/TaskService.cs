using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsService.Domain;
using WindowsService.Infrastructure.DataAccess;
using WindowsService.Infrastructure.Helpers;

namespace WindowsService.Services
{
    public class TaskService
    {
        private readonly IUserRepository _usuarioRepo;
        private readonly int _maxIntentos;

        public TaskService(IUserRepository usuarioRepo, int maxIntentos)
        {
            if (usuarioRepo == null)
            {
                throw new ArgumentNullException("usuarioRepo");
            }

            if (maxIntentos <= 0)
            {
                throw new ArgumentOutOfRangeException("maxIntentos", "El numero maximo de intentos debe ser mayor que cero.");
            }

            _usuarioRepo = usuarioRepo;
            _maxIntentos = maxIntentos;
        }

        public async Task InicioAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogHelper.Warn("Cancelacion solicitada antes de iniciar el procesamiento de usuarios.");
                return;
            }

            IReadOnlyList<UserListarDto> usuarioNotif;
            try
            {
                usuarioNotif = await _usuarioRepo.GetUsersToProcessAsync(_maxIntentos, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                LogHelper.Warn("Obtencion de usuarios cancelada antes de iniciar el procesamiento.");
                return;
            }

            if (usuarioNotif == null || usuarioNotif.Count == 0)
            {
                LogHelper.Info("No hay poblacion a procesar.");
                return;
            }

            var total = usuarioNotif.Count;
            LogHelper.Info(string.Format("Iniciando procesamiento de {0} notificacion(es). Intentos maximos: {1}", total, _maxIntentos));

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

                LogHelper.Info(string.Format("******** {0} con usuario: {1} (id: {2}) ********", un.nombres, un.usuario, un.idUsuario));

                if (string.IsNullOrWhiteSpace(un.email))
                {
                    LogHelper.Error(string.Format("Procesamiento a {0}: correo vacio o nulo. Se omite.", un.usuario));
                    continue;
                }

                var intento = 0;
                var enviado = false;

                while (intento < _maxIntentos && !enviado && !cancellationToken.IsCancellationRequested)
                {
                    intento++;
                    try
                    {
                        LogHelper.Info(string.Format("Notificacion {0}: intento {1} de {2}.", un.usuario, intento, _maxIntentos));

                        await SimulateSendAsync(cancellationToken).ConfigureAwait(false);

                        LogHelper.Info(string.Format("Procesamiento a {0}: OK en {1} intento(s).", un.usuario, intento));
                        enviado = true;
                    }
                    catch (OperationCanceledException)
                    {
                        LogHelper.Warn(string.Format("Procesamiento a {0}: cancelado durante el intento {1}.", un.usuario, intento));
                        return;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(string.Format("Procesamiento a {0}: error en intento {1}.", un.idUsuario, intento), ex);

                        if (intento >= _maxIntentos)
                        {
                            LogHelper.Error(string.Format("Procesamiento a {0}: agoto reintentos ({1}). Marcado como fallido.", un.usuario, _maxIntentos));
                        }
                    }
                }

                if (!enviado && cancellationToken.IsCancellationRequested)
                {
                    LogHelper.Warn(string.Format("Procesamiento a {0}: cancelado por solicitud del servicio.", un.usuario));
                }
            }
        }

        private static async Task SimulateSendAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
