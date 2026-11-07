using System.Configuration;
using System.Data.SqlClient;

namespace TestPatrones
{
    public class ConexionBD
    {
        public static SqlConnection ObtenerConexion()
        {
            var configuracion = ConfigurationManager.ConnectionStrings["AmorBajoLupa"];

            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la cadena de conexión 'AmorBajoLupa'. " +
                    "Comprueba App_Data/SqlServer.secrets.config y la referencia configSource del Web.config.");
            }

            return new SqlConnection(configuracion.ConnectionString);
        }
    }
}
