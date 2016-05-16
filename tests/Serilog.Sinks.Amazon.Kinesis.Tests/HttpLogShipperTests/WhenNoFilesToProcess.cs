using System.IO;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    class WhenNoFilesToProcess : HttpLogShipperBaseTestBase
    {
        [Test]
        public void AndBookmarkCannotBeCreated()
        {
            GivenSinkOptionsAreSet();
            GivenPersistedBookmarkIsLocked();

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();
        }

        [Test]
        public void AndBookmarkHasNoData_ThenShipperDoesNotDoAnything()
        {
            GivenSinkOptionsAreSet();
            GivenPersistedBookmark();
            GivenNoLogFilesInDirectory();

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBeNull();
            CurrentLogFilePosition.ShouldBe(0);
        }

        [Test]
        public void AndBookmarkHasData_ThenShipperDoesNotDoAnything()
        {
            GivenSinkOptionsAreSet();
            GivenPersistedBookmark(Path.Combine(Path.GetTempPath(), "fake"), base.Fixture.Create<long>());
            GivenFileDoesNotExist(CurrentLogFileName);
            GivenNoLogFilesInDirectory();

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBeNull();
            CurrentLogFilePosition.ShouldBe(0);
        }
    }
}
