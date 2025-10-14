using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.ui
{
    public partial class HoverableHeader : Header
    {
        private readonly Box hoverIndicator;
        private readonly float defaultHeight;
        private readonly float expandedHeight;
        private bool isExpanded = false;
        private const float ExpandDuration = 300;

        /// <summary>
        /// Event fired when the header is expanded or collapsed.
        /// </summary>
        public event System.Action<bool> OnExpandStateChanged;

        public HoverableHeader(float defaultHeight = 35, float expandedHeight = 60)
        {
            this.defaultHeight = defaultHeight;
            this.expandedHeight = expandedHeight;
            Height = defaultHeight;

            AddInternal(
                hoverIndicator = new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 3,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
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