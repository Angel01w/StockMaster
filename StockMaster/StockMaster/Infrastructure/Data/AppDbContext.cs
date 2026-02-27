using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;

namespace StockMaster.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<MotivoMovimiento> MotivosMovimiento => Set<MotivoMovimiento>();
        public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

        public DbSet<Area> Areas => Set<Area>();
        public DbSet<UsuarioArea> UsuarioAreas => Set<UsuarioArea>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

    
            modelBuilder.Entity<Usuario>()
                .Property(u => u.AreaId)
                .HasColumnName("AreaId");

            modelBuilder.Entity<Producto>()
                .Property(p => p.IdArea)
                .HasColumnName("IdArea");


            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.IdRole)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany()
                .HasForeignKey(p => p.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Proveedor)
                .WithMany()
                .HasForeignKey(p => p.IdProveedor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Producto)
                .WithMany()
                .HasForeignKey(m => m.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Motivo)
                .WithMany()
                .HasForeignKey(m => m.IdMotivo)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MotivoMovimiento>()
                .Property(x => x.TipoAplica)
                .HasMaxLength(10);

            modelBuilder.Entity<MovimientoInventario>()
                .Property(x => x.Tipo)
                .HasMaxLength(10);


            modelBuilder.Entity<UsuarioArea>()
                .ToTable("UsuarioAreas");

            modelBuilder.Entity<UsuarioArea>()
                .HasKey(x => new { x.IdUsuario, x.IdArea });

            modelBuilder.Entity<UsuarioArea>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsuarioArea>()
                .HasOne(x => x.Area)
                .WithMany(a => a.UsuarioAreas)
                .HasForeignKey(x => x.IdArea)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}