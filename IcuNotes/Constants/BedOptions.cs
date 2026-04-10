using System;
using System.Collections.Generic;
using System.Text;

namespace IcuNotes.Constants
{
    public static class BedOptions
    {
        // This list contains all valid bed values in the app.
        // It is generated once when the app starts.
        public static readonly IReadOnlyList<string> All = GenerateBeds();

        // This method creates the full list of beds:
        // B-01 to B-15, E-01 to E-15, F-01 to F-15, G-01 to G-15, H-01 to H-15.
        private static IReadOnlyList<string> GenerateBeds()
        {
            var beds = new List<string>();

            // These are the allowed bed sections.
            // var sections = new[] { "B", "E", "F", "G", "H" };
            var sections = new[] { "B", "F" };

            // For each section, create beds from 01 to 15.
            foreach (var section in sections)
            {
                for (int i = 1; i <= 15; i++)
                {
                    beds.Add($"{section}{i:D2}");
                }
            }

            return beds;
        }

        // This method checks whether a bed value is valid or not.
        public static bool IsValid(string? bed)
        {
            // Reject null, empty, or whitespace values.
            if (string.IsNullOrWhiteSpace(bed))
                return false;

            // Return true only if the bed exists in the predefined list.
            return All.Contains(bed);
        }
    }
}
