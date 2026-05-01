using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace IcuNotes.Models
{
    // This tells us which visible list the medication belongs to
    // inside the Cardiology section.
    //
    // We are using one medication table for Cardiology medications,
    // but we can still show the medications in different UI groups.
    public enum CardiologyMedicationCategory
    {
        // Vasopressors and inotropes.
        // Examples: Noradrenaline, Adrenaline, Dobutamine, Vasopressin.
        Support = 1,

        // Other cardiovascular medications.
        // Examples: Amiodarone, beta blockers, diuretics, antihypertensives.
        Cardiovascular = 2
    }

    public class Cardiology
    {
        // Primary key for the Cardiology table.
        public int Id { get; set; }

        // Foreign key to Patient.
        // One patient will have one cardiology record.
        public int PatientId { get; set; }

        // Navigation property back to the parent patient.
        public Patient? Patient { get; set; }

        // Heart rate.
        // Example: 90
        public int? HeartRate { get; set; }

        // Systolic blood pressure.
        // Example: 120
        public int? SystolicBloodPressure { get; set; }

        // Diastolic blood pressure.
        // Example: 70
        public int? DiastolicBloodPressure { get; set; }

        // Central venous pressure.
        // Example: 8
        public int? CentralVenousPressure { get; set; }

        // Inferior vena cava measurement.
        // We are keeping this as a nullable integer for now, according to your plan.
        public int? InferiorVenaCava { get; set; }

        // Cardiac index.
        // This is decimal because values are commonly written like 2.4 or 3.1.
        public decimal? CardiacIndex { get; set; }

        // Cardiac output.
        // This is decimal because values are commonly written like 4.5 or 5.2.
        public decimal? CardiacOutput { get; set; }

        // Systemic vascular resistance.
        // Usually stored as a whole number.
        public int? SystemicVascularResistance { get; set; }

        // Pulmonary capillary wedge pressure.
        // Usually stored as a whole number.
        public int? PulmonaryCapillaryWedgePressure { get; set; }

        // Free-text notes for extra cardiology information.
        // Examples:
        // - "Poor LV function"
        // - "Post PCI"
        // - "Temporary pacemaker inserted"
        // - "Echo showed severe MR"
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // All saved medication rows for the Cardiology section.
        // This is the real stored collection in the database.
        public List<CardiologyMedication> Medications { get; set; } = new();

        // Mean arterial pressure.
        //
        // This is calculated from SBP and DBP.
        // It is not stored in the database because it can always be regenerated.
        //
        // Formula:
        // MAP = (SBP + 2 x DBP) / 3
        [NotMapped]
        public int? MeanArterialPressure
        {
            get
            {
                if (SystolicBloodPressure == null || DiastolicBloodPressure == null)
                {
                    return null;
                }

                return (SystolicBloodPressure.Value + (2 * DiastolicBloodPressure.Value)) / 3;
            }
        }

        // Filtered helper view for support medications.
        // This is not a separate saved list in the database.
        [NotMapped]
        public IEnumerable<CardiologyMedication> SupportMedications =>
            Medications.Where(m => m.Category == CardiologyMedicationCategory.Support);

        // Filtered helper view for cardiovascular medications.
        // This is not a separate saved list in the database.
        [NotMapped]
        public IEnumerable<CardiologyMedication> CardiovascularMedications =>
            Medications.Where(m => m.Category == CardiologyMedicationCategory.Cardiovascular);
    }

    public class CardiologyMedication
    {
        // Primary key for the CardiologyMedication table.
        public int Id { get; set; }

        // Foreign key to the Cardiology record.
        public int CardiologyId { get; set; }

        // Navigation property back to the parent Cardiology section.
        public Cardiology? Cardiology { get; set; }

        // Foreign key to the shared Medication catalog.
        public int MedicationId { get; set; }

        // Navigation property to the reusable medication name.
        public Medication? Medication { get; set; }

        // Tells us whether this medication should appear under
        // Support medications or Cardiovascular medications in the UI.
        public CardiologyMedicationCategory Category { get; set; }

        // Patient-specific dose.
        // Examples:
        // - Noradrenaline 0.2
        // - Dobutamine 5
        // - Amiodarone 900
        public decimal? Dose { get; set; }

        // Helper property for the default/shared frequency coming
        // from the reusable Medication catalog.
        // This is not stored in the database.
        [NotMapped]
        public string? DefaultFrequency => Medication?.Frequency;
    }
}