using System;
using System.Collections.Generic;

namespace StockMaster.Domain.Entities;

public class Producto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }

    public int IdCategoria { get; set; }
    public Categoria? Categoria { get; set; }

    public int IdProveedor { get; set; }
    public Proveedor? Proveedor { get; set; }

    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }

    public int StockActual { get; set; }
    public int StockMinimo { get; set; }

    public int IdArea { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
}