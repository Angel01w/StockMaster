using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;
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
            if (req is null)
                return BadRequest(new { message = "Solicitud inválida." });

            if (string.IsNullOrWhiteSpace(req.NombreCompleto) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Todos los campos son obligatorios." });

            var email = req.Email.Trim();
            var username = req.Username.Trim();

            if (await _db.Usuarios.AnyAsync(x => x.Email == email))
                return BadRequest(new { message = "Ese email ya está registrado." });

            if (await _db.Usuarios.AnyAsync(x => x.Username == username))
                return BadRequest(new { message = "Ese usuario ya existe." });

            var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Nombre == "Usuario");
            if (role is null)
                return BadRequest(new { message = "No existe el rol 'Usuario'." });

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

            var token = GenerateJwt(user.IdUsuario, user.Username, role.Nombre, null, null);

            return Ok(new AuthResponse(
                user.IdUsuario,
                user.NombreCompleto,
                user.Email,
                user.Username,
                role.Nombre,
                null,
                null,
                token
            ));
        }
        catch
        {
            return StatusCode(500, new { message = "Error interno al registrar usuario." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            if (req is null)
                return BadRequest(new { message = "Solicitud inválida." });

            if (string.IsNullOrWhiteSpace(req.Login) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Debe ingresar usuario y contraseña." });

            var login = req.Login.Trim().ToLower();

            var user = await _db.Usuarios.FirstOrDefaultAsync(x =>
                x.Username.ToLower() == login ||
                x.Email.ToLower() == login
            );

            if (user is null)
                return Unauthorized(new { message = "El usuario no existe." });

            if (!string.Equals(user.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "La cuenta está desactivada. Contacte al administrador." });

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return StatusCode(500, new { message = "La cuenta no tiene contraseña válida registrada." });

            var passwordOk = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            if (!passwordOk)
                return Unauthorized(new { message = "La contraseña es incorrecta." });

            var roleName = await _db.Roles
                .Where(r => r.IdRole == user.IdRole)
                .Select(r => r.Nombre)
                .FirstOrDefaultAsync() ?? "Usuario";

            user.UltimoAcceso = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var areaId = await ResolveAreaIdAsync(user.IdUsuario);
            var proveedorId = await ResolveProveedorIdAsync(user.IdUsuario);

            var token = GenerateJwt(user.IdUsuario, user.Username, roleName, areaId, proveedorId);

            return Ok(new AuthResponse(
                user.IdUsuario,
                user.NombreCompleto,
                user.Email,
                user.Username,
                roleName,
                areaId,
                proveedorId,
                token
            ));
        }
        catch
        {
            return StatusCode(500, new { message = "Error interno al iniciar sesión." });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        try
        {
            if (req is null)
                return BadRequest(new { message = "Solicitud inválida." });

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { message = "Username y NewPassword son obligatorios." });

            var user = await _db.Usuarios.FirstOrDefaultAsync(x => x.Username == req.Username);
            if (user is null)
                return NotFound(new { message = "Usuario no existe." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Password actualizado correctamente." });
        }
        catch
        {
            return StatusCode(500, new { message = "Error interno al cambiar contraseña." });
        }
    }

    private async Task<int?> ResolveAreaIdAsync(int idUsuario)
    {
        return await _db.Usuarios
            .Where(x => x.IdUsuario == idUsuario)
            .Select(x => x.AreaId)
            .FirstOrDefaultAsync();
    }

    private async Task<int?> ResolveProveedorIdAsync(int idUsuario)
    {
        return await _db.Usuarios
            .Where(x => x.IdUsuario == idUsuario)
            .Select(x => x.IdProveedor)
            .FirstOrDefaultAsync();
    }

    private string GenerateJwt(int idUsuario, string username, string role, int? areaId, int? proveedorId)
    {
        var key = _config["Jwt:Key"] ?? throw new Exception("Falta Jwt:Key");
        var issuer = _config["Jwt:Issuer"] ?? "StockMaster";
        var audience = _config["Jwt:Audience"] ?? "StockMaster";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("IdUsuario", idUsuario.ToString())
        };

        if (areaId.HasValue)
            claims.Add(new Claim("AreaId", areaId.Value.ToString()));

        if (proveedorId.HasValue)
            claims.Add(new Claim("ProveedorId", proveedorId.Value.ToString()));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}