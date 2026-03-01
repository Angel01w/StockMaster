using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Domain.Entities
{
    [Table("UsuarioCategorias")]
    public class UsuarioCategoria
    {
        public int IdUsuario { get; set; }
        public int IdCategoria { get; set; }

        public Usuario? Usuario { get; set; }
        public Categoria? Categoria { get; set; }
    }
}