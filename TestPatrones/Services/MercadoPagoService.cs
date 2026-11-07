using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestPatrones.Services
{
    public class MercadoPagoOrderResponse
    {
        public string OrderId { get; set; }
        public string CheckoutUrl { get; set; }
    }

    public static class MercadoPagoService
    {
        private static readonly HttpClient cliente = new HttpClient();

        public static async Task<MercadoPagoOrderResponse> CrearOrdenAsync(
    string correo,
    string referenciaExterna,
    string claveIdempotencia,
    string urlRetorno)

        {
            string accessToken =
                ConfigurationManager.AppSettings["MercadoPagoAccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception(
                    "No se encontró MercadoPagoAccessToken en la configuración.");

            if (string.IsNullOrWhiteSpace(correo))
                throw new Exception("No se recibió el correo del usuario.");

            if (string.IsNullOrWhiteSpace(referenciaExterna))
                throw new Exception("La referencia de la compra es obligatoria.");

            if (string.IsNullOrWhiteSpace(claveIdempotencia))
                throw new Exception("La clave de idempotencia es obligatoria.");

            if (string.IsNullOrWhiteSpace(urlRetorno))
                throw new Exception("La URL de retorno es obligatoria.");

            var datosOrden = new
            {
                type = "online",
                processing_mode = "manual",
                total_amount = "3.00",
                external_reference = referenciaExterna,
                description = "Test premium - Amor Bajo Lupa",
                payer = new
                {
                    email = correo
                },
                config = new
                {
                    online = new
                    {
                        success_url = urlRetorno,
                        failure_url = urlRetorno,
                        pending_url = urlRetorno,
                        auto_return = "approved"
                    }
                },

                items = new[]
                {
                    new
                    {
                        title = "Test: ¿Estás listo realmente para una relación?",
                        quantity = 1,
                        unit_price = "3.00"
                    }
                }
            };

            string json = JsonConvert.SerializeObject(datosOrden);

            using (var solicitud = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.mercadopago.com/v1/orders"))
            {
                solicitud.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                solicitud.Headers.Add(
                    "X-Idempotency-Key",
                    claveIdempotencia
                );

                solicitud.Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                using (HttpResponseMessage respuesta =
                    await cliente.SendAsync(solicitud))
                {
                    string contenido =
                        await respuesta.Content.ReadAsStringAsync();

                    if (!respuesta.IsSuccessStatusCode)
                    {
                        throw new Exception(
                            "Mercado Pago rechazó la creación de la orden. " +
                            "HTTP " + (int)respuesta.StatusCode + ": " +
                            contenido
                        );
                    }

                    JObject resultado = JObject.Parse(contenido);

                    string orderId = (string)resultado["id"];
                    string checkoutUrl = (string)resultado["checkout_url"];

                    if (string.IsNullOrWhiteSpace(orderId) ||
                        string.IsNullOrWhiteSpace(checkoutUrl))
                    {
                        throw new Exception(
                            "Mercado Pago no devolvió el ID o la URL de pago."
                        );
                    }

                    return new MercadoPagoOrderResponse
                    {
                        OrderId = orderId,
                        CheckoutUrl = checkoutUrl
                    };
                }
            }
        }

        public static async Task<JObject> ConsultarOrdenAsync(string orderId)
        {
            string accessToken =
                ConfigurationManager.AppSettings["MercadoPagoAccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("No se encontró MercadoPagoAccessToken.");

            if (string.IsNullOrWhiteSpace(orderId))
                throw new Exception("El ID de la orden es obligatorio.");

            using (var solicitud = new HttpRequestMessage(
                HttpMethod.Get,
                "https://api.mercadopago.com/v1/orders/" +
                Uri.EscapeDataString(orderId)))
            {
                solicitud.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                using (HttpResponseMessage respuesta =
                    await cliente.SendAsync(solicitud))
                {
                    string contenido =
                        await respuesta.Content.ReadAsStringAsync();

                    if (!respuesta.IsSuccessStatusCode)
                    {
                        throw new Exception(
                            "No se pudo consultar la orden. HTTP " +
                            (int)respuesta.StatusCode);
                    }

                    return JObject.Parse(contenido);
                }
            }
        }

    }
}