using Evaluacion1_LoretoPradel.Data;
using Evaluacion1_LoretoPradel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Evaluacion1_LoretoPradel.Hubs
{
    [Authorize]
    public class ReservasHub : Hub
    {
        private readonly ReservasContext _context;
        private readonly WebhookService _webhookService;

        public ReservasHub(ReservasContext context, WebhookService webhookService)
        {
            _context = context;
            _webhookService = webhookService;
        }

        public async Task CambiarEstado(int reservaId, string nuevoEstado)
        {
            var estadosValidos = new[] { "Pendiente", "Confirmada", "Cancelada" };

            if (!estadosValidos.Contains(nuevoEstado))
            {
                await Clients.Caller.SendAsync("ErrorReserva", $"El estado '{nuevoEstado}' no es válido.");
                return;
            }

            var reserva = await _context.Reserva.FindAsync(reservaId);

            if (reserva == null)
            {
                await Clients.Caller.SendAsync("ErrorReserva", $"La reserva {reservaId} no existe.");
                return;
            }

            reserva.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            var usuario = Context.User?.Identity?.Name ?? "Desconocido";

            await Clients.All.SendAsync("EstadoActualizado", reservaId, nuevoEstado, usuario);

            // Avisa al servicio externo que la reserva cambió de estado
            await _webhookService.EnviarAsync("reserva.estado_actualizado", new
            {
                reservaId,
                estado = nuevoEstado,
                usuario
            });
        }
    }
}