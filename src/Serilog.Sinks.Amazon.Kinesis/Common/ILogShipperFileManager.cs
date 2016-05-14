namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    interface ILogShipperFileManager
    {
        bool FileExists(string filePath);
        long GetFileLengthExclusiveAccess(string filePath);
        string[] GetFiles(string path, string searchPattern);
        void LockAndDeleteFile(string filePath);
    }
}
