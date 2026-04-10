using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IcuNotes.Models
{
    public class Medication
    {
        // Primary key
        public int Id { get; set; }

        // Medication name only
        // Required because a medication without a name is useless.
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}