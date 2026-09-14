using FieldCheck.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FieldCheck.Api.Data;

public class FieldCheckDbContext(DbContextOptions<FieldCheckDbContext> options) : DbContext(options)
{
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<InspectionPhoto> InspectionPhotos => Set<InspectionPhoto>();

    // SQL Server datetime2 carries no offset, so EF materializes DateTimeKind.Unspecified.
    // Stamp values as UTC on the way out so serializers emit "Z" instead of a local offset.
    private static readonly ValueConverter<DateTime, DateTime> UtcKind =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Enums are stored as strings, guarded by CHECK constraints so the database rejects
        // values the application does not know about.
        b.Entity<Site>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Region).HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Asset>(e =>
        {
            e.Property(x => x.Tag).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => new { x.SiteId, x.Tag }).IsUnique();
            e.HasIndex(x => new { x.SiteId, x.Status });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Assets_InspectionIntervalDays", "[InspectionIntervalDays] BETWEEN 1 AND 365");
                t.HasCheckConstraint("CK_Assets_Status", "[Status] IN ('InService', 'OutOfService')");
            });
            e.HasOne(x => x.Site).WithMany(s => s.Assets).HasForeignKey(x => x.SiteId);
        });

        b.Entity<Inspection>(e =>
        {
            e.Property(x => x.Inspector).HasMaxLength(100);
            e.Property(x => x.InspectedAtUtc).HasConversion(UtcKind);
            e.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.AssetId, x.InspectedAtUtc }).IsDescending(false, true)
                .HasDatabaseName("IX_Inspections_AssetId_InspectedAtUtc");
            e.ToTable(t => t.HasCheckConstraint("CK_Inspections_Severity",
                "[Severity] IN ('None', 'Minor', 'Major', 'Critical')"));
            e.HasOne(x => x.Asset).WithMany(a => a.Inspections).HasForeignKey(x => x.AssetId);
        });

        b.Entity<InspectionPhoto>(e =>
        {
            e.Property(x => x.BlobName).HasMaxLength(200);
            e.Property(x => x.ContentType).HasMaxLength(100);
            e.Property(x => x.UploadedAtUtc).HasConversion(UtcKind);
            e.HasOne(x => x.Inspection).WithMany(i => i.Photos).HasForeignKey(x => x.InspectionId);
        });
    }
}
