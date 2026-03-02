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

    private int GetUserId()
    {
        var raw =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("IdUsuario")?.Value;

        if (!int.TryParse(raw, out var id) || id <= 0)
            throw new InvalidOperationException("Token sin IdUsuario válido.");

        return id;
    }

    private string GetRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    private bool IsAdmin() =>
        string.Equals(GetRole(), "Admin", StringComparison.OrdinalIgnoreCase);

    private bool IsAuditor() =>
        string.Equals(GetRole(), "Auditor", StringComparison.OrdinalIgnoreCase);

    private async Task<List<int>> GetUserCategoriasAsync()
    {
        if (IsAdmin() || IsAuditor())
            return new List<int>();

        var userId = GetUserId();

        return await _db.UsuarioCategorias
            .AsNoTracking()
            .Where(x => x.IdUsuario == userId)
            .Select(x => x.IdCategoria)
            .ToListAsync();
    }

    private int? GetAreaFromToken()
    {
        var raw = User.FindFirst("AreaId")?.Value;
        if (int.TryParse(raw, out var id) && id > 0)
            return id;

        return null;
    }

    [HttpGet]
    public async Task<ActionResult<List<Producto>>> GetAll()
    {
        var q = _db.Productos.AsNoTracking().AsQueryable();

        if (!IsAdmin() && !IsAuditor())
        {
            var cats = await GetUserCategoriasAsync();
            if (!cats.Any()) return Unauthorized("Usuario sin categorías asignadas.");

            q = q.Where(p => cats.Contains(p.IdCategoria));
        }

        var list = await q.OrderBy(x => x.Nombre).ToListAsync();
        return list;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetById(int id)
    {
        var q = _db.Productos.AsNoTracking().Where(x => x.IdProducto == id);

        if (!IsAdmin() && !IsAuditor())
        {
            var cats = await GetUserCategoriasAsync();
            if (!cats.Any()) return Unauthorized("Usuario sin categorías asignadas.");

            q = q.Where(p => cats.Contains(p.IdCategoria));
        }

        var item = await q.FirstOrDefaultAsync();
        if (item == null) return NotFound();
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> Create([FromBody] Producto model)
    {
        if (IsAuditor()) return Forbid();

        if (!IsAdmin())
        {
            var cats = await GetUserCategoriasAsync();
            if (!cats.Any()) return Unauthorized("Usuario sin categorías asignadas.");

            if (!cats.Contains(model.IdCategoria)) return Forbid();

            var areaId = GetAreaFromToken();
            if (!areaId.HasValue) return Unauthorized("Usuario sin área asignada.");

            model.IdArea = areaId.Value;
        }

        _db.Productos.Add(model);
        await _db.SaveChangesAsync();

        return Ok(model);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Producto model)
    {
        if (IsAuditor()) return Forbid();
        if (id != model.IdProducto) return BadRequest();

        var existing = await _db.Productos.FirstOrDefaultAsync(x => x.IdProducto == id);
        if (existing == null) return NotFound();

        if (!IsAdmin())
        {
            var cats = await GetUserCategoriasAsync();
            if (!cats.Any()) return Unauthorized();

            if (!cats.Contains(existing.IdCategoria)) return Forbid();
            if (!cats.Contains(model.IdCategoria)) return Forbid();

            var areaId = GetAreaFromToken();
            if (!areaId.HasValue) return Unauthorized();

            model.IdArea = areaId.Value;
        }

        _db.Entry(existing).CurrentValues.SetValues(model);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (IsAuditor()) return Forbid();

        var existing = await _db.Productos.FirstOrDefaultAsync(x => x.IdProducto == id);
        if (existing == null) return NotFound();

        if (!IsAdmin())
        {
            var cats = await GetUserCategoriasAsync();
            if (!cats.Any()) return Unauthorized();

            if (!cats.Contains(existing.IdCategoria)) return Forbid();
        }

        _db.Productos.Remove(existing);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}