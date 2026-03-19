using NLog;
using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuControl.ViewModels
{
    public class MenuLoginViewModel : BindableBase
    {
        public MenuLoginViewModel(IContainerProvider  provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
        }

        IContainerProvider containerProvider;
        IEventAggregator eventAggregator;

        private readonly string HelpHandbook_path = Path.Combine(Directory.GetCurrentDirectory(), "user_guide");

        private DelegateCommand _openHelpHandbookCommand;
        public DelegateCommand OpenHelpHandbookCommand => _openHelpHandbookCommand ??
            (_openHelpHandbookCommand = new DelegateCommand(() =>
            {
                string file_path = FindHelpHandbookFile();
                try
                {
                    Process.Start(new ProcessStartInfo(file_path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("文件读取异常", ex);
                }
            }));

        private DelegateCommand<object> _changeLogAndDebugModeCommand;
        public DelegateCommand<object> ChangeLogAndDebugModeCommand => _changeLogAndDebugModeCommand ??
            (_changeLogAndDebugModeCommand = new DelegateCommand<object>((b) =>
            {
                bool mode = b as bool? ?? false;
                eventAggregator.GetEvent<ChangeLogAndDebugPageEvent>().Publish(mode);
            }));

        private string FindHelpHandbookFile()
        {
            if (string.IsNullOrEmpty(HelpHandbook_path) || !Directory.Exists(HelpHandbook_path))
            {
                throw new DirectoryNotFoundException($"指定的路径不存在: {HelpHandbook_path}");
            }

            // 定义可能的文件扩展名列表
            string[] possibleExtensions = { ".pdf", ".chm", ".html", ".htm", ".txt", ".docx", ".doc" };

            // 优先查找无扩展名的文件
            string fullPathWithoutExtension = Path.Combine(HelpHandbook_path, "HelpHandbook");
            if (File.Exists(fullPathWithoutExtension))
            {
                return fullPathWithoutExtension;
            }

            // 查找带常见扩展名的文件
            foreach (string extension in possibleExtensions)
            {
                string fullPath = Path.Combine(HelpHandbook_path, "HelpHandbook" + extension);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // 尝试查找目录下所有包含"HelpHandbook"的文件
            string[] matchingFiles = Directory.GetFiles(HelpHandbook_path, "*HelpHandbook*.*");
            if (matchingFiles.Length > 0)
            {
                return matchingFiles[0]; // 返回找到的第一个文件
            }

            return null; // 未找到匹配文件
        }
    }
}
