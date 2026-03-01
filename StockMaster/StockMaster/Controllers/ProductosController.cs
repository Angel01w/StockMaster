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
        int StockMinimo,
        int? IdArea
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
        int StockMinimo,
        int? IdArea
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
            User.FindFirst("Role")?.Value ??
            User.FindFirst("rol")?.Value ??
            User.FindFirst("Rol")?.Value ??
            "";
    }

    private bool IsAdmin() =>
        string.Equals(GetRole(), "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(GetRole(), "admin", StringComparison.OrdinalIgnoreCase);

    private bool IsAuditor() =>
        string.Equals(GetRole(), "Auditor", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(GetRole(), "auditor", StringComparison.OrdinalIgnoreCase);

    private async Task<int?> ResolveAreaIdOrNullAsync()
    {
        var raw =
            User.FindFirst("IdArea")?.Value ??
            User.FindFirst("idArea")?.Value ??
            User.FindFirst("AreaId")?.Value ??
            User.FindFirst("areaId")?.Value;

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

    private async Task<int?> ResolveProveedorIdOrNullAsync()
    {
        var userId = GetUserId();

        var prov = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == userId)
            .Select(u => (int?)u.IdProveedor)
            .FirstOrDefaultAsync();

        if (prov.HasValue && prov.Value > 0) return prov.Value;
        return null;
    }

    private async Task<int> ResolveAreaIdForCreateAsync(int? dtoAreaId)
    {
        if (IsAdmin())
        {
            if (dtoAreaId.HasValue && dtoAreaId.Value > 0) return dtoAreaId.Value;
            return 2;
        }

        var fromUser = await ResolveAreaIdOrNullAsync();
        if (fromUser.HasValue && fromUser.Value > 0) return fromUser.Value;

        throw new InvalidOperationException("No se pudo resolver el IdArea del usuario.");
    }

    [HttpGet]
    public async Task<ActionResult<List<Producto>>> GetAll()
    {
        var areaId = await ResolveAreaIdOrNullAsync();
        var proveedorId = await ResolveProveedorIdOrNullAsync();

        if (!areaId.HasValue && !(IsAdmin() || IsAuditor()))
            return Unauthorized("Token sin IdArea/AreaId válido.");

        if (!(IsAdmin() || IsAuditor()) && !proveedorId.HasValue)
            return Unauthorized("Usuario sin IdProveedor asignado.");

        var q = _db.Productos.AsNoTracking().AsQueryable();

        if (!(IsAdmin() || IsAuditor()))
        {
            q = q.Where(x => x.IdArea == areaId.Value);
            q = q.Where(x => x.IdProveedor == proveedorId.Value);
        }

        var list = await q.OrderBy(x => x.Nombre).ToListAsync();
        return list;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetById(int id)
    {
        var areaId = await ResolveAreaIdOrNullAsync();
        var proveedorId = await ResolveProveedorIdOrNullAsync();

        if (!areaId.HasValue && !(IsAdmin() || IsAuditor()))
            return Unauthorized("Token sin IdArea/AreaId válido.");

        if (!(IsAdmin() || IsAuditor()) && !proveedorId.HasValue)
            return Unauthorized("Usuario sin IdProveedor asignado.");

        var q = _db.Productos.AsNoTracking().Where(x => x.IdProducto == id);

        if (!(IsAdmin() || IsAuditor()))
            q = q.Where(x => x.IdArea == areaId.Value && x.IdProveedor == proveedorId.Value);

        var item = await q.FirstOrDefaultAsync();
        if (item is null) return NotFound();
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> Create([FromBody] ProductoCreateDto dto)
    {
        if (IsAuditor()) return Forbid();

        var proveedorId = await ResolveProveedorIdOrNullAsync();
        if (!(IsAdmin() || IsAuditor()) && !proveedorId.HasValue)
            return Unauthorized("Usuario sin IdProveedor asignado.");

        int areaId;
        try { areaId = await ResolveAreaIdForCreateAsync(dto.IdArea); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        if (!(IsAdmin() || IsAuditor()))
        {
            var userArea = await ResolveAreaIdOrNullAsync();
            if (!userArea.HasValue) return Unauthorized("Token sin IdArea/AreaId válido.");
            areaId = userArea.Value;

            if (dto.IdProveedor != proveedorId.Value)
                return Forbid();
        }

        var codigo = (dto.Codigo ?? "").Trim();
        var nombre = (dto.Nombre ?? "").Trim();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return BadRequest("Código y Nombre son obligatorios.");

        var exists = await _db.Productos.AnyAsync(x => x.IdArea == areaId && x.Codigo == codigo);
        if (exists) return BadRequest("Ya existe un producto con ese código en esa área.");

        var producto = new Producto
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            IdCategoria = dto.IdCategoria,
            IdProveedor = IsAdmin() ? dto.IdProveedor : proveedorId.Value,
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

        var areaId = await ResolveAreaIdOrNullAsync();
        var proveedorId = await ResolveProveedorIdOrNullAsync();

        if (!areaId.HasValue && !IsAdmin())
            return Unauthorized("Token sin IdArea/AreaId válido.");

        if (!IsAdmin() && !proveedorId.HasValue)
            return Unauthorized("Usuario sin IdProveedor asignado.");

        var q = _db.Productos.Where(x => x.IdProducto == id);

        if (!IsAdmin())
            q = q.Where(x => x.IdArea == areaId.Value && x.IdProveedor == proveedorId.Value);

        var producto = await q.FirstOrDefaultAsync();
        if (producto is null) return NotFound();

        if (IsAdmin() && dto.IdArea.HasValue && dto.IdArea.Value > 0)
            producto.IdArea = dto.IdArea.Value;

        var codigo = (dto.Codigo ?? "").Trim();
        var nombre = (dto.Nombre ?? "").Trim();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
            return BadRequest("Código y Nombre son obligatorios.");

        var dup = await _db.Productos.AnyAsync(x =>
            x.IdArea == producto.IdArea && x.Codigo == codigo && x.IdProducto != id);

        if (dup) return BadRequest("Ya existe otro producto con ese código en esa área.");

        producto.Codigo = codigo;
        producto.Nombre = nombre;
        producto.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        producto.IdCategoria = dto.IdCategoria;

        if (IsAdmin())
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

        var areaId = await ResolveAreaIdOrNullAsync();
        var proveedorId = await ResolveProveedorIdOrNullAsync();

        if (!areaId.HasValue && !IsAdmin())
            return Unauthorized("Token sin IdArea/AreaId válido.");

        if (!IsAdmin() && !proveedorId.HasValue)
            return Unauthorized("Usuario sin IdProveedor asignado.");

        var q = _db.Productos.Where(x => x.IdProducto == id);

        if (!IsAdmin())
            q = q.Where(x => x.IdArea == areaId.Value && x.IdProveedor == proveedorId.Value);

        var producto = await q.FirstOrDefaultAsync();
        if (producto is null) return NotFound();

        _db.Productos.Remove(producto);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}