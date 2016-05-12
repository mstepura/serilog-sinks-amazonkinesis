using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primitives;
using Serilog.Sinks.Amazon.Kinesis.Common;
using Serilog.Sinks.Amazon.Kinesis.Logging;

namespace Serilog.Sinks.Amazon.Kinesis
{
    abstract class HttpLogShipperBase<TRecord, TResponse> : IDisposable
    {
        const long ERROR_SHARING_VIOLATION = 0x20;
        const long ERROR_LOCK_VIOLATION = 0x21;

        private readonly ILog _logger;
        protected ILog Logger => _logger;

        private readonly ILogReaderFactory _logReaderFactory;

        protected readonly int _batchPostingLimit;
        protected readonly string _bookmarkFilename;
        protected readonly string _candidateSearchPath;
        protected readonly string _logFolder;
        readonly TimeSpan _period;
        protected readonly string _streamName;
        readonly Throttle _throttle;

        protected HttpLogShipperBase(
            KinesisSinkStateBase state,
            ILogReaderFactory logReaderFactory)
        {
            _logger = LogProvider.GetLogger(GetType());

            _logReaderFactory = logReaderFactory;

            _period = state.SinkOptions.Period;
            _throttle = new Throttle(OnTick, _period);
            _batchPostingLimit = state.SinkOptions.BatchPostingLimit;
            _streamName = state.SinkOptions.StreamName;
            _bookmarkFilename = Path.GetFullPath(state.SinkOptions.BufferBaseFilename + ".bookmark");
            _logFolder = Path.GetDirectoryName(_bookmarkFilename);
            _candidateSearchPath = Path.GetFileName(state.SinkOptions.BufferBaseFilename) + "*.json";

            Logger.InfoFormat("Candidate search path is {0}", _candidateSearchPath);
            Logger.InfoFormat("Log folder is {0}", _logFolder);
        }

        public void Emit()
        {
            _throttle.ThrottleAction();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
        }

        protected abstract TRecord PrepareRecord(MemoryStream stream);
        protected abstract TResponse SendRecords(List<TRecord> records, out bool successful);
        protected abstract void HandleError(TResponse response, int originalRecordCount);
        public event EventHandler<LogSendErrorEventArgs> LogSendError;

        protected void OnLogSendError(LogSendErrorEventArgs e)
        {
            var handler = LogSendError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        ///     Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">
        ///     If true, called because the object is being disposed; if false,
        ///     the object is being disposed from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _throttle.Dispose();
        }


