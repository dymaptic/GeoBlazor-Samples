using Microsoft.EntityFrameworkCore;
using SpatialDispatch.Data.Entities;

namespace SpatialDispatch.Data;

/// <summary>
///     The dispatch database. Three tables, two of which happen to have a spatial column.
/// </summary>
/// <remarks>
///     Spatial values are stored as SQL Server <c>geography</c> rather than <c>geometry</c>, which is what
///     makes <c>STDistance</c> return meters instead of degrees. The column type is written out explicitly
///     even where the provider would choose it anyway, because the audience needs to see the decision.
/// </remarks>
public sealed class DispatchDbContext(DbContextOptions<DispatchDbContext> options) : DbContext(options)
{
    public DbSet<Territory> Territories => Set<Territory>();

    public DbSet<Technician> Technicians => Set<Technician>();

    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Territory>(territory =>
        {
            // The demo contract fixes every ID, so the seed inserts them verbatim rather than letting SQL
            // Server hand out identity values that would not match the document or the slides.
            territory.Property(t => t.Id).ValueGeneratedNever();
            territory.Property(t => t.Name).HasMaxLength(100);
            territory.Property(t => t.Boundary).HasColumnType("geography");
        });

        modelBuilder.Entity<Technician>(technician =>
        {
            technician.Property(t => t.Id).ValueGeneratedNever();
            technician.Property(t => t.Name).HasMaxLength(100);
            technician.Property(t => t.LocationLabel).HasMaxLength(200);
            technician.Property(t => t.Location).HasColumnType("geography");

            technician.HasOne(t => t.Territory)
                .WithMany(t => t.Technicians)
                .HasForeignKey(t => t.TerritoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Job>(job =>
        {
            job.Property(j => j.Id).ValueGeneratedNever();
            job.Property(j => j.Customer).HasMaxLength(200);
            job.Property(j => j.Summary).HasMaxLength(400);
            job.Property(j => j.MapAnchor).HasMaxLength(200);
            job.Property(j => j.Priority).HasConversion<string>().HasMaxLength(20);
            job.Property(j => j.Location).HasColumnType("geography");
        });
    }
}
