using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SchoolClass>(e =>
        {
            e.ToTable("classes", t =>
                t.HasCheckConstraint("ck_classes_total_seats_positive", "total_seats > 0"));
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
        });

        model.Entity<Hold>(e =>
        {
            e.ToTable("holds");
            e.HasKey(h => h.Id);

            e.Property(h => h.StudentId).HasMaxLength(17).IsRequired();

            // Store status as readable text ("HELD") instead of a number.
            e.Property(h => h.Status)
                .HasMaxLength(16)
                .HasConversion(s => s.ToApiString(), s => HoldStatusExtensions.FromApiString(s));

            e.HasOne(h => h.Class)
                .WithMany(c => c.Holds)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // Database-level safety net for "one active application per student per class":
            // at most one HELD or CONFIRMED row per (class, student).
            e.HasIndex(h => new { h.ClassId, h.StudentId })
                .IsUnique()
                .HasFilter("status IN ('HELD', 'CONFIRMED')")
                .HasDatabaseName("ux_holds_active_student_per_class");

            // Fast seat counting per class.
            e.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt });

            // Fast "my applications" lookup.
            e.HasIndex(h => h.StudentId);
        });
    }
}
