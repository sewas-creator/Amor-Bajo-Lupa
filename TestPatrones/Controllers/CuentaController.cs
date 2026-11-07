using System;
using System.Data.SqlClient;
using System.Security.Claims;
using TestPatrones;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace TestPatrones.Controllers
{
    public class CuentaController : Controller
    {

        [AllowAnonymous]
        public ActionResult Acceso(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Iniciar sesión con Google
        [AllowAnonymous]
        public ActionResult IniciarSesion(string returnUrl)
        {
            var callbackUrl = Url.Action(
                "GoogleCallback",
                "Cuenta",
                new { returnUrl = returnUrl },
                Request.Url.Scheme
            );

            HttpContext.GetOwinContext().Authentication.Challenge(
    new AuthenticationProperties
    {
        RedirectUri = callbackUrl
    },
    "Google"
);

            return new HttpUnauthorizedResult();
        }

        // Recibir al usuario después de autenticarse

        [Authorize]
        public ActionResult GoogleCallback(string returnUrl)
        {
            try
            {
                var identidad = User.Identity as ClaimsIdentity;

                if (identidad == null)
                    return Content("No se pudo obtener la identidad de Google.");

                var googleId = identidad.FindFirst(ClaimTypes.NameIdentifier);
                var correo = identidad.FindFirst(ClaimTypes.Email);

                if (googleId == null || string.IsNullOrWhiteSpace(googleId.Value))
                    return Content("No se pudo obtener el Google ID.");

                string idGoogle = googleId.Value;
                string email = correo != null ? correo.Value : null;

                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    int idUsuario;

                    // Comprobar si el usuario ya existe
                    using (var comando = new SqlCommand(
                        "SELECT IdUsuario FROM Usuarios WHERE GoogleId = @GoogleId",
                        conexion))
                    {
                        comando.Parameters.AddWithValue("@GoogleId", idGoogle);

                        object resultado = comando.ExecuteScalar();

                        if (resultado != null)
                        {
                            // Actualizar el correo si ya existe
                            idUsuario = Convert.ToInt32(resultado);

                            using (var actualizar = new SqlCommand(
                                @"UPDATE Usuarios
                          SET Correo = @Correo
                          WHERE IdUsuario = @IdUsuario",
                                conexion))
                            {
                                actualizar.Parameters.AddWithValue(
                                    "@Correo",
                                    (object)email ?? DBNull.Value
                                );

                                actualizar.Parameters.AddWithValue(
                                    "@IdUsuario", idUsuario
                                );

                                actualizar.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Registrar un usuario nuevo
                            using (var insertar = new SqlCommand(
                                @"INSERT INTO Usuarios (GoogleId, Correo)
                          OUTPUT INSERTED.IdUsuario
                          VALUES (@GoogleId, @Correo)",
                                conexion))
                            {
                                insertar.Parameters.AddWithValue(
                                    "@GoogleId", idGoogle
                                );

                                insertar.Parameters.AddWithValue(
                                    "@Correo", (object)email ?? DBNull.Value
                                );

                                idUsuario = Convert.ToInt32(
                                    insertar.ExecuteScalar()
                                );
                            }
                        }
                    }
                }

                // Regresar a la página solicitada
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                return Content(
                    "Error al registrar el usuario: " +
                    Server.HtmlEncode(ex.Message)
                );
            }
        }


        // Cerrar sesión
        [Authorize]
        public ActionResult CerrarSesion()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
    CookieAuthenticationDefaults.AuthenticationType
);

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult VerificarSesion()
        {
            var identidad = User.Identity as ClaimsIdentity;

            var googleId = identidad.FindFirst(ClaimTypes.NameIdentifier);
            var correo = identidad.FindFirst(ClaimTypes.Email);

            ViewBag.Autenticado = User.Identity.IsAuthenticated;
            ViewBag.Nombre = User.Identity.Name;
            ViewBag.GoogleId = googleId != null ? googleId.Value : "No encontrado";
            ViewBag.Correo = correo != null ? correo.Value : "No encontrado";

            return View();
        }
    }
}
