using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Ui
{
    public partial class DifficultyButton : ClickableContainer
    {
        private readonly Box background;
        private readonly Container borderContainer;
        private readonly SpriteText difficultyText;
        private readonly Beatmap beatmap;
        
        public event Action<Beatmap> OnSelected;

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

        public DifficultyButton(Beatmap beatmap)
        {
            this.beatmap = beatmap;
            
            RelativeSizeAxes = Axes.X;
            Height = 50;

            Children = new Drawable[]
            {
                borderContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 25,
                    BorderThickness = 3,
                    BorderColour = Colour4.White,
                    Alpha = 0,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    }
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 25,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.White,
                            Alpha = 0.2f
                        },
                        difficultyText = new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = beatmap.DifficultyName ?? "Unknown",
                            Font = new FontUsage(size: 18, weight: "Bold"),
                            Colour = Colour4.White
                        }
                    }
                }
            };
        }

        private void updateSelectionState()
        {
            if (isSelected)
                borderContainer.FadeIn(200, Easing.OutQuint);
            else
                borderContainer.FadeOut(200, Easing.OutQuint);
        }

        protected override bool OnHover(HoverEvent e)
        {
            background.FadeTo(0.4f, 200, Easing.OutQuint);
            this.ScaleTo(1.05f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeTo(0.2f, 200, Easing.OutQuint);
            this.ScaleTo(1f, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            this.ScaleTo(0.95f, 100, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, 100, Easing.OutQuint);

            OnSelected?.Invoke(beatmap);

            return base.OnClick(e);
        }
    }
}
