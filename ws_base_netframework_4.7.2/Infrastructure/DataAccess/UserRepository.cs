using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using WindowsService.Domain;

namespace WindowsService.Infrastructure.DataAccess
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<UserListarDto> GetUsersToProcess(int cant)
        {
            var usuarios = new List<UserListarDto>();

            try
            {
                using (var conexion = new SqlConnection(_connectionString))
                using (var comando = new SqlCommand("[PRUEBA].[sp_GetUsuarioXIntento]", conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.Parameters.AddWithValue("@numIntentos", Convert.ToInt32(cant));
                    comando.CommandTimeout = 0;

                    conexion.Open();

                    using (var reader = comando.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuarios.Add(new UserListarDto
                            {
                                idUsuario = Convert.ToInt32(reader[0]),
                                usuario = Convert.ToString(reader[1]),
                                nombres = Convert.ToString(reader[2]),
                                apellidos = Convert.ToString(reader[3]),
                                email = Convert.ToString(reader[4]),
                                telefono = Convert.ToInt32(reader[5]),
                                idRolUsuario = Convert.ToInt32(reader[6]),
                                Rol = Convert.ToString(reader[7])
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string mensaje = "Error en el procedimiento almacenado [PRUEBA].[sp_GetUsuarioXIntento], revise el logError para mayor detalle.";
                LogHelper.Info(mensaje);
                LogHelper.Error("Error: " + e.ToString());
                LogHelper.Error("Mensaje: " + e.Message);
                LogHelper.Error("InnerException: " + e.InnerException);
            }

            return usuarios;
        }
    }
}
