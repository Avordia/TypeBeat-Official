using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osuTK.Graphics;
using TypeBeat.Game.ui; // Assuming CentralLogo is in this namespace
using TypeBeat.Game.UI;  // Assuming MenuButton is in this namespace

namespace TypeBeat.Game
{
    public partial class MainScreen : Screen
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                // White background
                new Box
                {
                    Colour = Color4.White,
                    RelativeSizeAxes = Axes.Both,
                },

                new Box
                {
                    Colour = Color4.Black,
                    RelativeSizeAxes = Axes.X,
                    Height = 2, 
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                },
                
                new Box
                {
                    Colour = Color4.Black,
                    RelativeSizeAxes = Axes.X,
                    Height = 2,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                },
                new CentralLogo
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
            };
        }
    }
}