using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Domain.Entities;

[Table("Categorias")]
public class Categoria
{
    [Key]
    public int IdCategoria { get; set; }

    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Descripcion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? IdProveedor { get; set; }
}