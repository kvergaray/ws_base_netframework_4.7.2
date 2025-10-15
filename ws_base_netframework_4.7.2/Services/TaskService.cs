using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsService.Infrastructure.DataAccess;
using WindowsService.Infrastructure.Helpers;

namespace WindowsService.Services
{
    public class TaskService
    {
        private readonly UserRepository _usuarioRepo;
        private readonly int _maxIntentos;

        public TaskService(UserRepository usuarioRepo)
        {
            _usuarioRepo = usuarioRepo;
            _maxIntentos = 3;
        }

        public void Inicio()
        {
            var usuarioNotif = _usuarioRepo.GetUsersToProcess(_maxIntentos);

            if (usuarioNotif == null || usuarioNotif.Count <= 0)
            {
                LogHelper.Info("No hay población a procesar.");
                return;
            }

            var total = usuarioNotif.Count;
            LogHelper.Info($"Iniciando procesamiento de {total} notificación(es). Intentos máximos: {_maxIntentos}");

            foreach (var un in usuarioNotif)
            {
                if (un == null)
                {
                    LogHelper.Warn("Elemento nulo en la colección usuarioNotif. Se omite.");
                    continue;
                }
                LogHelper.Info($"******** {un.nombres} con usuario: {un.usuario} (id: {un.idUsuario}) ********");

                if (string.IsNullOrWhiteSpace(un.email))
                {
                    LogHelper.Error($"Procesamiento a {un.usuario}: correo vacío o nulo. Se omite.");
                    continue;
                }

                var intento = 0;
                var enviado = false;
                Exception lastEx = null;

                while (intento < Math.Max(1, _maxIntentos) && !enviado)
                {
                    intento++;
                    try
                    {
                        LogHelper.Info($"Notificación {un.usuario}: intento {intento} de {_maxIntentos}.");

                        // Aquí va tu lógica de envío
                        //List<string> arrAttachmentPath = new List<string>();
                        //enviado = MessagingClient.SendMail("",
                        //    _msgConfig.From, _msgConfig.To, _msgConfig.Cc, _msgConfig.Bcc, _msgConfig.Asunto, arrAttachmentPath.ToArray());

                        LogHelper.Info($"Procesamiento a {un.usuario}: OK en {intento} intento(s).");
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        LogHelper.Error($"Procesamiento a {un.idUsuario}: error en intento {intento}.", ex);

                        if (intento >= _maxIntentos)
                        {
                            LogHelper.Error($"Procesamiento a {un.usuario}: agotó reintentos ({_maxIntentos}). Marcado como fallido.");
                        }
                    }
                }
            }
        }
    }
}
