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
    /// Clickable list item representing a beatpack in the sidebar.
    /// </summary>
    public partial class BeatpackListItem : Container
    {
        public Action<LocalBeatpack> OnSelected { get; set; }

        private readonly LocalBeatpack beatpack;
        private Box background;
        private Container content;

        private bool isSelected;

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                updateSelectionState();
            }
        }

        public BeatpackListItem(LocalBeatpack beatpack)
        {
            this.beatpack = beatpack;

            RelativeSizeAxes = Axes.X;
            Height = 80;
            Masking = true;
            CornerRadius = 15;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(40, 40, 40, 255)
                },
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(10),
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5),
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = beatpack.Name,
                                Font = FontUsage.Default.With(family: "Kodchasan-Bold", size: 18),
                                Colour = Color4.White
                            },
                            new SpriteText
                            {
                                Text = beatpack.IsFinished ? "Finished" : "(Unfinished)",
                                Font = FontUsage.Default.With(family: "Inter", size: 14),
                                Colour = beatpack.IsFinished ? Color4.LightGreen : Color4.Orange
                            }
                        }
                    }
                }
            };
        }

        protected override bool OnClick(ClickEvent e)
        {
            OnSelected?.Invoke(beatpack);
            return true;
        }

        protected override bool OnHover(HoverEvent e)
        {
            content.ScaleTo(1.05f, 200, Easing.OutQuint);
            background.FadeColour(new Color4(50, 50, 50, 255), 200);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            content.ScaleTo(1f, 200, Easing.OutQuint);
            updateSelectionState();
        }

        private void updateSelectionState()
        {
            if (isSelected)
            {
                background.FadeColour(new Color4(60, 60, 60, 255), 200);
            }
            else
            {
                background.FadeColour(new Color4(40, 40, 40, 255), 200);
            }
        }
    }
}

