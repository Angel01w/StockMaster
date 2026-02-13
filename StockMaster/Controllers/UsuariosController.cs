using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;
using BCrypt.Net;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsuariosController(AppDbContext db) => _db = db;

    public record UsuarioCreateDto(string NombreCompleto, string Email, string Username, string Password, int IdRole);
    public record UsuarioUpdateDto(string NombreCompleto, string Email, string Username, int IdRole, string Estado);
    public record UsuarioListDto(
        int IdUsuario,
        string NombreCompleto,
        string Email,
        string Username,
        string Estado,
        DateTime? UltimoAcceso,
        int IdRole,
        string Rol
    );

    [HttpGet]
    // [Authorize(Roles = "Admin,Auditor")]
    public async Task<ActionResult<List<UsuarioListDto>>> GetAll([FromQuery] string? search, [FromQuery] int? rol, [FromQuery] string? estado)
    {
        var q = _db.Usuarios.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.NombreCompleto.Contains(search) || x.Email.Contains(search) || x.Username.Contains(search));

        if (rol.HasValue)
            q = q.Where(x => x.IdRole == rol.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(x => x.Estado == estado);

        var data = await (from u in q
                          join r in _db.Roles.AsNoTracking() on u.IdRole equals r.IdRole
                          orderby u.IdUsuario descending
                          select new UsuarioListDto(
                              u.IdUsuario,
                              u.NombreCompleto,
                              u.Email,
                              u.Username,
                              u.Estado,
                              u.UltimoAcceso,
                              u.IdRole,
                              r.Nombre
                          )).ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id:int}")]
    // [Authorize(Roles = "Admin,Auditor")]
    public async Task<ActionResult<UsuarioListDto>> GetById(int id)
    {
        var data = await (from u in _db.Usuarios.AsNoTracking()
                          join r in _db.Roles.AsNoTracking() on u.IdRole equals r.IdRole
                          where u.IdUsuario == id
                          select new UsuarioListDto(
                              u.IdUsuario,
                              u.NombreCompleto,
                              u.Email,
                              u.Username,
                              u.Estado,
                              u.UltimoAcceso,
                              u.IdRole,
                              r.Nombre
                          )).FirstOrDefaultAsync();

        return data is null ? NotFound() : Ok(data);
    }

    [HttpPost]
    // [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(UsuarioCreateDto dto)
    {
        if (await _db.Usuarios.AnyAsync(x => x.Email == dto.Email))
            return BadRequest("Ese email ya existe.");

        if (await _db.Usuarios.AnyAsync(x => x.Username == dto.Username))
            return BadRequest("Ese username ya existe.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.IdRole == dto.IdRole);
        if (role is null) return BadRequest("Rol inválido.");

        var user = new Usuario
        {
            NombreCompleto = dto.NombreCompleto.Trim(),
            Email = dto.Email.Trim(),
            Username = dto.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IdRole = dto.IdRole,
            Estado = "Activo",
            CreatedAt = DateTime.UtcNow
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.IdUsuario });
    }

    [HttpPut("{id:int}")]
    // [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UsuarioUpdateDto dto)
    {
        var user = await _db.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);
        if (user is null) return NotFound();

        // validar uniques (si cambia)
        if (user.Email != dto.Email && await _db.Usuarios.AnyAsync(x => x.Email == dto.Email))
            return BadRequest("Ese email ya existe.");

        if (user.Username != dto.Username && await _db.Usuarios.AnyAsync(x => x.Username == dto.Username))
            return BadRequest("Ese username ya existe.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.IdRole == dto.IdRole);
        if (role is null) return BadRequest("Rol inválido.");

        user.NombreCompleto = dto.NombreCompleto.Trim();
        user.Email = dto.Email.Trim();
        user.Username = dto.Username.Trim();
        user.IdRole = dto.IdRole;
        user.Estado = dto.Estado;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:int}/estado")]
    // [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] string estado)
    {
        if (estado != "Activo" && estado != "Inactivo")
            return BadRequest("Estado debe ser Activo o Inactivo.");

        var user = await _db.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);
        if (user is null) return NotFound();

        user.Estado = estado;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:int}/password")]
    // [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CambiarPassword(int id, [FromBody] string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return BadRequest("Password mínimo 6 caracteres.");

        var user = await _db.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);
        if (user is null) return NotFound();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    // [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);
        if (user is null) return NotFound();

        _db.Usuarios.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
