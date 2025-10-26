// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.Editor.UI
{
    /// <summary>
    /// Context menu for beatmap difficulty items.
    /// </summary>
    public partial class BeatmapContextMenu : Container
    {
        private readonly Action onDelete;
        
        public BeatmapContextMenu(Action onDelete)
        {
            this.onDelete = onDelete;
            
            AutoSizeAxes = Axes.Both;
            Masking = true;
            CornerRadius = 5;
            Alpha = 0;
            Depth = -1000; // Render on top
        }
        
        protected override void LoadComplete()
        {
            base.LoadComplete();
            
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(40, 40, 40, 255)
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        createMenuItem("Delete", Color4.Red, onDelete)
                    }
                }
            };
        }
        
        private Drawable createMenuItem(string text, Color4 textColor, Action action)
        {
            var container = new Container
            {
                Width = 150,
                Height = 35,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Transparent
                    },
                    new SpriteText
                    {
                        Text = text,
                        Font = FontUsage.Default.With(family: "Inter", size: 14),
                        Colour = textColor,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Left = 10 }
                    },
                    new HoverClickContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Action = () =>
                        {
                            action?.Invoke();
                            Hide();
                        }
                    }
                }
            };
            
            return container;
        }
        
        public void Show(Vector2 position)
        {
            Position = position;
            this.FadeIn(100);
        }
        
        public new void Hide()
        {
            this.FadeOut(100);
        }
        
        protected override bool OnClick(ClickEvent e)
        {
            // Prevent click from propagating
            return true;
        }
        
        /// <summary>
        /// Helper container for hover effects and click handling.
        /// </summary>
        private partial class HoverClickContainer : Container
        {
            public Action Action { get; set; }
            
            private Box hoverBox;
            
            protected override void LoadComplete()
            {
                base.LoadComplete();
                
                Add(hoverBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Alpha = 0
                });
            }
            
            protected override bool OnHover(HoverEvent e)
            {
                hoverBox.FadeTo(0.1f, 100);
                return base.OnHover(e);
            }
            
            protected override void OnHoverLost(HoverLostEvent e)
            {
                hoverBox.FadeOut(100);
                base.OnHoverLost(e);
            }
            
            protected override bool OnClick(ClickEvent e)
            {
                Action?.Invoke();
                return true;
            }
        }
    }
}
