using NLog.Targets;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OperationLogManager.libs
{
    [Target("ObservableCollectionTarget")]
    public sealed class ObservableCollectionTarget : TargetWithLayout
    {
        public ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();

        protected override void Write(LogEventInfo logEvent)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(new LogEntry
                {
                    Time = logEvent.TimeStamp,
                    Level = logEvent.Level,
                    Message = logEvent.Message,
                    LoggerName = logEvent.LoggerName,
                    Exception = logEvent.Exception == null ?  "null": logEvent.Exception.Message,
                });
            });
        }
    }

    public class LogEntry
    {
        public DateTime Time { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string LoggerName { get; set; }
        public string Exception { get; set; }
    }

    // 扩展方法，用于判断日志级别
    public static class LogLevelExtensions
    {
        public static bool IsInfoOrWarning(this LogLevel level)
        {
            return level == LogLevel.Info || level == LogLevel.Warn;
        }

        public static bool IsErrorOrFatal(this LogLevel level)
        {
            return level == LogLevel.Error || level == LogLevel.Fatal;
        }
    }

}
