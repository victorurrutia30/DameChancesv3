namespace DameChanceSV2.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Contrasena { get; set; }
        public bool Estado { get; set; }
        public int RolId { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
