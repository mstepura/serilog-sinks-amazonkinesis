using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogShipperFileManagerTests
{
    class GetFileLengthExclusiveAccessTests : FileTestBase
    {
        [Test]
        public void WhenFileDoesNotExist_ThenIOException()
        {
            Should.Throw<IOException>(
                () => Target.GetFileLengthExclusiveAccess(FileName)
                );
        }

        [TestCase(100L)]
        [TestCase(42L)]
        public void WhenFileExistsAndNotLocked_ThenCorrectLength(long length)
        {
            File.WriteAllBytes(FileName, new byte[length]);
            Target.GetFileLengthExclusiveAccess(FileName).ShouldBe(length);
        }

        [Test]
        public void WhenFileExistsAndOpenedForWriting_ThenIOException()
        {
            File.WriteAllBytes(FileName, new byte[42]);
            using (File.Open(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                Should.Throw<IOException>(
                    () => Target.GetFileLengthExclusiveAccess(FileName)
                    );
            }
        }

        [TestCase(100L)]
        [TestCase(42L)]
        public void WhenFileExistsAndOpenedForReading_ThenCorrectLength(long length)
        {
            File.WriteAllBytes(FileName, new byte[length]);
            using (File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Target.GetFileLengthExclusiveAccess(FileName).ShouldBe(length);
            }
        }
    }
}
