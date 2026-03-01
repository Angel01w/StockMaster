using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public record RegisterRequest(string NombreCompleto, string Email, string Username, string Password);
    public record LoginRequest(string Login, string Password);
    public record ResetPasswordRequest(string Username, string NewPassword);

    public record AuthResponse(
        int IdUsuario,
        string NombreCompleto,
        string Email,
        string Username,
        string Rol,
        int? AreaId,
        int? ProveedorId,
        string Token
    );

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        try
        {
            if (req is null) return BadRequest("Body vacío.");

            if (string.IsNullOrWhiteSpace(req.NombreCompleto) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Todos los campos son obligatorios.");

            var email = req.Email.Trim();
            var username = req.Username.Trim();

            var emailExists = await _db.Usuarios.AnyAsync(x => x.Email == email);
            if (emailExists) return BadRequest("Ese email ya está registrado.");

            var userExists = await _db.Usuarios.AnyAsync(x => x.Username == username);
            if (userExists) return BadRequest("Ese usuario ya existe.");

            var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Nombre == "Usuario");
            if (role is null) return BadRequest("No existe el rol 'Usuario'. Crea los roles en la BD.");

            var user = new Usuario
            {
                NombreCompleto = req.NombreCompleto.Trim(),
                Email = email,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                IdRole = role.IdRole,
                Estado = "Activo",
                CreatedAt = DateTime.UtcNow
            };

            _db.Usuarios.Add(user);
            await _db.SaveChangesAsync();

            var roleName = role.Nombre ?? "Usuario";
            var areaIdResolved = await ResolveAreaIdAsync(user.IdUsuario);
            var proveedorIdResolved = await ResolveProveedorIdAsync(user.IdUsuario);

            var token = GenerateJwt(user.IdUsuario, user.Username ?? "", roleName, areaIdResolved, proveedorIdResolved);

            return Ok(new AuthResponse(
                user.IdUsuario,
                user.NombreCompleto ?? "",
                user.Email ?? "",
                user.Username ?? "",
                roleName,
                areaIdResolved,
                proveedorIdResolved,
                token
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"REGISTER 500: {ex.GetType().Name} | {ex.Message}");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            if (req is null) return BadRequest("Body vacío.");

            if (string.IsNullOrWhiteSpace(req.Login) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Login y Password son obligatorios.");

            var login = req.Login.Trim().ToLower();

            var user = await _db.Usuarios.FirstOrDefaultAsync(x =>
                ((x.Username ?? "").ToLower() == login) ||
                ((x.Email ?? "").ToLower() == login)
            );

            if (user is null) return Unauthorized("Credenciales inválidas.");

            if (!string.Equals(user.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Usuario inactivo.");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return StatusCode(500, "Usuario sin PasswordHash en BD.");

            var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            if (!ok) return Unauthorized("Credenciales inválidas.");

            var roleName = await _db.Roles
                .AsNoTracking()
                .Where(r => r.IdRole == user.IdRole)
                .Select(r => r.Nombre)
                .FirstOrDefaultAsync();

            roleName = string.IsNullOrWhiteSpace(roleName) ? "Usuario" : roleName;

            user.UltimoAcceso = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var areaIdResolved = await ResolveAreaIdAsync(user.IdUsuario);
            var proveedorIdResolved = await ResolveProveedorIdAsync(user.IdUsuario);

            var token = GenerateJwt(user.IdUsuario, user.Username ?? "", roleName, areaIdResolved, proveedorIdResolved);

            return Ok(new AuthResponse(
                user.IdUsuario,
                user.NombreCompleto ?? "",
                user.Email ?? "",
                user.Username ?? "",
                roleName,
                areaIdResolved,
                proveedorIdResolved,
                token
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"LOGIN 500: {ex.GetType().Name} | {ex.Message}");
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        try
        {
            if (req is null) return BadRequest("Body vacío.");

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest("Username y NewPassword son obligatorios.");

            var username = req.Username.Trim();

            var user = await _db.Usuarios.FirstOrDefaultAsync(x => x.Username == username);
            if (user is null) return NotFound("Usuario no existe.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _db.SaveChangesAsync();

            return Ok("Password actualizado.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"RESET 500: {ex.GetType().Name} | {ex.Message}");
        }
    }

    private async Task<int?> ResolveAreaIdAsync(int idUsuario)
    {
        var u = await _db.Usuarios
            .AsNoTracking()
            .Where(x => x.IdUsuario == idUsuario)
            .Select(x => new { x.AreaId })
            .FirstOrDefaultAsync();

        if (u is null) return null;

        if (u.AreaId.HasValue && u.AreaId.Value > 0)
            return u.AreaId.Value;

        var area = await _db.UsuarioAreas
            .AsNoTracking()
            .Where(x => x.IdUsuario == idUsuario)
            .Select(x => (int?)x.IdArea)
            .FirstOrDefaultAsync();

        return area;
    }

    private async Task<int?> ResolveProveedorIdAsync(int idUsuario)
    {
        int? TryGet(string propName)
        {
            try
            {
                return _db.Usuarios
                    .AsNoTracking()
                    .Where(x => x.IdUsuario == idUsuario)
                    .Select(x => EF.Property<int?>(x, propName))
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        var p =
            TryGet("IdProveedor") ??
            TryGet("idProveedor") ??
            TryGet("ProveedorId") ??
            TryGet("proveedorId");

        if (p.HasValue && p.Value > 0) return p.Value;
        return null;
    }

    private string GenerateJwt(int idUsuario, string username, string role, int? areaId, int? proveedorId)
    {
        var key = _config["Jwt:Key"];
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var minutesStr = _config["Jwt:Minutes"];

        if (string.IsNullOrWhiteSpace(key))
            throw new Exception("Falta Jwt:Key en appsettings.json");

        if (string.IsNullOrWhiteSpace(issuer))
            issuer = "StockMaster";

        if (string.IsNullOrWhiteSpace(audience))
            audience = "StockMaster";

        var minutes = 240;
        if (!string.IsNullOrWhiteSpace(minutesStr) && int.TryParse(minutesStr, out var m) && m > 0)
            minutes = m;

        username ??= "";
        role ??= "Usuario";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim("idUsuario", idUsuario.ToString()),
            new Claim("IdUsuario", idUsuario.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role),
            new Claim("Role", role),
            new Claim("rol", role),
            new Claim("Rol", role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (areaId.HasValue && areaId.Value > 0)
        {
            claims.Add(new Claim("IdArea", areaId.Value.ToString()));
            claims.Add(new Claim("idArea", areaId.Value.ToString()));
            claims.Add(new Claim("AreaId", areaId.Value.ToString()));
            claims.Add(new Claim("areaId", areaId.Value.ToString()));
        }

        if (proveedorId.HasValue && proveedorId.Value > 0)
        {
            claims.Add(new Claim("IdProveedor", proveedorId.Value.ToString()));
            claims.Add(new Claim("idProveedor", proveedorId.Value.ToString()));
            claims.Add(new Claim("ProveedorId", proveedorId.Value.ToString()));
            claims.Add(new Claim("proveedorId", proveedorId.Value.ToString()));
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}