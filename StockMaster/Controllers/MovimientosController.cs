using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.API.Dtos;
using StockMaster.Infrastructure.Data;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovimientosController : ControllerBase
{
    private readonly AppDbContext _db;
    public MovimientosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MovimientoReadDto>>> GetAll(
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

        var list = await q
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.IdMovimiento)
            .Select(x => new MovimientoReadDto
            {
                IdMovimiento = x.IdMovimiento,
                Fecha = x.Fecha,
                Tipo = x.Tipo,
                IdProducto = x.IdProducto,
                ProductoNombre = x.Producto != null ? x.Producto.Nombre : "",
                Cantidad = x.Cantidad,
                IdMotivo = x.IdMotivo,
                MotivoNombre = x.Motivo != null ? x.Motivo.Nombre : "",
                Documento = x.Documento,
                IdUsuario = x.IdUsuario,
                UsuarioNombre = x.Usuario != null ? x.Usuario.NombreCompleto : "",
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StockMaster.API.Dtos.MovimientoCreateDto mov)
    {
        if (mov == null) return BadRequest("Body requerido.");
        if (mov.IdProducto <= 0) return BadRequest("IdProducto inválido.");
        if (mov.IdUsuario <= 0) return BadRequest("IdUsuario inválido.");
        if (mov.Cantidad <= 0) return BadRequest("Cantidad debe ser mayor a 0.");
        if (mov.Tipo != "Entrada" && mov.Tipo != "Salida") return BadRequest("Tipo inválido.");

        var producto = await _db.Productos.FirstOrDefaultAsync(x => x.IdProducto == mov.IdProducto);
        if (producto is null) return BadRequest("Producto no existe.");

        if (mov.Tipo == "Salida" && producto.StockActual < mov.Cantidad)
            return BadRequest("Stock insuficiente para realizar la salida.");

        using var tx = await _db.Database.BeginTransactionAsync();

        if (mov.Tipo == "Entrada")
            producto.StockActual += mov.Cantidad;
        else
            producto.StockActual -= mov.Cantidad;

        _db.Entry(producto).State = EntityState.Modified;

        var entity = new StockMaster.Domain.Entities.MovimientoInventario
        {
            Fecha = mov.Fecha == default ? DateTime.UtcNow.Date : mov.Fecha,
            Tipo = mov.Tipo,
            IdProducto = mov.IdProducto,
            Cantidad = mov.Cantidad,
            IdMotivo = mov.IdMotivo == 0 ? null : mov.IdMotivo,
            Documento = string.IsNullOrWhiteSpace(mov.Documento) ? null : mov.Documento.Trim(),
            IdUsuario = mov.IdUsuario,
            CreatedAt = DateTime.UtcNow
        };

        _db.MovimientosInventario.Add(entity);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var created = await _db.MovimientosInventario
            .AsNoTracking()
            .Include(x => x.Producto)
            .Include(x => x.Usuario)
            .Include(x => x.Motivo)
            .FirstOrDefaultAsync(x => x.IdMovimiento == entity.IdMovimiento);

        if (created == null) return Ok(new MovimientoReadDto
        {
            IdMovimiento = entity.IdMovimiento,
            Fecha = entity.Fecha,
            Tipo = entity.Tipo,
            IdProducto = entity.IdProducto,
            ProductoNombre = "",
            Cantidad = entity.Cantidad,
            IdMotivo = entity.IdMotivo,
            MotivoNombre = "",
            Documento = entity.Documento,
            IdUsuario = entity.IdUsuario,
            UsuarioNombre = "",
            CreatedAt = entity.CreatedAt
        });

        return Ok(new MovimientoReadDto
        {
            IdMovimiento = created.IdMovimiento,
            Fecha = created.Fecha,
            Tipo = created.Tipo,
            IdProducto = created.IdProducto,
            ProductoNombre = created.Producto != null ? created.Producto.Nombre : "",
            Cantidad = created.Cantidad,
            IdMotivo = created.IdMotivo,
            MotivoNombre = created.Motivo != null ? created.Motivo.Nombre : "",
            Documento = created.Documento,
            IdUsuario = created.IdUsuario,
            UsuarioNombre = created.Usuario != null ? created.Usuario.NombreCompleto : "",
            CreatedAt = created.CreatedAt
        });
    }
}

namespace StockMaster.API.Dtos
{
    public class MovimientoCreateDto
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = "";
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public int? IdMotivo { get; set; }
        public string? Documento { get; set; }
        public int IdUsuario { get; set; }
    }
}