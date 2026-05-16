using IcuNotes.Data;
using IcuNotes.Models;
using Microsoft.EntityFrameworkCore;

namespace IcuNotes.Services
{
    public class PatientService
    {
        // Create a fresh DbContext for each operation.
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public PatientService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // Return all active patients.
        public async Task<List<Patient>> GetAllAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var patients = await context.Patients
                .AsNoTracking()
                // Load the related admission unit so pages can display its name safely.
                .Include(p => p.AdmissionUnitCatalog)
                .ToListAsync();

            return patients
                .OrderBy(p => GetBedPrefix(p.Bed))
                .ThenBy(p => GetBedNumber(p.Bed))
                .ThenBy(p => p.Name)
                .ToList();
        }

        // Return one active patient by Id.
        public async Task<Patient?> GetByIdAsync(int id)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Patients
                .AsNoTracking()
                // Load related data needed by PatientDetails.razor.
                .Include(p => p.AdmissionUnitCatalog)
                .Include(p => p.PatientSummary)
                .Include(p => p.PatientDateEvents)
                .Include(p => p.Neurology)
                    .ThenInclude(n => n!.Medications)
                        .ThenInclude(nm => nm.Medication)
                .Include(p => p.Cardiology)
                    .ThenInclude(c => c!.Medications)
                        .ThenInclude(cm => cm.Medication)
                .Include(p => p.GastroIntestinal)
                    .ThenInclude(g => g!.Items)
                        .ThenInclude(gi => gi.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // Add a new active patient.
        public async Task AddAsync(Patient patient)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        // Update an existing active patient.
        //
        // This version updates:
        // - the main Patient fields
        // - PatientSummary
        // - Neurology
        // - Cardiology
        // - GastroIntestinal
        // - PatientDateEvents
        public async Task UpdateAsync(Patient patient)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Load the existing patient from the database together with
            // the related child data that can be edited on PatientDetails.
            var existingPatient = await context.Patients
                .Include(p => p.PatientSummary)
                .Include(p => p.PatientDateEvents)
                .Include(p => p.Neurology)
                    .ThenInclude(n => n!.Medications)
                .Include(p => p.Cardiology)
                    .ThenInclude(c => c!.Medications)
                .Include(p => p.GastroIntestinal)
                    .ThenInclude(g => g!.Items)
                .FirstOrDefaultAsync(p => p.Id == patient.Id);

            if (existingPatient is null)
                return;

            // 1) Update the main Patient fields.
            existingPatient.Name = patient.Name;
            existingPatient.Age = patient.Age;
            existingPatient.Weight = patient.Weight;
            existingPatient.Bed = patient.Bed;
            existingPatient.Diagnosis = patient.Diagnosis;
            existingPatient.AdmissionUnitCatalogId = patient.AdmissionUnitCatalogId;

            // 2) Update or create PatientSummary.
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

            // 3) Update or create Neurology.
            if (patient.Neurology is null)
            {
                // If the incoming object has no neurology section,
                // leave the current database neurology unchanged.
            }
            else if (existingPatient.Neurology is null)
            {
                // Create a new neurology row if the database does not have one yet.
                existingPatient.Neurology = new Neurology
                {
                    PatientId = existingPatient.Id,
                    GcsEye = patient.Neurology.GcsEye,
                    GcsVerbal = patient.Neurology.GcsVerbal,
                    GcsMotor = patient.Neurology.GcsMotor,
                    Pupils = patient.Neurology.Pupils,
                    MotorStatus = patient.Neurology.MotorStatus
                };

                // Add the incoming neurology medications to the new neurology row.
                foreach (var incomingMedication in patient.Neurology.Medications ?? new List<NeurologyMedication>())
                {
                    // Ignore incomplete rows where no catalog medication was selected yet.
                    if (incomingMedication.MedicationId <= 0)
                        continue;

                    existingPatient.Neurology.Medications.Add(new NeurologyMedication
                    {
                        MedicationId = incomingMedication.MedicationId,
                        Category = incomingMedication.Category,
                        Dose = incomingMedication.Dose
                    });
                }
            }
            else
            {
                // Update the existing neurology scalar fields.
                existingPatient.Neurology.GcsEye = patient.Neurology.GcsEye;
                existingPatient.Neurology.GcsVerbal = patient.Neurology.GcsVerbal;
                existingPatient.Neurology.GcsMotor = patient.Neurology.GcsMotor;
                existingPatient.Neurology.Pupils = patient.Neurology.Pupils;
                existingPatient.Neurology.MotorStatus = patient.Neurology.MotorStatus;

                // Update NeurologyMedication rows.
                var incomingMedications = patient.Neurology.Medications ?? new List<NeurologyMedication>();

                // Collect the real database Ids coming from the page.
                var incomingExistingMedicationIds = incomingMedications
                    .Where(m => m.Id > 0)
                    .Select(m => m.Id)
                    .ToHashSet();

                // Remove database rows that no longer exist in the incoming list.
                var medicationsToRemove = existingPatient.Neurology.Medications
                    .Where(dbMedication => !incomingExistingMedicationIds.Contains(dbMedication.Id))
                    .ToList();

                foreach (var dbMedication in medicationsToRemove)
                {
                    context.Remove(dbMedication);
                }

                // Add new rows and update existing rows.
                foreach (var incomingMedication in incomingMedications)
                {
                    // Ignore incomplete rows where no catalog medication was selected yet.
                    if (incomingMedication.MedicationId <= 0)
                        continue;

                    if (incomingMedication.Id == 0)
                    {
                        // This is a new neurology medication row created on the page.
                        existingPatient.Neurology.Medications.Add(new NeurologyMedication
                        {
                            MedicationId = incomingMedication.MedicationId,
                            Category = incomingMedication.Category,
                            Dose = incomingMedication.Dose
                        });
                    }
                    else
                    {
                        // This is an existing neurology medication row.
                        var existingMedication = existingPatient.Neurology.Medications
                            .FirstOrDefault(m => m.Id == incomingMedication.Id);

                        if (existingMedication is null)
                        {
                            // Safety fallback:
                            // if for some reason it was not loaded, add it as a new row.
                            existingPatient.Neurology.Medications.Add(new NeurologyMedication
                            {
                                MedicationId = incomingMedication.MedicationId,
                                Category = incomingMedication.Category,
                                Dose = incomingMedication.Dose
                            });
                        }
                        else
                        {
                            existingMedication.MedicationId = incomingMedication.MedicationId;
                            existingMedication.Category = incomingMedication.Category;
                            existingMedication.Dose = incomingMedication.Dose;
                        }
                    }
                }
            }

            // 4) Update or create Cardiology.
            if (patient.Cardiology is null)
            {
                // If the incoming object has no cardiology section,
                // leave the current database cardiology unchanged.
            }
            else if (existingPatient.Cardiology is null)
            {
                // Create a new cardiology row if the database does not have one yet.
                existingPatient.Cardiology = new Cardiology
                {
                    PatientId = existingPatient.Id,
                    HeartRate = patient.Cardiology.HeartRate,
                    SystolicBloodPressure = patient.Cardiology.SystolicBloodPressure,
                    DiastolicBloodPressure = patient.Cardiology.DiastolicBloodPressure,
                    CentralVenousPressure = patient.Cardiology.CentralVenousPressure,
                    InferiorVenaCava = patient.Cardiology.InferiorVenaCava,
                    CardiacIndex = patient.Cardiology.CardiacIndex,
                    CardiacOutput = patient.Cardiology.CardiacOutput,
                    SystemicVascularResistance = patient.Cardiology.SystemicVascularResistance,
                    PulmonaryCapillaryWedgePressure = patient.Cardiology.PulmonaryCapillaryWedgePressure,
                    Notes = patient.Cardiology.Notes
                };

                // Add the incoming cardiology medications to the new cardiology row.
                foreach (var incomingMedication in patient.Cardiology.Medications ?? new List<CardiologyMedication>())
                {
                    // Ignore incomplete rows where no catalog medication was selected yet.
                    if (incomingMedication.MedicationId <= 0)
                        continue;

                    existingPatient.Cardiology.Medications.Add(new CardiologyMedication
                    {
                        MedicationId = incomingMedication.MedicationId,
                        Category = incomingMedication.Category,
                        Dose = incomingMedication.Dose
                    });
                }
            }
            else
            {
                // Update the existing cardiology scalar fields.
                existingPatient.Cardiology.HeartRate = patient.Cardiology.HeartRate;
                existingPatient.Cardiology.SystolicBloodPressure = patient.Cardiology.SystolicBloodPressure;
                existingPatient.Cardiology.DiastolicBloodPressure = patient.Cardiology.DiastolicBloodPressure;
                existingPatient.Cardiology.CentralVenousPressure = patient.Cardiology.CentralVenousPressure;
                existingPatient.Cardiology.InferiorVenaCava = patient.Cardiology.InferiorVenaCava;
                existingPatient.Cardiology.CardiacIndex = patient.Cardiology.CardiacIndex;
                existingPatient.Cardiology.CardiacOutput = patient.Cardiology.CardiacOutput;
                existingPatient.Cardiology.SystemicVascularResistance = patient.Cardiology.SystemicVascularResistance;
                existingPatient.Cardiology.PulmonaryCapillaryWedgePressure = patient.Cardiology.PulmonaryCapillaryWedgePressure;
                existingPatient.Cardiology.Notes = patient.Cardiology.Notes;

                // Update CardiologyMedication rows.
                var incomingMedications = patient.Cardiology.Medications ?? new List<CardiologyMedication>();

                // Collect the real database Ids coming from the page.
                var incomingExistingMedicationIds = incomingMedications
                    .Where(m => m.Id > 0)
                    .Select(m => m.Id)
                    .ToHashSet();

                // Remove database rows that no longer exist in the incoming list.
                var medicationsToRemove = existingPatient.Cardiology.Medications
                    .Where(dbMedication => !incomingExistingMedicationIds.Contains(dbMedication.Id))
                    .ToList();

                foreach (var dbMedication in medicationsToRemove)
                {
                    context.Remove(dbMedication);
                }

                // Add new rows and update existing rows.
                foreach (var incomingMedication in incomingMedications)
                {
                    // Ignore incomplete rows where no catalog medication was selected yet.
                    if (incomingMedication.MedicationId <= 0)
                        continue;

                    if (incomingMedication.Id == 0)
                    {
                        // This is a new cardiology medication row created on the page.
                        existingPatient.Cardiology.Medications.Add(new CardiologyMedication
                        {
                            MedicationId = incomingMedication.MedicationId,
                            Category = incomingMedication.Category,
                            Dose = incomingMedication.Dose
                        });
                    }
                    else
                    {
                        // This is an existing cardiology medication row.
                        var existingMedication = existingPatient.Cardiology.Medications
                            .FirstOrDefault(m => m.Id == incomingMedication.Id);

                        if (existingMedication is null)
                        {
                            // Safety fallback:
                            // if for some reason it was not loaded, add it as a new row.
                            existingPatient.Cardiology.Medications.Add(new CardiologyMedication
                            {
                                MedicationId = incomingMedication.MedicationId,
                                Category = incomingMedication.Category,
                                Dose = incomingMedication.Dose
                            });
                        }
                        else
                        {
                            existingMedication.MedicationId = incomingMedication.MedicationId;
                            existingMedication.Category = incomingMedication.Category;
                            existingMedication.Dose = incomingMedication.Dose;
                        }
                    }
                }
            }

            // 5) Update or create GastroIntestinal.
            if (patient.GastroIntestinal is null)
            {
                // If the incoming object has no GIT section,
                // leave the current database GIT section unchanged.
            }
            else if (existingPatient.GastroIntestinal is null)
            {
                // Create a new GIT row if the database does not have one yet.
                existingPatient.GastroIntestinal = new GastroIntestinal
                {
                    PatientId = existingPatient.Id,
                    FeedingRoute = patient.GastroIntestinal.FeedingRoute,
                    LastBowelMotionDate = patient.GastroIntestinal.LastBowelMotionDate,
                    IntraAbdominalPressure = patient.GastroIntestinal.IntraAbdominalPressure,
                    AbdominalGirth = patient.GastroIntestinal.AbdominalGirth,
                    HasGiBleeding = patient.GastroIntestinal.HasGiBleeding,
                    GitNotes = patient.GastroIntestinal.GitNotes
                };

                // Add the incoming formula, IV fluid, and other medication rows
                // to the new GIT section.
                foreach (var incomingItem in patient.GastroIntestinal.Items ?? new List<GastroIntestinalItem>())
                {
                    // Ignore incomplete rows where no catalog item was selected yet.
                    if (incomingItem.MedicationId <= 0)
                        continue;

                    existingPatient.GastroIntestinal.Items.Add(new GastroIntestinalItem
                    {
                        MedicationId = incomingItem.MedicationId,
                        Category = incomingItem.Category,
                        DoseOrRate = incomingItem.DoseOrRate
                    });
                }
            }
            else
            {
                // Update the existing GIT scalar fields.
                existingPatient.GastroIntestinal.FeedingRoute = patient.GastroIntestinal.FeedingRoute;
                existingPatient.GastroIntestinal.LastBowelMotionDate = patient.GastroIntestinal.LastBowelMotionDate;
                existingPatient.GastroIntestinal.IntraAbdominalPressure = patient.GastroIntestinal.IntraAbdominalPressure;
                existingPatient.GastroIntestinal.AbdominalGirth = patient.GastroIntestinal.AbdominalGirth;
                existingPatient.GastroIntestinal.HasGiBleeding = patient.GastroIntestinal.HasGiBleeding;
                existingPatient.GastroIntestinal.GitNotes = patient.GastroIntestinal.GitNotes;

                // Update formula, IV fluid, and other medication rows.
                var incomingItems = patient.GastroIntestinal.Items ?? new List<GastroIntestinalItem>();

                // Collect the real database Ids coming from the page.
                var incomingExistingItemIds = incomingItems
                    .Where(i => i.Id > 0)
                    .Select(i => i.Id)
                    .ToHashSet();

                // Remove database rows that no longer exist in the incoming list.
                var itemsToRemove = existingPatient.GastroIntestinal.Items
                    .Where(dbItem => !incomingExistingItemIds.Contains(dbItem.Id))
                    .ToList();

                foreach (var dbItem in itemsToRemove)
                {
                    context.Remove(dbItem);
                }

                // Add new rows and update existing rows.
                foreach (var incomingItem in incomingItems)
                {
                    // Ignore incomplete rows where no catalog item was selected yet.
                    if (incomingItem.MedicationId <= 0)
                        continue;

                    if (incomingItem.Id == 0)
                    {
                        // This is a new GIT item row created on the page.
                        existingPatient.GastroIntestinal.Items.Add(new GastroIntestinalItem
                        {
                            MedicationId = incomingItem.MedicationId,
                            Category = incomingItem.Category,
                            DoseOrRate = incomingItem.DoseOrRate
                        });
                    }
                    else
                    {
                        // This is an existing GIT item row.
                        var existingItem = existingPatient.GastroIntestinal.Items
                            .FirstOrDefault(i => i.Id == incomingItem.Id);

                        if (existingItem is null)
                        {
                            // Safety fallback:
                            // if for some reason it was not loaded, add it as a new row.
                            existingPatient.GastroIntestinal.Items.Add(new GastroIntestinalItem
                            {
                                MedicationId = incomingItem.MedicationId,
                                Category = incomingItem.Category,
                                DoseOrRate = incomingItem.DoseOrRate
                            });
                        }
                        else
                        {
                            existingItem.MedicationId = incomingItem.MedicationId;
                            existingItem.Category = incomingItem.Category;
                            existingItem.DoseOrRate = incomingItem.DoseOrRate;
                        }
                    }
                }
            }

            // 6) Update PatientDateEvents.
            //
            // We compare the incoming events from the page with the events
            // already stored in the database.
            var incomingEvents = patient.PatientDateEvents ?? new List<PatientDateEvent>();

            // Remove database events that are no longer present in the incoming list.
            // We only compare events that already have a real Id greater than 0.
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

        // Return all admission units for the dropdown list.
        public async Task<List<AdmissionUnitCatalog>> GetAdmissionUnitsAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.AdmissionUnitCatalogs
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // Add a reusable admission unit to the shared catalog only if it does not exist already.
        // This lets the UI create a new unit once, then reuse it later for other patients.
        public async Task<AdmissionUnitCatalog> AddAdmissionUnitIfMissingAsync(AdmissionUnitCatalog admissionUnit)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var cleanedName = (admissionUnit.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                throw new ArgumentException("Admission unit name is required.", nameof(admissionUnit));
            }

            // Look for an existing admission unit with the same name.
            // We compare by normalized lowercase text to reduce duplicates.
            var normalizedName = cleanedName.ToLower();

            var existingAdmissionUnit = await context.AdmissionUnitCatalogs
                .FirstOrDefaultAsync(u => u.Name.ToLower() == normalizedName);

            if (existingAdmissionUnit is not null)
            {
                return existingAdmissionUnit;
            }

            var newAdmissionUnit = new AdmissionUnitCatalog
            {
                Name = cleanedName
            };

            context.AdmissionUnitCatalogs.Add(newAdmissionUnit);
            await context.SaveChangesAsync();

            return newAdmissionUnit;
        }

