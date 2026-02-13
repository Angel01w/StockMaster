using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriasController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Categoria>>> GetAll([FromQuery] string? search)
    {
        var q = _db.Categorias.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Nombre.Contains(search));

        return await q.OrderBy(x => x.Nombre).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Categoria>> GetById(int id)
    {
        var item = await _db.Categorias.AsNoTracking().FirstOrDefaultAsync(x => x.IdCategoria == id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Categoria>> Create(Categoria categoria)
    {
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = categoria.IdCategoria }, categoria);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Categoria dto)
    {
        var item = await _db.Categorias.FirstOrDefaultAsync(x => x.IdCategoria == id);
        if (item is null) return NotFound();

        item.Nombre = dto.Nombre;
        item.Descripcion = dto.Descripcion;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Categorias.FirstOrDefaultAsync(x => x.IdCategoria == id);
        if (item is null) return NotFound();

        _db.Categorias.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
