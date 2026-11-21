using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Evaluacion1_LoretoPradel.Data;
using Evaluacion1_LoretoPradel.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Evaluacion1_LoretoPradel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // La pasarela no usa JWT: su origen se valida con la firma HMAC
    public class WebhooksController : ControllerBase
    {
        private readonly ReservasContext _context;
        private readonly IHubContext<ReservasHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            ReservasContext context,
            IHubContext<ReservasHub> hubContext,
            IConfiguration configuration,
            ILogger<WebhooksController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
        }

        // POST: api/Webhooks/pagos
        // Recibe el aviso de la pasarela de pagos cuando un paciente paga su hora
        [HttpPost("pagos")]
        public async Task<IActionResult> RecibirPago()
        {
            // 1. Leer el cuerpo tal como llegó, porque la firma se calcula sobre ese texto exacto
            using var lector = new StreamReader(Request.Body);
            var cuerpo = await lector.ReadToEndAsync();

            // 2. Validar el origen: la firma recibida debe ser igual a la calculada con el secreto
            var firmaRecibida = Request.Headers["X-Firma"].ToString();
            var secreto = _configuration["Webhooks:SecretoPagos"]!;
            var firmaEsperada = CalcularFirma(cuerpo, secreto);

            if (firmaRecibida != firmaEsperada)
            {
                _logger.LogWarning("Webhook de pagos rechazado: la firma no es válida.");
                return Unauthorized();
            }

            // 3. Convertir el JSON recibido en un objeto
            PagoWebhookDto? pago;
            try
            {
                pago = JsonSerializer.Deserialize<PagoWebhookDto>(cuerpo,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                _logger.LogWarning("Webhook de pagos rechazado: el JSON no tiene el formato esperado.");
                return BadRequest();
            }

            if (pago == null)
            {
                return BadRequest();
            }

            _logger.LogInformation("Webhook de pagos recibido: evento {Evento}, reserva {ReservaId}, monto {Monto}.",
                pago.Evento, pago.ReservaId, pago.Monto);

            // Solo se procesa el pago aprobado; otros eventos se aceptan con 200 para que la pasarela no reintente
            if (pago.Evento != "pago.aprobado")
            {
                return Ok();
            }

            var reserva = await _context.Reserva.FindAsync(pago.ReservaId);

            if (reserva == null)
            {
                _logger.LogWarning("Webhook de pagos: la reserva {ReservaId} no existe.", pago.ReservaId);
                return NotFound();
            }

            // 4. Si la pasarela reintenta el mismo aviso, la reserva ya está confirmada y no se repite nada
            if (reserva.Estado == "Confirmada")
            {
                _logger.LogInformation("Webhook de pagos: la reserva {ReservaId} ya estaba confirmada. Aviso repetido, sin cambios.",
                    reserva.Id);
                return Ok();
            }

            reserva.Estado = "Confirmada";
            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook de pagos: la reserva {ReservaId} quedó confirmada.", reserva.Id);

            // 5. Avisar al panel por SignalR (el resumen SSE se actualiza solo al leer la base de datos)
            await _hubContext.Clients.All.SendAsync("EstadoActualizado", reserva.Id, reserva.Estado, "Pasarela de pagos");

            return Ok();
        }

        // Calcula la firma HMAC-SHA256 del cuerpo usando el secreto compartido
        private static string CalcularFirma(string cuerpo, string secreto)
        {
            var bytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secreto), Encoding.UTF8.GetBytes(cuerpo));
            return Convert.ToHexString(bytes).ToLower();
        }
    }

    // Datos que envía la pasarela de pagos
    public record PagoWebhookDto(string Evento, int ReservaId, decimal Monto);
}