using osu.Framework.Testing;

namespace TypeBeat.Game.Tests.Visual
{
    public abstract partial class TypeBeatTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new TypeBeatTestSceneTestRunner();

        private partial class TypeBeatTestSceneTestRunner : TypeBeatGameBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }
    }
}
