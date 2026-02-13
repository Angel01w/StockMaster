using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Domain.Entities
{
    [Table("Productos")]
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required]
        [MaxLength(40)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Descripcion { get; set; }

        [Required]
        public int IdCategoria { get; set; }

        [Required]
        public int IdProveedor { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal PrecioCompra { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal PrecioVenta { get; set; }

        [Required]
        public int StockActual { get; set; }

        [Required]
        public int StockMinimo { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        
        public Categoria? Categoria { get; set; }
        public Proveedor? Proveedor { get; set; }
    }
}
