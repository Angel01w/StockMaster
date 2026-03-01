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
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriasController(AppDbContext db) => _db = db;

    public record CategoriaCreateDto(string Nombre, string? Descripcion, int? IdProveedor);
    public record CategoriaUpdateDto(string Nombre, string? Descripcion, int? IdProveedor);

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
    public async Task<ActionResult<List<Categoria>>> GetAll()
    {
        var q = _db.Categorias.AsNoTracking().AsQueryable();

        if (!IsAdmin() && !IsAuditor())
        {
            var prov = await ResolveProveedorIdOrNullAsync();
            if (!prov.HasValue) return Unauthorized("Usuario sin IdProveedor asignado.");
            q = q.Where(c => c.IdProveedor == prov.Value);
        }

        return await q.OrderBy(x => x.Nombre).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Categoria>> GetById(int id)
    {
        var q = _db.Categorias.AsNoTracking().Where(x => x.IdCategoria == id);

        if (!IsAdmin() && !IsAuditor())
        {
            var prov = await ResolveProveedorIdOrNullAsync();
            if (!prov.HasValue) return Unauthorized("Usuario sin IdProveedor asignado.");
            q = q.Where(c => c.IdProveedor == prov.Value);
        }

        var item = await q.FirstOrDefaultAsync();
        if (item is null) return NotFound();
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Categoria>> Create([FromBody] CategoriaCreateDto dto)
    {
        if (IsAuditor()) return Forbid();

        if (dto is null) return BadRequest("Body vacío.");

        var nombre = Clean(dto.Nombre);
        if (string.IsNullOrWhiteSpace(nombre)) return BadRequest("Nombre requerido.");

        int provToUse;

        if (IsAdmin())
        {
            if (!dto.IdProveedor.HasValue || dto.IdProveedor.Value <= 0)
                return BadRequest("IdProveedor requerido para Admin.");

            provToUse = dto.IdProveedor.Value;
        }
        else
        {
            var prov = await ResolveProveedorIdOrNullAsync();
            if (!prov.HasValue) return Unauthorized("Usuario sin IdProveedor asignado.");
            provToUse = prov.Value;
        }

        var exists = await _db.Categorias.AsNoTracking()
            .AnyAsync(c => c.IdProveedor == provToUse && c.Nombre == nombre);

        if (exists) return Conflict("Ya existe una categoría con ese nombre para este proveedor.");

        var entity = new Categoria
        {
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            IdProveedor = provToUse,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = IsAdmin() ? null : GetUserId()
        };

        _db.Categorias.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.IdCategoria }, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoriaUpdateDto dto)
    {
        if (IsAuditor()) return Forbid();

        if (dto is null) return BadRequest("Body vacío.");

        var nombre = Clean(dto.Nombre);
        if (string.IsNullOrWhiteSpace(nombre)) return BadRequest("Nombre requerido.");

        var q = _db.Categorias.Where(x => x.IdCategoria == id);

        if (!IsAdmin())
        {
            var prov = await ResolveProveedorIdOrNullAsync();
            if (!prov.HasValue) return Unauthorized("Usuario sin IdProveedor asignado.");
            q = q.Where(x => x.IdProveedor == prov.Value);
        }

        var item = await q.FirstOrDefaultAsync();
        if (item is null) return NotFound();

        var provToUse = item.IdProveedor;

        if (IsAdmin() && dto.IdProveedor.HasValue && dto.IdProveedor.Value > 0)
            provToUse = dto.IdProveedor.Value;

        var dup = await _db.Categorias.AsNoTracking().AnyAsync(x =>
            x.IdProveedor == provToUse && x.Nombre == nombre && x.IdCategoria != id);

        if (dup) return Conflict("Ya existe otra categoría con ese nombre para este proveedor.");

        item.Nombre = nombre;
        item.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        item.IdProveedor = provToUse;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (IsAuditor()) return Forbid();

        var q = _db.Categorias.Where(x => x.IdCategoria == id);

        if (!IsAdmin())
        {
            var prov = await ResolveProveedorIdOrNullAsync();
            if (!prov.HasValue) return Unauthorized("Usuario sin IdProveedor asignado.");
            q = q.Where(x => x.IdProveedor == prov.Value);
        }

        var item = await q.FirstOrDefaultAsync();
        if (item is null) return NotFound();

        var used = await _db.Productos.AsNoTracking().AnyAsync(p => p.IdCategoria == id);
        if (used) return Conflict("No se puede eliminar: hay productos usando esta categoría.");

        _db.Categorias.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}