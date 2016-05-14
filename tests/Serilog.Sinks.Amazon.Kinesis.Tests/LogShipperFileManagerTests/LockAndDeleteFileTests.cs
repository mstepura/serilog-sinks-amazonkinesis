using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogShipperFileManagerTests
{
    [TestFixture]
    class LockAndDeleteFileTests : FileTestBase
    {
        [Test]
        public void WhenFileDoesNotExist_ThenIOException()
        {
            Should.Throw<IOException>(
                () => Target.LockAndDeleteFile(FileName)
                );
        }

        [TestCase(FileAccess.Read, FileShare.Read)]
        [TestCase(FileAccess.Write, FileShare.ReadWrite)]
        public void WhenFileIsOpened_ThenIOException(
            FileAccess fileAccess,
            FileShare fileShare
            )
        {
            File.WriteAllBytes(FileName, new byte[42]);
            using (File.Open(FileName, FileMode.OpenOrCreate, fileAccess, fileShare))
            {
                Should.Throw<IOException>(
                    () => Target.LockAndDeleteFile(FileName)
                );
            }
        }

        [Test(Description = "Special case for files opened with share delete option.")]
        [TestCase(FileAccess.Read)]
        [TestCase(FileAccess.Write)]
        public void WhenFileIsOpenedWithDeleteShare_ThenIOException(
            FileAccess fileAccess
        )
        {
            File.WriteAllBytes(FileName, new byte[42]);
            using (File.Open(FileName, FileMode.OpenOrCreate, fileAccess, FileShare.Delete))
            {
                Should.Throw<IOException>(
                    () => Target.LockAndDeleteFile(FileName)
                );
            }
        }

        [Test]
        public void WhenFileIsNotOpened_ThenDeleteSucceeds()
        {
            File.WriteAllBytes(FileName, new byte[42]);
            Target.LockAndDeleteFile(FileName);

            File.Exists(FileName).ShouldBeFalse();
        }
    }
}
