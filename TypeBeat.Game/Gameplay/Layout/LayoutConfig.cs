using osuTK;

namespace TypeBeat.Game.Gameplay.Layout
{
    public class LayoutConfig
    {
        public float CenterLineYFraction { get; set; } = 0.5f;
        public float HalfGapXFraction { get; set; } = 0.06f;
        public float SpawnMarginXFraction { get; set; } = 0.12f;
        
        /// <summary>
        /// Horizontal offset in pixels from screen center where lines converge.
        /// Positive = right, Negative = left, 0 = exact center.
        /// </summary>
        public float CenterOffsetX { get; set; } = 0f;

        public float GetCenterLineY(Vector2 drawSize) => clamp01(CenterLineYFraction) * drawSize.Y;

        public (float leftX, float rightX) GetDestinationXs(Vector2 drawSize)
        {
            float cx = (drawSize.X * 0.5f) + CenterOffsetX; // Apply horizontal offset
            float halfGap = clamp01(HalfGapXFraction) * drawSize.X;
            return (cx - halfGap, cx + halfGap);
        }

        public (float leftSpawnX, float rightSpawnX) GetSpawnXs(Vector2 drawSize)
        {
            float margin = clamp01(SpawnMarginXFraction) * drawSize.X;
            return (-margin, drawSize.X + margin);
        }

        public (Vector2 leftDest, Vector2 rightDest) GetDestinations(Vector2 drawSize)
        {
            var (lx, rx) = GetDestinationXs(drawSize);
            float y = GetCenterLineY(drawSize);
            return (new Vector2(lx, y), new Vector2(rx, y));
        }

        private static float clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }
}
