using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osuTK;

namespace TypeBeat.Game.ui
{
    public partial class HoverContainer : Container
    {
        private const float hover_scale = 1.05f;
        private const float hover_duration = 250;

        protected override bool OnHover(HoverEvent e)
        {
            this.ScaleTo(hover_scale, hover_duration, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            this.ScaleTo(1f, hover_duration, Easing.OutQuint);
            base.OnHoverLost(e);
        }
    }
}