using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.Ui
{
    public partial class HoverableFooter : Footer
    {
        private readonly Box hoverIndicator;
        private readonly float defaultHeight;
        private readonly float expandedHeight;
        private bool isExpanded = false;
        private const float ExpandDuration = 300;
        public event System.Action<bool> OnExpandStateChanged;

        public HoverableFooter(System.Action onLoginRequested, float defaultHeight = 35, float expandedHeight = 60) : base(onLoginRequested)
        {
            this.defaultHeight = defaultHeight;
            this.expandedHeight = expandedHeight;
            Height = defaultHeight;

            AddInternal(
                hoverIndicator = new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 3,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Colour = Colour4.White,
                    Alpha = 0,
                }
            );
        }

        protected override bool OnHover(HoverEvent e)
        {
            hoverIndicator.FadeIn(200, Easing.OutQuint);
            
            if (!isExpanded)
            {
                this.ResizeHeightTo(expandedHeight, ExpandDuration, Easing.OutQuint);
                isExpanded = true;
                OnExpandStateChanged?.Invoke(true);
            }
            
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            hoverIndicator.FadeOut(200, Easing.OutQuint);
            
            if (isExpanded)
            {
                this.ResizeHeightTo(defaultHeight, ExpandDuration, Easing.OutQuint);
                isExpanded = false;
                OnExpandStateChanged?.Invoke(false);
            }
            
            base.OnHoverLost(e);
        }
    }
}