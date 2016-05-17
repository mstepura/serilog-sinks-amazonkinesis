using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using Moq.Language.Flow;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Serilog.Sinks.Amazon.Kinesis.Common;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    [TestFixture]
    abstract class HttpLogShipperBaseTestBase
    {
        private MockRepository _mockRepository;
        protected IFixture Fixture { get; private set; }
        protected LogShipperSUT Target { get; private set; }

        protected Mock<ILogShipperOptions> Options { get; private set; }
        protected Mock<ILogReaderFactory> LogReaderFactory { get; private set; }
        protected Mock<ILogReader> LogReader { get; private set; }
        protected Mock<IPersistedBookmarkFactory> PersistedBookmarkFactory { get; private set; }
        protected Mock<IPersistedBookmark> PersistedBookmark { get; private set; }
        protected Mock<ILogShipperFileManager> LogShipperFileManager { get; private set; }
        protected string LogFileNamePrefix { get; private set; }
        protected string LogFolder { get; private set; }
        protected string[] LogFiles { get; private set; }


        protected string CurrentLogFileName { get; private set; }
        protected long CurrentLogFilePosition { get; private set; }

        [SetUp]
        public void SetUp()
        {
            CurrentLogFileName = null;
            CurrentLogFilePosition = 0;

            _mockRepository = new MockRepository(MockBehavior.Strict);

            Fixture = new Fixture().Customize(
                new AutoMoqCustomization()
                );

            Options = _mockRepository.Create<ILogShipperOptions>();
            Fixture.Inject(Options.Object);

            LogReaderFactory = _mockRepository.Create<ILogReaderFactory>();
            Fixture.Inject(LogReaderFactory.Object);

            PersistedBookmarkFactory = _mockRepository.Create<IPersistedBookmarkFactory>();
            Fixture.Inject(PersistedBookmarkFactory.Object);

            LogShipperFileManager = _mockRepository.Create<ILogShipperFileManager>();
            Fixture.Inject(LogShipperFileManager.Object);
        }

        protected void GivenSinkOptionsAreSet()
        {
            LogFolder = Path.GetDirectoryName(Path.GetTempPath());
            LogFileNamePrefix = Guid.NewGuid().ToString("N");

            Options.SetupGet(x => x.BufferBaseFilename)
                .Returns(Path.Combine(LogFolder, LogFileNamePrefix));
            Options.SetupGet(x => x.StreamName)
                .Returns(Fixture.Create<string>());
            Options.SetupGet(x => x.BatchPostingLimit)
                .Returns(5);
        }

        protected void GivenLogFilesInDirectory(int files = 5)
        {
            LogFiles = Fixture.CreateMany<string>(files)
                .Select(x => Path.Combine(LogFolder, LogFileNamePrefix + x + ".json"))
                .OrderBy(x => x)
                .ToArray();

            SetUpLogShipperFileManagerGetFiles()
                .Returns(LogFiles);
        }

        protected void GivenFileDeleteSucceeds(string filePath)
        {
            LogShipperFileManager.Setup(x => x.LockAndDeleteFile(filePath));
        }

        protected void GivenPersistedBookmarkIsLocked()
        {
            PersistedBookmarkFactory
                .Setup(x => x.Create(It.IsAny<string>())).Returns((IPersistedBookmark)null);
        }

        protected void GivenPersistedBookmark(string logFileName = null, long position = 0)
        {
            CurrentLogFileName = logFileName;
            CurrentLogFilePosition = position;

            PersistedBookmark = _mockRepository.Create<IPersistedBookmark>();
            PersistedBookmark.Setup(x => x.Dispose());
            PersistedBookmark.SetupGet(x => x.FileName).Returns(() => CurrentLogFileName);
            PersistedBookmark.SetupGet(x => x.Position).Returns(() => CurrentLogFilePosition);
            PersistedBookmark.Setup(x => x.UpdatePosition(It.IsAny<long>()))
                .Callback((long pos) => { CurrentLogFilePosition = pos; });
            PersistedBookmark.Setup(x => x.UpdateFileNameAndPosition(It.IsAny<string>(), It.IsAny<long>()))
                .Callback((string fileName, long pos) =>
                {
                    CurrentLogFileName = fileName;
                    CurrentLogFilePosition = pos;
                });

            PersistedBookmarkFactory
                .Setup(
                    x => x.Create(It.Is<string>(s => s == Options.Object.BufferBaseFilename + ".bookmark"))
                )
                .Returns(PersistedBookmark.Object);
        }

        protected void GivenLogReaderCreateThrows(string fileName, long position)
        {
            LogReaderFactory.Setup(x => x.Create(fileName, position)).Throws<IOException>();
        }

        protected void WhenLogShipperIsCreated()
        {
            _mockRepository.Create<ILogShipperProtectedDelegator>();
            Target = Fixture.Create<LogShipperSUT>();

            Target.LogSendError += TargetOnLogSendError;
        }

        protected void WhenLogShipperIsCalled()
        {
            Target.ShipIt();
        }

        private void TargetOnLogSendError(object sender, LogSendErrorEventArgs logSendErrorEventArgs)
        {
            throw logSendErrorEventArgs.Exception;
        }

        private ISetup<ILogShipperFileManager, string[]> SetUpLogShipperFileManagerGetFiles()
        {
            return LogShipperFileManager
                .Setup(x => x.GetFiles(
                    It.Is<string>(s => s == LogFolder),
                    It.Is<string>(s => s == LogFileNamePrefix + "*.json")
                    )
                );
        }
    }
}
