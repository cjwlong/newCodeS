using NLog.Config;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows;
using NLog.Targets;
using System.Diagnostics;
using System.IO;

namespace OperationLogManager.libs
{
    public sealed class LoggingService : IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<LoggingService> _lazy = new Lazy<LoggingService>(() => new LoggingService());
        private readonly ObservableCollectionTarget _observableTarget;
        private bool _disposedValue;

        public static LoggingService Instance => _lazy.Value;
        public ObservableCollection<LogEntry> AllLogs => _observableTarget.LogEntries;
        public ObservableCollection<LogEntry> OperationLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> ExceptionLogs { get; } = new ObservableCollection<LogEntry>();

        // 私有构造函数
        private LoggingService()
        {
            _observableTarget = new ObservableCollectionTarget();

            // **添加目录创建逻辑**
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            ConfigureNLog();

            // 监听日志分类
            AllLogs.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (LogEntry log in e.NewItems)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (log.Level.IsInfoOrWarning())
                                OperationLogs.Add(log);
                            else if (log.Level.IsErrorOrFatal())
                                ExceptionLogs.Add(log);
                        });
                    }
                }
            };
        }

        private void ConfigureNLog()
        {
            var config = new LoggingConfiguration();
            config.AddTarget("observable", _observableTarget);

            var datatime = DateTime.Now;

            // **修改文件目标，添加日期动态文件名**
            var fileTarget = new FileTarget("file")
            {
                // 使用${basedir}获取程序基目录，${date:format=yyyyMMdd}生成当天日期（格式可自定义）
                FileName = "${basedir}/logs/${date:format=yyyyMMdd}.log",
                Layout = "${longdate} ${level:uppercase=true} ${message} ${exception:format=tostring}",

                // 自动创建日志目录（重要！避免路径不存在错误）
                CreateDirs = true,

                // 其他可选配置（如归档、保留天数等）
                //ArchiveFileName = "${basedir}/logs/archive/${date:format=yyyyMMdd}_${level:uppercase=true}.log",
                //ArchiveNumbering = ArchiveNumberingMode.Date,
                //MaxArchiveDays = 120
            };
            config.AddTarget("file", fileTarget);

            // 配置日志规则
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, _observableTarget));
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));

            LogManager.Configuration = config;
        }

        // 日志方法
        public void DIYLog(string message, string level, Exception ex = null)
        {
            switch (level)
            {
                case "Info":
                    LogInfo(message);
                    break;
                case "Warn":
                    LogWarning(message);
                    break;
                case "Error":
                    LogError(message, ex);
                    break;
                default:
                    break;
            }
        }
        public void LogInfo(string message) => _logger.Info(message);
        public void LogWarning(string message) => _logger.Warn(message);
        public void LogError(string message, Exception ex = null) => _logger.Error(ex, message);

        // Dispose 方法
        public void Dispose()
        {
            if (!_disposedValue)
            {
                LogManager.Shutdown();
                _disposedValue = true;
            }
        }
    }
}
