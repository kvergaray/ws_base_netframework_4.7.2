using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using WindowsService.Domain;
using WindowsService.Infrastructure.Helpers;

namespace WindowsService.Infrastructure.DataAccess.Oracle
{
    public class UserRepositoryOracle //: IUserRepository --habilitar si se utilizara la base de datos Oracle
    {
        private const string ParamCursor = "p_cursor";
        private readonly string _connectionString;

        public UserRepositoryOracle()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["BD1"]?.ConnectionString
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'BD1' en la configuración.");
        }

        /// <summary>
        /// Ejecuta PRUEBA.sp_GetUsuarioXIntento(p_numIntentos IN NUMBER, p_cursor OUT SYS_REFCURSOR)
        /// y mapea el cursor a UserListarDto.
        /// </summary>
        public async Task<IReadOnlyList<UserListarDto>> GetUsersToProcessAsync(int maxAttempts, CancellationToken cancellationToken)
        {
            var usuarios = new List<UserListarDto>();

            try
            {
                using (var conexion = new OracleConnection(_connectionString))
                using (var comando = new OracleCommand("PRUEBA.sp_GetUsuarioXIntento", conexion))
                {
                    comando.BindByName = true;
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandTimeout = 0;

                    // Parámetros
                    comando.Parameters.Add(new OracleParameter("p_numIntentos", OracleDbType.Int32, ParameterDirection.Input)
                    {
                        Value = maxAttempts
                    });
                    comando.Parameters.Add(new OracleParameter(ParamCursor, OracleDbType.RefCursor, ParameterDirection.Output));

                    await conexion.OpenAsync(cancellationToken).ConfigureAwait(false);

                    // Ejecuta el SP para poblar el OUT cursor
                    await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                    // Obtiene el cursor y el DataReader
                    using (var refCursor = (OracleRefCursor)comando.Parameters[ParamCursor].Value)
                    using (var reader = refCursor.GetDataReader())
                    {
                        while (await Task.Run(() => reader.Read(), cancellationToken).ConfigureAwait(false))
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
                var mensaje = $"Error ejecutando el procedimiento almacenado PRUEBA.sp_GetUsuarioXIntento. Revisa el log para más detalle.";
                LogHelper.Info(mensaje);
                LogHelper.Error("Error durante la obtención de usuarios (Oracle).", e);
                throw;
            }

            return usuarios;
        }
    }
}
