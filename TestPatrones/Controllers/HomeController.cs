
using System;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using TestPatrones;
using System.Linq;
using System.Data;

namespace TestPatrones.Controllers
{
    public class HomeController : Controller
    {
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

        // Comprobar conexión con SQL Server
        public ActionResult ProbarConexion()
        {
            try
            {
                using (var conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();
                }

                return Content("¡Conexión a AmorBajoLupa exitosa!");
            }
            catch (Exception ex)
            {
                return Content("Error de conexión: " + ex.Message);
            }
        }

        // Página principal de Amor Bajo Lupa
        public ActionResult Index()
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
