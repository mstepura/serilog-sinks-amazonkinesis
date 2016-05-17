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

            Array.ForEach(LogFiles.Take(LogFiles.Length - 1).ToArray(), GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Last();
            var bookmarkedPosition = Fixture.Create<long>();
            GivenPersistedBookmark(bookmarkedFile, bookmarkedPosition);

            GivenLogReaderCreateThrows(CurrentLogFileName, CurrentLogFilePosition);

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBe(bookmarkedFile, "Bookmarked log file name should not change");
            CurrentLogFilePosition.ShouldBe(bookmarkedPosition, "Bookmarked position should not change");

            LogFiles.ShouldBe(new[] { bookmarkedFile }, "Only one shall remain!");
        }

        [Test]
        public void AndBookmarkedLogIsAtTheEnd_ThenPreviousFilesAreDeletedButNotLast()
        {
            GivenSinkOptionsAreSet();
            GivenLogFilesInDirectory();

            Array.ForEach(LogFiles.Take(LogFiles.Length - 1).ToArray(), GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Last();
            var bookmarkedPosition = Fixture.Create<long>();
            GivenPersistedBookmark(bookmarkedFile, bookmarkedPosition);

            GivenLogReader(CurrentLogFileName, CurrentLogFilePosition, 0);

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBe(bookmarkedFile, "Bookmarked log file name should not change");
            CurrentLogFilePosition.ShouldBe(bookmarkedPosition, "Bookmarked position should not change");

            LogFiles.ShouldBe(new[] { bookmarkedFile }, "Only one shall remain!");
        }

        [Test]
        public void AndBookmarkedLogIsAtTheEndOfFirstFile_ThenAllNextFilesAreRead()
        {
            GivenSinkOptionsAreSet();
            GivenLogFilesInDirectory(files: 2);

            var initialFile = LogFiles[0];
            var otherFile = LogFiles[1];

            GivenFileDeleteSucceeds(initialFile);
            GivenPersistedBookmark(initialFile, Fixture.Create<long>());

            GivenLockedFileLength(initialFile, length: CurrentLogFilePosition);
            GivenLockedFileLength(otherFile, length: Options.Object.BatchPostingLimit * 2);

            GivenLogReader(initialFile, length: CurrentLogFilePosition, maxStreams: 0);
            GivenLogReader(otherFile, length: Options.Object.BatchPostingLimit * 2, maxStreams: int.MaxValue);

            GivenSendIsSuccessful();

            WhenLogShipperIsCreated();
            WhenLogShipperIsCalled();

            LogFiles.ShouldBe(new[] { otherFile }, "Only one shall remain!");

            CurrentLogFileName.ShouldBe(otherFile);
            CurrentLogFilePosition.ShouldBe(Options.Object.BatchPostingLimit * 2);

            SentBatches.ShouldBe(2);
            SentRecords.ShouldBe(Options.Object.BatchPostingLimit * 2);
        }
    }
}
