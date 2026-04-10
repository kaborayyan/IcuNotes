using IcuNotes.Models;

namespace IcuNotes.Services
{
    public class PatientDraftService
    {
        // Keys used to store the current patient draft in Preferences.
        // Each key is like a small label under which the value is saved.
        private const string DraftNameKey = "patients_draft_name";
        private const string DraftAgeKey = "patients_draft_age";
        private const string DraftWeightKey = "patients_draft_weight";
        private const string DraftBedKey = "patients_draft_bed";
        private const string DraftDiagnosisKey = "patients_draft_diagnosis";
        private const string DraftAdmissionUnitCatalogIdKey = "patients_draft_admission_unit_catalog_id";
        private const string DraftEditModeKey = "patients_draft_is_edit_mode";
        private const string DraftPatientIdKey = "patients_draft_patient_id";

        // Save the current form state as a draft.
        // We call this often while the user is typing or changing values.
        public Task SaveDraftAsync(Patient patient, bool isEditMode)
        {
            // Save simple text values.
            Preferences.Default.Set(DraftNameKey, patient.Name ?? string.Empty);
            Preferences.Default.Set(DraftBedKey, patient.Bed ?? string.Empty);
            Preferences.Default.Set(DraftDiagnosisKey, patient.Diagnosis ?? string.Empty);

            // Save Age only if it has a value.
            if (patient.Age.HasValue)
                Preferences.Default.Set(DraftAgeKey, patient.Age.Value);
            else
                Preferences.Default.Remove(DraftAgeKey);

            // Save Weight as text.
            // Using text is simple and works fine for now.
            if (patient.Weight.HasValue)
                Preferences.Default.Set(DraftWeightKey, patient.Weight.Value.ToString());
            else
                Preferences.Default.Remove(DraftWeightKey);

            // Save Admission Unit Id only if a unit is selected.
            if (patient.AdmissionUnitCatalogId.HasValue)
                Preferences.Default.Set(DraftAdmissionUnitCatalogIdKey, patient.AdmissionUnitCatalogId.Value);
            else
                Preferences.Default.Remove(DraftAdmissionUnitCatalogIdKey);

            // Save whether the form is in Add mode or Edit mode.
            Preferences.Default.Set(DraftEditModeKey, isEditMode);

            // Save the patient Id only when editing an existing patient.
            if (patient.Id > 0)
                Preferences.Default.Set(DraftPatientIdKey, patient.Id);
            else
                Preferences.Default.Remove(DraftPatientIdKey);

            return Task.CompletedTask;
        }

        // Try to restore a saved patient draft from Preferences.
        public PatientDraftResult LoadDraft()
        {
            var draftName = Preferences.Default.Get(DraftNameKey, string.Empty);
            var draftBed = Preferences.Default.Get(DraftBedKey, string.Empty);
            var draftDiagnosis = Preferences.Default.Get(DraftDiagnosisKey, string.Empty);
            var draftIsEditMode = Preferences.Default.Get(DraftEditModeKey, false);
            var draftPatientId = Preferences.Default.Get(DraftPatientIdKey, 0);

            int? draftAge = null;
            if (Preferences.Default.ContainsKey(DraftAgeKey))
            {
                draftAge = Preferences.Default.Get(DraftAgeKey, 0);
            }

            decimal? draftWeight = null;
            var draftWeightText = Preferences.Default.Get(DraftWeightKey, string.Empty);

            if (decimal.TryParse(draftWeightText, out var parsedWeight))
            {
                draftWeight = parsedWeight;
            }

            int? draftAdmissionUnitCatalogId = null;
            if (Preferences.Default.ContainsKey(DraftAdmissionUnitCatalogIdKey))
            {
                draftAdmissionUnitCatalogId = Preferences.Default.Get(DraftAdmissionUnitCatalogIdKey, 0);
            }

            // Decide whether there is anything meaningful to restore.
            var hasDraft =
                !string.IsNullOrWhiteSpace(draftName) ||
                draftAge.HasValue ||
                draftWeight.HasValue ||
                !string.IsNullOrWhiteSpace(draftBed) ||
                !string.IsNullOrWhiteSpace(draftDiagnosis) ||
                draftAdmissionUnitCatalogId.HasValue ||
                draftPatientId > 0;

            if (!hasDraft)
            {
                return new PatientDraftResult
                {
                    HasDraft = false
                };
            }

            // Rebuild a Patient object from the saved draft values.
            return new PatientDraftResult
            {
                HasDraft = true,
                IsEditMode = draftIsEditMode,
                Patient = new Patient
                {
                    Id = draftPatientId,
                    Name = draftName,
                    Age = draftAge,
                    Weight = draftWeight,
                    Bed = string.IsNullOrWhiteSpace(draftBed) ? null : draftBed,
                    Diagnosis = string.IsNullOrWhiteSpace(draftDiagnosis) ? null : draftDiagnosis,
                    AdmissionUnitCatalogId = draftAdmissionUnitCatalogId
                }
            };
        }

        // Remove the saved draft completely.
        public Task ClearDraftAsync()
        {
            Preferences.Default.Remove(DraftNameKey);
            Preferences.Default.Remove(DraftAgeKey);
            Preferences.Default.Remove(DraftWeightKey);
            Preferences.Default.Remove(DraftBedKey);
            Preferences.Default.Remove(DraftDiagnosisKey);
            Preferences.Default.Remove(DraftAdmissionUnitCatalogIdKey);
            Preferences.Default.Remove(DraftEditModeKey);
            Preferences.Default.Remove(DraftPatientIdKey);

            return Task.CompletedTask;
        }
    }

    public class PatientDraftResult
    {
        public bool HasDraft { get; set; }

        public bool IsEditMode { get; set; }

        public Patient Patient { get; set; } = new();
    }
}