// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK.Graphics;

namespace TypeBeat.Game.Editor.UI
{
    /// <summary>
    /// Button component for beatmap difficulties in the dashboard.
    /// </summary>
    public partial class BeatmapDifficultyItem : Container
    {
        private readonly LocalBeatmap beatmap;
        private readonly Action<LocalBeatmap> onEdit;
        private readonly Action<LocalBeatmap> onDelete;
        
        private Box background;
        private Container content;
        private BeatmapContextMenu contextMenu;

        public BeatmapDifficultyItem(LocalBeatmap beatmap, Action<LocalBeatmap> onEdit = null, Action<LocalBeatmap> onDelete = null)
        {
            this.beatmap = beatmap;
            this.onEdit = onEdit;
            this.onDelete = onDelete;

            Width = 400;
            Height = 60;
            Masking = true;
            CornerRadius = 10;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Children = new Drawable[]
            {
                // Outer container for border
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 14,
                    Children = new Drawable[]
                    {
                        // Border box
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(184, 115, 62, 255) // #B8733E
                        },
                        // Content container with inner background
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Margin = new MarginPadding(2), // Border thickness
                            Masking = true,
                            CornerRadius = 12,
                            Children = new Drawable[]
                            {
                                background = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = new Color4(184, 115, 62, 255)
                                },
                                content = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new SpriteText
                                    {
                                        Text = beatmap.DifficultyName,
                                        Font = FontUsage.Default.With(family: "Inter-Bold", size: 24),
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre
                                    }
                                }
                            }
                        }
                    }
                },
                // Context menu
                contextMenu = new BeatmapContextMenu(() => onDelete?.Invoke(beatmap))
            };
        }
        
        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == osuTK.Input.MouseButton.Right)
            {
                // Show context menu at mouse position
                contextMenu.Show(ToLocalSpace(e.ScreenSpaceMousePosition));
                return true;
            }
            
            return base.OnMouseDown(e);
        }
        
        protected override bool OnClick(ClickEvent e)
        {
            // Hide context menu if it's visible
            if (contextMenu.Alpha > 0)
            {
                contextMenu.Hide();
                return true;
            }
            
            // Invoke the edit callback if provided
            onEdit?.Invoke(beatmap);
            return true;
        }

        protected override bool OnHover(HoverEvent e)
        {
            content.ScaleTo(1.05f, 200, Easing.OutQuint);
            background.FadeColour(new Color4(194, 125, 72, 255), 200);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            content.ScaleTo(1f, 200, Easing.OutQuint);
            background.FadeColour(new Color4(184, 115, 62, 255), 200);
        }
    }
}

