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
public class ProveedoresController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProveedoresController(AppDbContext db) => _db = db;

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
        if (IsAdmin() || IsAuditor()) return null;

        var raw =
            User.FindFirst("IdProveedor")?.Value ??
            User.FindFirst("idProveedor")?.Value ??
            User.FindFirst("ProveedorId")?.Value ??
            User.FindFirst("proveedorId")?.Value;

        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var fromToken) && fromToken > 0)
            return fromToken;

        var userId = GetUserId();

        var fromUser = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == userId)
            .Select(u => (int?)u.IdProveedor)
            .FirstOrDefaultAsync();

        if (fromUser.HasValue && fromUser.Value > 0)
            return fromUser.Value;

        return null;
    }

    [HttpGet]
    public async Task<ActionResult<List<Proveedor>>> GetAll()
    {
        var proveedorId = await ResolveProveedorIdOrNullAsync();

        var q = _db.Proveedores.AsNoTracking().AsQueryable();

        if (!IsAdmin() && !IsAuditor())
        {
            if (!proveedorId.HasValue) return Ok(new List<Proveedor>());
            q = q.Where(p => p.IdProveedor == proveedorId.Value);
        }

        var list = await q.OrderBy(x => x.NombreEmpresa).ToListAsync();
        return list;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Proveedor>> GetById(int id)
    {
        var proveedorId = await ResolveProveedorIdOrNullAsync();

        var q = _db.Proveedores.AsNoTracking().Where(x => x.IdProveedor == id);

        if (!IsAdmin() && !IsAuditor())
        {
            if (!proveedorId.HasValue) return NotFound();
            q = q.Where(x => x.IdProveedor == proveedorId.Value);
        }

        var item = await q.FirstOrDefaultAsync();
        if (item is null) return NotFound();
        return item;
    }
}