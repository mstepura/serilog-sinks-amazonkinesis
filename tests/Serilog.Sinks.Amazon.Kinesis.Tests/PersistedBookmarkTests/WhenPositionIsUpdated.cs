using System;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.PersistedBookmarkTests
{
    class WhenPositionIsUpdated : PersistedBookmarkTestBase
    {
        [Test]
        public void WhenUpdatedAfterCreating_ThenFileNameAndPositionAreCorrect()
        {
            GivenFileDoesNotExist();
            WhenBookmarkIsCreated();
            WhenUpdatedWithFileNameAndPosition();
            WhenUpdatedWithPosition();

            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(Position),
                () => Target.FileName.ShouldBe(FileName)
                );
        }

        [Test]
        public void WhenUpdatedAfterOpening_ThenFileNameAndPositionAreCorrect()
        {
            GivenFileDoesNotExist();
            WhenBookmarkIsCreated();
            WhenUpdatedWithFileNameAndPosition();
            WhenBookmarkIsClosed();
            WhenBookmarkIsCreated();

            WhenUpdatedWithPosition();

            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(Position),
                () => Target.FileName.ShouldBe(FileName)
                );
        }

        [Test]
        public void WhenNoFileNameIsSet_ThenThrowException()
        {
            GivenFileDoesNotExist();
            WhenBookmarkIsCreated();

            Should.Throw<InvalidOperationException>(
                () => WhenUpdatedWithPosition()
                );
        }
    }
}
