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
            join u0 in _db.Usuarios.AsNoTracking() on m.IdUsuario equals u0.IdUsuario into uj
            from u in uj.DefaultIfEmpty()
            select new
            {
                m,
                p,
                mm,
                u
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
                Motivo = x.mm != null ? x.mm.Nombre : null,
                x.m.IdUsuario,
                Responsable = x.u != null ? x.u.NombreCompleto : null
            })
            .ToListAsync();

        return Ok(list);
    }

    public sealed class MovimientoCreateDto
    {
        public int IdProducto { get; set; }
        public string Tipo { get; set; } = "";
        public int Cantidad { get; set; }
        public int? IdMotivo { get; set; }
        public DateTime? Fecha { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] MovimientoCreateDto dto)
    {
        if (IsAuditor()) return Forbid();

        if (dto == null) return BadRequest();
        if (dto.IdProducto <= 0) return BadRequest("IdProducto inválido.");
        if (dto.Cantidad <= 0) return BadRequest("Cantidad inválida.");

        var tipo = (dto.Tipo ?? "").Trim();
        if (!string.Equals(tipo, "Entrada", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tipo, "Salida", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Tipo inválido. Use 'Entrada' o 'Salida'.");

        var producto = await _db.Productos.FirstOrDefaultAsync(x => x.IdProducto == dto.IdProducto);
        if (producto == null) return NotFound("Producto no existe.");

        if (!IsAdmin())
        {
            var areas = await GetUserAreasAsync();
            if (!areas.Any()) return Unauthorized("Usuario sin áreas asignadas.");
            if (!areas.Contains(producto.IdArea)) return Forbid();
        }

        int? motivoId = dto.IdMotivo;
        if (motivoId.HasValue)
        {
            var existeMotivo = await _db.MotivosMovimiento.AsNoTracking().AnyAsync(x => x.IdMotivo == motivoId.Value);
            if (!existeMotivo) return BadRequest("Motivo inválido.");
        }
        else
        {
            motivoId = 0;
        }

        var isEntrada = string.Equals(tipo, "Entrada", StringComparison.OrdinalIgnoreCase);
        var isSalida = string.Equals(tipo, "Salida", StringComparison.OrdinalIgnoreCase);

        if (isSalida)
        {
            if (producto.StockActual < dto.Cantidad)
                return BadRequest("Stock insuficiente.");
            producto.StockActual -= dto.Cantidad;
        }
        else
        {
            producto.StockActual += dto.Cantidad;
        }

        var userId = GetUserId();

        var mov = new MovimientoInventario
        {
            Fecha = dto.Fecha ?? DateTime.Now,
            Tipo = isEntrada ? "Entrada" : "Salida",
            IdProducto = dto.IdProducto,
            Cantidad = dto.Cantidad,
            IdMotivo = motivoId.Value,
            IdUsuario = userId
        };

        _db.MovimientosInventario.Add(mov);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            mov.IdMovimiento,
            mov.Fecha,
            mov.Tipo,
            mov.IdProducto,
            mov.Cantidad,
            mov.IdMotivo,
            mov.IdUsuario
        });
    }
}