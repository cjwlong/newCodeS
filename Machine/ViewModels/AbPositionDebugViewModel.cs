using OperationLogManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using SharedResource.libs;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Ookii.Dialogs.Wpf;
using Prism.Services.Dialogs;
using System.Windows.Markup;
using System.Windows.Automation;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Machine.Interfaces;
using Microsoft.Win32;
using static Community.CsharpSqlite.Sqlite3;
using Machine.Models;

namespace Machine.ViewModels
{
   public class AbPositionDebugViewModel : BindableBase
    {
    
         public AbPositionDebugViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
   

            machConfig = containerProvider.Resolve<MachineConfigManager>();
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            globalCraftPara = containerProvider.Resolve<GlobalCraftPara>();

            FindFileCommand = new DelegateCommand(FindFileAsync);
            SaveCommand = new DelegateCommand<string>(SaveConfig);

  
            DeleteCommand = new DelegateCommand<string>(DeleteConfig);
        
            MoveAxisCommand = new DelegateCommand<string>((r) =>
            {
                if (IsReadonly)
                {
                    string axis = null;
                    double pos = 0;
                    //移动轴
                    switch (r.ToString())
                    {
                        case "X":
                            axis = "X";
                            pos = CurrentPosition.XPresetPlace;
                            break;
                        case "Y":
                            axis = "Y";
                            pos = CurrentPosition.YPresetPlace;
                            break;
                        case "Z":
                            axis = "Z";
                            pos = CurrentPosition.ZPresetPlace;
                            break;
                        case "A":
                            axis = "A";
                            pos = CurrentPosition.APresetPlace;
                            break;
                        case "B":
                            axis = "B";
                            pos = CurrentPosition.BPresetPlace;
                            break;
                        default:
                            break;
                    }
                    eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(axis, pos, true));
                }
            });

            AmendCommand = new DelegateCommand(() =>
            {
                if (IsReadonly)
                {
                    IsReadonly = false;
                    Isamend = true;
                }
            });

            CreatCommand = new DelegateCommand(() =>
            {
                CurrentPosition = machConfig.CreatePositionConfig();
                IsReadonly = false;
                Isamend = false;
            });

