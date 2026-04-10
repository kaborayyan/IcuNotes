using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace IcuNotes.Models
{
    // Glasgow Coma Scale - Eye response.
    // The numeric values are the real score values.
    public enum GcsEyeResponse
    {
        [Display(Name = "No eye opening (1)")]
        NoEyeOpening = 1,

        [Display(Name = "Eye opening to pain (2)")]
        ToPain = 2,

        [Display(Name = "Eye opening to speech (3)")]
        ToSpeech = 3,

        [Display(Name = "Spontaneous eye opening (4)")]
        Spontaneous = 4
    }

    // Glasgow Coma Scale - Verbal response.
    // The numeric values are the real score values.
    public enum GcsVerbalResponse
    {
        [Display(Name = "No verbal response (1)")]
        NoVerbalResponse = 1,

        [Display(Name = "Incomprehensible sounds (2)")]
        IncomprehensibleSounds = 2,

        [Display(Name = "Inappropriate words (3)")]
        InappropriateWords = 3,

        [Display(Name = "Confused conversation (4)")]
        ConfusedConversation = 4,

        [Display(Name = "Oriented (5)")]
        Oriented = 5
    }

    // Glasgow Coma Scale - Motor response.
    // The numeric values are the real score values.
    public enum GcsMotorResponse
    {
        [Display(Name = "No motor response (1)")]
        NoMotorResponse = 1,

        [Display(Name = "Extension to pain (2)")]
        ExtensionToPain = 2,

        [Display(Name = "Abnormal flexion to pain (3)")]
        AbnormalFlexionToPain = 3,

        [Display(Name = "Withdraws from pain (4)")]
        WithdrawsFromPain = 4,

        [Display(Name = "Localizes pain (5)")]
        LocalizesPain = 5,

        [Display(Name = "Obeys commands (6)")]
        ObeysCommands = 6
    }

    // This tells us which visible list the medication belongs to
    // inside the Neurology section.
    public enum NeurologyMedicationCategory
    {
        Sedation = 1,
        Neuro = 2
    }

    public class Neurology
    {
        // Primary key for the Neurology table.
        public int Id { get; set; }

        // Foreign key to Patient.
        // One patient will have one neurology record.
        public int PatientId { get; set; }

        // Navigation property back to the parent patient.
        public Patient? Patient { get; set; }

        // GCS components stored separately.
        public GcsEyeResponse? GcsEye { get; set; }

        public GcsVerbalResponse? GcsVerbal { get; set; }

        public GcsMotorResponse? GcsMotor { get; set; }

        // Pupils text.
        // Example: "Equal and reactive" or "Left fixed dilated".
        [MaxLength(100)]
        public string? Pupils { get; set; }

        // Motor status text.
        // Example: "Moves all limbs" or "Right-sided weakness".
        [MaxLength(100)]
        public string? MotorStatus { get; set; }

        // All saved medication rows for the Neurology section.
        // We save them in one collection, then split them in the UI by Category.
        public List<NeurologyMedication> Medications { get; set; } = new();

        // Convenience property for total GCS.
        // This is calculated from the 3 selected enum values.
        // It is not stored in the database.
        [NotMapped]
        public int? GcsTotal
        {
            get
            {
                if (GcsEye == null || GcsVerbal == null || GcsMotor == null)
                {
                    return null;
                }

                return (int)GcsEye.Value
                     + (int)GcsVerbal.Value
                     + (int)GcsMotor.Value;
            }
        }

        // Helper list for sedation medications.
        // This is only for easier use in the UI later.
        // It is not stored in the database as a separate column/table.
        [NotMapped]
        public List<NeurologyMedication> SedationMedications =>
            Medications
                .Where(m => m.Category == NeurologyMedicationCategory.Sedation)
                .ToList();

        // Helper list for neuro medications.
        // This is only for easier use in the UI later.
        [NotMapped]
        public List<NeurologyMedication> NeuroMedications =>
            Medications
                .Where(m => m.Category == NeurologyMedicationCategory.Neuro)
                .ToList();
    }

    public class NeurologyMedication
    {
        // Primary key for the NeurologyMedication table.
        public int Id { get; set; }

        // Foreign key to the Neurology record.
        public int NeurologyId { get; set; }

        // Navigation property back to the parent Neurology section.
        public Neurology? Neurology { get; set; }

        // Foreign key to the shared Medication catalog.
        public int MedicationId { get; set; }

        // Navigation property to the reusable medication name.
        public Medication? Medication { get; set; }

        // Tells us whether this medication should appear under
        // Sedation medications or Neuro medications in the UI.
        public NeurologyMedicationCategory Category { get; set; }

        // Patient-specific dose, route and frequency.
        // Example: "100 mg/h iv infusion"
        [MaxLength(100)]
        public string? Dose { get; set; }
        
    }
}