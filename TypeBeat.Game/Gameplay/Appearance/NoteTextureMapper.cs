using System.Collections.Generic;
using osu.Framework.Logging;

namespace TypeBeat.Game.Gameplay.Appearance
{
    /// <summary>
    /// Maps a note character (e.g., "D0", "D#2", "G5") to its corresponding texture file name
    /// Supports unlimited octaves by applying the pattern dynamically.
    /// </summary>
    public static class NoteTextureMapper
    {
        /// <summary>
        /// Gets the texture file name (without extension) for a given note character.
        /// Now supports unlimited octaves dynamically.
        /// </summary>
        /// <param name="character">The note character from the beatmap (e.g., "C#0", "D2", "G5").</param>
        /// <returns>The name of the texture file (e.g., "C#0" or "D#0-A#0").</returns>
        public static string GetTextureName(string character)
        {
            if (string.IsNullOrEmpty(character))
                return "A1"; // Safety check

            // Parse the note to extract base note and octave
            string noteName;
            int octave;
            
            if (!ParseNote(character, out noteName, out octave))
            {
                Logger.Log($"[NoteTextureMapper] Failed to parse note '{character}'. Using A1 as default.", LoggingTarget.Runtime, LogLevel.Error);
                return "A1";
            }

            // Apply texture mapping pattern based on note name
            // The pattern repeats for each octave pair
            string textureName = GetTextureForNote(noteName, octave);
            
            return textureName;
        }

        /// <summary>
        /// Parses a note character into base note name and octave.
        /// </summary>
        private static bool ParseNote(string character, out string noteName, out int octave)
        {
            noteName = "";
            octave = 0;

            if (string.IsNullOrEmpty(character))
                return false;

            // Check if it ends with a sharp and digits
            if (character.Length >= 3 && character[character.Length - 2] == '#')
            {
                // Format: "C#0", "D#2"
                noteName = character.Substring(0, 2); // "C#", "D#"
                return int.TryParse(character.Substring(2), out octave);
            }
            else if (character.Length >= 2)
            {
                // Format: "C0", "D2", "G5"
                noteName = character.Substring(0, 1); // "C", "D", "G"
                return int.TryParse(character.Substring(1), out octave);
            }

            return false;
        }

        /// <summary>
        /// Gets the texture name for a note based on its name and octave.
        /// The pattern cycles every 2 octaves.
        /// </summary>
        private static string GetTextureForNote(string noteName, int octave)
        {
            // Determine if we're in an even or odd octave
            bool isEvenOctave = (octave % 2) == 0;

            // Apply the mapping pattern (same as original but dynamic)
            switch (noteName)
            {
                // White keys
                case "C":
                    return isEvenOctave ? "C0" : "B0-G1"; // C in even octaves uses C0, C in odd octaves uses B0-G1
                case "D":
                case "E":
                case "F":
                case "G":
                    return isEvenOctave ? "D0-A0" : "B0-G1";
                case "A":
                    return isEvenOctave ? "D0-A0" : "A1";
                case "B":
                    return isEvenOctave ? "B0-G1" : "B1";

                // Black keys (sharps)
                case "C#":
                    return isEvenOctave ? "C#0" : "C#1-G#1";
                case "D#":
                case "F#":
                case "G#":
                    return isEvenOctave ? "D#0-A#0" : "C#1-G#1";
                case "A#":
                    return isEvenOctave ? "D#0-A#0" : "A#1";

                default:
                    Logger.Log($"[NoteTextureMapper] Unknown note name '{noteName}'. Using A1 as default.", LoggingTarget.Runtime, LogLevel.Error);
                    return "A1";
            }
        }
    }
}