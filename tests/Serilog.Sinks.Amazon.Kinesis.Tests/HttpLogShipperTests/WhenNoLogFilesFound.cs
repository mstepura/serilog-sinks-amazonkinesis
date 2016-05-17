using System.IO;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    class WhenNoLogFilesFound : HttpLogShipperBaseTestBase
    {
        [Test]
        public void AndBookmarkHasNoData_ThenShipperDoesNotDoAnything()
        {
            GivenPersistedBookmark();
            GivenLogFilesInDirectory(0);

            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBeNull();
            CurrentLogFilePosition.ShouldBe(0);
        }

        [Test]
        public void AndBookmarkHasData_ThenShipperDoesNotDoAnything()
        {
            GivenPersistedBookmark(Path.Combine(Path.GetTempPath(), "fake"), base.Fixture.Create<long>());
            GivenLogFilesInDirectory(0);

            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBeNull();
            CurrentLogFilePosition.ShouldBe(0);
        }
    }
}
