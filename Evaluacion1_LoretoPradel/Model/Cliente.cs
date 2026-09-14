using System.ComponentModel.DataAnnotations;

namespace Evaluacion1_LoretoPradel.Model
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [Phone]
        public string Telefono { get; set; }


    }
}
