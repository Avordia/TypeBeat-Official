using osu.Framework.Graphics;
using osu.Framework.Screens;
using NUnit.Framework;

namespace TypeBeat.Game.Tests.Visual
{
    [TestFixture]
    public partial class TestSceneMainScreen : TypeBeatTestScene
    {
        public TestSceneMainScreen()
        {
            Add(new ScreenStack(new MainScreen()) { RelativeSizeAxes = Axes.Both });
        }
    }
}
