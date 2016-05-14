using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogShipperFileManagerTests
{
    class FileExistsTests : FileTestBase
    {
        [Test]
        public void WhenFileDoesNotExist_ThenFalse()
        {
            Target.FileExists(FileName).ShouldBeFalse();
        }

        [Test]
        public void WhenFileExists_ThenTrue()
        {
            File.WriteAllBytes(FileName, new byte[0]);
            Target.FileExists(FileName).ShouldBeTrue();
        }
    }
}
