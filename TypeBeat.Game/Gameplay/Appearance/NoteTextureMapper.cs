using System.Collections.Generic;
using osu.Framework.Logging;

namespace TypeBeat.Game.Gameplay.Appearance
{
    /// <summary>
    /// Maps a note character (e.g., "D0") to its corresponding texture file name
    /// (e.g., "D0-A0") based on the explicit mapping rules.
    /// </summary>
    public static class NoteTextureMapper
    {
        private static readonly Dictionary<string, string> texture_map = new Dictionary<string, string>
        {
            // Note Character -> Texture File Name (based on your list)
            { "C0", "C0" },
            { "D0", "D0-A0" },
            { "E0", "D0-A0" },
            { "F0", "D0-A0" },
            { "G0", "D0-A0" },
            { "A0", "D0-A0" },
            { "B0", "B0-G1" },
            { "C1", "B0-G1" },
            { "D1", "B0-G1" },
            { "E1", "B0-G1" },
            { "F1", "B0-G1" },
            { "G1", "B0-G1" },
            { "A1", "A1" },
            { "B1", "B1" },
            { "C#0", "C#0" },
            { "D#0", "D#0-A#0" },
            { "F#0", "D#0-A#0" },
            { "G#0", "D#0-A#0" },
            { "A#0", "D#0-A#0" },
            { "C#1", "C#1-G#1" },
            { "D#1", "C#1-G#1" },
            { "F#1", "C#1-G#1" },
            { "G#1", "C#1-G#1" },
            { "A#1", "A#1" }
        };

        /// <summary>
        /// Gets the texture file name (without extension) for a given note character.
        /// </summary>
        /// <param name="character">The note character from the beatmap (e.g., "C#0").</param>
        /// <returns>The name of the texture file (e.g., "C#0" or "D#0-A#0").</returns>
        public static string GetTextureName(string character)
        {
            if (string.IsNullOrEmpty(character))
                return "A1"; // Safety check

            if (texture_map.TryGetValue(character, out var textureName))
            {
                return textureName;
            }

            // Fallback for any notes not in the map
            Logger.Log($"[NoteTextureMapper] Note character '{character}' not found in map. Using A1 as default.", LoggingTarget.Runtime, LogLevel.Error);
            return "A1"; // Use A1.png as a default
        }
    }
}