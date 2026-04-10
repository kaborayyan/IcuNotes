using System;
using System.ComponentModel.DataAnnotations;

namespace IcuNotes.Models
{
    // This table stores important dated events in the patient's ICU journey.
    // Examples:
    // 01-Apr-2026 -> Admission
    // 03-Apr-2026 -> Intubation
    // 05-Apr-2026 -> Extubation
    // 08-Apr-2026 -> Transfer to ward
    public class PatientDateEvent
    {
        public int Id { get; set; }

        // Foreign key to the related patient.
        // This tells Entity Framework which patient owns this event.
        public int PatientId { get; set; }

        // Navigation property back to the patient.
        public Patient? Patient { get; set; }

        // The date and time when this event happened.
        public DateTime EventDate { get; set; }

        // A short label for the event.
        // Examples: "Admission", "Intubation", "Extubation", "Discharge"
        [MaxLength(100)]
        public string? Title { get; set; }
    }
}