using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Prism.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using SharedResource.events;
using SharedResource.libs;
using Prism.Events;
using Prism.Ioc;

namespace Machine.ViewModels
{
  public  class MachineDebugViewModel : BindableBase
    {
        //public event PropertyChangedEventHandler PropertyChanged;

        //protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        //private string _mainText = string.Empty;
        //public string MainText
        //{
        //    get => _mainText;
        //    set => SetProperty(ref _mainText, value);
        //}

        private string _mainText;
        public string MainText
        {
            get => _mainText;
            set
            {
                if (_mainText != value)
                {
                    _mainText = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _inputText = string.Empty;
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        private DelegateCommand _uploadCommand;
        public DelegateCommand UploadCommand => _uploadCommand ??
            (_uploadCommand = new DelegateCommand(ExecuteUpload)); 

        private DelegateCommand _executeCommand;
        public DelegateCommand ExecuteCommand => _executeCommand ??
            (_executeCommand = new DelegateCommand(async () => await ExecuteExecuteAsync()));

        //public ICommand UploadCommand { get; }
        //public ICommand ExecuteCommand { get; }
        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        public MachineDebugViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
           
        }

        private bool CanExecuteCommand(object parameter)
        {
            return true;
        }
        private bool ContainsChinese(string text)
        {
            // 中文 Unicode 范围：\u4e00-\u9fa5（常用汉字）
            return Regex.IsMatch(text, @"[\u4e00-\u9fa5]");
        }
        private bool ContainsChineseFull1(string text)
        {
            foreach (var ch in text)
            {
                if (ch >= 0x4e00 && ch <= 0x9fff) return true; // CJK 汉字
                if (ch >= 0x3400 && ch <= 0x4dbf) return true; // 扩展 A
                if (ch >= 0x20000 && ch <= 0x2a6df) return true; // 扩展 B
                if (ch >= 0x2f800 && ch <= 0x2fa1f) return true; // 扩展 E~F
            }
            return false;
        }

        private bool ContainsChineseFull(string text)
        {
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                // 去掉注释部分，只检查 '!' 前面的内容
                string codePart = line.Split('!')[0];

                foreach (var ch in codePart)
                {
                    // CJK 汉字
                    if (ch >= 0x4e00 && ch <= 0x9fff) return true;

                    // 扩展 A
                    if (ch >= 0x3400 && ch <= 0x4dbf) return true;

                    // 扩展 B
                    if (ch >= 0x20000 && ch <= 0x2a6df) return true;

                    // 扩展 E~F
                    if (ch >= 0x2f800 && ch <= 0x2fa1f) return true;
                }
            }

            return false;
        }
        private void ExecuteUpload()
        {
            try
            {
                var result = MessageBox.Show("是否上传？上传的文件内容会覆盖上面文本框内容？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {

                    return;

                }
                 var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    InputText = dialog.FileName;

                    string content  = System.IO.File.ReadAllText(dialog.FileName);
                    if (ContainsChineseFull(content))
                    {
                        MessageBox.Show("文件内容包含中文，请删除后再上传。", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                  MainText = "";
                  MainText =content;
                    MessageBox.Show("File uploaded successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
     
        private async Task ExecuteExecuteAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MainText))
                {
                    MessageBox.Show("Please enter some text to execute.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Process text (remove comments, validate, etc.)
                //string processedText = ProcessText(MainText);
                var args = new RunRequestEventArgs { Data = MainText };
                eventAggregator.GetEvent<RunRequestEvent>().Publish(args);

                // 等待订阅者完成
                var result = await args.CompletionSource.Task;
                if(result)
                {
                    MessageBox.Show("执行完成！");
                }
                //eventAggregator.GetEvent<Cmd_StartProcessPrepareEvent>().Publish(new(list, globalCraftPara.TargetFile_path));
                //MessageBox.Show($"Execution completed!\n\nProcessed text length: {processedText.Length} characters",
                //    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ProcessText(string text)
        {
            // Remove lines starting with '!' (comments)
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var processedLines = new System.Collections.Generic.List<string>();

            foreach (var line in lines)
            {
                string trimmedLine = line.TrimStart();
                if (!trimmedLine.StartsWith("!"))
                {
                    processedLines.Add(line);
                }
            }

            return string.Join(Environment.NewLine, processedLines);
        }
    
}
}
