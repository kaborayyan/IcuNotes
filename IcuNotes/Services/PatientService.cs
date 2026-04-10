using IcuNotes.Data;
using IcuNotes.Models;
using Microsoft.EntityFrameworkCore;

namespace IcuNotes.Services
{
    public class PatientService
    {
        // Create a fresh DbContext for each operation
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public PatientService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // Return all active patients
        public async Task<List<Patient>> GetAllAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var patients = await context.Patients
                .AsNoTracking()
                // Load the related admission unit so pages can display its name safely
                .Include(p => p.AdmissionUnitCatalog)
                .ToListAsync();

            return patients
                .OrderBy(p => GetBedPrefix(p.Bed))
                .ThenBy(p => GetBedNumber(p.Bed))
                .ThenBy(p => p.Name)
                .ToList();
        }

        // Return one active patient by Id
        public async Task<Patient?> GetByIdAsync(int id)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Patients
                .AsNoTracking()
                // Load related data needed by PatientDetails.razor
                .Include(p => p.AdmissionUnitCatalog)
                .Include(p => p.PatientSummary)
                .Include(p => p.PatientDateEvents)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // Add a new active patient
        public async Task AddAsync(Patient patient)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        // Update an existing active patient.
        // This version updates the patient itself plus the related
        // PatientSummary and PatientDateEvents in a controlled way.
        public async Task UpdateAsync(Patient patient)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Load the existing patient from the database together with
            // the related child data that can be edited on PatientDetails.
            var existingPatient = await context.Patients
                .Include(p => p.PatientSummary)
                .Include(p => p.PatientDateEvents)
                .FirstOrDefaultAsync(p => p.Id == patient.Id);

            if (existingPatient is null)
                return;

            // 1) Update the main Patient fields
            existingPatient.Name = patient.Name;
            existingPatient.Age = patient.Age;
            existingPatient.Weight = patient.Weight;
            existingPatient.Bed = patient.Bed;
            existingPatient.Diagnosis = patient.Diagnosis;
            existingPatient.AdmissionUnitCatalogId = patient.AdmissionUnitCatalogId;

            // 2) Update or create PatientSummary
            if (patient.PatientSummary is null)
            {
                // If the incoming object has no summary, do nothing here.
                // This keeps the current database summary unchanged.
            }
            else if (existingPatient.PatientSummary is null)
            {
                // Create a new summary row if the database does not have one yet.
                existingPatient.PatientSummary = new PatientSummary
                {
                    PatientId = existingPatient.Id,
                    CombinedHistory = patient.PatientSummary.CombinedHistory,
                    EventsTodo = patient.PatientSummary.EventsTodo
                };
            }
            else
            {
                // Update the existing summary row.
                existingPatient.PatientSummary.CombinedHistory = patient.PatientSummary.CombinedHistory;
                existingPatient.PatientSummary.EventsTodo = patient.PatientSummary.EventsTodo;
            }

            // 3) Update PatientDateEvents
            // We compare the incoming events from the page with the events
            // already stored in the database.
            var incomingEvents = patient.PatientDateEvents ?? new List<PatientDateEvent>();

            // Remove database events that are no longer present in the incoming list.
            // We only compare events that already have a real Id (> 0).
            var incomingExistingIds = incomingEvents
                .Where(e => e.Id > 0)
                .Select(e => e.Id)
                .ToHashSet();

            var eventsToRemove = existingPatient.PatientDateEvents
                .Where(dbEvent => !incomingExistingIds.Contains(dbEvent.Id))
                .ToList();

            foreach (var dbEvent in eventsToRemove)
            {
                context.Remove(dbEvent);
            }