            InputCommand = new DelegateCommand(async () =>
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "请选择配置文件";
                dialog.Multiselect = false;
                dialog.Filter = "配置文件（*.json）|*.json";

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    try
                    {
                        string filepath = dialog.FileName;
                        string filename = Path.GetFileName(filepath);
                        if (File.Exists(preinstallPara_path + $"\\{filename}"))
                        {
                            MessageBox.Show("文件名重复或文件已存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var paramFile = await machConfig.LoadConfigAsync<PositionModel>(filepath);
                        CurrentPosition = paramFile;
                        if (paramFile.Name == null)
                        {
                            MessageBox.Show("错误：无法识别的配置文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            LoggingService.Instance.LogError("导入文件失败", new Exception("无法识别的配置文件"));
                            return;
                        }
                        CurrentPosition.Name = Path.GetFileNameWithoutExtension(filepath);
                        await machConfig.SaveConfigAsync(CurrentPosition, Path.Combine(preinstallPara_path, $"{filename}"));

                        LoadConfigFiles();
                        MessageBox.Show("导入成功");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("导入文件异常", ex);
                    }
                }
            });

            OutputCommand = new DelegateCommand<string>(async (fileName) =>
            {
                if (string.IsNullOrEmpty(fileName))
                    return;

                try
                {
                    if (!ConfigFiles.Contains(fileName))
                    {
                        return;
                    }
                    fileName += ".json";

                   
                    var preinstall_filePath = Path.Combine(preinstallPara_path, fileName);
                    string outputPath = "";

                    if (File.Exists(preinstall_filePath))
                    {
                        var dialog = new VistaFolderBrowserDialog
                        {
                            Description = "请选择保存路径",
                            UseDescriptionForTitle = true
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            outputPath = dialog.SelectedPath;

                            await Task.Run(() =>
                            {
                                string TarOutputFile = outputPath + $"\\{fileName}";
                                if (File.Exists(TarOutputFile)) File.Delete(TarOutputFile);
                                File.Copy(preinstall_filePath, TarOutputFile);
                            });

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"导出文件：{fileName}成功！");
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("导出文件异常", ex);
                }
            });

            Initialize();
        }

        private readonly IContainerProvider containerProvider;
    
        private MachineConfigManager machConfig;
        private readonly IEventAggregator eventAggregator;

        public GlobalCraftPara globalCraftPara { get; set; }
        private readonly string preinstallPara_path = Path.Combine(ConfigStore.StoreDir, "positionAbsolute");

        private string _selectedConfigFile;
        private string _searchText;
        //private ConfigModel _currentConfig;
        private PositionModel _currentPosition;
        private bool _isReadonly = true;

        private bool _isamend = false;
        public bool Isamend
        {
            get { return _isamend; }
            set { SetProperty(ref _isamend, value); }
        }

        private ObservableCollection<string> _configFiles = new ObservableCollection<string>();
        public ObservableCollection<string> ConfigFiles
        {
            get => _configFiles;
            set => SetProperty(ref _configFiles, value);
        }

        private ObservableCollection<string> _filteredConfigFiles = new ObservableCollection<string>();
        public ObservableCollection<string> FilteredConfigFiles
        {
            get => _filteredConfigFiles;
            set => SetProperty(ref _filteredConfigFiles, value);
        }

        public string SelectedConfigFile
        {
            get { return _selectedConfigFile; }
            set
            {
                if (SetProperty(ref _selectedConfigFile, value) && !string.IsNullOrEmpty(value))
                {
                    LoadConfig(value);
                }
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { SetProperty(ref _searchText, value); }
        }


        public PositionModel CurrentPosition
        {
            get { return _currentPosition; }
            set => SetProperty(ref _currentPosition, value);
        }

        public bool IsReadonly
        {
            get { return _isReadonly; }
            set { SetProperty(ref _isReadonly, value); }
        }

        public DelegateCommand FindFileCommand { get; private set; }
        public DelegateCommand<string> SaveCommand { get; private set; }
        public DelegateCommand AmendCommand { get; private set; }
        public DelegateCommand CreatCommand { get; private set; }
        public DelegateCommand ValidateCommand { get; private set; }
        public DelegateCommand<string> DeleteCommand { get; private set; }
        public DelegateCommand<string> ApplyCommand { get; private set; }
        public DelegateCommand<string> MoveAxisCommand { get; private set; }
        public DelegateCommand InputCommand { get; private set; }
        public DelegateCommand<string> OutputCommand { get; private set; }

        private void Initialize()
        {
            // 加载配置文件列表
            LoadConfigFiles();

            // 创建默认配置
            //CurrentConfig = configService.CreateDefaultConfig();
            CurrentPosition = machConfig.CreatePositionConfig();

            ResetFilter();
        }

        private void ResetFilter()
        {
            FilteredConfigFiles.Clear();
            foreach (var file in ConfigFiles)
            {
                FilteredConfigFiles.Add(file);
            }
        }

        private async void LoadConfigFiles()
        {
            try
            {
                var preinstall_files = await machConfig.GetConfigFilesAsync(preinstallPara_path);


                ConfigFiles.Clear();
                foreach (var file in preinstall_files)
                {
                    ConfigFiles.Add(file);
                }

                ResetFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置文件列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadConfig(string fileName)
        {
            fileName += ".json";
    
            var preinstall_filePath = Path.Combine(preinstallPara_path, fileName);

            try
            {

                if (File.Exists(preinstall_filePath))
                {
                    CurrentPosition = await machConfig.LoadConfigAsync<PositionModel>(preinstall_filePath);
                }
            }
            catch (Exception)
            {
                //MessageBox.Show($"加载配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsReadonly = true;
                Isamend = false;
            }
        }

        private async void FindFileAsync()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                ResetFilter();
                return;
            }

            try
            {
                // 清空当前筛选结果
                FilteredConfigFiles.Clear();

                // 模拟异步搜索
                await Task.Run(() =>
                {
                    var results = ConfigFiles.Where(f => f.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

                    // 在UI线程更新结果
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var file in results)
                        {
                            FilteredConfigFiles.Add(file);
                        }

                        if (FilteredConfigFiles.Count == 1)
                        {
                            SelectedConfigFile = FilteredConfigFiles[0];
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("搜索文件时出错", ex);
            }
        }

        private async void SaveConfig(string file_param)
        {
            try
            {


                if (!Isamend)
                {
                    if (ConfigFiles.Contains(CurrentPosition.Name))
                    {
                        MessageBox.Show("文件名重复，请更改", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                await machConfig.SaveConfigAsync(CurrentPosition, Path.Combine(preinstallPara_path, $"{CurrentPosition.Name}.json"));


                MessageBox.Show($"文件已保存", "信息", MessageBoxButton.OK, MessageBoxImage.Information);

                IsReadonly = true;

                // 重新加载配置文件列表
                LoadConfigFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsReadonly = true;
                Isamend = false;
            }
        }

        private void CreateNewConfig()
        {
            CurrentPosition = machConfig.CreatePositionConfig();
            IsReadonly = false;
            Isamend = false;
        }

     

        private async void DeleteConfig(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            if (MessageBox.Show($"确定要删除文件 {fileName} ？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    if (ConfigFiles.Contains(fileName))
                    {
                        ConfigFiles.Remove(fileName);
                    }

                    if (FilteredConfigFiles.Contains(fileName))
                    {
                        FilteredConfigFiles.Remove(fileName);
                    }

                    fileName += ".json";

                    var preinstall_filePath = Path.Combine(preinstallPara_path, fileName);

                    
                    if (File.Exists(preinstall_filePath))
                        File.Delete(preinstall_filePath);

                    LoadConfigFiles();

                    if (SelectedConfigFile == null)
                    {
                        CreateNewConfig();
                        SelectedConfigFile = null;
                    }

                    LoggingService.Instance.LogInfo($"文件 {fileName} 已删除");
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"删除配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoggingService.Instance.LogError($"删除 {fileName} 文件失败", ex);
                }
                finally
                {
                    IsReadonly = true;
                    Isamend = false;
                }
            }
        }

    


    }
}

