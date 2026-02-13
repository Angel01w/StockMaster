using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Domain.Entities
{
    [Table("Proveedores")]
    public class Proveedor
    {
        [Key]
        public int IdProveedor { get; set; }

        [Required]
        [MaxLength(120)]
        public string NombreEmpresa { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? PersonaContacto { get; set; }

        [MaxLength(120)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Telefono { get; set; }

        [MaxLength(255)]
        public string? Direccion { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