        private void OnTick()
        {
            try
            {
                // Locking the bookmark ensures that though there may be multiple instances of this
                // class running, only one will ship logs at a time.

                using (var bookmark = PersistedBookmark.Create(_bookmarkFilename))
                {
                    do
                    {
                        long nextLineBeginsAtOffset = bookmark.Position;
                        string currentFilePath = bookmark.FileName;

                        Logger.TraceFormat("Bookmark is currently at offset {0} in '{1}'", nextLineBeginsAtOffset, currentFilePath);

                        var fileSet = GetFileSet();

                        if (currentFilePath == null || !File.Exists(currentFilePath))
                        {
                            nextLineBeginsAtOffset = 0;
                            currentFilePath = fileSet.FirstOrDefault();
                            Logger.InfoFormat("New log file is {0}", currentFilePath);

                            bookmark.UpdateFileNameAndPosition(currentFilePath, nextLineBeginsAtOffset);

                            if (currentFilePath == null)
                            {
                                Logger.InfoFormat("No log file is found. Nothing to do.");
                                break;
                            }
                        }

                        // delete all previous files - we will not read them anyway
                        foreach (var fileToDelete in fileSet.TakeWhile(f => !FileNamesEqual(f, currentFilePath)))
                        {
                            TryLockAndDeleteFile(fileToDelete);
                        }

                        // now we are interested in current file and all after it.
                        fileSet =
                            fileSet.SkipWhile(f => !FileNamesEqual(f, currentFilePath))
                                .ToArray();

                        var initialPosition = nextLineBeginsAtOffset;
                        List<TRecord> records;
                        do
                        {
                            var batch = ReadRecordBatch(currentFilePath, nextLineBeginsAtOffset, _batchPostingLimit);
                            records = batch.Item2;
                            if (records.Count > 0)
                            {
                                bool successful;
                                var response = SendRecords(records, out successful);

                                if (!successful)
                                {
                                    HandleError(response, records.Count);
                                    break;
                                }
                            }
                            nextLineBeginsAtOffset = batch.Item1;
                        } while (records.Count >= _batchPostingLimit);

                        if (initialPosition < nextLineBeginsAtOffset)
                        {
                            Logger.TraceFormat("Advancing bookmark from '{0}' to '{1}'", initialPosition, nextLineBeginsAtOffset);
                            bookmark.UpdatePosition(nextLineBeginsAtOffset);
                        }
                        else
                        {
                            Logger.TraceFormat("Found no records to process");

                            // Only advance the bookmark if no other process has the
                            // current file locked, and its length is as we found it.

                            if (fileSet.Length > 1)
                            {
                                Logger.TraceFormat("BufferedFilesCount: {0}; checking if can advance to the next file", fileSet.Length);
                                var weAreAtEndOfTheFileAndItIsNotLockedByAnotherThread = WeAreAtEndOfTheFileAndItIsNotLockedByAnotherThread(currentFilePath, nextLineBeginsAtOffset);
                                if (weAreAtEndOfTheFileAndItIsNotLockedByAnotherThread)
                                {
                                    Logger.TraceFormat("Advancing bookmark from '{0}' to '{1}'", currentFilePath, fileSet[1]);
                                    bookmark.UpdateFileNameAndPosition(fileSet[1], 0);
                                }
                            }
                            else
                            {
                                Logger.TraceFormat("This is a single log file, and we are in the end of it. Nothing to do.");
                                break;
                            }
                        }
                    } while (true);
                }
            }
            catch (IOException ex)
            {
                long win32ErrorCode = GetWin32ErrorCode(ex);

                if (win32ErrorCode == ERROR_SHARING_VIOLATION || win32ErrorCode == ERROR_LOCK_VIOLATION)
                {
                    Logger.TraceException("Swallowed I/O exception", ex);
                }
                else
                {
                    Logger.ErrorException("Unexpected I/O exception", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Exception while emitting periodic batch", ex);
                OnLogSendError(new LogSendErrorEventArgs(string.Format("Error in shipping logs to '{0}' stream)", _streamName), ex));
            }
        }

        private Tuple<long, List<TRecord>> ReadRecordBatch(string currentFilePath, long initialPosition, int maxRecords)
        {
            var records = new List<TRecord>(maxRecords);
            long positionSent;
            using (var reader = _logReaderFactory.Create(currentFilePath, initialPosition))
            {
                do
                {
                    var stream = reader.ReadLine();
                    if (stream.Length == 0)
                    {
                        break;
                    }
                    records.Add(PrepareRecord(stream));
                } while (records.Count < maxRecords);

                positionSent = reader.Position;
            }

            return Tuple.Create(positionSent, records);
        }

        private bool TryLockAndDeleteFile(string fileToDelete)
        {
            try
            {
                using (var stream = new FileStream(fileToDelete, FileMode.Open, FileAccess.ReadWrite,
                    FileShare.None, 128, FileOptions.DeleteOnClose))
                {
                    Logger.InfoFormat("Opened {0} in exclusive mode, deleting...", fileToDelete);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.WarnException("Exception opening {0} in exclusive mode", ex, fileToDelete);
                return false;
            }
        }

        private long GetWin32ErrorCode(IOException ex)
        {
#if NET40
            long win32ErrorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex) & 0xFFFF;
#else
            long win32ErrorCode = ex.HResult & 0xFFFF;
#endif
            return win32ErrorCode;
        }

        private bool WeAreAtEndOfTheFileAndItIsNotLockedByAnotherThread(string file, long nextLineBeginsAtOffset)
        {
            try
            {
                using (var fileStream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return fileStream.Length <= nextLineBeginsAtOffset;
                }
            }
            catch (IOException ex)
            {
                long win32ErrorCode = GetWin32ErrorCode(ex);

                if (win32ErrorCode == ERROR_SHARING_VIOLATION || win32ErrorCode == ERROR_LOCK_VIOLATION)
                {
                    Logger.TraceException("Swallowed I/O exception while testing locked status of {0}", ex, file);
                }
                else
                {
                    Logger.ErrorException("Unexpected I/O exception while testing locked status of {0}", ex, file);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Unexpected exception while testing locked status of {0}", ex, file);
            }

            return false;
        }

        private string[] GetFileSet()
        {
            var fileSet = Directory.GetFiles(_logFolder, _candidateSearchPath)
                .OrderBy(n => n)
                .ToArray();
            var fileSetDesc = string.Join(";", fileSet);
            Logger.TraceFormat("FileSet contains: {0}", fileSetDesc);
            return fileSet;
        }

        private static bool FileNamesEqual(string fileName1, string fileName2)
        {
            return string.Equals(fileName1, fileName2, StringComparison.OrdinalIgnoreCase);
        }
    }
}