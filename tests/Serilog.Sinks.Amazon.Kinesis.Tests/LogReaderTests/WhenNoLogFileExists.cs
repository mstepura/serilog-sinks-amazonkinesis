using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogReaderTests
{
    class WhenNoLogFileExists : LogReaderTestBase
    {
        protected override void Given()
        {
            GivenLogFileDoesNotExist();
            GivenInitialPosition(0);
        }

        [Test]
        public void ThenExceptionIsThrown()
        {
            Should.Throw<IOException>(
                () => WhenLogReaderIsCreated()
                );
        }
    }
}
