using System.ComponentModel.DataAnnotations;

namespace Evaluacion1_LoretoPradel.Model
{
    public class Reserva
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; } //? opcional, puede estar vacío

        [Required]
        [MaxLength(100)]
        public string NombreProfesional { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)] //evita un precio negativo o 0
        public decimal PrecioHora { get; set; }

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = "Pendiente"; //valor por defecto


    }
}
