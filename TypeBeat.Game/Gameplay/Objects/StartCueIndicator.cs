using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using TypeBeat.Game.Gameplay.Layout;
using osu.Framework.Logging;
using osuTK;

namespace TypeBeat.Game.Gameplay.Objects
{
	/// <summary>
	/// A standalone visual indicator to show when gameplay/music will begin.
	/// Independent of the note scheduler and beatmap notes.
	/// Spawns at gameplay t=0 and completes at a specified absolute arrival time.
	/// </summary>
	public partial class StartCueIndicator : CompositeDrawable
	{
		private readonly double startTimeAbsMs;
		private readonly double arrivalTimeAbsMs;
		private CircularContainer ring;
		private bool firstLogDone = false;
		private readonly LayoutConfig layout;

		/// <param name="startTimeAbsMs">Absolute (Clock.CurrentTime) when the cue starts animating (usually now).</param>
		/// <param name="arrivalTimeAbsMs">Absolute (Clock.CurrentTime) when the cue should complete (e.g., gameplayStart + first note EndTime).</param>
		/// <param name="layout">Layout config to align the cue with the center line.</param>
		public StartCueIndicator(double startTimeAbsMs, double arrivalTimeAbsMs, LayoutConfig layout)
		{
			this.startTimeAbsMs = startTimeAbsMs;
			this.arrivalTimeAbsMs = arrivalTimeAbsMs;
			this.layout = layout;
			RelativeSizeAxes = Axes.Both;
			Anchor = Anchor.TopLeft;
			Origin = Anchor.TopLeft;
			Depth = -2000; // render above playfield notes
			InternalChild = ring = new CircularContainer
			{
				Size = new Vector2(140, 140),
				BorderColour = Colour4.Yellow,
				BorderThickness = 6,
				Masking = true,
				Child = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0.12f, Colour = Colour4.Yellow },
				Alpha = 0,
				Anchor = Anchor.Centre,
				Origin = Anchor.Centre
			};
		}

		/// <summary>
		/// Immediately consumes/hides the cue (e.g., after first hit/miss is processed).
		/// </summary>
		public void ConsumeNow()
		{
			this.FadeOut(120).Expire();
		}

		protected override void Update()
		{
			base.Update();

			double nowAbs = Clock.CurrentTime; // absolute clock
			if (!firstLogDone)
			{
				firstLogDone = true;
				Logger.Log($"[StartCueIndicator] First Update: now={nowAbs:F0} startAbs={startTimeAbsMs:F0} arriveAbs={arrivalTimeAbsMs:F0}", LoggingTarget.Runtime, LogLevel.Important);
			}

			// Before start: hidden
			if (nowAbs < startTimeAbsMs)
			{
				ring.Alpha = 0;
				return;
			}

			// Normalize progress from 0..1 across [startAbs, arriveAbs]
			double duration = System.Math.Max(1, arrivalTimeAbsMs - startTimeAbsMs);
			double p = System.Math.Clamp((nowAbs - startTimeAbsMs) / duration, 0, 1);

			// Visual: grow and brighten toward the center as arrival approaches
			float width = DrawSize.X;
			float height = DrawSize.Y;
			var size = new Vector2(width, height);
			float y = layout != null ? layout.GetCenterLineY(size) : size.Y * 0.6f;
			ring.Position = new Vector2(size.X * 0.5f, y);
			ring.Scale = new Vector2(0.6f + 0.8f * (float)p);
			ring.Alpha = 0.12f + 0.88f * (float)p;

			if (p >= 1)
			{
				// Brief flash on arrival, then expire
				ring.FadeTo(1f, 40).Then().FadeOut(160);
				Expire();
			}
		}
	}
}


