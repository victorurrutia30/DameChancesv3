using System.Collections.Generic;
using System.Data.SqlClient;
using DameChanceSV2.Models;
using Microsoft.Data.SqlClient;

namespace DameChanceSV2.DAL
{
    public class RolDAL
    {
        private readonly Database _database;

        public RolDAL(Database database)
        {
            _database = database;
        }

        // Retorna una lista de roles existentes en la tabla Roles.
        public List<Rol> GetRoles()
        {
            List<Rol> roles = new List<Rol>();
            using (SqlConnection conn = _database.GetConnection())
            {
                string query = "SELECT Id, Nombre FROM Roles";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            roles.Add(new Rol
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return roles;
        }
    }
}
