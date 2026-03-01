namespace StockMaster.Domain.Entities;

public class MovimientoInventario
{
    public long IdMovimiento { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = "Entrada";
    public int Cantidad { get; set; }

    public int IdProducto { get; set; }
    public int IdMotivo { get; set; }
    public int IdUsuario { get; set; }

    public Producto Producto { get; set; } = null!;
    public MotivoMovimiento Motivo { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}