using System.ComponentModel.DataAnnotations;

namespace DameChanceSV2.Models
{
    public class RecuperarContrasenaViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Correo { get; set; }
    }
}
