using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;

namespace StockMaster.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UsuarioArea> UsuarioAreas { get; set; }
    public DbSet<UsuarioCategoria> UsuarioCategorias { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<MotivoMovimiento> MotivosMovimiento { get; set; }
    public DbSet<Area> Areas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasKey(r => r.IdRole);

        modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.IdRole)
            .HasConstraintName("FK_Usuarios_Roles_IdRole");

        modelBuilder.Entity<UsuarioArea>()
            .HasKey(x => new { x.IdUsuario, x.IdArea });

        modelBuilder.Entity<UsuarioCategoria>()
            .HasKey(x => new { x.IdUsuario, x.IdCategoria });

        modelBuilder.Entity<UsuarioCategoria>()
            .HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.IdUsuario)
            .HasConstraintName("FK_UsuarioCategorias_Usuarios_IdUsuario");

        modelBuilder.Entity<UsuarioCategoria>()
            .HasOne(x => x.Categoria)
            .WithMany()
            .HasForeignKey(x => x.IdCategoria)
            .HasConstraintName("FK_UsuarioCategorias_Categorias_IdCategoria");

        modelBuilder.Entity<Area>().HasKey(a => a.IdArea);

        modelBuilder.Entity<Categoria>().HasKey(c => c.IdCategoria);

        modelBuilder.Entity<Proveedor>().HasKey(p => p.IdProveedor);

        modelBuilder.Entity<Producto>().HasKey(p => p.IdProducto);

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

        modelBuilder.Entity<Producto>()
            .HasOne<Area>()
            .WithMany()
            .HasForeignKey(p => p.IdArea)
            .HasConstraintName("FK_Productos_Areas");

        modelBuilder.Entity<MotivoMovimiento>().HasKey(m => m.IdMotivo);

        modelBuilder.Entity<MovimientoInventario>().HasKey(m => m.IdMovimiento);

        modelBuilder.Entity<MovimientoInventario>()
            .Property(m => m.IdMovimiento)
            .ValueGeneratedOnAdd()
            .HasColumnType("bigint");

        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.Producto)
            .WithMany(p => p.MovimientosInventario)
            .HasForeignKey(m => m.IdProducto)
            .HasConstraintName("FK_MovimientosInventario_Productos_IdProducto");

        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.Motivo)
            .WithMany()
            .HasForeignKey(m => m.IdMotivo)
            .HasConstraintName("FK_MovimientosInventario_Motivos_IdMotivo");

        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.IdUsuario)
            .HasConstraintName("FK_MovimientosInventario_Usuarios_IdUsuario");
    }
}