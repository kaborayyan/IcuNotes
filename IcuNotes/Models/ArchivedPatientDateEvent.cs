using System;
using System.ComponentModel.DataAnnotations;

namespace IcuNotes.Models
{
    // This table stores dated events for an archived patient.
    // It is the archived version of PatientDateEvent.
    public class ArchivedPatientDateEvent
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key to the archived patient
        public int ArchivedPatientId { get; set; }

        // Navigation property back to the archived patient
        public ArchivedPatient? ArchivedPatient { get; set; }

        // Date and time of the archived event
        public DateTime EventDate { get; set; }

        // Short event title
        // Examples: "Admission", "Intubation", "Extubation", "Transfer"
        [MaxLength(100)]
        public string? Title { get; set; }
    }
}