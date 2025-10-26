// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System.Collections.Generic;

namespace TypeBeat.Game.Configuration
{
    /// <summary>
    /// Centralized list of available game modes for easy scalability.
    /// </summary>
    public static class Gamemodes
    {
        /// <summary>
        /// List of all available game modes.
        /// </summary>
        public static readonly List<string> Available = new List<string>
        {
            "TypeBeat",
            "TypeNote"
        };

        /// <summary>
        /// Default game mode.
        /// </summary>
        public const string Default = "TypeBeat";
    }
}
