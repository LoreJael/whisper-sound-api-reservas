using Evaluacion1_LoretoPradel.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Evaluacion1_LoretoPradel.Hubs
{
    [Authorize]
    public class ReservasHub : Hub
    {
        private readonly ReservasContext _context;

        public ReservasHub(ReservasContext context)
        {
            _context = context;
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
        }
    }
}