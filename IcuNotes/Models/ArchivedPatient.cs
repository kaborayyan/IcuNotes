using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IcuNotes.Models
{
    public class ArchivedPatient
    {
        // Primary key for the archive table
        public int Id { get; set; }

        // Patient name is required
        [Required]
        public string Name { get; set; } = string.Empty;

        // Optional basic data
        public int? Age { get; set; }

        public decimal? Weight { get; set; }

        // Diagnosis snapshot at time of archiving
        [MaxLength(500)]
        public string? Diagnosis { get; set; }

        // Archived combined history text
        [MaxLength(5000)]
        public string? CombinedHistory { get; set; }

        // Navigation property for archived dated events.
        // One archived patient can have many archived timeline events.
        public List<ArchivedPatientDateEvent> ArchivedPatientDateEvents { get; set; } = new();
    }
}