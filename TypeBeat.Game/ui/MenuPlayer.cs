using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.ui
{
    public partial class MenuPlayer : CompositeDrawable
    {
        public readonly Bindable<bool> IsPlaying = new Bindable<bool>(true);
        public Action OnPrevious;
        public Action OnTogglePlay;
        public Action OnNext;

        private Button prevButton;
        private Button playPauseButton;
        private Button nextButton;

        private Texture texPrev;
        private Texture texPlay;
        private Texture texPause;
        private Texture texNext;

        public Vector2 Spacing { get; internal set; }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            texPrev = textures.Get("images/audioplayer/AudioPlayerPrev.png");
            texPlay = textures.Get("images/audioplayer/AudioPlayerPlay.png");
            texPause = textures.Get("images/audioplayer/AudioPlayerPause.png");
            texNext = textures.Get("images/audioplayer/AudioPlayerNext.png");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Margin = new MarginPadding { Bottom = 25 };

            InternalChild = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(10),
                Children = new Drawable[]
                {
                    prevButton = new Button(texPrev)
                    {
                        Action = () =>
                        {
                            OnPrevious?.Invoke();
                            IsPlaying.Value = true;
                        }
                    },
                    playPauseButton = new Button(IsPlaying.Value ? texPause : texPlay)
                    {
                        Action = () => IsPlaying.Value = !IsPlaying.Value
                    },
                    nextButton = new Button(texNext)
                    {
                        Action = () =>
                        {
                            OnNext?.Invoke();
                            IsPlaying.Value = true;
                        }
                    }
                }
            };

            IsPlaying.ValueChanged += e =>
            {
                playPauseButton.Texture = e.NewValue ? texPause : texPlay;
                OnTogglePlay?.Invoke();
            };
        }

        private partial class Button : ClickableContainer
        {
            private readonly Box background;
            private readonly Sprite sprite;

            public Texture Texture
            {
                get => sprite.Texture;
                set => sprite.Texture = value;
            }

            public Button(Texture texture)
            {
                Size = new Vector2(15);
                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0f
                    },
                    sprite = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Texture = texture,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        FillMode = FillMode.Fit,
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.ScaleTo(1.2f, 200, Easing.OutQuint);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ScaleTo(1f, 200, Easing.OutQuint);
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                this.ScaleTo(1.1f, 100, Easing.OutQuint);
                return base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                this.ScaleTo(IsHovered ? 1.2f : 1f, 100, Easing.OutQuint);
                base.OnMouseUp(e);
            }
        }
    }
}