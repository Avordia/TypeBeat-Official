using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using TypeBeat.Game;

namespace TypeBeat.Game.Tests.Visual
{
    [TestFixture]
    public partial class TestSceneMainMenu : TypeBeatTestScene
    {
        public TestSceneMainMenu()
        {
            ScreenStack screenStack;
            Add(screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both });
            screenStack.Push(new MainScreen());
        }
    }
}