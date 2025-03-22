using System;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using DameChanceSV2.Models;

namespace DameChanceSV2.DAL
{
    public class PerfilDeUsuarioDAL
    {
        private readonly Database _database;

        public PerfilDeUsuarioDAL(Database database)
        {
            _database = database;
        }

        // Inserta un nuevo perfil y retorna el Id insertado.
        public int InsertPerfil(PerfilDeUsuario perfil)
        {
            int newId = 0;
            using (SqlConnection conn = _database.GetConnection())
            {
                string query = @"INSERT INTO PerfilesDeUsuario (UsuarioId, Edad, Genero, Intereses, Ubicacion, ImagenPerfil)
                                 OUTPUT INSERTED.Id
                                 VALUES (@UsuarioId, @Edad, @Genero, @Intereses, @Ubicacion, @ImagenPerfil)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", perfil.UsuarioId);
                    cmd.Parameters.AddWithValue("@Edad", perfil.Edad);
                    cmd.Parameters.AddWithValue("@Genero", perfil.Genero);
                    cmd.Parameters.AddWithValue("@Intereses", perfil.Intereses ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ubicacion", perfil.Ubicacion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImagenPerfil", perfil.ImagenPerfil ?? (object)DBNull.Value);

                    conn.Open();
                    newId = (int)cmd.ExecuteScalar();
                }
            }
            return newId;
        }
    }
}
