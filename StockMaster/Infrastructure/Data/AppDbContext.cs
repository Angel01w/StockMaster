using Microsoft.EntityFrameworkCore;
using StockMaster.Domain.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario -> Role
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.IdRole)
                .OnDelete(DeleteBehavior.Restrict);

            // Producto -> Categoria
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany()
                .HasForeignKey(p => p.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            // Producto -> Proveedor
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Proveedor)
                .WithMany()
                .HasForeignKey(p => p.IdProveedor)
                .OnDelete(DeleteBehavior.Restrict);

            // Movimiento -> Producto
            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Producto)
                .WithMany()
                .HasForeignKey(m => m.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // Movimiento -> Usuario
            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Movimiento -> Motivo (nullable)
            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Motivo)
                .WithMany()
                .HasForeignKey(m => m.IdMotivo)
                .OnDelete(DeleteBehavior.SetNull);

            // Opcional: defaults tipo string
            modelBuilder.Entity<MotivoMovimiento>()
                .Property(x => x.TipoAplica)
                .HasMaxLength(10);

            modelBuilder.Entity<MovimientoInventario>()
                .Property(x => x.Tipo)
                .HasMaxLength(10);
        }
    }
}
