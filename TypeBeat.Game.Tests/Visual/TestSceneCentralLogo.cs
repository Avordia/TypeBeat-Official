using osu.Framework.Graphics;
using TypeBeat.Game.ui;

namespace TypeBeat.Game.Tests.Visual
{
    public partial class TestSceneCentralLogo : TypeBeatTestScene
    {
        public TestSceneCentralLogo()
        {
            Add(new CentralLogo
            {
                RelativeSizeAxes = Axes.Both
            });
        }
    }
}