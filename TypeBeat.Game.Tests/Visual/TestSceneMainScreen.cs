using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;

namespace TypeBeat.Game.Tests.Visual
{
    public partial class TestSceneMainScreen : TypeBeatTestScene
    {
        private Container mainContainer;
        private SpriteText logoText;
        private SpriteText titleText;
        private SpriteText playerText;

        [SetUp]
        public void SetUp()
        {
            Add(mainContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    logoText = new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                        Text = "Logo",
                        Font = new FontUsage(size: 40)
                    },
                    titleText = new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.Centre,
                        Y = 50,
                        Text = "Song Title",
                        Font = new FontUsage(size: 20)
                    },
                    playerText = new SpriteText
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.Centre,
                        Text = "Player Controls",
                        Font = new FontUsage(size: 20)
                    }
                }
            });

            AddStep("Move logo left", () => logoText.MoveToX(100));
            AddStep("Move logo center", () => logoText.MoveToX(0));
            AddSliderStep("Scale texts", 0.5f, 2f, 1f, scale =>
            {
                if (logoText == null || titleText == null || playerText == null)
                    return;

                logoText.Scale = new Vector2(scale);
                titleText.Scale = new Vector2(scale);
                playerText.Scale = new Vector2(scale);
            });
        }
    }
}