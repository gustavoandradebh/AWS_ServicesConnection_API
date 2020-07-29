using Microsoft.EntityFrameworkCore;
using NFD.Domain.Data;

namespace NFD.Infrastructure
{
    public class AmazonRDSContext : DbContext
    {
        public AmazonRDSContext(DbContextOptions<AmazonRDSContext> options) : base(options) { }

        public DbSet<ImageEntity> Images { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ImageEntity>(entity =>
            {
                entity.HasKey(p => p.Id);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
