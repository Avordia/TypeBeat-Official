using osu.Framework.Audio.Track;
using osu.Framework.Bindables;

namespace TypeBeat.Game.Gameplay.Timing
{
    /// <summary>
    /// Thin wrapper around an osu!Framework Track providing a single source of gameplay time
    /// and an optional latency offset for calibration.
    /// </summary>
    public class Conductor
    {
        public Track? Track { get; private set; }

        /// <summary>
        /// Additional offset applied to Track.CurrentTime for input/audio calibration (milliseconds).
        /// Positive shifts events later; negative shifts earlier.
        /// </summary>
        public BindableDouble LatencyOffsetMs { get; } = new BindableDouble(0);

        public Conductor(Track? track)
        {
            Track = track;
        }

        public void SetTrack(Track? track) => Track = track;

        public double CurrentTime => (Track?.CurrentTime ?? 0) + LatencyOffsetMs.Value;

        public void Start() => Track?.Start();
        public void Stop() => Track?.Stop();
        public void Pause() => Track?.Stop();
        public void Seek(double timeMs) => Track?.Seek(timeMs);
    }
}
