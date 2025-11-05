using osuTK;
using System.Collections.Generic; // Added for Dictionary

namespace TypeBeat.Game.Gameplay.Layout
{
    /// <summary>
    /// Defines the layout for the TypeNote (piano roll) visual style.
    /// Supports unlimited octaves using dynamic calculation.
    /// </summary>
    public class TypeNoteLayoutConfig
    {
        /// <summary>
        /// X-position where notes spawn (as a fraction of screen width).
        /// 1.0 is the right edge. >1.0 is off-screen to the right.
        /// </summary>
        public float SpawnXFraction { get; set; } = 1.1f;

        /// <summary>
        /// X-position where notes should be hit (as a fraction of screen width).
        /// 0.0 is the left edge.
        /// </summary>
        public float DestinationXFraction { get; set; } = 0.2f;

        /// <summary>
        /// Base Y-position for the lowest note (e.g., C0) as a fraction of screen height.
        /// 0.5 is the center.
        /// </summary>
        public float YBaseFraction { get; set; } = 0.7f; // Start notes lower on screen

        /// <summary>
        /// How many pixels to move *up* for each semitone (half-step).
        /// This is the "fixed vertical distance" you can adjust.
        /// </summary>
        public float YStepPx { get; set; } = 14f;

        // --- Note Name to Y-Position Mapping ---
        // Maps note names to their visual Y position (like a music sheet).
        // Sharps share the same Y position as their natural notes.
        // Only 7 positions per octave (C, D, E, F, G, A, B).
        private static readonly Dictionary<string, int> note_offsets = new Dictionary<string, int>
        {
            { "C", 0 },
            { "C#", 0 },  // Same line as C
            { "D", 1 },
            { "D#", 1 },  // Same line as D
            { "E", 2 },
            { "F", 3 },
            { "F#", 3 },  // Same line as F
            { "G", 4 },
            { "G#", 4 },  // Same line as G
            { "A", 5 },
            { "A#", 5 },  // Same line as A
            { "B", 6 }
        };

        /// <summary>
        /// Gets the Y-step for a given note, treating sharps as the same position as their natural notes.
        /// This creates a music-sheet appearance where C and C# are on the same line.
        /// Now supports unlimited octaves dynamically.
        /// Format: "C0", "D#2", "G5", etc.
        /// Returns 0 for C0, 1 for D0, 2 for E0, 7 for C1, etc.
        /// </summary>
        public static int GetYStep(string character)
        {
            if (string.IsNullOrEmpty(character) || character == "/")
                return 0;

            // Parse note name and octave
            // Format: [Note][#]?[Octave]
            // Examples: "C0", "C#0", "D2", "G#5"
            
            int octave = 0;
            string noteName = "";
            
            // Check if it ends with a sharp and a digit
            if (character.Length >= 3 && character[character.Length - 2] == '#')
            {
                // Format: "C#0", "D#2"
                noteName = character.Substring(0, 2); // "C#", "D#"
                if (int.TryParse(character.Substring(2), out octave))
                {
                    // Successfully parsed
                }
            }
            else if (character.Length >= 2)
            {
                // Format: "C0", "D2", "G5"
                noteName = character.Substring(0, 1); // "C", "D", "G"
                if (int.TryParse(character.Substring(1), out octave))
                {
                    // Successfully parsed
                }
            }
            if (note_offsets.TryGetValue(noteName, out int offset))
            {
                return (octave * 7) + offset;
            }
            return 0;
        }

        public float GetSpawnX(Vector2 drawSize) => drawSize.X * SpawnXFraction;
        public float GetDestinationX(Vector2 drawSize) => drawSize.X * DestinationXFraction;
        public float GetNoteY(Vector2 drawSize, int noteStep)
        {
            float yBase = drawSize.Y * YBaseFraction;
            float yOffset = noteStep * YStepPx;
            return yBase - yOffset; // Subtract offset so higher notes go *up*
        }

        /// <summary>
        /// Gets the stem direction offset for a note.
        /// Notes B0 and higher have stems pointing up, requiring Y-offset correction.
        /// Returns the pixel offset to apply (negative = move up, positive = move down).
        /// </summary>
        public static float GetStemDirectionOffset(string character)
        {
            if (string.IsNullOrEmpty(character))
                return 0;

            // Parse the note to determine if it's B0 or higher
            int yStep = GetYStep(character);
            
            // B0 has yStep = 6 (octave 0, offset 6)
            // Any note with yStep >= 6 needs correction (B0, C1, D1, etc.)
            if (yStep >= 6)
            {
                // Stem-up notes: move down to align the circular notehead
                return 39.5f; // Adjust this value based on your note image stem length
            }

            return 2.9f; // C0-A0 are already correctly positioned
        }
    }
}