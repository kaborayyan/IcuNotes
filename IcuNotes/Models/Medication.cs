using System.ComponentModel.DataAnnotations;

namespace IcuNotes.Models
{
    public class Medication
    {
        // Primary key for the Medication table.
        public int Id { get; set; }

        // Reusable medication name.
        // Examples: Propofol, Vasopressin
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Default or usual frequency / dosing style for this medication.
        // Examples: "mcg/kg/min", "BD", "TID", "q8h".
        // Optional for now, because some medications may be added first
        // before deciding the exact usual frequency text.
        [MaxLength(50)]
        public string? Frequency { get; set; }
    }
}