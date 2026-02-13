using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Domain.Entities
{
    [Table("MovimientosInventario")]
    public class MovimientoInventario
    {
        [Key]
        public long IdMovimiento { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        [MaxLength(10)]
        public string Tipo { get; set; } = string.Empty; 

        [Required]
        public int IdProducto { get; set; }

        [Required]
        public int Cantidad { get; set; }

        public int? IdMotivo { get; set; }

        [MaxLength(60)]
        public string? Documento { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       
        public Producto? Producto { get; set; }
        public MotivoMovimiento? Motivo { get; set; }
        public Usuario? Usuario { get; set; }
    }
}
