using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Evaluacion1_LoretoPradel.Services
{
    // Envía avisos (webhooks) a un servicio externo cuando pasa algo con una reserva
    public class WebhookService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhookService> _logger;

        public WebhookService(HttpClient httpClient, IConfiguration configuration, ILogger<WebhookService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // Arma el aviso, lo firma y lo envía. Si falla, reintenta hasta 3 veces.
        public async Task EnviarAsync(string evento, object datos)
        {
            var url = _configuration["Webhooks:UrlDestino"];
            var secreto = _configuration["Webhooks:SecretoSalida"]!;

            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Webhook {Evento} no enviado: no hay URL de destino configurada.", evento);
                return;
            }

            // Cuerpo del aviso: qué pasó, cuándo pasó y los datos de la reserva
            var cuerpo = JsonSerializer.Serialize(new
            {
                evento,
                fecha = DateTime.UtcNow,
                datos
            });

            var firma = CalcularFirma(cuerpo, secreto);

            for (int intento = 1; intento <= 3; intento++)
            {
                try
                {
                    var peticion = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
                    };
                    peticion.Headers.Add("X-Firma", firma);

                    var respuesta = await _httpClient.SendAsync(peticion);

                    if (respuesta.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Webhook {Evento} enviado correctamente en el intento {Intento}.", evento, intento);
                        return;
                    }

                    _logger.LogWarning("Webhook {Evento}: el intento {Intento} respondió con código {Codigo}.",
                        evento, intento, (int)respuesta.StatusCode);
                }
                catch (Exception ex)
                {
                    // Sin conexión, URL inválida o el servicio tardó demasiado en responder
                    _logger.LogWarning("Webhook {Evento}: el intento {Intento} falló. {Mensaje}", evento, intento, ex.Message);
                }

                // Espera antes del siguiente intento: 2 segundos, luego 4
                if (intento < 3)
                {
                    await Task.Delay(2000 * intento);
                }
            }

            _logger.LogError("Webhook {Evento} no se pudo enviar después de 3 intentos.", evento);
        }

        // Calcula la firma HMAC-SHA256 del cuerpo usando el secreto compartido
        private static string CalcularFirma(string cuerpo, string secreto)
        {
            var bytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secreto), Encoding.UTF8.GetBytes(cuerpo));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}