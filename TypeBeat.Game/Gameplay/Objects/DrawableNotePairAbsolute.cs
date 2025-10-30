using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osuTK;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Layout;

namespace TypeBeat.Game.Gameplay.Objects
{
	/// <summary>
	/// Visual note pair that behaves exactly like DrawableNotePair, but uses ABSOLUTE timing.
	/// startAbsMs/endAbsMs are compared directly to Clock.CurrentTime, independent of gameplay offset.
	/// </summary>
	public partial class DrawableNotePairAbsolute : CompositeDrawable
	{
		private readonly double startAbsMs;
		private readonly double endAbsMs;
		private readonly bool isSpace;
		private readonly LayoutConfig layout;
		private readonly NoteAppearanceConfig appearance;

		private Sprite leftLine = null!;
		private Sprite rightLine = null!;
		private bool hasLoggedFirstUpdate = false;
		private bool wasHit = false;

		[Resolved]
		private TextureStore textures { get; set; } = null!;

		public DrawableNotePairAbsolute(double startAbsMs, double endAbsMs, bool isSpace,
			LayoutConfig layout, NoteAppearanceConfig appearance)
		{
			this.startAbsMs = startAbsMs;
			this.endAbsMs = endAbsMs;
			this.isSpace = isSpace;
			this.layout = layout;
			this.appearance = appearance;

			RelativeSizeAxes = Axes.Both;
			Anchor = Anchor.TopLeft;
			Origin = Anchor.TopLeft;

			const float note_scale = 1.14f;
			InternalChildren = new Drawable[]
			{
				leftLine = new Sprite
				{
					Anchor = Anchor.TopLeft,
					Origin = Anchor.Centre,
					Scale = new Vector2(note_scale, note_scale)
				},
				rightLine = new Sprite
				{
					Anchor = Anchor.TopLeft,
					Origin = Anchor.Centre,
					Scale = new Vector2(-note_scale, note_scale)
				}
			};
		}

		protected override void LoadComplete()
		{
			base.LoadComplete();
		}

		[BackgroundDependencyLoader]
		private void load()
		{
			var texture = textures.Get("images/LeftToRightNote.png");
			leftLine.Texture = texture;
			rightLine.Texture = texture;

			var colour = isSpace ? appearance.SpaceColour : appearance.LetterColour;
			leftLine.Colour = rightLine.Colour = colour;

			Logger.Log($"[DrawableNotePairAbsolute] Loaded texture 'LeftToRightNote.png' startAbs={startAbsMs} endAbs={endAbsMs}", LoggingTarget.Runtime, LogLevel.Important);
		}

		public void OnHit()
		{
			wasHit = true;
			this.FadeOut(150).Expire();
		}

		protected override void Update()
		{
			base.Update();

			if (wasHit)
				return;

			double nowAbs = Clock.CurrentTime;
			float width = DrawSize.X;
			float height = DrawSize.Y;
			var size = new Vector2(width, height);

			if (!hasLoggedFirstUpdate)
			{
				hasLoggedFirstUpdate = true;
				Logger.Log($"[DrawableNotePairAbsolute] First Update: nowAbs={nowAbs:F0} startAbs={startAbsMs:F0} endAbs={endAbsMs:F0} size={width}x{height}", LoggingTarget.Runtime, LogLevel.Important);
			}

			var (leftSpawnX, rightSpawnX) = layout.GetSpawnXs(size);
			var (leftDestX, rightDestX) = layout.GetDestinationXs(size);
			float y = layout.GetCenterLineY(size);

			if (nowAbs < startAbsMs)
			{
				leftLine.Alpha = 0;
				rightLine.Alpha = 0;
				leftLine.Position = new Vector2(leftSpawnX, y);
				rightLine.Position = new Vector2(rightSpawnX, y);
				return;
			}

			double dur = System.Math.Max(1, endAbsMs - startAbsMs);
			double p = System.Math.Clamp((nowAbs - startAbsMs) / dur, 0, 1);

			if (p >= 1)
			{
				Expire();
				return;
			}

			float fadeProgress = System.Math.Max(0, (float)((p - 0.8) / 0.2));
			float alpha = 1f - fadeProgress;
			leftLine.Alpha = alpha;
			rightLine.Alpha = alpha;

			float lp = (float)p;
			float lx = (float)(leftSpawnX + (leftDestX - leftSpawnX) * lp);
			float rx = (float)(rightSpawnX + (rightDestX - rightSpawnX) * lp);
			leftLine.Position = new Vector2(lx, y);
			rightLine.Position = new Vector2(rx, y);
		}
	}
}


