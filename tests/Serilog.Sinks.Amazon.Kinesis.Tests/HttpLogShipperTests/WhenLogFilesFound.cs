using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    class WhenLogFilesFound : HttpLogShipperBaseTestBase
    {
        [Test]
        public void AndBookmarkIsGreaterThanAllFiles_ThenFilesAreDeleted()
        {
            var files = Fixture.CreateMany<string>().ToArray();

            GivenSinkOptionsAreSet();
            GivenLogFilesInDirectory(files);
            Array.ForEach(files, GivenFileDeleteSucceeds);

            var bookmarkedFile = files.Max() + "z";
            GivenPersistedBookmark(bookmarkedFile, Fixture.Create<long>());

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            Array.ForEach(files,
                file => LogShipperFileManager.Verify(x => x.LockAndDeleteFile(file), Times.Once)
                );
        }
    }
}
