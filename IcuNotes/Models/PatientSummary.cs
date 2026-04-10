using System.ComponentModel.DataAnnotations;

namespace IcuNotes.Models
{
    public class PatientSummary
    {
        public int Id { get; set; }

        // This links the summary to one patient.
        // Because this is an int, it is required by default.
        public int PatientId { get; set; }

        // Navigation property back to the patient.
        // This lets Entity Framework understand the relationship.
        public Patient? Patient { get; set; }

        // A longer free-text summary for the patient
        [MaxLength(5000)]
        public string? CombinedHistory { get; set; }

        // A short text field for things that still need to be done.
        [MaxLength(1000)]
        public string? EventsTodo { get; set; }
        
    }
}