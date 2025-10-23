using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using WindowsService.Domain;
using WindowsService.Infrastructure.Helpers;

namespace WindowsService.Infrastructure.DataAccess.Sql
{
    public class UserRepositorySql : IUserRepository // habilita si usarás SQL Server
    {
        private readonly string _connectionString;

        public UserRepositorySql()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["BD1"]?.ConnectionString
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'BD1' en la configuración.");
        }

        /// <summary>
        /// Ejecuta [PRUEBA].[sp_GetUsuarioXIntento](@numIntentos INT) y mapea el DataReader a UserListarDto.
        /// </summary>
        public async Task<IReadOnlyList<UserListarDto>> GetUsersToProcessAsync(int maxAttempts, CancellationToken cancellationToken)
        {
            var usuarios = new List<UserListarDto>();

            try
            {
                using (var conexion = new SqlConnection(_connectionString))
                using (var comando = new SqlCommand("[PRUEBA].[sp_GetUsuarioXIntento]", conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandTimeout = 0;

                    // Parámetro de entrada
                    comando.Parameters.Add(new SqlParameter("@numIntentos", SqlDbType.Int) { Value = maxAttempts });

                    await conexion.OpenAsync(cancellationToken).ConfigureAwait(false);

                    using (var reader = await comando
                        .ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            usuarios.Add(new UserListarDto
                            {
                                idUsuario = DataReaderHelper.ReadInt32(reader, 0),
                                usuario = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                nombres = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                apellidos = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                telefono = DataReaderHelper.ReadNullableInt32(reader, 5),
                                idRolUsuario = DataReaderHelper.ReadInt32(reader, 6),
                                Rol = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                const string mensaje = "Error ejecutando el procedimiento almacenado [PRUEBA].[sp_GetUsuarioXIntento]. Revisa el log para más detalle.";
                LogHelper.Info(mensaje);
                LogHelper.Error("Error durante la obtención de usuarios (SQL Server).", e);
                throw;
            }

            return usuarios;
        }
    }
}
