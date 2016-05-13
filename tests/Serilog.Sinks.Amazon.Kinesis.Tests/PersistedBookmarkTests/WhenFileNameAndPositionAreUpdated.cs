using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.PersistedBookmarkTests
{
    class WhenFileNameAndPositionAreUpdated : PersistedBookmarkTestBase
    {
        [TestCase(true, false)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void WhenUpdatedAfterOpening_ThenFileNameAndPositionAreCorrect(
            bool fileExists,
            bool containsGarbage
            )
        {
            if (fileExists) { GivenFileExist(); } else { GivenFileDoesNotExist(); }
            if (containsGarbage) GivenFileContainsGarbage(1000);

            WhenBookmarkIsCreated();
            WhenUpdatedWithFileNameAndPosition();

            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(Position),
                () => Target.FileName.ShouldBe(FileName)
                );
        }

        [TestCase(true, false)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void WhenClosedAndReOpened_ThenFileNameAndPositionAreCorrect(
            bool fileExists,
            bool containsGarbage
            )
        {
            if (fileExists) { GivenFileExist(); } else { GivenFileDoesNotExist(); }
            if (containsGarbage) GivenFileContainsGarbage(1000);

            WhenBookmarkIsCreated();
            WhenUpdatedWithFileNameAndPosition();
            WhenBookmarkIsClosed();
            WhenBookmarkIsCreated();

            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(Position),
                () => Target.FileName.ShouldBe(FileName)
                );
        }
    }
}
