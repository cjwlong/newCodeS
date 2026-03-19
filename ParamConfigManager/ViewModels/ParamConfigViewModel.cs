using ParamConfigManager.interfaces;
using ParamConfigManager.libs;
using ParamConfigManager.tools;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.IO;
using OperationLogManager.libs;
using Prism.Services.Dialogs;
using Prism.Ioc;
using System.Windows.Markup;
using Prism.Events;
using System.Windows.Automation;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using SharedResource.libs;
using Machine.Interfaces;
using SharedResource.events;
using Microsoft.Win32;
using static Community.CsharpSqlite.Sqlite3;
using Ookii.Dialogs.Wpf;
using SharedResource.tools;

namespace ParamConfigManager.ViewModels
{
    internal class ParamConfigViewModel : BindableBase
    {
        public ParamConfigViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            configService = containerProvider.Resolve<IConfigService>();
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            globalCraftPara = containerProvider.Resolve<GlobalCraftPara>();

            FindFileCommand = new DelegateCommand(FindFileAsync);
            SaveCommand = new DelegateCommand<string>(SaveConfig);

            ValidateCommand = new DelegateCommand(ValidateConfig);
            DeleteCommand = new DelegateCommand<string>(DeleteConfig);
            ApplyCommand = new DelegateCommand<string>(ApplyFile);
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
                    eventAggregator.GetEvent<PreinstallForAxisMoveEvent>().Publish(new(axis, pos, CurrentPosition.IsAbsolute));
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
                CurrentConfig = configService.CreateDefaultConfig();
                CurrentPosition = configService.CreatePositionConfig();
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
                        if (File.Exists(craftPara_path + $"\\{filename}") || File.Exists(preinstallPara_path + $"\\{filename}"))
                        {
                            MessageBox.Show("文件名重复或文件已存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        bool iscraftPara = true;
                        var inputParamFile = await configService.LoadConfigAsync<ConfigModel>(filepath);
                        CurrentConfig = inputParamFile;
                        if (inputParamFile.EtchingName == null)
                        {
                            var paramFile = await configService.LoadConfigAsync<PositionModel>(filepath);
                            CurrentPosition = paramFile;
                            if (paramFile.Name == null)
                            {
                                MessageBox.Show("错误：无法识别的配置文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                LoggingService.Instance.LogError("导入文件失败", new Exception("无法识别的配置文件"));
                                return;
                            }
                            else iscraftPara = false;
                        }
                        else iscraftPara = true;
                        
                        if (iscraftPara)
                        {
                            CurrentConfig.EtchingName = Path.GetFileNameWithoutExtension(filepath);
                            await configService.SaveConfigAsync(CurrentConfig, Path.Combine(craftPara_path, $"{filename}"));
                        }
                        else
                        {
                            CurrentPosition.Name = Path.GetFileNameWithoutExtension(filepath);
                            await configService.SaveConfigAsync(CurrentPosition, Path.Combine(preinstallPara_path, $"{filename}"));
                        }
                        
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

                    var craft_filePath = Path.Combine(craftPara_path, fileName);
                    var preinstall_filePath = Path.Combine(preinstallPara_path, fileName);
                    string outputPath = "";

                    if (File.Exists(craft_filePath))
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
                                File.Copy(craft_filePath, TarOutputFile);
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
        private readonly IConfigService configService;
        private readonly IEventAggregator eventAggregator;

        public GlobalCraftPara globalCraftPara { get; set; }

        private readonly string craftPara_path = Path.Combine(ConfigStore.StoreDir, "craft");
        private readonly string preinstallPara_path = Path.Combine(ConfigStore.StoreDir, "preinstall");

        private string _selectedConfigFile;
        private string _searchText;
        private ConfigModel _currentConfig;
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

        public ConfigModel CurrentConfig
        {
            get { return _currentConfig; }
            set { SetProperty(ref _currentConfig, value); }
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
            CurrentConfig = configService.CreateDefaultConfig();
            CurrentPosition = configService.CreatePositionConfig();

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
                var files = await configService.GetConfigFilesAsync(craftPara_path);
                var preinstall_files = await configService.GetConfigFilesAsync(preinstallPara_path);

                ConfigFiles.Clear();
                foreach (var file in files)
                {
                    ConfigFiles.Add(file);
                }

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
            var config_filePath = Path.Combine(craftPara_path, fileName);
            var preinstall_filePath = Path.Combine(preinstallPara_path, fileName);

            try
            {
                if (File.Exists(config_filePath))
                {
                    CurrentConfig = await configService.LoadConfigAsync<ConfigModel>(config_filePath);
                }
                if (File.Exists(preinstall_filePath))
                {
                    CurrentPosition = await configService.LoadConfigAsync<PositionModel>(preinstall_filePath);
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
                if (file_param.ToString() == "工艺配置")
                {
                    if (!Isamend)
                    {
                        if (ConfigFiles.Contains(CurrentConfig.EtchingName))
                        {
                            MessageBox.Show("文件名重复，请更改", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    await configService.SaveConfigAsync(CurrentConfig, Path.Combine(craftPara_path, $"{CurrentConfig.EtchingName}.json"));
                }
                else if (file_param.ToString() == "预设点位")
                {
                    if (!Isamend)
                    {
                        if (ConfigFiles.Contains(CurrentPosition.Name))
                        {
                            MessageBox.Show("文件名重复，请更改", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    await configService.SaveConfigAsync(CurrentPosition, Path.Combine(preinstallPara_path, $"{CurrentPosition.Name}.json"));
                }

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
            CurrentConfig = configService.CreateDefaultConfig();
            CurrentPosition = configService.CreatePositionConfig();
            IsReadonly = false;
            Isamend = false;
        }

        private void ValidateConfig()
        {
            var validationResults = CurrentConfig.Validate();

            if (validationResults.Count == 0)
            {
                MessageBox.Show("参数验证通过，所有参数均符合要求", "验证结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var errorMessage = "以下参数不符合要求:\n\n";
                foreach (var result in validationResults)
                {
                    errorMessage += $"{result.Key}: {result.Value}\n";
                }

                MessageBox.Show(errorMessage, "验证结果", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

                    var filePath = Path.Combine(craftPara_path, fileName);
                    var preinstall_filePath = Path.Combine(preinstallPara_path, fileName);

                    if (File.Exists(filePath))
                        File.Delete(filePath);
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

        private async void ApplyFile(string fileName)
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

                var filePath = Path.Combine(craftPara_path, fileName);

                List<string> tuple_laser = new List<string>() { $"{CurrentConfig.LaserParameters.Frequency}", $"{CurrentConfig.LaserParameters.Divider}", $"", $"{CurrentConfig.LaserParameters.PowerPercentage}" };
                eventAggregator.GetEvent<SetLaserParamEvent>().Publish(tuple_laser);
                eventAggregator.GetEvent<SetAxesParamEvent>().Publish(CurrentConfig.AxesParameters);

                if (File.Exists(filePath))
                {
                    string textContent = SetupCraftParam();
                    string originalContent = File.ReadAllText(globalCraftPara.SourceFile_path);

                    string newContent = textContent + "\n\r" + originalContent;
                    await File.WriteAllTextAsync(globalCraftPara.TargetFile_path, newContent);

                    globalCraftPara.FileName = CurrentConfig.EtchingName;
                    globalCraftPara.BallDiameter = CurrentConfig.BallDiameter;
                    globalCraftPara.XProcessPlace = CurrentConfig.AxesParameters.XProcessPlace;
                    globalCraftPara.YProcessPlace = CurrentConfig.AxesParameters.YProcessPlace;
                    globalCraftPara.ZProcessPlace = CurrentConfig.AxesParameters.ZProcessPlace;
                    globalCraftPara.AProcessPlace = CurrentConfig.AxesParameters.AProcessPlace;
                    globalCraftPara.BProcessPlace = CurrentConfig.AxesParameters.BProcessPlace;

                    MessageBox.Show("配置已应用！", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoggingService.Instance.LogInfo($"应用 {fileName} 工艺参数：" +
                    $"\n\t工件尺寸：{CurrentConfig.BallDiameter}" +
                    $"\n\t激光参数：功率{CurrentConfig.LaserParameters.PowerPercentage}%，频率{CurrentConfig.LaserParameters.Frequency}kHz，分频数{CurrentConfig.LaserParameters.Divider}。" +
                    $"\n\t轴参数：加工位置：X-->{CurrentConfig.AxesParameters.XProcessPlace}，Y-->{CurrentConfig.AxesParameters.YProcessPlace}，Z-->{CurrentConfig.AxesParameters.ZProcessPlace}，A-->{CurrentConfig.AxesParameters.AProcessPlace}，B-->{CurrentConfig.AxesParameters.BProcessPlace}" +
                    $"\n\t速度：X-->{CurrentConfig.AxesParameters.XSpeed}，Y-->{CurrentConfig.AxesParameters.YSpeed}，Z-->{CurrentConfig.AxesParameters.ZSpeed}，A-->{CurrentConfig.AxesParameters.ASpeed}，B-->{CurrentConfig.AxesParameters.BSpeed}" +
                    $"\n\t加速度：X-->{CurrentConfig.AxesParameters.XAccelerate}，Y-->{CurrentConfig.AxesParameters.YAccelerate}，Z-->{CurrentConfig.AxesParameters.ZAccelerate}，A-->{CurrentConfig.AxesParameters.AAccelerate}，B-->{CurrentConfig.AxesParameters.BAccelerate}" +
                    $"\n\t减速度：X-->{CurrentConfig.AxesParameters.XDecelerate}，Y-->{CurrentConfig.AxesParameters.YDecelerate}，Z-->{CurrentConfig.AxesParameters.ZDecelerate}，A-->{CurrentConfig.AxesParameters.ADecelerate}，B-->{CurrentConfig.AxesParameters.BDecelerate}" +
                    $"\n\t脚本参数：轴号：A-->{CurrentConfig.ScriptParameters.A}，B-->{CurrentConfig.ScriptParameters.B}" +
                    $"刻槽数-->{CurrentConfig.ScriptParameters.m_Number}，槽台比-->{CurrentConfig.ScriptParameters.m_SlotRatio}, 螺旋倾角-->{CurrentConfig.ScriptParameters.m_SpiralDip}" +
                    $"\n\t加工起始角度-->{CurrentConfig.ScriptParameters.m_StartForMove}，加工终止角度-->{CurrentConfig.ScriptParameters.m_EndForMove}，" +
                    $"起始位置角度-->{CurrentConfig.ScriptParameters.m_StartForMachi}，终止位置角度-->{CurrentConfig.ScriptParameters.m_EndForMachi}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"文件 {fileName} 工艺参数应用失败", ex);
                MessageBox.Show("工艺参数应用失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsReadonly = true;
                Isamend = false;
            }
        }

        private string SetupCraftParam()
        {
            List<string> lines = new List<string>();

            lines.Add($"GLOBAL INT A = {CurrentConfig.ScriptParameters.A}");
            lines.Add($"GLOBAL INT B = {CurrentConfig.ScriptParameters.B}");
            lines.Add($"REAL m_StartForMove = {CurrentConfig.ScriptParameters.m_StartForMove}");
            lines.Add($"REAL m_EndForMove = {CurrentConfig.ScriptParameters.m_EndForMove}");
            lines.Add($"REAL m_StartForMachi = {CurrentConfig.ScriptParameters.m_StartForMachi}");
            lines.Add($"REAL m_EndForMachi = {CurrentConfig.ScriptParameters.m_EndForMachi}");
            //lines.Add($"REAL m_SlotWidth = {CurrentConfig.ScriptParameters.m_SlotWidth}");
            lines.Add($"REAL m_Loop = {CurrentConfig.ScriptParameters.m_Loop}");
            lines.Add($"REAL m_Number = {CurrentConfig.ScriptParameters.m_Number}");
            lines.Add($"REAL m_mc = {CurrentConfig.ScriptParameters.m_c}");
            //lines.Add($"REAL m_SpiralDip = {CurrentConfig.ScriptParameters.m_SpiralDip}");
            lines.Add($"REAL m_RotationRangeOfBaxis = {CurrentConfig.ScriptParameters.m_RotationRangeOfBaxis}");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
