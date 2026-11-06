
using System.Configuration;
using System.Data.SqlClient;

namespace TestPatrones
{
    public class ConexionBD
    {
        public static SqlConnection ObtenerConexion()
        {
            string cadena = ConfigurationManager
                .ConnectionStrings["AmorBajoLupa"]
                .ConnectionString;

            return new SqlConnection(cadena);
        }
    }
}
