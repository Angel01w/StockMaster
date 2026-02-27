using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Domain.Entities
{
    [Table("UsuarioAreas")]
    public class UsuarioArea
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required]
        public int IdArea { get; set; }

        public Usuario? Usuario { get; set; }
        public Area? Area { get; set; }
    }
}