using System.Text.Json;
using Evaluacion1_LoretoPradel.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evaluacion1_LoretoPradel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventosController : ControllerBase
    {
        private readonly ReservasContext _context;

        public EventosController(ReservasContext context)
        {
            _context = context;
        }

        // GET: api/Eventos/resumen
        // Deja la conexión abierta y envía el resumen de reservas con Server-Sent Events
        [HttpGet("resumen")]
        public async Task GetResumen(CancellationToken cancellationToken)
        {
            // Indica al navegador que esta respuesta es un flujo de eventos SSE
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");

            int numeroEvento = 0;
            string ultimoResumen = "";

            try
            {
                // Si la conexión se corta, el navegador espera 5 segundos y se reconecta solo
                await Response.WriteAsync("retry: 5000\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Consulta la base de datos para armar el resumen actual
                    var resumen = new
                    {
                        pendientes = await _context.Reserva.CountAsync(r => r.Estado == "Pendiente", cancellationToken),
                        confirmadas = await _context.Reserva.CountAsync(r => r.Estado == "Confirmada", cancellationToken),
                        canceladas = await _context.Reserva.CountAsync(r => r.Estado == "Cancelada", cancellationToken),
                        total = await _context.Reserva.CountAsync(cancellationToken)
                    };

                    var json = JsonSerializer.Serialize(resumen);

                    // Solo envía un evento si el resumen cambió desde el último envío
                    if (json != ultimoResumen)
                    {
                        numeroEvento++;

                        await Response.WriteAsync($"id: {numeroEvento}\n", cancellationToken);
                        await Response.WriteAsync("event: resumen\n", cancellationToken);
                        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);

                        ultimoResumen = json;
                    }

                    // Espera 3 segundos antes de volver a revisar la base de datos
                    await Task.Delay(3000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // El cliente cerró la conexión (cerró la pestaña o cerró sesión). No es un error.
            }
        }
    }
}