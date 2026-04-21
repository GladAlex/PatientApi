using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PatientApi.Models;

namespace PatientApi.Data;

public class PatientApiDbContext : DbContext
{
    public PatientApiDbContext(DbContextOptions<PatientApiDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientName> PatientNames => Set<PatientName>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Patient
        modelBuilder.Entity<Patient>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.BirthDate).IsRequired();
            e.HasIndex(p => p.BirthDate).HasDatabaseName("IX_Patients_BirthDate");
        });

        // PatientName - same Guid is PK and FK
        modelBuilder.Entity<PatientName>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Family).IsRequired().HasMaxLength(255);
            e.Property(n => n.Use).HasMaxLength(50);

            // ValueComparer required by EF Core when using a ValueConverter on a collection type.
            // Without it EF Core cannot detect changes to the list (snapshot comparison fails).
            var givenComparer = new ValueComparer<List<string>>(
                (l1, l2) => l1 != null && l2 != null && l1.SequenceEqual(l2),
                l => l.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                l => l.ToList()   // snapshot: deep-copy so mutations are detected
            );

            // Store Given as JSON string
            e.Property(n => n.Given)
                .HasConversion(
                    v => string.Join("|", v),
                    v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList()
                )
                .HasMaxLength(1000)
                .Metadata.SetValueComparer(givenComparer); ;

            // One-to-one: PatientName.Id == Patient.Id
            e.HasOne(n => n.Patient)
                .WithOne(p => p.Name)
                .HasForeignKey<PatientName>(n => n.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
