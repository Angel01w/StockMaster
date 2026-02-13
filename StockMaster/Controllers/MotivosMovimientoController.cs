using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MotivosMovimientoController : ControllerBase
{
    private readonly AppDbContext _db;
    public MotivosMovimientoController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MotivoMovimiento>>> GetAll()
        => await _db.MotivosMovimiento.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<MotivoMovimiento>> Create(MotivoMovimiento motivo)
    {
        _db.MotivosMovimiento.Add(motivo);
        await _db.SaveChangesAsync();
        return Ok(motivo);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MotivoMovimiento dto)
    {
        var item = await _db.MotivosMovimiento.FirstOrDefaultAsync(x => x.IdMotivo == id);
        if (item is null) return NotFound();

        item.Nombre = dto.Nombre;
        item.TipoAplica = dto.TipoAplica;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.MotivosMovimiento.FirstOrDefaultAsync(x => x.IdMotivo == id);
        if (item is null) return NotFound();

        _db.MotivosMovimiento.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
