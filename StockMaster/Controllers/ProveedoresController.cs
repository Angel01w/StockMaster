using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProveedoresController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProveedoresController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Proveedor>>> GetAll([FromQuery] string? search)
    {
        var q = _db.Proveedores.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x =>
                x.NombreEmpresa.Contains(search) ||
                (x.PersonaContacto != null && x.PersonaContacto.Contains(search)) ||
                (x.Email != null && x.Email.Contains(search)));

        return await q.OrderBy(x => x.NombreEmpresa).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Proveedor>> GetById(int id)
    {
        var item = await _db.Proveedores.AsNoTracking().FirstOrDefaultAsync(x => x.IdProveedor == id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Proveedor>> Create(Proveedor proveedor)
    {
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = proveedor.IdProveedor }, proveedor);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Proveedor dto)
    {
        var item = await _db.Proveedores.FirstOrDefaultAsync(x => x.IdProveedor == id);
        if (item is null) return NotFound();

        item.NombreEmpresa = dto.NombreEmpresa;
        item.PersonaContacto = dto.PersonaContacto;
        item.Email = dto.Email;
        item.Telefono = dto.Telefono;
        item.Direccion = dto.Direccion;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Proveedores.FirstOrDefaultAsync(x => x.IdProveedor == id);
        if (item is null) return NotFound();

        _db.Proveedores.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
