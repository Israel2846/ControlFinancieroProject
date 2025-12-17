using Microsoft.EntityFrameworkCore;
using ControlFinancieroProject.Models;

namespace ControlFinancieroProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Transaccion> Transaccion { get; set; } = default!;
        public DbSet<Categoria> Categoria { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.Monto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.SaldoAnterior)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.SaldoNuevo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .HasOne(t => t.Categoria)
                .WithMany(c => c.Transacciones)
                .HasForeignKey(t => t.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
