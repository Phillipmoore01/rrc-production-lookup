using Microsoft.EntityFrameworkCore;

namespace Rrc.Production.Data;

public class ProductionDbContext(DbContextOptions<ProductionDbContext> options) : DbContext(options)
{
    public DbSet<Lease> Leases => Set<Lease>();

    public DbSet<LeaseProductionMonth> ProductionMonths => Set<LeaseProductionMonth>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lease>(lease =>
        {
            lease.Property(l => l.DistrictCode).HasMaxLength(2).IsUnicode(false);
            lease.Property(l => l.LeaseNumber).HasMaxLength(6).IsUnicode(false);
            lease.Property(l => l.WellType).HasConversion<string>().HasMaxLength(3);
            lease.Property(l => l.LeaseName).HasMaxLength(64);
            lease.Property(l => l.OperatorName).HasMaxLength(64);
            lease.Property(l => l.CountyName).HasMaxLength(32);

            lease.HasIndex(l => new { l.DistrictCode, l.LeaseNumber, l.WellType }).IsUnique();
            lease.HasIndex(l => l.OperatorName);
            lease.HasIndex(l => l.CountyName);
        });

        modelBuilder.Entity<LeaseProductionMonth>(month =>
        {
            month.HasKey(m => new { m.LeaseId, m.Month });

            month.Property(m => m.OilBarrels).HasPrecision(14, 2);
            month.Property(m => m.GasMcf).HasPrecision(14, 2);
            month.Property(m => m.CondensateBarrels).HasPrecision(14, 2);
            month.Property(m => m.CasingheadGasMcf).HasPrecision(14, 2);

            month.HasOne(m => m.Lease)
                .WithMany(l => l.ProductionMonths)
                .HasForeignKey(m => m.LeaseId);
        });
    }
}
