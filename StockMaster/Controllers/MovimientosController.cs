using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovimientosController : ControllerBase
{
    private readonly AppDbContext _db;
    public MovimientosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MovimientoInventario>>> GetAll(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? tipo,
        [FromQuery] int? productoId,
        [FromQuery] int? usuarioId)
    {
        var q = _db.MovimientosInventario
            .AsNoTracking()
            .Include(x => x.Producto)
            .Include(x => x.Usuario)
            .Include(x => x.Motivo)
            .AsQueryable();

        if (desde.HasValue) q = q.Where(x => x.Fecha >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(x => x.Fecha <= hasta.Value.Date);
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(x => x.Tipo == tipo);
        if (productoId.HasValue) q = q.Where(x => x.IdProducto == productoId.Value);
        if (usuarioId.HasValue) q = q.Where(x => x.IdUsuario == usuarioId.Value);

        return await q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.IdMovimiento).ToListAsync();
    }

    [HttpPost]
    public async Task<IActionResult> Create(MovimientoInventario mov)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(x => x.IdProducto == mov.IdProducto);
        if (producto is null) return BadRequest("Producto no existe.");

        if (mov.Cantidad <= 0) return BadRequest("Cantidad debe ser mayor a 0.");
        if (mov.Tipo != "Entrada" && mov.Tipo != "Salida") return BadRequest("Tipo inválido.");

        if (mov.Tipo == "Salida" && producto.StockActual < mov.Cantidad)
            return BadRequest("Stock insuficiente para realizar la salida.");

        // Transacción (stock + movimiento)
        using var tx = await _db.Database.BeginTransactionAsync();

        if (mov.Tipo == "Entrada")
            producto.StockActual += mov.Cantidad;
        else
            producto.StockActual -= mov.Cantidad;

        _db.MovimientosInventario.Add(mov);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(mov);
    }
}
