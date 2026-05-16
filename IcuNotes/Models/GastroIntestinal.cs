using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace IcuNotes.Models
{
    // This tells us which visible list the selected catalog item belongs to
    // inside the GIT section.
    //
    // We are reusing the shared Medication catalog for formulas, IV fluids,
    // and other GIT medications. This keeps the first version simple and
    // follows the same general pattern used by the Neurology section.
    public enum GastroIntestinalItemCategory
    {
        Formula = 1,
        IVFluid = 2,
        OtherMedication = 3
    }

    public class GastroIntestinal
    {
        // Primary key for the GastroIntestinal table.
        public int Id { get; set; }

        // Foreign key to Patient.
        // One patient will have one GIT record.
        public int PatientId { get; set; }

        // Navigation property back to the parent patient.
        public Patient? Patient { get; set; }

        // Feeding route is stored as normal text.
        // The UI will later provide the fixed dropdown options:
        // "Not inserted", "Oral", "NGT", "NJT", "OGT", and "PEG".
        // Keeping it as string makes the model simpler and avoids creating
        // an enum just for display choices.
        [MaxLength(30)]
        public string FeedingRoute { get; set; } = "Not inserted";

        // Date of the last bowel motion, if known.
        public DateTime? LastBowelMotionDate { get; set; }

        // Intra-abdominal pressure as a whole number.
        // Nullable because it will not be measured for every patient.
        public int? IntraAbdominalPressure { get; set; }

        // Abdominal girth as a whole number.
        // Nullable because it may not be needed for every patient.
        public int? AbdominalGirth { get; set; }

        // Simple checkbox for GI bleeding.
        public bool HasGiBleeding { get; set; }

        // Free-text notes for anything else related to the GIT system.
        public string? GitNotes { get; set; }

        // All saved formula, IV fluid, and other medication rows for the GIT section.
        // This is the real stored collection in the database.
        public List<GastroIntestinalItem> Items { get; set; } = new();

        // Filtered helper view for enteral formula rows.
        // This is not a separate saved list in the database.
        [NotMapped]
        public IEnumerable<GastroIntestinalItem> FormulaItems =>
            Items.Where(i => i.Category == GastroIntestinalItemCategory.Formula);

        // Filtered helper view for IV fluid rows.
        // This is not a separate saved list in the database.
        [NotMapped]
        public IEnumerable<GastroIntestinalItem> IVFluidItems =>
            Items.Where(i => i.Category == GastroIntestinalItemCategory.IVFluid);

        // Filtered helper view for other GIT medication rows.
        // This is not a separate saved list in the database.
        [NotMapped]
        public IEnumerable<GastroIntestinalItem> OtherMedicationItems =>
            Items.Where(i => i.Category == GastroIntestinalItemCategory.OtherMedication);
    }

    public class GastroIntestinalItem
    {
        // Primary key for the GastroIntestinalItem table.
        public int Id { get; set; }

        // Foreign key to the parent GIT record.
        public int GastroIntestinalId { get; set; }

        // Navigation property back to the parent GIT section.
        public GastroIntestinal? GastroIntestinal { get; set; }

        // Foreign key to the shared Medication catalog.
        // In this app, the same catalog can contain medication names,
        // formula names, and IV fluid names.
        public int MedicationId { get; set; }

        // Navigation property to the reusable catalog item.
        public Medication? Medication { get; set; }

        // Tells us whether this item appears under Formula,
        // IV Fluids, or Other GIT Medications in the UI.
        public GastroIntestinalItemCategory Category { get; set; }

        // Patient-specific value for this row.
        // Examples:
        // - Formula: "40" means 40 ml/hr.
        // - IV fluid: "80" means 80 ml/hr.
        // - Other medication: "40" may mean 40 mg, depending on the catalog frequency.
        public decimal? DoseOrRate { get; set; }

        // Helper property for the default/shared frequency coming
        // from the reusable Medication catalog.
        // This is not stored in the database.
        [NotMapped]
        public string? DefaultFrequency => Medication?.Frequency;
    }
}