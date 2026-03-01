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
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriasController(AppDbContext db) => _db = db;

    public record CategoriaCreateDto(string Nombre, string? Descripcion);
    public record CategoriaUpdateDto(string Nombre, string? Descripcion);

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

    private async Task<IQueryable<Categoria>> BuildScopedQueryAsync()
    {
        var q = _db.Categorias.AsNoTracking().AsQueryable();

        if (IsAdmin() || IsAuditor())
            return q;

        var userId = GetUserId();

        var allowedIds = _db.UsuarioCategorias
            .AsNoTracking()
            .Where(x => x.IdUsuario == userId)
            .Select(x => x.IdCategoria);

        q = q.Where(c =>
            c.CreatedByUserId == userId ||
            allowedIds.Contains(c.IdCategoria)
        );

        return q;
    }

    [HttpGet]
    public async Task<ActionResult<List<Categoria>>> GetAll()
    {
        var q = await BuildScopedQueryAsync();
        var list = await q.OrderBy(x => x.Nombre).ToListAsync();
        return list;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Categoria>> GetById(int id)
    {
        var q = await BuildScopedQueryAsync();
        var item = await q.FirstOrDefaultAsync(x => x.IdCategoria == id);
        if (item is null) return NotFound();
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Categoria>> Create(CategoriaCreateDto dto)
    {
        if (dto is null) return BadRequest();

        var nombre = (dto.Nombre ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nombre)) return BadRequest("Nombre requerido.");

        var userId = GetUserId();

        if (!IsAdmin() && !IsAuditor())
        {
            var existsMine = await _db.Categorias.AsNoTracking()
                .AnyAsync(c => c.Nombre == nombre && c.CreatedByUserId == userId);
            if (existsMine) return Conflict("Ya existe una categoría con ese nombre.");
        }

        var entity = new Categoria
        {
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            CreatedByUserId = (IsAdmin() || IsAuditor()) ? null : userId
        };

        _db.Categorias.Add(entity);
        await _db.SaveChangesAsync();

        if (!IsAdmin() && !IsAuditor())
        {
            var link = new UsuarioCategoria { IdUsuario = userId, IdCategoria = entity.IdCategoria };
            _db.UsuarioCategorias.Add(link);
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.IdCategoria }, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoriaUpdateDto dto)
    {
        if (dto is null) return BadRequest();

        var nombre = (dto.Nombre ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nombre)) return BadRequest("Nombre requerido.");

        var item = await _db.Categorias.FirstOrDefaultAsync(x => x.IdCategoria == id);
        if (item is null) return NotFound();

        if (!IsAdmin() && !IsAuditor())
        {
            var userId = GetUserId();

            var allowed = await _db.UsuarioCategorias.AsNoTracking()
                .AnyAsync(x => x.IdUsuario == userId && x.IdCategoria == id);

            var can = allowed || item.CreatedByUserId == userId;
            if (!can) return Forbid();
        }

        item.Nombre = nombre;
        item.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Categorias.FirstOrDefaultAsync(x => x.IdCategoria == id);
        if (item is null) return NotFound();

        if (!IsAdmin() && !IsAuditor())
        {
            var userId = GetUserId();

            var allowed = await _db.UsuarioCategorias.AsNoTracking()
                .AnyAsync(x => x.IdUsuario == userId && x.IdCategoria == id);

            var can = item.CreatedByUserId == userId || allowed;
            if (!can) return Forbid();

            if (item.CreatedByUserId != userId) return Forbid();
        }

        var used = await _db.Productos.AsNoTracking().AnyAsync(p => p.IdCategoria == id);
        if (used) return Conflict("No se puede eliminar: hay productos usando esta categoría.");

        var links = _db.UsuarioCategorias.Where(x => x.IdCategoria == id);
        _db.UsuarioCategorias.RemoveRange(links);

        _db.Categorias.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}