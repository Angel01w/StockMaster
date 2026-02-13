using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using StockMaster.Infrastructure.Data;

namespace StockMaster.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Producto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoriaId,
        [FromQuery] int? proveedorId,
        [FromQuery] bool? stockBajo)
    {
        var q = _db.Productos
            .AsNoTracking()
            .Include(x => x.Categoria)
            .Include(x => x.Proveedor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Nombre.Contains(search) || x.Codigo.Contains(search));

        if (categoriaId.HasValue)
            q = q.Where(x => x.IdCategoria == categoriaId.Value);

        if (proveedorId.HasValue)
            q = q.Where(x => x.IdProveedor == proveedorId.Value);

        if (stockBajo == true)
            q = q.Where(x => x.StockActual <= x.StockMinimo);

        return await q.OrderBy(x => x.Nombre).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetById(int id)
    {
        var item = await _db.Productos
            .AsNoTracking()
            .Include(x => x.Categoria)
            .Include(x => x.Proveedor)
            .FirstOrDefaultAsync(x => x.IdProducto == id);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> Create(Producto producto)
    {
        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = producto.IdProducto }, producto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Producto dto)
    {
        var item = await _db.Productos.FirstOrDefaultAsync(x => x.IdProducto == id);
        if (item is null) return NotFound();

        item.Codigo = dto.Codigo;
        item.Nombre = dto.Nombre;
        item.Descripcion = dto.Descripcion;
        item.IdCategoria = dto.IdCategoria;
        item.IdProveedor = dto.IdProveedor;
        item.PrecioCompra = dto.PrecioCompra;
        item.PrecioVenta = dto.PrecioVenta;
        item.StockMinimo = dto.StockMinimo;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Productos.FirstOrDefaultAsync(x => x.IdProducto == id);
        if (item is null) return NotFound();

        _db.Productos.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("stock-bajo")]
    public async Task<ActionResult<List<Producto>>> StockBajo()
    {
        var data = await _db.Productos
            .AsNoTracking()
            .Where(x => x.StockActual <= x.StockMinimo)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        return Ok(data);
    }
}
