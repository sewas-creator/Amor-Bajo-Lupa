
using System;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using TestPatrones;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using TestPatrones.Services;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace TestPatrones.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult DiagnosticarMercadoPago()
        {
            string token = System.Configuration.ConfigurationManager
                .AppSettings["MercadoPagoAccessToken"];

            bool archivoEncontrado = System.IO.File.Exists(
                Server.MapPath("~/App_Data/GoogleAuth.secrets.config")
            );

            return Content(
                "Token encontrado: " + (!string.IsNullOrWhiteSpace(token)) +
                "\nLongitud del token: " + (token == null ? 0 : token.Length) +
                "\nArchivo encontrado: " + archivoEncontrado
            );
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarResultadoAvanzado(string respuestas)
        {
            try
            {
                // Comprobar que llegaron las 30 respuestas
                if (string.IsNullOrWhiteSpace(respuestas))
                {
                    return Json(new { ok = false, mensaje = "No se recibieron respuestas." });
                }

                string[] partes = respuestas.Split(',');

                if (partes.Length != 30)
                {
                    return Json(new { ok = false, mensaje = "El test debe contener 30 respuestas." });
                }

                int[] valores = new int[30];

                for (int i = 0; i < 30; i++)
                {
                    if (!int.TryParse(partes[i], out valores[i]) ||
                        (valores[i] != 0 && valores[i] != 2 && valores[i] != 5))
                    {
                        return Json(new { ok = false, mensaje = "Hay respuestas inválidas." });
                    }
                }

                // Pesos correspondientes a las 30 preguntas
                int[] pesos =
                {
            3, 2, 2, 2, 3, 2,
            3, 2, 2, 2, 2, 3,
            2, 2, 2, 2, 2, 3,
            2, 2, 2, 2, 2, 3,
            3, 3, 2, 2, 2, 2
        };

                // Categoría correspondiente a cada pregunta
                string[] categoriasPreguntas =
                {
            "Digital", "Digital", "Digital", "Digital", "Digital", "Digital",
            "Comunicación", "Comunicación", "Comunicación", "Comunicación", "Comunicación", "Comunicación",
            "Cambios", "Cambios", "Cambios", "Cambios", "Cambios", "Cambios",
            "Tiempo", "Tiempo", "Tiempo", "Tiempo", "Tiempo", "Tiempo",
            "Límites", "Límites", "Límites", "Límites", "Límites", "Límites"
        };

                int puntuacionTotal = 0;
                int puntuacionMaxima = 0;

                Dictionary<string, int> puntosCategoria =
                    new Dictionary<string, int>();

                Dictionary<string, int> maximosCategoria =
                    new Dictionary<string, int>();

                string[] categorias =
                {
            "Digital", "Comunicación", "Cambios", "Tiempo", "Límites"
        };

                foreach (string categoria in categorias)
                {
                    puntosCategoria[categoria] = 0;
                    maximosCategoria[categoria] = 0;
                }

                // Calcular puntuación general y por categoría
                for (int i = 0; i < 30; i++)
                {
                    int puntos = valores[i] * pesos[i];
                    int maximo = 5 * pesos[i];

                    puntuacionTotal += puntos;
                    puntuacionMaxima += maximo;

                    string categoria = categoriasPreguntas[i];

                    puntosCategoria[categoria] += puntos;
                    maximosCategoria[categoria] += maximo;
                }

                // Convertir la puntuación general a porcentaje
                int porcentaje = (int)Math.Round(
                    (double)puntuacionTotal / puntuacionMaxima * 100,
                    MidpointRounding.AwayFromZero
                );

                // Identificar la categoría con mayor porcentaje
                string categoriaPrincipal = "";
                double mayorPorcentaje = -1;

                foreach (string categoria in categorias)
                {
                    double porcentajeCategoria =
                        (double)puntosCategoria[categoria] /
                        maximosCategoria[categoria] * 100;

                    if (porcentajeCategoria > mayorPorcentaje)
                    {
                        mayorPorcentaje = porcentajeCategoria;
                        categoriaPrincipal = categoria;
                    }
                }

                // Obtener el Google ID del usuario autenticado
                var identidad = User.Identity as ClaimsIdentity;

                if (identidad == null)
                {
                    return Json(new
                    {
                        ok = false,
                        mensaje = "No se pudo obtener la identidad del usuario."
                    });
                }

                var googleId = identidad.FindFirst(ClaimTypes.NameIdentifier);

                if (googleId == null || string.IsNullOrWhiteSpace(googleId.Value))
                {
                    return Json(new
                    {
                        ok = false,
                        mensaje = "No se pudo obtener el Google ID."
                    });
                }

                // Guardar intento y resultado en una misma transacción
                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    using (var transaccion = conexion.BeginTransaction())
                    {
                        int idUsuario;

                        using (var buscarUsuario = new SqlCommand(
                            @"SELECT IdUsuario
                      FROM Usuarios
                      WHERE GoogleId = @GoogleId",
                            conexion,
                            transaccion))
                        {
                            buscarUsuario.Parameters.AddWithValue(
                                "@GoogleId", googleId.Value
                            );

                            object resultadoUsuario =
                                buscarUsuario.ExecuteScalar();

                            if (resultadoUsuario == null ||
                                resultadoUsuario == DBNull.Value)
                            {
                                throw new Exception(
                                    "El usuario de Google no está registrado en la base de datos."
                                );
                            }

                            idUsuario = Convert.ToInt32(resultadoUsuario);
                        }

                        int idIntento;

                        using (var insertarIntento = new SqlCommand(
                            @"INSERT INTO Intentos (IdTest, IdUsuario)
                      OUTPUT INSERTED.IdIntento
                      VALUES (3, @IdUsuario)",
                            conexion,
                            transaccion))
                        {
                            insertarIntento.Parameters.AddWithValue(
                                "@IdUsuario", idUsuario
                            );

                            idIntento = Convert.ToInt32(
                                insertarIntento.ExecuteScalar()
                            );
                        }

                        using (var insertarResultado = new SqlCommand(
                            @"INSERT INTO Resultados
                      (IdIntento, PuntuacionTotal, CategoriaPrincipal)
                      VALUES
                      (@IdIntento, @Puntuacion, @Categoria)",
                            conexion,
                            transaccion))
                        {
                            insertarResultado.Parameters.AddWithValue(
                                "@IdIntento", idIntento
                            );

                            insertarResultado.Parameters.AddWithValue(
                                "@Puntuacion", porcentaje
                            );

                            insertarResultado.Parameters.AddWithValue(
                                "@Categoria", categoriaPrincipal
                            );

                            insertarResultado.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                    }
                }

                return Json(new
                {
                    ok = true,
                    porcentaje = porcentaje,
                    categoriaPrincipal = categoriaPrincipal,
                    mensaje = "Resultado guardado correctamente."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    ok = false,
                    mensaje = "Error al guardar el resultado: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarResultado(int porcentaje, string categoriaPrincipal)
        {
            if (porcentaje < 0 || porcentaje > 100)
            {
                return Json(new { ok = false, mensaje = "Porcentaje no válido." });
            }

            string[] categoriasPermitidas = {
        "Digital", "Comunicación", "Cambios", "Tiempo", "Límites"
    };

            if (string.IsNullOrWhiteSpace(categoriaPrincipal) ||
                !categoriasPermitidas.Contains(categoriaPrincipal))
            {
                return Json(new { ok = false, mensaje = "Categoría no válida." });
            }

            try
            {
                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    using (var transaccion = conexion.BeginTransaction())
                    {
                        try
                        {
                            // Buscar el identificador del test
                            int idTest;

                            using (var comando = new System.Data.SqlClient.SqlCommand(
                                "SELECT IdTest FROM Tests WHERE Nombre = @Nombre",
                                conexion, transaccion))
                            {
                                comando.Parameters.AddWithValue(
                                    "@Nombre", "¿Qué está ocultando?");

                                object resultado = comando.ExecuteScalar();

                                if (resultado == null)
                                {
                                    throw new Exception(
                                        "No se encontró el test en la tabla Tests.");
                                }

                                idTest = Convert.ToInt32(resultado);
                            }

                            // Registrar el intento anónimo
                            int idIntento;

                            using (var comando = new System.Data.SqlClient.SqlCommand(
                                @"INSERT INTO Intentos (IdTest, IdUsuario)
                          OUTPUT INSERTED.IdIntento
                          VALUES (@IdTest, NULL)",
                                conexion, transaccion))
                            {
                                comando.Parameters.AddWithValue("@IdTest", idTest);

                                idIntento = Convert.ToInt32(
                                    comando.ExecuteScalar());
                            }

                            // Guardar el resultado
                            using (var comando = new System.Data.SqlClient.SqlCommand(
                                @"INSERT INTO Resultados
                          (IdIntento, PuntuacionTotal, CategoriaPrincipal)
                          VALUES (@IdIntento, @Puntuacion, @Categoria)",
                                conexion, transaccion))
                            {
                                comando.Parameters.AddWithValue("@IdIntento", idIntento);
                                comando.Parameters.AddWithValue("@Puntuacion", porcentaje);
                                comando.Parameters.AddWithValue("@Categoria", categoriaPrincipal);

                                comando.ExecuteNonQuery();
                            }

                            transaccion.Commit();
                        }
                        catch
                        {
                            transaccion.Rollback();
                            throw;
                        }
                    }
                }

                return Json(new
                {
                    ok = true,
                    mensaje = "Resultado guardado correctamente."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    ok = false,
                    mensaje = ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarResultadoPatrones(int porcentaje, string categoriaPrincipal)
        {
            if (porcentaje < 0 || porcentaje > 100)
                return Json(new { ok = false, mensaje = "Porcentaje no válido." });

            string[] categoriasPermitidas = {
        "Emociones", "Expectativas", "Hábitos"
    };

            if (string.IsNullOrWhiteSpace(categoriaPrincipal) ||
                !categoriasPermitidas.Contains(categoriaPrincipal))
            {
                return Json(new { ok = false, mensaje = "Categoría no válida." });
            }

            try
            {
                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    int idTest;

                    using (var comando = new SqlCommand(
                        "SELECT IdTest FROM Tests WHERE Nombre = @Nombre", conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@Nombre", "Descubre tus patrones");

                        object resultado = comando.ExecuteScalar();

                        if (resultado == null)
                            throw new Exception("No se encontró el segundo test en la tabla Tests.");

                        idTest = Convert.ToInt32(resultado);
                    }

                    int idIntento;

                    using (var comando = new SqlCommand(
                        @"INSERT INTO Intentos (IdTest, IdUsuario)
                  OUTPUT INSERTED.IdIntento
                  VALUES (@IdTest, NULL)", conexion))
                    {
                        comando.Parameters.AddWithValue("@IdTest", idTest);
                        idIntento = Convert.ToInt32(comando.ExecuteScalar());
                    }

                    using (var comando = new SqlCommand(
                        @"INSERT INTO Resultados
                  (IdIntento, PuntuacionTotal, CategoriaPrincipal)
                  VALUES (@IdIntento, @Puntuacion, @Categoria)", conexion))
                    {
                        comando.Parameters.AddWithValue("@IdIntento", idIntento);
                        comando.Parameters.AddWithValue("@Puntuacion", porcentaje);
                        comando.Parameters.AddWithValue("@Categoria", categoriaPrincipal);
                        comando.ExecuteNonQuery();
                    }
                }

                return Json(new
                {
                    ok = true,
                    mensaje = "Resultado guardado correctamente."
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }


        // Página principal de Amor Bajo Lupa
        public ActionResult Index()
        {
            return View();
        }

        // Artículo: autoconocimiento
        public ActionResult ArticuloConocerse()
        {
            return View();
        }

        // Artículo: comunicación en pareja
        public ActionResult ArticuloComunicacion()
        {
            return View();
        }

        // Artículo: reconocimiento de las emociones
        public ActionResult ArticuloEmociones()
        {
            return View();
        }

        public ActionResult ArticuloLimites()
        {
            return View();
        }

        // Test actual
        public ActionResult TestPatrones()
        {
            return View();
        }

        // Página del futuro test
        public ActionResult DescubrePatrones()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Información sobre Amor Bajo Lupa.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contacto.";
            return View();
        }

        [Authorize]
        public ActionResult TestAvanzado()
        {
            return View();
        }

        [Authorize]
        public ActionResult DescubrePatronesAvanzado()
        {
            return View();
        }


        [Authorize]
        public ActionResult TestPreparacionRelacion(bool vistaPrevia = false)
        {
            try
            {
                var identidad = User.Identity as ClaimsIdentity;

                var googleIdClaim =
                    identidad?.FindFirst(ClaimTypes.NameIdentifier);

                if (googleIdClaim == null ||
                    string.IsNullOrWhiteSpace(googleIdClaim.Value))
                {
                    return RedirectToAction("Acceso", "Cuenta");
                }

                // La vista previa solo se permite desde localhost.
                bool esVistaPrevia = vistaPrevia && Request.IsLocal;

                bool puedeIniciar = esVistaPrevia;

                // Si no es una vista previa, se comprueba la compra real.
                if (!esVistaPrevia)
                {
                    using (var conexion = ConexionBD.ObtenerConexion())
                    {
                        conexion.Open();

                        using (var comando = new SqlCommand(
                            @"SELECT COUNT(*)
                      FROM ComprasTestPremium C
                      INNER JOIN Usuarios U
                        ON U.IdUsuario = C.IdUsuario
                      WHERE U.GoogleId = @GoogleId
                        AND C.IdTest = 5
                        AND C.Estado = 'Aprobado'
                        AND C.Consumido = 0",
                            conexion))
                        {
                            comando.Parameters.AddWithValue(
                                "@GoogleId", googleIdClaim.Value);

                            puedeIniciar =
                                Convert.ToInt32(comando.ExecuteScalar()) > 0;
                        }
                    }
                }

                ViewBag.PuedeIniciar = puedeIniciar;
                ViewBag.VistaPrevia = esVistaPrevia;

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error comprobando acceso al test premium: " + ex);

                ViewBag.PuedeIniciar = false;
                ViewBag.VistaPrevia = false;

                TempData["ErrorPago"] =
                    "No se pudo comprobar tu acceso. Inténtalo nuevamente.";

                return View();
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarResultadoTestPremium(string respuestas)
        {
            SqlTransaction transaccion = null;

            try
            {
                // Comprobar la identidad de Google.
                var identidad = User.Identity as ClaimsIdentity;

                var googleIdClaim =
                    identidad?.FindFirst(ClaimTypes.NameIdentifier);

                if (googleIdClaim == null ||
                    string.IsNullOrWhiteSpace(googleIdClaim.Value))
                {
                    Response.StatusCode = 401;

                    return Json(new
                    {
                        ok = false,
                        mensaje = "Debes iniciar sesión para guardar el resultado."
                    });
                }

                // Validar que lleguen exactamente 10 respuestas.
                string[] partes = (respuestas ?? "").Split(',');

                if (partes.Length != 10)
                {
                    Response.StatusCode = 400;

                    return Json(new
                    {
                        ok = false,
                        mensaje = "La cantidad de respuestas no es válida."
                    });
                }

                int[] valores = new int[10];

                for (int i = 0; i < partes.Length; i++)
                {
                    if (!int.TryParse(
                        partes[i],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out valores[i]) ||
                        valores[i] < 1 ||
                        valores[i] > 5)
                    {
                        Response.StatusCode = 400;

                        return Json(new
                        {
                            ok = false,
                            mensaje = "Hay respuestas inválidas. Revisa el test."
                        });
                    }
                }

                // Categorías de las 10 preguntas.
                string[] categoriasPorPregunta =
                {
            "Bienestar emocional",
            "Bienestar emocional",
            "Experiencias pasadas",
            "Experiencias pasadas",
            "Amor y apego",
            "Amor y apego",
            "Autonomía personal",
            "Autonomía personal",
            "Preparación emocional",
            "Preparación emocional"
        };

                // Las preguntas negativas se invierten al calcular la puntuación.
                bool[] preguntasPositivas =
                {
            true, false, true, false, true,
            false, true, false, true, true
        };

                string[] nombresCategorias =
                {
            "Bienestar emocional",
            "Experiencias pasadas",
            "Amor y apego",
            "Autonomía personal",
            "Preparación emocional"
        };

                // Calcular los porcentajes en el servidor.
                var puntuaciones = new Dictionary<string, List<int>>();

                foreach (string nombre in nombresCategorias)
                {
                    puntuaciones[nombre] = new List<int>();
                }

                for (int i = 0; i < valores.Length; i++)
                {
                    int puntuacion = preguntasPositivas[i]
                        ? valores[i]
                        : 6 - valores[i];

                    puntuaciones[categoriasPorPregunta[i]].Add(puntuacion);
                }

                var porcentajes = new Dictionary<string, int>();

                foreach (string nombre in nombresCategorias)
                {
                    double promedio = puntuaciones[nombre].Average();

                    int porcentaje = (int)Math.Round(
                        ((promedio - 1) / 4.0) * 100,
                        MidpointRounding.AwayFromZero);

                    porcentajes[nombre] = porcentaje;
                }

                int porcentajeTotal = (int)Math.Round(
                    porcentajes.Values.Average(),
                    MidpointRounding.AwayFromZero);

                // La categoría principal es aquella que necesita más atención.
                string categoriaPrincipal = nombresCategorias
                    .OrderBy(nombre => porcentajes[nombre])
                    .First();

                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    // Buscar al usuario autenticado.
                    int idUsuario;

                    using (var comandoUsuario = new SqlCommand(
                        @"SELECT IdUsuario
                  FROM Usuarios
                  WHERE GoogleId = @GoogleId",
                        conexion))
                    {
                        comandoUsuario.Parameters.AddWithValue(
                            "@GoogleId", googleIdClaim.Value);

                        object resultadoUsuario =
                            comandoUsuario.ExecuteScalar();

                        if (resultadoUsuario == null ||
                            resultadoUsuario == DBNull.Value)
                        {
                            Response.StatusCode = 400;

                            return Json(new
                            {
                                ok = false,
                                mensaje = "No se encontró tu cuenta de usuario."
                            });
                        }

                        idUsuario = Convert.ToInt32(resultadoUsuario);
                    }

                    // La transacción protege el intento y la compra.
                    transaccion = conexion.BeginTransaction(
                        IsolationLevel.Serializable);

                    // Bloquear una compra aprobada que todavía no se haya utilizado.
                    int idCompra;

                    using (var comandoCompra = new SqlCommand(
                        @"SELECT TOP (1) IdCompra
                  FROM ComprasTestPremium WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                  WHERE IdUsuario = @IdUsuario
                    AND IdTest = 5
                    AND Estado = 'Aprobado'
                    AND Consumido = 0
                  ORDER BY FechaAprobacion DESC, IdCompra DESC",
                        conexion,
                        transaccion))
                    {
                        comandoCompra.Parameters.AddWithValue(
                            "@IdUsuario", idUsuario);

                        object resultadoCompra =
                            comandoCompra.ExecuteScalar();

                        if (resultadoCompra == null ||
                            resultadoCompra == DBNull.Value)
                        {
                            transaccion.Rollback();

                            Response.StatusCode = 403;

                            return Json(new
                            {
                                ok = false,
                                mensaje = "No tienes una compra aprobada disponible para este test."
                            });
                        }

                        idCompra = Convert.ToInt32(resultadoCompra);
                    }

                    // Crear el intento del quinto test.
                    int idIntento;

                    using (var comandoIntento = new SqlCommand(
                        @"INSERT INTO Intentos
                    (IdTest, IdUsuario, Fecha)
                  OUTPUT INSERTED.IdIntento
                  VALUES
                    (5, @IdUsuario, GETDATE())",
                        conexion,
                        transaccion))
                    {
                        comandoIntento.Parameters.AddWithValue(
                            "@IdUsuario", idUsuario);

                        idIntento = Convert.ToInt32(
                            comandoIntento.ExecuteScalar());
                    }

                    // Guardar el resultado calculado por el servidor.
                    using (var comandoResultado = new SqlCommand(
                        @"INSERT INTO Resultados
                    (IdIntento, PuntuacionTotal, CategoriaPrincipal, FechaGeneracion)
                  VALUES
                    (@IdIntento, @PuntuacionTotal, @CategoriaPrincipal, GETDATE())",
                        conexion,
                        transaccion))
                    {
                        comandoResultado.Parameters.AddWithValue(
                            "@IdIntento", idIntento);

                        comandoResultado.Parameters.AddWithValue(
                            "@PuntuacionTotal", porcentajeTotal);

                        comandoResultado.Parameters.AddWithValue(
                            "@CategoriaPrincipal", categoriaPrincipal);

                        comandoResultado.ExecuteNonQuery();
                    }

                    // Marcar la compra como consumida.
                    using (var comandoActualizar = new SqlCommand(
                        @"UPDATE ComprasTestPremium
                  SET Consumido = 1,
                      IdIntento = @IdIntento,
                      FechaConsumo = GETDATE()
                  WHERE IdCompra = @IdCompra
                    AND Estado = 'Aprobado'
                    AND Consumido = 0",
                        conexion,
                        transaccion))
                    {
                        comandoActualizar.Parameters.AddWithValue(
                            "@IdIntento", idIntento);

                        comandoActualizar.Parameters.AddWithValue(
                            "@IdCompra", idCompra);

                        int filasActualizadas =
                            comandoActualizar.ExecuteNonQuery();

                        if (filasActualizadas != 1)
                        {
                            throw new Exception(
                                "No se pudo confirmar el consumo de la compra.");
                        }
                    }

                    // Confirmar todas las operaciones juntas.
                    transaccion.Commit();
                    transaccion = null;
                }

                return Json(new
                {
                    ok = true,
                    mensaje = "Tu resultado se guardó correctamente.",
                    porcentaje = porcentajeTotal,
                    categoriaPrincipal = categoriaPrincipal
                });
            }
            catch (Exception ex)
            {
                if (transaccion != null)
                {
                    try
                    {
                        transaccion.Rollback();
                    }
                    catch
                    {
                        // Se conserva el error original.
                    }
                }

                System.Diagnostics.Trace.TraceError(
                    "Error guardando resultado del test premium: " + ex);

                Response.StatusCode = 500;

                return Json(new
                {
                    ok = false,
                    mensaje = "No se pudo guardar tu resultado. Inténtalo nuevamente."
                });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearOrdenTestPremium()
        {
            try
            {
                var identidad = User.Identity as ClaimsIdentity;

                if (identidad == null)
                    return RedirectToAction("Acceso", "Cuenta");

                var googleIdClaim = identidad.FindFirst(
                    ClaimTypes.NameIdentifier);

                var correoClaim = identidad.FindFirst(
                    ClaimTypes.Email);

                if (googleIdClaim == null ||
                    correoClaim == null ||
                    string.IsNullOrWhiteSpace(googleIdClaim.Value) ||
                    string.IsNullOrWhiteSpace(correoClaim.Value))
                {
                    TempData["ErrorPago"] =
                        "No se pudieron obtener los datos de tu cuenta de Google.";

                    return RedirectToAction("TestPreparacionRelacion");
                }

                int idUsuario;
                int idCompra;
                string referenciaExterna;
                string claveIdempotencia;

                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    using (var transaccion = conexion.BeginTransaction())
                    {
                        try
                        {
                            using (var comandoUsuario = new SqlCommand(
                                @"SELECT IdUsuario
                          FROM Usuarios
                          WHERE GoogleId = @GoogleId",
                                conexion, transaccion))
                            {
                                comandoUsuario.Parameters.AddWithValue(
                                    "@GoogleId", googleIdClaim.Value);

                                object resultado = comandoUsuario.ExecuteScalar();

                                if (resultado == null ||
                                    resultado == DBNull.Value)
                                {
                                    throw new Exception(
                                        "No se encontró tu usuario registrado.");
                                }

                                idUsuario = Convert.ToInt32(resultado);
                            }

                            // Comprobar si ya tiene una compra aprobada
                            // y todavía no consumida.
                            using (var comandoAprobado = new SqlCommand(
                                @"SELECT COUNT(*)
                          FROM ComprasTestPremium
                          WHERE IdUsuario = @IdUsuario
                            AND IdTest = 5
                            AND Estado = 'Aprobado'
                            AND Consumido = 0",
                                conexion, transaccion))
                            {
                                comandoAprobado.Parameters.AddWithValue(
                                    "@IdUsuario", idUsuario);

                                int comprasDisponibles = Convert.ToInt32(
                                    comandoAprobado.ExecuteScalar());

                                if (comprasDisponibles > 0)
                                {
                                    transaccion.Commit();

                                    return RedirectToAction(
                                        "TestPreparacionRelacion");
                                }
                            }

                            // Reutilizar una compra pendiente si existe.
                            using (var comandoPendiente = new SqlCommand(
                                @"SELECT TOP 1
                              IdCompra,
                              ReferenciaExterna,
                              CONVERT(VARCHAR(36), ClaveIdempotencia)
                          FROM ComprasTestPremium
                          WHERE IdUsuario = @IdUsuario
                            AND IdTest = 5
                            AND Estado = 'Pendiente'
                            AND Consumido = 0
                          ORDER BY FechaCreacion DESC",
                                conexion, transaccion))
                            {
                                comandoPendiente.Parameters.AddWithValue(
                                    "@IdUsuario", idUsuario);

                                using (var lector = comandoPendiente.ExecuteReader())
                                {
                                    if (lector.Read())
                                    {
                                        idCompra = Convert.ToInt32(lector[0]);
                                        referenciaExterna = lector[1].ToString();
                                        claveIdempotencia = lector[2].ToString();
                                    }
                                    else
                                    {
                                        idCompra = 0;
                                        referenciaExterna = null;
                                        claveIdempotencia = null;
                                    }
                                }
                            }

                            // Crear una nueva compra si no existe una pendiente.
                            if (idCompra == 0)
                            {
                                referenciaExterna = Guid.NewGuid().ToString("N");
                                claveIdempotencia = Guid.NewGuid().ToString();

                                using (var comandoCompra = new SqlCommand(
                                    @"INSERT INTO ComprasTestPremium
                              (
                                  IdUsuario,
                                  IdTest,
                                  ReferenciaExterna,
                                  ClaveIdempotencia,
                                  Monto,
                                  Estado,
                                  Consumido
                              )
                              VALUES
                              (
                                  @IdUsuario,
                                  5,
                                  @Referencia,
                                  @Clave,
                                  3.00,
                                  'Pendiente',
                                  0
                              );

                              SELECT CAST(SCOPE_IDENTITY() AS INT);",
                                    conexion, transaccion))
                                {
                                    comandoCompra.Parameters.AddWithValue(
                                        "@IdUsuario", idUsuario);

                                    comandoCompra.Parameters.AddWithValue(
                                        "@Referencia", referenciaExterna);

                                    comandoCompra.Parameters.AddWithValue(
                                        "@Clave", Guid.Parse(claveIdempotencia));

                                    idCompra = Convert.ToInt32(
                                        comandoCompra.ExecuteScalar());
                                }
                            }

                            transaccion.Commit();
                        }
                        catch
                        {
                            transaccion.Rollback();
                            throw;
                        }
                    }
                }

                // Crear o recuperar la orden utilizando la misma clave
                // de idempotencia de esta compra.
                string urlRetorno = Url.Action(
    "PagoTestPremium",
    "Home",
    null,
    Request.Url.Scheme
);

                MercadoPagoOrderResponse orden =
                    await MercadoPagoService.CrearOrdenAsync(
                        correoClaim.Value,
                        referenciaExterna,
                        claveIdempotencia,
                        urlRetorno);

                // Guardar el identificador de la orden.
                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    using (var comando = new SqlCommand(
                        @"UPDATE ComprasTestPremium
                  SET MercadoPagoOrderId = @OrderId
                  WHERE IdCompra = @IdCompra
                    AND IdUsuario = @IdUsuario
                    AND Estado = 'Pendiente'",
                        conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@OrderId", orden.OrderId);

                        comando.Parameters.AddWithValue(
                            "@IdCompra", idCompra);

                        comando.Parameters.AddWithValue(
                            "@IdUsuario", idUsuario);

                        comando.ExecuteNonQuery();
                    }
                }

                return Redirect(orden.CheckoutUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al crear orden premium: " + ex);

                TempData["ErrorPago"] =
                    "No se pudo iniciar el pago. Inténtalo nuevamente.";

                return RedirectToAction("TestPreparacionRelacion");
            }
        }

        [Authorize]
        public async Task<ActionResult> PagoTestPremium(string order_id)
        {
            try
            {
                var identidad = User.Identity as ClaimsIdentity;

                if (identidad == null)
                    return RedirectToAction("Acceso", "Cuenta");

                var googleIdClaim =
                    identidad.FindFirst(ClaimTypes.NameIdentifier);

                if (googleIdClaim == null ||
                    string.IsNullOrWhiteSpace(googleIdClaim.Value) ||
                    string.IsNullOrWhiteSpace(order_id))
                {
                    TempData["ErrorPago"] =
                        "No se pudo identificar la compra.";
                    return RedirectToAction("TestPreparacionRelacion");
                }

                int idCompra;
                int idUsuario;
                string referenciaExterna;
                string orderGuardada;
                decimal monto;
                string estadoCompra;
                bool consumido;

                // Buscar la compra asociada al usuario autenticado.
                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    using (var comando = new SqlCommand(
                        @"SELECT C.IdCompra, C.IdUsuario,
                         C.ReferenciaExterna, C.MercadoPagoOrderId,
                         C.Monto, C.Estado, C.Consumido
                  FROM ComprasTestPremium C
                  INNER JOIN Usuarios U
                    ON U.IdUsuario = C.IdUsuario
                  WHERE U.GoogleId = @GoogleId
                    AND C.IdTest = 5
                    AND C.MercadoPagoOrderId = @OrderId",
                        conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@GoogleId", googleIdClaim.Value);

                        comando.Parameters.AddWithValue(
                            "@OrderId", order_id);

                        using (var lector = comando.ExecuteReader())
                        {
                            if (!lector.Read())
                            {
                                TempData["ErrorPago"] =
                                    "No encontramos una compra asociada a tu cuenta.";
                                return RedirectToAction(
                                    "TestPreparacionRelacion");
                            }

                            idCompra = Convert.ToInt32(lector["IdCompra"]);
                            idUsuario = Convert.ToInt32(lector["IdUsuario"]);
                            referenciaExterna =
                                lector["ReferenciaExterna"].ToString();
                            orderGuardada =
                                lector["MercadoPagoOrderId"].ToString();
                            monto = Convert.ToDecimal(lector["Monto"]);
                            estadoCompra = lector["Estado"].ToString();
                            consumido = Convert.ToBoolean(lector["Consumido"]);
                        }
                    }
                }

                if (consumido)
                {
                    TempData["ErrorPago"] =
                        "Esta compra ya fue utilizada.";
                    return RedirectToAction("TestPreparacionRelacion");
                }

                // Consultar directamente la orden en Mercado Pago.
                JObject orden =
                    await MercadoPagoService.ConsultarOrdenAsync(order_id);

                string idApi = (string)orden["id"];
                string referenciaApi =
                    (string)orden["external_reference"];
                string moneda = (string)orden["currency"];

                decimal montoApi;
                decimal montoPagado;

                bool montoValido =
                    decimal.TryParse(
                        (string)orden["total_amount"],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out montoApi);

                bool pagoTotalValido =
                    decimal.TryParse(
                        (string)orden["total_paid_amount"],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out montoPagado);

                JArray pagos =
                    orden["transactions"]?["payments"] as JArray;

                bool pagoAcreditado =
                    pagos != null &&
                    pagos.Any(p =>
                        (string)p["status"] == "processed" &&
                        (string)p["status_detail"] == "accredited");

                // Comprobar identidad de la orden, referencia,
                // importe, moneda y estado del pago.
                bool compraValida =
                    idApi == orderGuardada &&
                    idApi == order_id &&
                    referenciaApi == referenciaExterna &&
                    monto == 3.00m &&
                    montoValido &&
                    montoApi == 3.00m &&
                    pagoTotalValido &&
                    montoPagado >= 3.00m &&
                    moneda == "PEN" &&
                    pagoAcreditado;

                if (!compraValida)
                {
                    TempData["ErrorPago"] =
                        "El pago todavía no está aprobado o no se pudo verificar. " +
                        "Si acabas de pagar, espera un momento e inténtalo nuevamente.";

                    return RedirectToAction("TestPreparacionRelacion");
                }

                // Aprobar la compra únicamente después de verificarla.
                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    using (var comando = new SqlCommand(
                        @"UPDATE ComprasTestPremium
                  SET Estado = 'Aprobado',
                      FechaAprobacion = ISNULL(
                          FechaAprobacion, GETDATE())
                  WHERE IdCompra = @IdCompra
                    AND IdUsuario = @IdUsuario
                    AND IdTest = 5
                    AND ReferenciaExterna = @Referencia
                    AND MercadoPagoOrderId = @OrderId
                    AND Consumido = 0
                    AND Estado IN ('Pendiente', 'Aprobado')",
                        conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@IdCompra", idCompra);

                        comando.Parameters.AddWithValue(
                            "@IdUsuario", idUsuario);

                        comando.Parameters.AddWithValue(
                            "@Referencia", referenciaExterna);

                        comando.Parameters.AddWithValue(
                            "@OrderId", orderGuardada);

                        int filas = comando.ExecuteNonQuery();

                        if (filas == 0)
                        {
                            TempData["ErrorPago"] =
                                "No se pudo actualizar el estado de tu compra.";
                            return RedirectToAction(
                                "TestPreparacionRelacion");
                        }
                    }
                }

                TempData["PagoExitoso"] =
                    "¡Pago verificado! Ya puedes realizar tu test premium.";

                return RedirectToAction("TestPreparacionRelacion");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error verificando pago premium: " + ex);

                TempData["ErrorPago"] =
                    "Ocurrió un problema al verificar el pago.";

                return RedirectToAction("TestPreparacionRelacion");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarResultadoPatronesAvanzado(string respuestas)
        {
            if (string.IsNullOrWhiteSpace(respuestas))
            {
                return Json(new
                {
                    ok = false,
                    mensaje = "No se recibieron las respuestas."
                });
            }

            string[] valores = respuestas.Split(',');

            if (valores.Length != 30)
            {
                return Json(new
                {
                    ok = false,
                    mensaje = "El test debe contener exactamente 30 respuestas."
                });
            }

            int[] puntuaciones = new int[30];

            for (int i = 0; i < valores.Length; i++)
            {
                int valor;

                if (!int.TryParse(valores[i], out valor) || valor < 0 || valor > 3)
                {
                    return Json(new
                    {
                        ok = false,
                        mensaje = "Una o más respuestas no son válidas."
                    });
                }

                puntuaciones[i] = valor;
            }

            // Categorías: seis preguntas por cada una.
            string[] categorias =
            {
        "Emociones",
        "Expectativas",
        "Hábitos",
        "Comunicación",
        "Autonomía y límites"
    };

            int[] sumas = new int[5];

            for (int i = 0; i < puntuaciones.Length; i++)
            {
                int indiceCategoria = i / 6;
                sumas[indiceCategoria] += puntuaciones[i];
            }

            int total = 0;

            foreach (int puntuacion in puntuaciones)
            {
                total += puntuacion;
            }

            // Máximo posible: 30 preguntas x 3 puntos = 90.
            int porcentaje = (int)Math.Round((total / 90.0) * 100);

            int categoriaMayor = 0;

            for (int i = 1; i < sumas.Length; i++)
            {
                if (sumas[i] > sumas[categoriaMayor])
                {
                    categoriaMayor = i;
                }
            }

            string categoriaPrincipal = categorias[categoriaMayor];

            // Obtener el identificador de Google del usuario autenticado.
            var identidad = User.Identity as ClaimsIdentity;

            string googleId = identidad.FindFirst(
                ClaimTypes.NameIdentifier).Value;

            using (SqlConnection conexion = ConexionBD.ObtenerConexion())
            {
                conexion.Open();

                SqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    int idUsuario;

                    // Buscar el usuario registrado.
                    using (SqlCommand comandoUsuario = new SqlCommand(
                        "SELECT IdUsuario FROM Usuarios WHERE GoogleId = @GoogleId",
                        conexion, transaccion))
                    {
                        comandoUsuario.Parameters.AddWithValue("@GoogleId", googleId);

                        object resultadoUsuario = comandoUsuario.ExecuteScalar();

                        if (resultadoUsuario == null)
                        {
                            transaccion.Rollback();

                            return Json(new
                            {
                                ok = false,
                                mensaje = "No se encontró tu cuenta registrada."
                            });
                        }

                        idUsuario = Convert.ToInt32(resultadoUsuario);
                    }

                    int idIntento;

                    // Guardar el intento del test número 4.
                    using (SqlCommand comandoIntento = new SqlCommand(
                        @"INSERT INTO Intentos (IdTest, IdUsuario)
                  VALUES (4, @IdUsuario);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        conexion, transaccion))
                    {
                        comandoIntento.Parameters.AddWithValue(
                            "@IdUsuario", idUsuario);

                        idIntento = Convert.ToInt32(
                            comandoIntento.ExecuteScalar());
                    }

                    // Guardar el resultado asociado al intento.
                    using (SqlCommand comandoResultado = new SqlCommand(
                        @"INSERT INTO Resultados
                  (IdIntento, PuntuacionTotal, CategoriaPrincipal)
                  VALUES
                  (@IdIntento, @PuntuacionTotal, @CategoriaPrincipal)",
                        conexion, transaccion))
                    {
                        comandoResultado.Parameters.AddWithValue(
                            "@IdIntento", idIntento);

                        comandoResultado.Parameters.AddWithValue(
                            "@PuntuacionTotal", porcentaje);

                        comandoResultado.Parameters.AddWithValue(
                            "@CategoriaPrincipal", categoriaPrincipal);

                        comandoResultado.ExecuteNonQuery();
                    }

                    transaccion.Commit();

                    return Json(new
                    {
                        ok = true,
                        porcentaje = porcentaje,
                        categoriaPrincipal = categoriaPrincipal,
                        mensaje = "Tu resultado se guardó correctamente."
                    });
                }
                catch (Exception)
                {
                    transaccion.Rollback();

                    return Json(new
                    {
                        ok = false,
                        mensaje = "Ocurrió un error al guardar tu resultado."
                    });
                }
            }
        }


    }
}
