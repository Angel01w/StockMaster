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
            throw new InvalidOperationException("Token sin IdUsuario válido.");

        return id;
    }

    private string GetRole()
    {
        return
            User.FindFirst(ClaimTypes.Role)?.Value ??
            User.FindFirst("role")?.Value ??
            User.FindFirst("Role")?.Value ??
            User.FindFirst("rol")?.Value ??
            User.FindFirst("Rol")?.Value ??
            "";
    }

    private bool IsAdmin() => string.Equals(GetRole(), "Admin", StringComparison.OrdinalIgnoreCase);
    private bool IsAuditor() => string.Equals(GetRole(), "Auditor", StringComparison.OrdinalIgnoreCase);

    private async Task<int?> ResolveProveedorIdOrNullAsync()
    {
        var raw =
            User.FindFirst("ProveedorId")?.Value ??
            User.FindFirst("proveedorId")?.Value ??
            User.FindFirst("IdProveedor")?.Value ??
            User.FindFirst("idProveedor")?.Value;

        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var provFromToken) && provFromToken > 0)
            return provFromToken;

        var userId = GetUserId();

        var provFromUser = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == userId)
            .Select(u => (int?)u.IdProveedor)
            .FirstOrDefaultAsync();

        if (provFromUser.HasValue && provFromUser.Value > 0)
            return provFromUser.Value;

        return null;
    }

    private static string Clean(string? s) => (s ?? "").Trim();

    [HttpGet]
    public async Task<ActionResult<List<Producto>>> GetAll()
    {
        var proveedorId = await ResolveProveedorIdOrNullAsync();

        var q = _db.Productos.AsNoTracking().AsQueryable();

        if (!IsAdmin() && !IsAuditor())
        {
            if (!proveedorId.HasValue)
                return Unauthorized("Usuario sin IdProveedor asignado.");

            q = q.Where(x => x.IdProveedor == proveedorId.Value);
        }

        var list = await q.OrderBy(x => x.Nombre).ToListAsync();
        return list;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetById(int id)
    {
        var proveedorId = await ResolveProveedorIdOrNullAsync();

        var q = _db.Productos.AsNoTracking().Where(x => x.IdProducto == id);

        if (!IsAdmin() && !IsAuditor())
        {
            if (!proveedorId.HasValue)
                return Unauthorized("Usuario sin IdProveedor asignado.");

            q = q.Where(x => x.IdProveedor == proveedorId.Value);
        }

        var item = await q.FirstOrDefaultAsync();
        if (item is null) return NotFound();
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> Create([FromBody] ProductoCreateDto dto)
    {
        if (IsAuditor()) return Forbid();

        var proveedorId = await ResolveProveedorIdOrNullAsync();

        int provToUse;
        if (IsAdmin())
        {
            if (dto.IdProveedor <= 0) return BadRequest("IdProveedor es obligatorio.");
            provToUse = dto.IdProveedor;
        }
        else
        {
            if (!proveedorId.HasValue)
                return Unauthorized("Usuario sin IdProveedor asignado.");

            provToUse = proveedorId.Value;
        }

        var codigo = Clean(dto.Codigo);
        var nombre = Clean(dto.Nombre);

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return BadRequest("Código y Nombre son obligatorios.");

        var catExists = await _db.Categorias.AsNoTracking()
            .AnyAsync(c => c.IdCategoria == dto.IdCategoria && c.IdProveedor == provToUse);

        if (!catExists && !IsAdmin())
            return BadRequest("La categoría no pertenece a tu proveedor.");

        if (IsAdmin())
        {
            var catProv = await _db.Categorias.AsNoTracking()
                .Where(c => c.IdCategoria == dto.IdCategoria)
                .Select(c => (int?)c.IdProveedor)
                .FirstOrDefaultAsync();

            if (catProv.HasValue && catProv.Value > 0 && catProv.Value != provToUse)
                return BadRequest("La categoría seleccionada no pertenece al proveedor indicado.");
        }

        var exists = await _db.Productos.AnyAsync(x => x.IdProveedor == provToUse && x.Codigo == codigo);
        if (exists) return BadRequest("Ya existe un producto con ese código para este proveedor.");

        var producto = new Producto
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            IdCategoria = dto.IdCategoria,
            IdProveedor = provToUse,
            PrecioCompra = dto.PrecioCompra,
            PrecioVenta = dto.PrecioVenta,
            StockActual = dto.StockActual,
            StockMinimo = dto.StockMinimo,
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

        var proveedorId = await ResolveProveedorIdOrNullAsync();

        var q = _db.Productos.Where(x => x.IdProducto == id);

        if (!IsAdmin())
        {
            if (!proveedorId.HasValue)
                return Unauthorized("Usuario sin IdProveedor asignado.");

            q = q.Where(x => x.IdProveedor == proveedorId.Value);
        }

        var producto = await q.FirstOrDefaultAsync();
        if (producto is null) return NotFound();

        int provToUse;
        if (IsAdmin())
        {
            if (dto.IdProveedor <= 0) return BadRequest("IdProveedor es obligatorio.");
            provToUse = dto.IdProveedor;
        }
        else
        {
            provToUse = producto.IdProveedor;
        }

        var codigo = Clean(dto.Codigo);
        var nombre = Clean(dto.Nombre);

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return BadRequest("Código y Nombre son obligatorios.");

        var dup = await _db.Productos.AnyAsync(x =>
            x.IdProveedor == provToUse && x.Codigo == codigo && x.IdProducto != id);

        if (dup) return BadRequest("Ya existe otro producto con ese código para este proveedor.");

        if (!IsAdmin())
        {
            var catOk = await _db.Categorias.AsNoTracking()
                .AnyAsync(c => c.IdCategoria == dto.IdCategoria && c.IdProveedor == provToUse);

            if (!catOk)
                return BadRequest("La categoría no pertenece a tu proveedor.");
        }
        else
        {
            var catProv = await _db.Categorias.AsNoTracking()
                .Where(c => c.IdCategoria == dto.IdCategoria)
                .Select(c => (int?)c.IdProveedor)
                .FirstOrDefaultAsync();

            if (catProv.HasValue && catProv.Value > 0 && catProv.Value != provToUse)
                return BadRequest("La categoría seleccionada no pertenece al proveedor indicado.");
        }

        producto.Codigo = codigo;
        producto.Nombre = nombre;
        producto.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        producto.IdCategoria = dto.IdCategoria;

        if (IsAdmin())
            producto.IdProveedor = provToUse;

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

        var proveedorId = await ResolveProveedorIdOrNullAsync();

        var q = _db.Productos.Where(x => x.IdProducto == id);

        if (!IsAdmin())
        {
            if (!proveedorId.HasValue)
                return Unauthorized("Usuario sin IdProveedor asignado.");

            q = q.Where(x => x.IdProveedor == proveedorId.Value);
        }

        var producto = await q.FirstOrDefaultAsync();
        if (producto is null) return NotFound();

        _db.Productos.Remove(producto);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}