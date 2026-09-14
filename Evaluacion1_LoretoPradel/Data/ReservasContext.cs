using Evaluacion1_LoretoPradel.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Evaluacion1_LoretoPradel.Data
{
    public class ReservasContext : DbContext
    {
        public ReservasContext(DbContextOptions<ReservasContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Reserva> Reserva { get; set; }
    }
}