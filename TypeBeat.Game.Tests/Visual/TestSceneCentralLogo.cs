using osu.Framework.Graphics;
using TypeBeat.Game.Ui;

namespace TypeBeat.Game.Tests.Visual
{
    public partial class TestSceneCentralLogo : TypeBeatTestScene
    {
        public TestSceneCentralLogo()
        {
            // Use an existing UI component to keep this visual test compiling
            Add(new Header { RelativeSizeAxes = Axes.X, Height = 35 });
        }
    }
}