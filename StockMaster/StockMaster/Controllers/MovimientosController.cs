using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<ActionResult> GetAll()
    {
        var q =
            from m in _db.MovimientosInventario.AsNoTracking()
            join p in _db.Productos.AsNoTracking() on m.IdProducto equals p.IdProducto
            join mm0 in _db.MotivosMovimiento.AsNoTracking() on m.IdMotivo equals mm0.IdMotivo into mmj
            from mm in mmj.DefaultIfEmpty()
            select new
            {
                m,
                p,
                mm
            };

        if (!IsAdmin() && !IsAuditor())
        {
            var areas = await GetUserAreasAsync();
            if (!areas.Any()) return Unauthorized("Usuario sin áreas asignadas.");

            q = q.Where(x => areas.Contains(x.p.IdArea));
        }

        var list = await q
            .OrderByDescending(x => x.m.Fecha)
            .Select(x => new
            {
                x.m.IdMovimiento,
                x.m.Fecha,
                x.m.Tipo,
                Producto = x.p.Nombre,
                x.m.Cantidad,
                x.m.IdMotivo,
                Motivo = x.mm != null ? x.mm.Nombre : null
            })
            .ToListAsync();

        return Ok(list);
    }
}