using System;
using System.Linq;
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
            GivenLogFilesInDirectory();
            Array.ForEach(LogFiles, GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Max() + "z";
            GivenPersistedBookmark(bookmarkedFile, Fixture.Create<long>());

            WhenLogShipperIsCalled();

            LogFiles.ShouldBeEmpty("No one shall remain!");
        }

        [Test]
        public void AndBookmarkedLogCannotBeOpened_ThenPreviousFilesAreDeletedButNotLast()
        {
            GivenLogFilesInDirectory();
            Array.ForEach(LogFiles.Take(LogFiles.Length - 1).ToArray(), GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Last();
            var bookmarkedPosition = Fixture.Create<long>();
            GivenPersistedBookmark(bookmarkedFile, bookmarkedPosition);
            GivenLogReaderCreateIOError(CurrentLogFileName, CurrentLogFilePosition);

            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBe(bookmarkedFile, "Bookmarked log file name should not change");
            CurrentLogFilePosition.ShouldBe(bookmarkedPosition, "Bookmarked position should not change");

            LogFiles.ShouldBe(new[] { bookmarkedFile }, "Only one shall remain!");
        }

        [Test]
        public void AndBookmarkedLogIsAtTheEnd_ThenPreviousFilesAreDeletedButNotLast()
        {
            GivenLogFilesInDirectory();
            Array.ForEach(LogFiles.Take(LogFiles.Length - 1).ToArray(), GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Last();
            var bookmarkedPosition = Fixture.Create<long>();
            GivenPersistedBookmark(bookmarkedFile, bookmarkedPosition);
            GivenLogReader(CurrentLogFileName, CurrentLogFilePosition, 0);

            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBe(bookmarkedFile, "Bookmarked log file name should not change");
            CurrentLogFilePosition.ShouldBe(bookmarkedPosition, "Bookmarked position should not change");

            LogFiles.ShouldBe(new[] { bookmarkedFile }, "Only one shall remain!");
        }

        [Test]
        public void AndBookmarkedLogIsAtTheEndOfFirstFile_ThenAllNextFilesAreRead()
        {
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

            WhenLogShipperIsCalled();

            LogFiles.ShouldBe(new[] { otherFile }, "Only one shall remain!");

            CurrentLogFileName.ShouldBe(otherFile);
            CurrentLogFilePosition.ShouldBe(Options.Object.BatchPostingLimit * 2);

            SentBatches.ShouldBe(2);
            SentRecords.ShouldBe(Options.Object.BatchPostingLimit * 2);
        }

        [Test]
        public void AndFailureLockingPreviousFile_ThenProcessingStops()
        {
            GivenLogFilesInDirectory(files: 2);

            var initialFile = LogFiles[0];
            var initialPosition = Fixture.Create<long>();
            var otherFile = LogFiles[1];

            GivenPersistedBookmark(initialFile, initialPosition);
            GivenFileCannotBeLocked(initialFile);
            GivenLogReader(initialFile, length: initialPosition, maxStreams: 0);

            WhenLogShipperIsCalled();

            LogFiles.ShouldBe(new[] { initialFile, otherFile }, "No files should be removed.");

            CurrentLogFileName.ShouldBe(initialFile);
            CurrentLogFilePosition.ShouldBe(initialPosition);

            SentBatches.ShouldBe(0);
            SentRecords.ShouldBe(0);
        }

        [Test]
        public void AndSendFailure_ThenPositionIsNotUpdated()
        {
            GivenLogFilesInDirectory(files: 2);
            var allFiles = LogFiles.ToArray();
            var initialFile = LogFiles[0];

            GivenPersistedBookmark(initialFile, 0);
            GivenLogReader(initialFile, length: Options.Object.BatchPostingLimit, maxStreams: Options.Object.BatchPostingLimit);
            GivenSendIsFailed();

            WhenLogShipperIsCalled();

            LogFiles.ShouldBe(allFiles, "Nothing shall be deleted.");

            CurrentLogFileName.ShouldBe(initialFile);
            CurrentLogFilePosition.ShouldBe(0);

            SentBatches.ShouldBe(0);
            SentRecords.ShouldBe(0);
            FailedBatches.ShouldBe(1);
            FailedRecords.ShouldBe(Options.Object.BatchPostingLimit);
        }

        [Test]
        public void AndLogFileWasTruncated_ThenFileIsReadFromTheBeginning()
        {
            GivenLogFilesInDirectory(1);
            var initialFile = LogFiles[0];
            const int realFileLength = 50;
            const int bookmarkedPosition = 100;

            GivenPersistedBookmark(initialFile, position: bookmarkedPosition);
            GivenLockedFileLength(initialFile, length: realFileLength);
            GivenLogReader(initialFile, length: realFileLength, maxStreams: int.MaxValue);
            GivenSendIsSuccessful();

            WhenLogShipperIsCalled();

            CurrentLogFileName.ShouldBe(initialFile);
            CurrentLogFilePosition.ShouldBe(realFileLength);
            SentBatches.ShouldBe((realFileLength + (Options.Object.BatchPostingLimit - 1)) / Options.Object.BatchPostingLimit);
            SentRecords.ShouldBe(realFileLength);
        }


    }
}
