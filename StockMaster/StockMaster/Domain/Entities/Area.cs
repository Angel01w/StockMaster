using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Domain.Entities
{
    [Table("Areas")]
    public class Area
    {
        [Key]
        public int IdArea { get; set; }

        [Required]
        [MaxLength(120)]
        public string Nombre { get; set; } = string.Empty;

        public ICollection<UsuarioArea>? UsuarioAreas { get; set; }
    }
}