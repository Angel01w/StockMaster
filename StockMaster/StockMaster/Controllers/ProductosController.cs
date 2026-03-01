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

    private async Task<List<int>> GetUserAreasAsync()
    {
        if (IsAdmin() || IsAuditor())
            return new List<int>();

        var userId = GetUserId();

        return await _db.UsuarioAreas
            .AsNoTracking()
            .Where(x => x.IdUsuario == userId)
            .Select(x => x.IdArea)
            .ToListAsync();
    }

    [HttpGet]
    public async Task<ActionResult<List<Producto>>> GetAll()
    {
        var q = _db.Productos.AsNoTracking().AsQueryable();

        if (!IsAdmin() && !IsAuditor())
        {
            var areas = await GetUserAreasAsync();
            if (!areas.Any()) return Unauthorized("Usuario sin áreas asignadas.");

            q = q.Where(p => areas.Contains(p.IdArea));
        }

        var list = await q.OrderBy(x => x.Nombre).ToListAsync();
        return list;
    }
}