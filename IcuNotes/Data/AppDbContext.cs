using IcuNotes.Models;
using Microsoft.EntityFrameworkCore;

namespace IcuNotes.Data
{
    public class AppDbContext : DbContext
    {
        // EF Core passes the database configuration here
        // from MauiProgram.cs when the app starts.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // This represents the Patients table in the database.
        // Each Patient object will become a row in that table.
        public DbSet<Patient> Patients { get; set; } = null!;

        // Archived patients are stored separately from active patients.
        public DbSet<ArchivedPatient> ArchivedPatients { get; set; } = null!;

        // Catalog table for admission unit dropdown values.
        public DbSet<AdmissionUnitCatalog> AdmissionUnitCatalogs { get; set; } = null!;

        // One summary row per patient.
        public DbSet<PatientSummary> PatientSummaries { get; set; } = null!;

        // Many timeline/date-event rows per patient.
        public DbSet<PatientDateEvent> PatientDateEvents { get; set; } = null!;

        // Many timeline/date-event rows per archived patient.
        public DbSet<ArchivedPatientDateEvent> ArchivedPatientDateEvents { get; set; } = null!;

        // Shared medication catalog.
        // This stores reusable medication names such as Propofol and Midazolam.
        public DbSet<Medication> Medications { get; set; } = null!;

        // One neurology row per patient.
        public DbSet<Neurology> Neurologies { get; set; } = null!;

        // Many medication rows can belong to one neurology section.
        public DbSet<NeurologyMedication> NeurologyMedications { get; set; } = null!;

        // This is the place for extra database configuration.
        // We use it to define relationships clearly so EF Core
        // does not guess the wrong foreign keys.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Patient -> AdmissionUnitCatalog
            // Many patients can belong to the same admission unit.
            // If an admission unit is deleted, set the foreign key to null
            // instead of deleting the patient.
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.AdmissionUnitCatalog)
                .WithMany()
                .HasForeignKey(p => p.AdmissionUnitCatalogId)
                .OnDelete(DeleteBehavior.SetNull);

            // Patient <-> PatientSummary
            // One patient has one summary, and one summary belongs to one patient.
            // We explicitly connect both navigation properties so EF Core
            // knows this uses PatientSummary.PatientId as the foreign key.
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.PatientSummary)
                .WithOne(ps => ps.Patient)
                .HasForeignKey<PatientSummary>(ps => ps.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient <-> Neurology
            // One patient has one neurology record, and one neurology record
            // belongs to one patient.
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Neurology)
                .WithOne(n => n.Patient)
                .HasForeignKey<Neurology>(n => n.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient <-> PatientDateEvent
            // One patient can have many date events,
            // and each date event belongs to one patient.
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.PatientDateEvents)
                .WithOne(pde => pde.Patient)
                .HasForeignKey(pde => pde.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Neurology <-> NeurologyMedication
            // One neurology section can have many medication rows,
            // and each medication row belongs to one neurology section.
            modelBuilder.Entity<Neurology>()
                .HasMany(n => n.Medications)
                .WithOne(nm => nm.Neurology)
                .HasForeignKey(nm => nm.NeurologyId)
                .OnDelete(DeleteBehavior.Cascade);

            // NeurologyMedication -> Medication
            // Many neurology medication rows can point to one shared
            // medication catalog item.
            // Restrict delete so a medication cannot be removed from the catalog
            // while it is still being used by patients.
            modelBuilder.Entity<NeurologyMedication>()
                .HasOne(nm => nm.Medication)
                .WithMany()
                .HasForeignKey(nm => nm.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ArchivedPatient <-> ArchivedPatientDateEvent
            // One archived patient can have many archived date events,
            // and each archived date event belongs to one archived patient.
            modelBuilder.Entity<ArchivedPatient>()
                .HasMany(ap => ap.ArchivedPatientDateEvents)
                .WithOne(apde => apde.ArchivedPatient)
                .HasForeignKey(apde => apde.ArchivedPatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // This makes PatientId unique in PatientSummaries,
            // which enforces one summary per patient.
            modelBuilder.Entity<PatientSummary>()
                .HasIndex(ps => ps.PatientId)
                .IsUnique();

            // This makes PatientId unique in Neurologies,
            // which enforces one neurology record per patient.
            modelBuilder.Entity<Neurology>()
                .HasIndex(n => n.PatientId)
                .IsUnique();

            // This makes medication names unique in the shared catalog.
            // That helps prevent duplicate entries like two separate
            // rows both named "Propofol".
            modelBuilder.Entity<Medication>()
                .HasIndex(m => m.Name)
                .IsUnique();
        }
    }
}