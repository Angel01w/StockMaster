using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RolesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Role>>> GetAll()
        => await _db.Roles.AsNoTracking().ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Role>> GetById(int id)
    {
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.IdRole == id);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<Role>> Create(Role role)
    {
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = role.IdRole }, role);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Role dto)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.IdRole == id);
        if (role is null) return NotFound();

        role.Nombre = dto.Nombre;
        role.Descripcion = dto.Descripcion;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.IdRole == id);
        if (role is null) return NotFound();

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
