using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using WindowsService.Domain;
using WindowsService.Infrastructure.Helpers;

namespace WindowsService.Infrastructure.DataAccess
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("La cadena de conexion no puede ser nula o vacia.", nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public static UserRepository FromConfig(string connectionName)
        {
            var connection = ConfigurationManager.ConnectionStrings[connectionName]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connection))
            {
                throw new InvalidOperationException(string.Format("No se encontro la cadena de conexion '{0}' en la configuracion de la aplicacion.", connectionName));
            }

            return new UserRepository(connection);
        }

        public async Task<IReadOnlyList<UserListarDto>> GetUsersToProcessAsync(int maxAttempts, CancellationToken cancellationToken)
        {
            var usuarios = new List<UserListarDto>();

            try
            {
                using (var conexion = new SqlConnection(_connectionString))
                using (var comando = new SqlCommand("[PRUEBA].[sp_GetUsuarioXIntento]", conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.Parameters.Add("@numIntentos", SqlDbType.Int).Value = Convert.ToInt32(maxAttempts);
                    comando.CommandTimeout = 0;

                    await conexion.OpenAsync(cancellationToken).ConfigureAwait(false);

                    using (var reader = await comando.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false))
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
                const string mensaje = "Error en el procedimiento almacenado [PRUEBA].[sp_GetUsuarioXIntento], revise el logError para mayor detalle.";
                LogHelper.Info(mensaje);
                LogHelper.Error("Error durante la obtencion de usuarios.", e);
                throw;
            }

            return usuarios;
        }
    }
}
