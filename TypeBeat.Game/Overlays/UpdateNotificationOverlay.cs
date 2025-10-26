using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Updates;

namespace TypeBeat.Game.Overlays
{
    public partial class UpdateNotificationOverlay : OverlayContainer
    {
        private FillFlowContainer content;
        private SpriteText versionText;
        private FillFlowContainer buttonContainer;
        
        [Resolved]
        private UpdateManager updateManager { get; set; }
        
        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            
            Child = new Container
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                AutoSizeAxes = Axes.Both,
                Margin = new MarginPadding(20),
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.8f
                    },
                    content = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(15),
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = "Update Available!",
                                Font = new FontUsage(size: 24),
                                Colour = Color4.White
                            },
                            versionText = new SpriteText
                            {
                                Text = $"Current version: {updateManager?.GetCurrentVersion() ?? "Unknown"}",
                                Font = new FontUsage(size: 16),
                                Colour = Color4.LightGray
                            },
                            buttonContainer = new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    new UpdateButton("Update Now", () => performUpdate()),
                                    new UpdateButton("Later", () => Hide())
                                }
                            }
                        }
                    }
                }
            };
            
            Hide();
        }
        
        public void ShowUpdateNotification()
        {
            Show();
        }
        
        private async void performUpdate()
        {
            versionText.Text = "Downloading update...";
            buttonContainer.Hide();
            
            var success = await updateManager.UpdateAsync();
            
            if (success)
            {
                versionText.Text = "Update complete! Restarting...";
                Schedule(() =>
                {
                    System.Threading.Thread.Sleep(2000);
                    updateManager.RestartApplication();
                });
            }
            else
            {
                versionText.Text = "Update failed. Please try again later.";
                buttonContainer.Show();
            }
        }
        
        protected override void PopIn()
        {
            this.FadeIn(300);
        }
        
        protected override void PopOut()
        {
            this.FadeOut(300);
        }
        
        private partial class UpdateButton : Container
        {
            private readonly System.Action action;
            
            public UpdateButton(string text, System.Action action)
            {
                this.action = action;
                AutoSizeAxes = Axes.Both;
                
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkBlue
                    },
                    new SpriteText
                    {
                        Text = text,
                        Font = new FontUsage(size: 18),
                        Colour = Color4.White,
                        Padding = new MarginPadding { Horizontal = 20, Vertical = 10 }
                    }
                };
            }
            
            protected override bool OnClick(ClickEvent e)
            {
                action?.Invoke();
                return true;
            }
            
            protected override bool OnHover(HoverEvent e)
            {
                this.ScaleTo(1.1f, 200);
                return true;
            }
            
            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ScaleTo(1f, 200);
            }
        }
    }
}
