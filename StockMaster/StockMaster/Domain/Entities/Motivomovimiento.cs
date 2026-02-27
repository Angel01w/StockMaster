using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Domain.Entities
{
    [Table("MotivosMovimiento")]
    public class MotivoMovimiento
    {
        [Key]
        public int IdMotivo { get; set; }

        [Required]
        [MaxLength(60)]
        public string Nombre { get; set; } = string.Empty;   

        [Required]
        [MaxLength(10)]
        public string TipoAplica { get; set; } = "Ambos";    

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