        // Update an existing reusable admission unit in the shared catalog.
        // This is used when the user wants to fix spelling mistakes
        // or rename a unit already in the dropdown list.
        public async Task UpdateAdmissionUnitAsync(AdmissionUnitCatalog admissionUnit)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var cleanedName = (admissionUnit.Name ?? string.Empty).Trim();

            if (admissionUnit.Id <= 0)
            {
                throw new ArgumentException("Admission unit Id is invalid.", nameof(admissionUnit));
            }

            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                throw new ArgumentException("Admission unit name is required.", nameof(admissionUnit));
            }

            var existingAdmissionUnit = await context.AdmissionUnitCatalogs
                .FirstOrDefaultAsync(u => u.Id == admissionUnit.Id);

            if (existingAdmissionUnit is null)
            {
                throw new InvalidOperationException("Admission unit was not found.");
            }

            var normalizedName = cleanedName.ToLower();

            var duplicateAdmissionUnit = await context.AdmissionUnitCatalogs
                .FirstOrDefaultAsync(u => u.Id != admissionUnit.Id && u.Name.ToLower() == normalizedName);

            if (duplicateAdmissionUnit is not null)
            {
                throw new InvalidOperationException("Another admission unit with the same name already exists.");
            }

            existingAdmissionUnit.Name = cleanedName;

            await context.SaveChangesAsync();
        }

        // Return all reusable medications for dropdown lists.
        public async Task<List<Medication>> GetMedicationsAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Medications
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        // Add a reusable medication to the shared catalog only if it does not exist already.
        // This lets the UI create a new drug once, then reuse it later for other patients.
        public async Task<Medication> AddMedicationIfMissingAsync(Medication medication)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var cleanedName = (medication.Name ?? string.Empty).Trim();
            var cleanedFrequency = string.IsNullOrWhiteSpace(medication.Frequency)
                ? null
                : medication.Frequency.Trim();

            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                throw new ArgumentException("Medication name is required.", nameof(medication));
            }

            // Look for an existing medication with the same name.
            // We compare by normalized lowercase text to reduce duplicates.
            var normalizedName = cleanedName.ToLower();

            var existingMedication = await context.Medications
                .FirstOrDefaultAsync(m => m.Name.ToLower() == normalizedName);

            if (existingMedication is not null)
            {
                // Optional small upgrade:
                // if the medication already exists but its Frequency is still empty,
                // fill it from the new value.
                if (string.IsNullOrWhiteSpace(existingMedication.Frequency) &&
                    !string.IsNullOrWhiteSpace(cleanedFrequency))
                {
                    existingMedication.Frequency = cleanedFrequency;
                    await context.SaveChangesAsync();
                }

                return existingMedication;
            }

            var newMedication = new Medication
            {
                Name = cleanedName,
                Frequency = cleanedFrequency
            };

            context.Medications.Add(newMedication);
            await context.SaveChangesAsync();

            return newMedication;
        }

        // Update an existing reusable medication in the shared catalog.
        // This is used when the user wants to fix spelling mistakes
        // or change the default frequency text.
        public async Task UpdateMedicationAsync(Medication medication)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var cleanedName = (medication.Name ?? string.Empty).Trim();
            var cleanedFrequency = string.IsNullOrWhiteSpace(medication.Frequency)
                ? null
                : medication.Frequency.Trim();

            if (medication.Id <= 0)
            {
                throw new ArgumentException("Medication Id is invalid.", nameof(medication));
            }

            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                throw new ArgumentException("Medication name is required.", nameof(medication));
            }

            var existingMedication = await context.Medications
                .FirstOrDefaultAsync(m => m.Id == medication.Id);

            if (existingMedication is null)
            {
                throw new InvalidOperationException("Medication was not found.");
            }

            var normalizedName = cleanedName.ToLower();

            var duplicateMedication = await context.Medications
                .FirstOrDefaultAsync(m => m.Id != medication.Id && m.Name.ToLower() == normalizedName);

            if (duplicateMedication is not null)
            {
                throw new InvalidOperationException("Another medication with the same name already exists.");
            }

            existingMedication.Name = cleanedName;
            existingMedication.Frequency = cleanedFrequency;

            await context.SaveChangesAsync();
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
        //
        // This version also copies:
        // - CombinedHistory from PatientSummary
        // - all PatientDateEvents into ArchivedPatientDateEvents
        //
        // Neurology and Cardiology are intentionally not copied yet.
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

        // Return all archived patients.
        public async Task<List<ArchivedPatient>> GetArchivedAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.ArchivedPatients
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        // Restore an archived patient back to the active patients table.
        public async Task RestorePatientAsync(int archivedPatientId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Find the archived patient first.
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

            // Remove it from archive after recreating it as an active patient.
            context.ArchivedPatients.Remove(archivedPatient);

            await context.SaveChangesAsync();
        }

        // Delete a patient permanently from the archived patients table.
        public async Task DeleteArchivedAsync(int archivedPatientId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var archivedPatient = await context.ArchivedPatients.FindAsync(archivedPatientId);

            if (archivedPatient is null)
                return;

            context.ArchivedPatients.Remove(archivedPatient);
            await context.SaveChangesAsync();
        }

        // Small helper: read the letter part of the bed name.
        // Example: "B-12" -> "B"
        private static string GetBedPrefix(string? bed)
        {
            if (string.IsNullOrWhiteSpace(bed))
                return string.Empty;

            var parts = bed.Split('-', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : bed;
        }

        // Small helper: read the numeric part of the bed name.
        // Example: "B-12" -> 12
        // If parsing fails, return a large number so unknown values go last.
        private static int GetBedNumber(string? bed)
        {
            if (string.IsNullOrWhiteSpace(bed))
                return int.MaxValue;

            var parts = bed.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1 && int.TryParse(parts[1], out var number))
                return number;

            return int.MaxValue;
        }
    }
}