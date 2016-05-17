using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    class WhenLogFilesFound : HttpLogShipperBaseTestBase
    {
        [Test]
        public void AndBookmarkIsGreaterThanAllFiles_ThenFilesAreDeleted()
        {
            GivenSinkOptionsAreSet();
            GivenLogFilesInDirectory();
            Array.ForEach(LogFiles, GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Max() + "z";
            GivenPersistedBookmark(bookmarkedFile, Fixture.Create<long>());

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            Array.ForEach(LogFiles,
                file => LogShipperFileManager.Verify(x => x.LockAndDeleteFile(file), Times.Once)
                );
        }

        [Test]
        public void AndBookmarkedLogCannotBeOpened_ThenPreviousFilesAreDeletedButNotLast()
        {
            GivenSinkOptionsAreSet();
            GivenLogFilesInDirectory();

            var filesToDelete = LogFiles.Take(LogFiles.Length - 1).ToArray();
            Array.ForEach(filesToDelete, GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Last();
            var bookmarkedPosition = Fixture.Create<long>();
            GivenPersistedBookmark(bookmarkedFile, bookmarkedPosition);

            GivenLogReaderCreateThrows(CurrentLogFileName, CurrentLogFilePosition);

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBe(bookmarkedFile, "Bookmarked log file name should not change");
            CurrentLogFilePosition.ShouldBe(bookmarkedPosition, "Bookmarked position should not change");

            LogShipperFileManager.Verify(x => x.LockAndDeleteFile(bookmarkedFile), Times.Never);
            Array.ForEach(filesToDelete,
                file => LogShipperFileManager.Verify(x => x.LockAndDeleteFile(file), Times.Once)
                );
        }

        [Test]
        public void AndBookmarkedLogIsAtTheEnd_ThenPreviousFilesAreDeletedButNotLast()
        {
            GivenSinkOptionsAreSet();
            GivenLogFilesInDirectory();

            var filesToDelete = LogFiles.Take(LogFiles.Length - 1).ToArray();
            Array.ForEach(filesToDelete, GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Last();
            var bookmarkedPosition = Fixture.Create<long>();
            GivenPersistedBookmark(bookmarkedFile, bookmarkedPosition);

            GivenLogReader(CurrentLogFilePosition, 0);

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBe(bookmarkedFile, "Bookmarked log file name should not change");
            CurrentLogFilePosition.ShouldBe(bookmarkedPosition, "Bookmarked position should not change");

            LogShipperFileManager.Verify(x => x.LockAndDeleteFile(bookmarkedFile), Times.Never);
            Array.ForEach(filesToDelete,
                file => LogShipperFileManager.Verify(x => x.LockAndDeleteFile(file), Times.Once)
                );
        }
    }
}
