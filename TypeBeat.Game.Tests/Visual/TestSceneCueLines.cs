using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Layout;
using TypeBeat.Game.Gameplay.Objects;

namespace TypeBeat.Game.Tests.Visual
{
    public partial class TestSceneCueLines : TypeBeatTestScene
    {
        private Container testContainer;
        private LayoutConfig layout = new LayoutConfig();
        private NoteAppearanceConfig appearance = new NoteAppearanceConfig();

        public TestSceneCueLines()
        {
            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            AddStep("Spawn cue line (500ms->2000ms)", () =>
            {
                testContainer.Clear();
                var pair = new DrawableNotePair(500, 2000, false, layout, appearance)
                {
                    TimeOffsetMs = 0
                };
                testContainer.Add(pair);
            });

            AddStep("Spawn cue line (0ms->1500ms)", () =>
            {
                testContainer.Clear();
                var pair = new DrawableNotePair(0, 1500, false, layout, appearance)
                {
                    TimeOffsetMs = 0
                };
                testContainer.Add(pair);
            });

            AddStep("Spawn space cue line (0ms->2000ms)", () =>
            {
                testContainer.Clear();
                var pair = new DrawableNotePair(0, 2000, true, layout, appearance)
                {
                    TimeOffsetMs = 0
                };
                testContainer.Add(pair);
            });

            AddStep("Spawn multiple cues", () =>
            {
                testContainer.Clear();
                
                for (int i = 0; i < 5; i++)
                {
                    double start = i * 500;
                    double end = start + 1500;
                    var pair = new DrawableNotePair(start, end, i % 2 == 1, layout, appearance)
                    {
                        TimeOffsetMs = 0
                    };
                    testContainer.Add(pair);
                }
            });
        }
    }
}
