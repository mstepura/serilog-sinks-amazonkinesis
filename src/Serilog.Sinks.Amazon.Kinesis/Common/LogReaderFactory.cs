using System.IO;

namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    class LogReaderFactory : ILogReaderFactory
    {
        public ILogReader Create(string fileName, long position)
        {
            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 128, FileOptions.SequentialScan);
            var length = stream.Length;
            if (position > length)
            {
                position = length;
            }
            stream.Seek(position, SeekOrigin.Begin);
            return new LogReader(stream);
        }
    }
}