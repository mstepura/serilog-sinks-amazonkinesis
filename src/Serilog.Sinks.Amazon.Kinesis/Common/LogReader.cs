using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    public interface ILogReader : IDisposable
    {
        /// <summary>
        /// Read next line until CR or LF character.
        /// Ignoring leading CR/LF (no empty lines returned),
        /// </summary>
        /// <returns>Stream with line content, or empty stream in case of EOF</returns>
        System.IO.MemoryStream ReadLine();
        long Position { get; }
    }

    public interface ILogReaderFactory
    {
        ILogReader Create(string fileName, long position);
    }

    class LogReaderFactory : ILogReaderFactory
    {
        public ILogReader Create(string fileName, long position)
        {
            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 128, FileOptions.SequentialScan);
            stream.Seek(position, SeekOrigin.Begin);
            return new LogReader(stream);
        }
    }


    sealed class LogReader : ILogReader
    {
        private readonly System.IO.Stream _logStream;

        internal LogReader(System.IO.Stream logStream)
        {
            _logStream = logStream;
        }

        public System.IO.MemoryStream ReadLine()
        {
            // check and skip BOM in the beginning of file
            if (_logStream.Position == 0)
            {
                var bom = Encoding.UTF8.GetPreamble();
                var bomBuffer = new byte[bom.Length];
                if (bomBuffer.Length != _logStream.Read(bomBuffer, 0, bomBuffer.Length)
                    || !bomBuffer.SequenceEqual(bom))
                {
                    _logStream.Position = 0;
                }
            }

            var result = new MemoryStream(256);
            int thisByte;
            while (0 <= (thisByte = _logStream.ReadByte()))
            {
                if (thisByte < 0)
                {
                    break; // EOF
                }
                if (thisByte == 0x10 || thisByte == 0x13)
                {
                    if (result.Length > 0)
                    {
                        break; // EOL found
                    }
                    continue; // Ignore CR/LF in the beginning of the line
                }

                result.WriteByte((byte)thisByte);
            }

            return result;
        }

        public long Position { get { return _logStream.Position; } }

        public void Dispose()
        {
            _logStream.Dispose();
        }
    }

}
