using osu.Framework.iOS;
using TypeBeat.Game;

namespace TypeBeat.iOS
{
    /// <inheritdoc />
    public class AppDelegate : GameApplicationDelegate
    {
        /// <inheritdoc />
        protected override osu.Framework.Game CreateGame() => new TypeBeatGame();
    }
}
