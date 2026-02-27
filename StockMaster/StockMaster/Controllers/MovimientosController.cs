using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;
using System.Security.Claims;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovimientosController : ControllerBase
{
    private readonly AppDbContext _db;
    public MovimientosController(AppDbContext db) => _db = db;

    public record MovimientoReadDto(
        long IdMovimiento,
        DateTime Fecha,
        string Tipo,
        long IdProducto,
        string Producto,
        int Cantidad,
        int? IdMotivo,
        string? Motivo,
        long IdUsuario,
        string Usuario,
        string? Documento
    );

    public record MovimientoCreateDto(
        DateTime? Fecha,
        string Tipo,
        int IdProducto,
        int Cantidad,
        int? IdMotivo,
        string? Documento
    );

    private int GetAreaId()
    {
        var raw =
            User.FindFirst("IdArea")?.Value ??
            User.FindFirst("AreaId")?.Value;

        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var id) || id <= 0)
            throw new InvalidOperationException("Token sin IdArea/AreaId válido.");

        return id;
    }

    private int GetUserId()
    {
        var raw =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("idUsuario")?.Value;

        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var id) || id <= 0)
            throw new InvalidOperationException("Token sin IdUsuario válido.");

        return id;
    }

    [HttpGet]
    public async Task<ActionResult<List<MovimientoReadDto>>> GetAll(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? tipo,
        [FromQuery] int? productoId
    )
    {
        var areaId = GetAreaId();

        var q = _db.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Producto)
            .Include(m => m.Motivo)
            .Include(m => m.Usuario)
            .Where(m => m.Producto != null && m.Producto.IdArea == areaId);

        if (desde.HasValue)
        {
            var d = desde.Value.Date;
            q = q.Where(m => m.CreatedAt >= d);
        }

        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
            q = q.Where(m => m.CreatedAt <= h);
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var t = tipo.Trim().ToLower();
            q = q.Where(m => (m.Tipo ?? "").ToLower() == t);
        }

        if (productoId.HasValue && productoId.Value > 0)
            q = q.Where(m => m.IdProducto == productoId.Value);

        var list = await q
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MovimientoReadDto(
                m.IdMovimiento,
                m.CreatedAt,
                m.Tipo ?? "",
                m.IdProducto,
                m.Producto != null ? m.Producto.Nombre : $"ID {m.IdProducto}",
                m.Cantidad,
                m.IdMotivo,
                m.Motivo != null ? m.Motivo.Nombre : null,
                m.IdUsuario,
                m.Usuario != null ? m.Usuario.NombreCompleto : $"ID {m.IdUsuario}",
                m.Documento
            ))
            .ToListAsync();

        return list;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MovimientoCreateDto dto)
    {
        var areaId = GetAreaId();
        var idUsuario = GetUserId();

        if (dto.IdProducto <= 0) return BadRequest("IdProducto inválido.");
        if (dto.Cantidad <= 0) return BadRequest("Cantidad debe ser mayor que 0.");
        if (string.IsNullOrWhiteSpace(dto.Tipo)) return BadRequest("Tipo es obligatorio.");

        var tipo = dto.Tipo.Trim();
        if (!tipo.Equals("Entrada", StringComparison.OrdinalIgnoreCase) &&
            !tipo.Equals("Salida", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Tipo debe ser 'Entrada' o 'Salida'.");

        var prod = await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == dto.IdProducto && p.IdArea == areaId);
        if (prod is null) return NotFound("Producto no existe en tu área.");

        if (dto.IdMotivo.HasValue && dto.IdMotivo.Value > 0)
        {
            var existsMotivo = await _db.MotivosMovimiento.AnyAsync(x => x.IdMotivo == dto.IdMotivo.Value);
            if (!existsMotivo) return BadRequest("IdMotivo no existe.");
        }

        var fecha = dto.Fecha.HasValue
            ? DateTime.SpecifyKind(dto.Fecha.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var mov = new MovimientoInventario
        {
            Tipo = tipo,
            IdProducto = dto.IdProducto,
            Cantidad = dto.Cantidad,
            IdMotivo = dto.IdMotivo,
            Documento = string.IsNullOrWhiteSpace(dto.Documento) ? null : dto.Documento.Trim(),
            IdUsuario = idUsuario,
            CreatedAt = fecha
        };

        if (tipo.Equals("Entrada", StringComparison.OrdinalIgnoreCase))
        {
            prod.StockActual += dto.Cantidad;
        }
        else
        {
            if (prod.StockActual < dto.Cantidad)
                return BadRequest("Stock insuficiente para realizar la salida.");

            prod.StockActual -= dto.Cantidad;
        }

        prod.UpdatedAt = DateTime.UtcNow;

        _db.MovimientosInventario.Add(mov);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, mov.IdMovimiento });
    }
}