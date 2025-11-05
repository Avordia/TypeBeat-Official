using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game
{
    public partial class DownloadNotification : Container
    {
        private readonly string beatpackName;
        private SpriteText statusText;
        private Box background;
        
        public DownloadNotification(string beatpackName)
        {
            this.beatpackName = beatpackName;
            
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            Width = 400;
            Height = 80;
            Masking = true;
            CornerRadius = 15;
            Alpha = 0;
            
            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(30, 30, 40, 230)
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(15),
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 8),
                            Children = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Text = beatpackName,
                                    Font = new FontUsage(size: 16, weight: "Bold"),
                                    Colour = Color4.White,
                                    Truncate = true
                                },
                                statusText = new SpriteText
                                {
                                    Text = "Downloading... 0%",
                                    Font = new FontUsage(size: 14),
                                    Colour = new Color4(200, 200, 200, 255)
                                }
                            }
                        }
                    }
                }
            };
            
            this.FadeIn(300);
        }
        
        public void SetProgress(float progress)
        {
            Schedule(() =>
            {
                statusText.Text = $"Downloading... {(int)(progress * 100)}%";
            });
        }
        
        public void SetComplete()
        {
            Schedule(() =>
            {
                statusText.Text = "Download complete! ✓";
                statusText.FadeColour(new Color4(0, 255, 100, 255), 200);
                background.FadeColour(new Color4(30, 60, 40, 230), 200);
            });
        }
        
        public void SetError(string error)
        {
            Schedule(() =>
            {
                background.FadeColour(new Color4(180, 50, 50, 230), 200);
                statusText.Text = $"Error: {error}";
                statusText.FadeColour(new Color4(255, 100, 100, 255), 200);
            });
        }
    }
}
