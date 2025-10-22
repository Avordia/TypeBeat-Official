using osuTK;
using System.Collections.Generic; // Added for Dictionary

namespace TypeBeat.Game.Gameplay.Layout
{
    /// <summary>
    /// Defines the layout for the TypeNote (piano roll) visual style.
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
        public float YStepPx { get; set; } = 10f;

        // --- Note Step Mapping ---
        // This maps a note character string to a Y-axis "step" number.
        // C0 is the base (0), C#0 is 1, D0 is 2, etc.
        private static readonly Dictionary<string, int> note_steps = new Dictionary<string, int>
        {
            // Octave 0
            { "C0", 0 },
            { "C#0", 1 },
            { "D0", 2 },
            { "D#0", 3 },
            { "E0", 4 },
            { "F0", 5 },
            { "F#0", 6 },
            { "G0", 7 },
            { "G#0", 8 },
            { "A0", 9 },
            { "A#0", 10 },
            { "B0", 11 },
            // Octave 1
            { "C1", 12 },
            { "C#1", 13 },
            { "D1", 14 },
            { "D#1", 15 },
            { "E1", 16 },
            { "F1", 17 },
            { "F#1", 18 },
            { "G1", 19 },
            { "G#1", 20 },
            { "A1", 21 },
            { "A#1", 22 },
            { "B1", 23 }
        };

        /// <summary>
        /// Gets the Y-step (0 for C0, 1 for C#0, etc.) for a given note.
        /// Returns 0 if the note is not recognized.
        /// </summary>
        public static int GetYStep(string character)
        {
            if (note_steps.TryGetValue(character, out int step))
            {
                return step;
            }
            return 0; // Default to base C0 position
        }

        // --- Helper Methods ---

        public float GetSpawnX(Vector2 drawSize) => drawSize.X * SpawnXFraction;
        public float GetDestinationX(Vector2 drawSize) => drawSize.X * DestinationXFraction;

        /// <summary>
        /// Calculates the final Y-pixel coordinate for a note.
        /// </summary>
        public float GetNoteY(Vector2 drawSize, int noteStep)
        {
            float yBase = drawSize.Y * YBaseFraction;
            float yOffset = noteStep * YStepPx;
            return yBase - yOffset; // Subtract offset so higher notes go *up*
        }
    }
}