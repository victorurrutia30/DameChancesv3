namespace DameChanceSV2.Models
{
    public class PerfilDeUsuario
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int Edad { get; set; }
        public string Genero { get; set; }
        public string Intereses { get; set; }
        public string Ubicacion { get; set; }
        public string ImagenPerfil { get; set; }
    }
}
