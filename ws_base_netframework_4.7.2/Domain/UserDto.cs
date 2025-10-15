using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace WindowsService.Domain
{
    public class UserDto
    {
        [Required(ErrorMessage = "Campo Usuario requerido")]
        public string usuario { get; set; }

        [JsonIgnore]
        //[Required(ErrorMessage = "Campo contrasenia requerido")]
        public string contrasenia { get; set; }

        [Required(ErrorMessage = "Campo Nombres requerido")]
        public string nombres { get; set; }

        [Required(ErrorMessage = "Campo Apellidos requerido")]
        public string apellidos { get; set; }

        [Required(ErrorMessage = "Campo Email requerido")]
        public string email { get; set; }

        [Required(ErrorMessage = "Campo telefono requerido")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "El campo Teléfono debe contener solo números.")]
        [Range(100000000, 999999999, ErrorMessage = "El campo Teléfono debe tener una longitud de 9 dígitos.")]
        public int? telefono { get; set; }

        [Required(ErrorMessage = "Campo idRolUsuario requerido")]
        [Range(1, 15, ErrorMessage = "El campo idRolUsuario debe estar entre 1 y 15")]
        public int? idRolUsuario { get; set; }

    }
    public class Role
    {
        public int idRolUsuario { get; set; }
        public string rol { get; set; }
    }

    public class UserListarDto : UserDto
    {
        [Required(ErrorMessage = "Campo idUsuario requerido")]
        public int idUsuario { get; set; }
        [Required(ErrorMessage = "Campo Rol requerido")]
        public string Rol { get; set; }
    }

    public class UserUpdateDto : UserDto
    {
        [Required(ErrorMessage = "Campo idUsuario requerido")]
        public int idUsuario { get; set; }

    }
}

