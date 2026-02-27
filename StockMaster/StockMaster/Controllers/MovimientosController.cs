using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
        int IdProducto,
        string Producto,
        string Tipo,
        int Cantidad,
        DateTime Fecha
    );

    public record MovimientoCreateDto(
        int IdProducto,
        string Tipo,
        int Cantidad,
        DateTime? Fecha,
        int? IdMotivo
    );

    private int GetUserId()
    {
        var raw =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("idUsuario")?.Value ??
            User.FindFirst("IdUsuario")?.Value;

        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var id) || id <= 0)
            throw new InvalidOperationException("Token sin IdUsuario válido (NameIdentifier / idUsuario).");

        return id;
    }

    private string GetRole()
    {
        return
            User.FindFirst(ClaimTypes.Role)?.Value ??
            User.FindFirst("role")?.Value ??
            User.FindFirst("Rol")?.Value ??
            "";
    }

    private bool IsAdmin() => string.Equals(GetRole(), "Admin", StringComparison.OrdinalIgnoreCase);
    private bool IsAuditor() => string.Equals(GetRole(), "Auditor", StringComparison.OrdinalIgnoreCase);

    private async Task<int?> ResolveAreaIdOrNullAsync()
    {
        var raw =
            User.FindFirst("IdArea")?.Value ??
            User.FindFirst("AreaId")?.Value;

        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var areaFromToken) && areaFromToken > 0)
            return areaFromToken;

        var userId = GetUserId();

        var areaFromUser = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == userId)
            .Select(u => (int?)u.AreaId)
            .FirstOrDefaultAsync();

        if (areaFromUser.HasValue && areaFromUser.Value > 0)
            return areaFromUser.Value;

        var areaFromBridge = await _db.UsuarioAreas
            .AsNoTracking()
            .Where(ua => ua.IdUsuario == userId)
            .Select(ua => (int?)ua.IdArea)
            .FirstOrDefaultAsync();

        if (areaFromBridge.HasValue && areaFromBridge.Value > 0)
            return areaFromBridge.Value;

        return null;
    }

    [HttpGet]
    public async Task<ActionResult<List<MovimientoReadDto>>> GetAll(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? tipo,
        [FromQuery] int? productoId
    )
    {
        var areaId = await ResolveAreaIdOrNullAsync();

        if (!areaId.HasValue && !IsAdmin())
            return Unauthorized("Token sin IdArea/AreaId válido.");

        var q =
            from m in _db.MovimientosInventario.AsNoTracking()
            join p in _db.Productos.AsNoTracking() on m.IdProducto equals p.IdProducto
            select new { m, p };

        if (areaId.HasValue)
            q = q.Where(x => x.p.IdArea == areaId.Value);

        if (desde.HasValue) q = q.Where(x => x.m.Fecha >= desde.Value);
        if (hasta.HasValue) q = q.Where(x => x.m.Fecha <= hasta.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(x => x.m.Tipo == tipo);
        if (productoId.HasValue && productoId.Value > 0) q = q.Where(x => x.m.IdProducto == productoId.Value);

        var list = await q
            .OrderByDescending(x => x.m.Fecha)
            .Select(x => new MovimientoReadDto(
                x.m.IdMovimiento,
                x.m.IdProducto,
                x.p.Nombre ?? "",
                x.m.Tipo ?? "",
                x.m.Cantidad,
                x.m.Fecha
            ))
            .ToListAsync();

        return list;
    }

    [HttpPost]
    public async Task<ActionResult<MovimientoReadDto>> Create([FromBody] MovimientoCreateDto dto)
    {
        if (IsAuditor()) return Forbid();

        var areaId = await ResolveAreaIdOrNullAsync();
        var userId = GetUserId();

        if (!areaId.HasValue && !IsAdmin())
            return Unauthorized("Token sin IdArea/AreaId válido.");

        if (dto.IdProducto <= 0) return BadRequest("IdProducto inválido.");
        if (string.IsNullOrWhiteSpace(dto.Tipo)) return BadRequest("Tipo es obligatorio.");
        if (dto.Cantidad <= 0) return BadRequest("Cantidad inválida.");

        var tipo = dto.Tipo.Trim();

        if (!string.Equals(tipo, "Entrada", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tipo, "Salida", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Tipo debe ser 'Entrada' o 'Salida'.");

        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == dto.IdProducto);
        if (producto is null) return NotFound("Producto no existe.");

        if (areaId.HasValue && producto.IdArea != areaId.Value)
            return Forbid();

        if (string.Equals(tipo, "Salida", StringComparison.OrdinalIgnoreCase) && producto.StockActual < dto.Cantidad)
            return BadRequest("Stock insuficiente.");

        var fecha = dto.Fecha.HasValue ? dto.Fecha.Value : DateTime.UtcNow;

        var mov = new MovimientoInventario
        {
            IdProducto = dto.IdProducto,
            Tipo = string.Equals(tipo, "Entrada", StringComparison.OrdinalIgnoreCase) ? "Entrada" : "Salida",
            Cantidad = dto.Cantidad,
            Fecha = fecha,
            IdUsuario = userId,
            IdMotivo = dto.IdMotivo,
            CreatedAt = DateTime.UtcNow
        };

        if (string.Equals(mov.Tipo, "Entrada", StringComparison.OrdinalIgnoreCase)) producto.StockActual += mov.Cantidad;
        else producto.StockActual -= mov.Cantidad;

        producto.UpdatedAt = DateTime.UtcNow;

        _db.MovimientosInventario.Add(mov);
        await _db.SaveChangesAsync();

        return Ok(new MovimientoReadDto(
            mov.IdMovimiento,
            mov.IdProducto,
            producto.Nombre ?? "",
            mov.Tipo ?? "",
            mov.Cantidad,
            mov.Fecha
        ));
    }
} 