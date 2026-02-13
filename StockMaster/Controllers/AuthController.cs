using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockMaster.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

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
    public record LoginRequest(string Username, string Password);

    public record AuthResponse(
        int IdUsuario,
        string NombreCompleto,
        string Email,
        string Username,
        string Rol,
        string Token
    );

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NombreCompleto) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Username) ||
            string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Todos los campos son obligatorios.");

        var emailExists = await _db.Usuarios.AnyAsync(x => x.Email == req.Email);
        if (emailExists) return BadRequest("Ese email ya está registrado.");

        var userExists = await _db.Usuarios.AnyAsync(x => x.Username == req.Username);
        if (userExists) return BadRequest("Ese usuario ya existe.");

        // Rol por defecto: Usuario
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == "Usuario");
        if (role is null) return BadRequest("No existe el rol 'Usuario'. Crea los roles en la BD.");

        var user = new StockMaster.Domain.Entities.Usuario
        {
            NombreCompleto = req.NombreCompleto.Trim(),
            Email = req.Email.Trim(),
            Username = req.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            IdRole = role.IdRole,
            Estado = "Activo",
            CreatedAt = DateTime.UtcNow
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user.IdUsuario, user.Username, role.Nombre);

        return Ok(new AuthResponse(
            user.IdUsuario,
            user.NombreCompleto,
            user.Email,
            user.Username,
            role.Nombre,
            token
        ));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Usuario y contraseña son obligatorios.");

        var user = await _db.Usuarios.FirstOrDefaultAsync(x => x.Username == req.Username);
        if (user is null) return Unauthorized("Credenciales inválidas.");

        if (user.Estado != "Activo")
            return Unauthorized("Usuario inactivo.");

        var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!ok) return Unauthorized("Credenciales inválidas.");

        var roleName = await _db.Roles
            .Where(r => r.IdRole == user.IdRole)
            .Select(r => r.Nombre)
            .FirstOrDefaultAsync() ?? "Usuario";

        user.UltimoAcceso = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user.IdUsuario, user.Username, roleName);

        return Ok(new AuthResponse(
            user.IdUsuario,
            user.NombreCompleto,
            user.Email,
            user.Username,
            roleName,
            token
        ));
    }

    private string GenerateJwt(int idUsuario, string username, string role)
    {
        var key = _config["Jwt:Key"];
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var minutesStr = _config["Jwt:Minutes"];

        if (string.IsNullOrWhiteSpace(key))
            throw new Exception("Falta Jwt:Key en appsettings.json");

        var minutes = 60;
        if (!string.IsNullOrWhiteSpace(minutesStr) && int.TryParse(minutesStr, out var m)) minutes = m;

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim("idUsuario", idUsuario.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

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
