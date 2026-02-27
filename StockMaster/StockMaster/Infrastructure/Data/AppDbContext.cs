using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;

namespace StockMaster.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UsuarioArea> UsuarioAreas { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<MotivoMovimiento> MotivosMovimiento { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>()
            .HasKey(r => r.IdRole);

        modelBuilder.Entity<Usuario>()
            .HasKey(u => u.IdUsuario);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.IdRole)
            .HasConstraintName("FK_Usuarios_Roles_IdRole");

        modelBuilder.Entity<UsuarioArea>()
            .HasKey(x => new { x.IdUsuario, x.IdArea });

        modelBuilder.Entity<Categoria>()
            .HasKey(c => c.IdCategoria);

        modelBuilder.Entity<Proveedor>()
            .HasKey(p => p.IdProveedor);

        modelBuilder.Entity<Producto>()
            .HasKey(p => p.IdProducto);

        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany()
            .HasForeignKey(p => p.IdCategoria)
            .HasConstraintName("FK_Productos_Categorias_IdCategoria");

        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Proveedor)
            .WithMany()
            .HasForeignKey(p => p.IdProveedor)
            .HasConstraintName("FK_Productos_Proveedores_IdProveedor");

        modelBuilder.Entity<MotivoMovimiento>()
            .HasKey(m => m.IdMotivo);

        modelBuilder.Entity<MovimientoInventario>()
            .HasKey(m => m.IdMovimiento);
    }
}