using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using WindowsService.Domain;
using WindowsService.Infrastructure.Helpers;

namespace WindowsService.Infrastructure.DataAccess.Postgres
{
    public class UserRepositoryPostgres // : IUserRepository  // habilita si corresponde
    {
        private readonly string _connectionString;

        public UserRepositoryPostgres()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["BD1"]?.ConnectionString
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'BD1' en la configuración.");
        }

        /// <summary>
        /// Invoca PRUEBA.sp_getusuarioxintento(p_numintentos INT) y mapea el result set a UserListarDto.
        /// </summary>
        public async Task<IReadOnlyList<UserListarDto>> GetUsersToProcessAsync(int maxAttempts, CancellationToken cancellationToken)
        {
            var usuarios = new List<UserListarDto>();

            try
            {
                using (var conexion = new NpgsqlConnection(_connectionString))
                using (var comando = new NpgsqlCommand("PRUEBA.sp_getusuarioxintento", conexion))
                {
                    // Npgsql ejecuta funciones con CommandType.StoredProcedure como SELECT * FROM func(...)
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandTimeout = 0;

                    comando.Parameters.AddWithValue("p_numintentos", NpgsqlTypes.NpgsqlDbType.Integer, maxAttempts);

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
                const string mensaje = "Error ejecutando la función PRUEBA.sp_getusuarioxintento (PostgreSQL). Revisa el log para más detalle.";
                LogHelper.Info(mensaje);
                LogHelper.Error("Error durante la obtención de usuarios (PostgreSQL).", e);
                throw;
            }

            return usuarios;
        }
    }
}
