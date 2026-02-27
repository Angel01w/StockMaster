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
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductosController(AppDbContext db) => _db = db;

    public record ProductoCreateDto(
        string Codigo,
        string Nombre,
        string? Descripcion,
        int IdCategoria,
        int IdProveedor,
        decimal PrecioCompra,
        decimal PrecioVenta,
        int StockActual,
        int StockMinimo
    );

    public record ProductoUpdateDto(
        string Codigo,
        string Nombre,
        string? Descripcion,
        int IdCategoria,
        int IdProveedor,
        decimal PrecioCompra,
        decimal PrecioVenta,
        int StockActual,
        int StockMinimo
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

    private bool IsAuditor()
    {
        var role =
            User.FindFirst(ClaimTypes.Role)?.Value ??
            User.FindFirst("role")?.Value ??
            User.FindFirst("Rol")?.Value;

        return string.Equals(role, "Auditor", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<int> ResolveAreaIdAsync()
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

        throw new InvalidOperationException("No se pudo resolver AreaId: no está en token ni en Usuarios.AreaId ni en UsuarioAreas.");
    }

    [HttpGet]
    public async Task<ActionResult<List<Producto>>> GetAll()
    {
        var areaId = await ResolveAreaIdAsync();

        var list = await _db.Productos
            .AsNoTracking()
            .Where(x => x.IdArea == areaId)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        return list;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetById(int id)
    {
        var areaId = await ResolveAreaIdAsync();

        var item = await _db.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdProducto == id && x.IdArea == areaId);

        if (item is null) return NotFound();
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> Create([FromBody] ProductoCreateDto dto)
    {
        if (IsAuditor()) return Forbid();

        var areaId = await ResolveAreaIdAsync();

        var codigo = (dto.Codigo ?? "").Trim();
        var nombre = (dto.Nombre ?? "").Trim();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return BadRequest("Código y Nombre son obligatorios.");

        var exists = await _db.Productos.AnyAsync(x => x.IdArea == areaId && x.Codigo == codigo);
        if (exists) return BadRequest("Ya existe un producto con ese código en tu área.");

        var producto = new Producto
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            IdCategoria = dto.IdCategoria,
            IdProveedor = dto.IdProveedor,
            PrecioCompra = dto.PrecioCompra,
            PrecioVenta = dto.PrecioVenta,
            StockActual = dto.StockActual,
            StockMinimo = dto.StockMinimo,
            IdArea = areaId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = producto.IdProducto }, producto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDto dto)
    {
        if (IsAuditor()) return Forbid();

        var areaId = await ResolveAreaIdAsync();

        var producto = await _db.Productos
            .FirstOrDefaultAsync(x => x.IdProducto == id && x.IdArea == areaId);

        if (producto is null) return NotFound();

        var codigo = (dto.Codigo ?? "").Trim();
        var nombre = (dto.Nombre ?? "").Trim();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return BadRequest("Código y Nombre son obligatorios.");

        var dup = await _db.Productos.AnyAsync(x =>
            x.IdArea == areaId && x.Codigo == codigo && x.IdProducto != id);

        if (dup) return BadRequest("Ya existe otro producto con ese código en tu área.");

        producto.Codigo = codigo;
        producto.Nombre = nombre;
        producto.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        producto.IdCategoria = dto.IdCategoria;
        producto.IdProveedor = dto.IdProveedor;
        producto.PrecioCompra = dto.PrecioCompra;
        producto.PrecioVenta = dto.PrecioVenta;
        producto.StockActual = dto.StockActual;
        producto.StockMinimo = dto.StockMinimo;
        producto.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (IsAuditor()) return Forbid();

        var areaId = await ResolveAreaIdAsync();

        var producto = await _db.Productos
            .FirstOrDefaultAsync(x => x.IdProducto == id && x.IdArea == areaId);

        if (producto is null) return NotFound();

        _db.Productos.Remove(producto);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}