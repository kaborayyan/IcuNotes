using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IcuNotes.Constants;

namespace IcuNotes.Models
{
    public class Patient : IValidatableObject
    {
        // Primary key for the patient table.
        public int Id { get; set; }

        // Patient name is required.
        // StringLength limits the maximum number of characters allowed.
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        // Age is optional.
        // int? allows the field to be empty.
        [Range(0, 120)]
        public int? Age { get; set; }

        // Weight is optional.
        // decimal is suitable for values like 70.5 or 82.3.
        [Range(0.5, 500)]
        public decimal? Weight { get; set; }

        // Bed is optional.
        // StringLength(4) matches values like B-01 or H-15.
        [StringLength(4)]
        public string? Bed { get; set; }

        // Main diagnosis or reason for ICU admission.
        // This is plain text, not a separate related table.
        [MaxLength(500)]
        public string? Diagnosis { get; set; }

        // Foreign key to AdmissionUnitCatalog.
        // This stores the selected admission unit Id.
        public int? AdmissionUnitCatalogId { get; set; }

        // Navigation property to the selected admission unit.
        public AdmissionUnitCatalog? AdmissionUnitCatalog { get; set; }

        // One-to-one navigation property.
        // A patient can have one summary record.
        public PatientSummary? PatientSummary { get; set; }

        // One-to-one navigation property.
        // A patient can have one neurology record.
        public Neurology? Neurology { get; set; }

        // One-to-many navigation property.
        // A patient can have many dated events in the timeline.
        public List<PatientDateEvent> PatientDateEvents { get; set; } = new();

        // Custom validation method.
        // This checks rules that cannot be fully handled by simple attributes.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // If Bed has a value, make sure it is one of the allowed bed options.
            if (!string.IsNullOrWhiteSpace(Bed) && !BedOptions.IsValid(Bed))
            {
                yield return new ValidationResult(
                    "Bed must be one of the valid bed values.",
                    new[] { nameof(Bed) });
            }
        }
    }
}