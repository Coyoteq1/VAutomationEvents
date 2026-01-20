using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace VAuto.Core
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    public class LogSettings
    {
        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
        public bool ConsoleLogging { get; set; } = true;
        public bool FileLogging { get; set; } = true;
        public bool AsyncLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "Logs";
        public string LogFileName { get; set; } = "VAuto.log";
        public long MaxLogSizeMB { get; set; } = 50;
        public int MaxLogFiles { get; set; } = 10;
        public HashSet<string> EnabledServices { get; set; } = new HashSet<string>();
        public string Environment { get; set; } = "Production";
    }

    public static class VLoggerCore
    {
        private static ManualLogSource _bepInExLogger;
        private static LogSettings _settings = new LogSettings();
        private static readonly object _fileLock = new object();
        private static string _currentLogFile;
        private static StreamWriter _fileWriter;
        private static readonly Queue<LogEntry> _asyncQueue = new Queue<LogEntry>();
        private static readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(0);
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static Task _asyncProcessorTask;

        private class LogEntry
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public string Service { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
            public Exception Exception { get; set; }
            public DateTime Timestamp { get; set; }

            public LogEntry(LogLevel level, string message, string service = null,
                          Dictionary<string, object> metadata = null, Exception ex = null)
            {
                Level = level;
                Message = message;
                Service = service;
                Metadata = metadata ?? new Dictionary<string, object>();
                Exception = ex;
                Timestamp = DateTime.UtcNow;
            }
        }

        public static void Initialize(ManualLogSource logger, LogSettings settings = null)
        {
            _bepInExLogger = logger;
            if (settings != null)
            {
                _settings = settings;
            }

            EnsureLogDirectory();
            InitializeFileWriter();

            if (_settings.AsyncLogging)
            {
                StartAsyncProcessor();
            }

            Structured("Logger initialized", "Logger", LogLevel.Info, new Dictionary<string, object>
            {
                ["Environment"] = _settings.Environment,
                ["MinLevel"] = _settings.MinimumLevel.ToString(),
                ["FileLogging"] = _settings.FileLogging,
                ["AsyncLogging"] = _settings.AsyncLogging
            });
        }

        public static void UpdateSettings(LogSettings newSettings)
        {
            var oldSettings = _settings;
            _settings = newSettings;

            if (oldSettings.AsyncLogging != newSettings.AsyncLogging)
            {
                if (newSettings.AsyncLogging)
                {
                    StartAsyncProcessor();
                }
                else
                {
                    StopAsyncProcessor();
                }
            }

            if (oldSettings.FileLogging != newSettings.FileLogging ||
                oldSettings.LogDirectory != newSettings.LogDirectory ||
                oldSettings.LogFileName != newSettings.LogFileName)
            {
                CloseFileWriter();
                if (newSettings.FileLogging)
                {
                    EnsureLogDirectory();
                    InitializeFileWriter();
                }
            }

            Structured("Logger settings updated", "Logger", LogLevel.Info, new Dictionary<string, object>
            {
                ["MinLevel"] = newSettings.MinimumLevel.ToString(),
                ["FileLogging"] = newSettings.FileLogging,
                ["AsyncLogging"] = newSettings.AsyncLogging
            });
        }

        private static void EnsureLogDirectory()
        {
            if (!Directory.Exists(_settings.LogDirectory))
            {
                Directory.CreateDirectory(_settings.LogDirectory);
            }
        }

        private static void InitializeFileWriter()
        {
            if (!_settings.FileLogging) return;

            _currentLogFile = Path.Combine(_settings.LogDirectory, _settings.LogFileName);
            RotateLogIfNeeded();

            _fileWriter = new StreamWriter(_currentLogFile, true) { AutoFlush = true };
        }

        private static void RotateLogIfNeeded()
        {
            if (!File.Exists(_currentLogFile)) return;

            var fileInfo = new FileInfo(_currentLogFile);
            if (fileInfo.Length >= _settings.MaxLogSizeMB * 1024 * 1024)
            {
                CloseFileWriter();

                // Rotate existing logs
                for (int i = _settings.MaxLogFiles - 1; i >= 1; i--)
                {
                    var oldFile = Path.Combine(_settings.LogDirectory, $"{Path.GetFileNameWithoutExtension(_settings.LogFileName)}.{i}.log");
                    var newFile = Path.Combine(_settings.LogDirectory, $"{Path.GetFileNameWithoutExtension(_settings.LogFileName)}.{i + 1}.log");

                    if (File.Exists(oldFile))
                    {
                        if (i == _settings.MaxLogFiles - 1)
                        {
                            File.Delete(oldFile);
                        }
                        else
                        {
                            File.Move(oldFile, newFile);
                        }
                    }
                }

                // Move current log to .1
                var backupFile = Path.Combine(_settings.LogDirectory, $"{Path.GetFileNameWithoutExtension(_settings.LogFileName)}.1.log");
                File.Move(_currentLogFile, backupFile);

                InitializeFileWriter();
            }
        }

        private static void CloseFileWriter()
        {
            _fileWriter?.Dispose();
            _fileWriter = null;
        }

        private static void StartAsyncProcessor()
        {
            if (_asyncProcessorTask != null) return;

            _asyncProcessorTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await _queueSemaphore.WaitAsync(_cancellationTokenSource.Token);

                        LogEntry entry;
                        lock (_asyncQueue)
                        {
                            if (_asyncQueue.Count == 0) continue;
                            entry = _asyncQueue.Dequeue();
                        }

                        ProcessLogEntry(entry);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Fallback to sync logging for async processor errors
                        _bepInExLogger?.LogError($"Async logging error: {ex.Message}");
                    }
                }
            });
        }

        private static void StopAsyncProcessor()
        {
            _cancellationTokenSource.Cancel();
            _asyncProcessorTask?.Wait();
            _asyncProcessorTask = null;

            // Process remaining entries synchronously
            lock (_asyncQueue)
            {
                while (_asyncQueue.Count > 0)
                {
                    var entry = _asyncQueue.Dequeue();
                    ProcessLogEntry(entry);
                }
            }
        }

        public static void Debug(string message, string context = null) => Log(LogLevel.Debug, message, context);
        public static void Info(string message, string context = null) => Log(LogLevel.Info, message, context);
        public static void Warn(string message, string context = null) => Log(LogLevel.Warn, message, context);
        public static void Error(string message, Exception ex = null, string context = null) => Log(LogLevel.Error, message, context, ex);

        private static void Log(LogLevel level, string message, string context = null, Exception ex = null)
        {
            if (level < _settings.MinimumLevel) return;

            var entry = new LogEntry(level, message, context, null, ex);

            if (_settings.AsyncLogging)
            {
                EnqueueAsync(entry);
            }
            else
            {
                ProcessLogEntry(entry);
            }
        }

        private static void EnqueueAsync(LogEntry entry)
        {
            lock (_asyncQueue)
            {
                _asyncQueue.Enqueue(entry);
            }
            _queueSemaphore.Release();
        }

        private static void ProcessLogEntry(LogEntry entry)
        {
            // Check service filtering
            if (!string.IsNullOrEmpty(entry.Service) && _settings.EnabledServices.Count > 0 &&
                !_settings.EnabledServices.Contains(entry.Service))
            {
                return;
            }

            var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var servicePart = string.IsNullOrEmpty(entry.Service) ? "" : $"[{entry.Service}] ";
            var metadataPart = entry.Metadata != null && entry.Metadata.Count > 0 ?
                $" | {string.Join(", ", entry.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}" : "";

            var formattedMessage = $"{timestamp} | {entry.Level.ToString().ToUpper()} | {_settings.Environment} | {servicePart}{entry.Message}{metadataPart}";

            if (entry.Exception != null)
            {
                formattedMessage += $"\nException: {entry.Exception.Message}\nStack Trace: {entry.Exception.StackTrace}";
            }

            // Console logging
            if (_settings.ConsoleLogging)
            {
                switch (entry.Level)
                {
                    case LogLevel.Debug:
                        _bepInExLogger?.LogDebug(formattedMessage);
                        break;
                    case LogLevel.Info:
                        _bepInExLogger?.LogInfo(formattedMessage);
                        break;
                    case LogLevel.Warn:
                        _bepInExLogger?.LogWarning(formattedMessage);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        _bepInExLogger?.LogError(formattedMessage);
                        break;
                }
            }

            // File logging
            if (_settings.FileLogging && _fileWriter != null)
            {
                lock (_fileLock)
                {
                    RotateLogIfNeeded();
                    _fileWriter.WriteLine(formattedMessage);
                }
            }
        }

        /// <summary>
        /// VAMP-style structured logging with service context
        /// </summary>
        public static void Structured(string message, string service = null, LogLevel level = LogLevel.Info,
            Dictionary<string, object> metadata = null, Exception ex = null)
        {
            if (level < _settings.MinimumLevel) return;

            var entry = new LogEntry(level, message, service, metadata, ex);

            if (_settings.AsyncLogging)
            {
                EnqueueAsync(entry);
            }
            else
            {
                ProcessLogEntry(entry);
            }
        }

        /// <summary>
        /// VAMP-style performance logging
        /// </summary>
        public static IDisposable VAMPPerformanceTimer(string operation, string service = null)
        {
            return new VAMPPerformanceTimerInstance(operation, service);
        }

        private class VAMPPerformanceTimerInstance : IDisposable
        {
            private readonly string _operation;
            private readonly string _service;
            private readonly Stopwatch _stopwatch;

            public VAMPPerformanceTimerInstance(string operation, string service)
            {
                _operation = operation;
                _service = service;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                Structured($"Operation completed: {_operation}", _service, LogLevel.Info,
                    new Dictionary<string, object> { ["Duration"] = $"{_stopwatch.ElapsedMilliseconds}ms" });
            }
        }

        public static void Shutdown()
        {
            Structured("Logger shutting down", "Logger", LogLevel.Info);

            StopAsyncProcessor();
            CloseFileWriter();
        }

        public static LogSettings GetCurrentSettings() => _settings;
    }
}