            // Add new events and update existing ones.
            foreach (var incomingEvent in incomingEvents)
            {
                if (incomingEvent.Id == 0)
                {
                    // This is a new event created on the page and not yet saved.
                    existingPatient.PatientDateEvents.Add(new PatientDateEvent
                    {
                        PatientId = existingPatient.Id,
                        EventDate = incomingEvent.EventDate,
                        Title = incomingEvent.Title
                    });
                }
                else
                {
                    // This is an existing event. Find it in the database and update it.
                    var existingEvent = existingPatient.PatientDateEvents
                        .FirstOrDefault(e => e.Id == incomingEvent.Id);

                    if (existingEvent is null)
                    {
                        // Safety fallback:
                        // if for some reason it was not loaded, add it as a new row.
                        existingPatient.PatientDateEvents.Add(new PatientDateEvent
                        {
                            PatientId = existingPatient.Id,
                            EventDate = incomingEvent.EventDate,
                            Title = incomingEvent.Title
                        });
                    }
                    else
                    {
                        existingEvent.EventDate = incomingEvent.EventDate;
                        existingEvent.Title = incomingEvent.Title;
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        // Return all admission units for the dropdown list
        public async Task<List<AdmissionUnitCatalog>> GetAdmissionUnitsAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.AdmissionUnitCatalogs
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // Add starter admission units only if the table is still empty.
        // This is a simple seed method for now.
        public async Task SeedAdmissionUnitsIfEmptyAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var hasAnyUnits = await context.AdmissionUnitCatalogs.AnyAsync();

            if (hasAnyUnits)
                return;

            var units = new List<AdmissionUnitCatalog>
            {
                new() { Name = "ER" },
                new() { Name = "Ward" },
                new() { Name = "Operating Room" },
                new() { Name = "Recovery" },
                new() { Name = "Another Hospital" }
            };

            context.AdmissionUnitCatalogs.AddRange(units);
            await context.SaveChangesAsync();
        }

        // Move a patient from the active table to the archive table.
        // This version also copies:
        // - CombinedHistory from PatientSummary
        // - all PatientDateEvents into ArchivedPatientDateEvents
        public async Task ArchiveAsync(int id)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var patient = await context.Patients
                .Include(p => p.PatientSummary)
                .Include(p => p.PatientDateEvents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient is null)
                return;

            var archivedPatient = new ArchivedPatient
            {
                Name = patient.Name,
                Age = patient.Age,
                Weight = patient.Weight,
                Diagnosis = patient.Diagnosis,
                CombinedHistory = patient.PatientSummary?.CombinedHistory
            };

            // Copy active timeline events into the archived timeline table.
            foreach (var patientDateEvent in patient.PatientDateEvents)
            {
                archivedPatient.ArchivedPatientDateEvents.Add(new ArchivedPatientDateEvent
                {
                    EventDate = patientDateEvent.EventDate,
                    Title = patientDateEvent.Title
                });
            }

            context.ArchivedPatients.Add(archivedPatient);
            context.Patients.Remove(patient);

            await context.SaveChangesAsync();
        }

        // Return all archived patients
        public async Task<List<ArchivedPatient>> GetArchivedAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.ArchivedPatients
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        // Restore an archived patient back to the active patients table
        public async Task RestorePatientAsync(int archivedPatientId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Find the archived patient first
            var archivedPatient = await context.ArchivedPatients
                .FirstOrDefaultAsync(x => x.Id == archivedPatientId);

            if (archivedPatient is null)
            {
                return;
            }

            // Create a new active patient row from the archived data.
            // We do NOT copy the Id because Patient and ArchivedPatient
            // are different tables and EF should create a fresh active record.
            var patient = new Patient
            {
                Name = archivedPatient.Name,
                Age = archivedPatient.Age,
                Weight = archivedPatient.Weight,
                Diagnosis = archivedPatient.Diagnosis
            };

            context.Patients.Add(patient);

            // Remove it from archive after recreating it as an active patient
            context.ArchivedPatients.Remove(archivedPatient);

            await context.SaveChangesAsync();
        }

        // Delete a patient permanently from the archived patients table
        public async Task DeleteArchivedAsync(int archivedPatientId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var archivedPatient = await context.ArchivedPatients.FindAsync(archivedPatientId);

            if (archivedPatient is null)
                return;

            context.ArchivedPatients.Remove(archivedPatient);
            await context.SaveChangesAsync();
        }

        // Helper method to extract the letter part of the bed value
        // Example: "B12" -> "B"
        private static string GetBedPrefix(string? bed)
        {
            if (string.IsNullOrWhiteSpace(bed))
                return string.Empty;

            var letters = new string(bed
                .TakeWhile(c => !char.IsDigit(c))
                .ToArray());

            return letters.Trim().ToUpper();
        }

        // Helper method to extract the number part of the bed value
        // Example: "B12" -> 12
        private static int GetBedNumber(string? bed)
        {
            if (string.IsNullOrWhiteSpace(bed))
                return int.MaxValue;

            var digits = new string(bed
                .SkipWhile(c => !char.IsDigit(c))
                .TakeWhile(char.IsDigit)
                .ToArray());

            return int.TryParse(digits, out var number) ? number : int.MaxValue;
        }
    }
}