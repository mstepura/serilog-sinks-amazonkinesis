using System.IO;

namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    class LogReaderFactory : ILogReaderFactory
    {
        public ILogReader Create(string fileName, long position)
        {
            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 128, FileOptions.SequentialScan);
            stream.Seek(position, SeekOrigin.Begin);
            return new LogReader(stream);
        }
    }
}