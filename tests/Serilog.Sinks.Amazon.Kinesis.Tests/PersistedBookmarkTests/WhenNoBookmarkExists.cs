using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.PersistedBookmarkTests
{
    class WhenNoBookmarkExists : PersistedBookmarkTestBase
    {
        protected override void Given()
        {
            GivenFileDoesNotExist();
        }

        protected override void When()
        {
            WhenBookmarkIsCreated();
        }

        [Test]
        public void ThenFileIsCreated()
        {
            File.Exists(BookmarkFileName).ShouldBeTrue();
        }

        [Test]
        public void ThenFileNameAndPositionAreEmpty()
        {
            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(0),
                () => Target.FileName.ShouldBeNull()
                );
        }
    }
}
