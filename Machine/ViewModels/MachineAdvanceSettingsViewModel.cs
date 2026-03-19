using Machine.Interfaces;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using PublishTools.tools;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.ViewModels
{
    internal class MachineAdvanceSettingsViewModel : BindableBase, INavigationAware
    {
        readonly IContainerProvider containerProvider;
        readonly IDialogService dialogService;
        readonly IRegionManager regionManager;

        string _title = "机床";
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string NavigationSigns { get => nameof(Views.MachineAdvanceSettings); }
        public MachineViewModel MachineVM { get; set; }
        public DelegateCommand ChangeFormulaFileCommand { get; set; }
        public MachineAdvanceSettingsViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            dialogService = containerProvider.Resolve<IDialogService>();
            MachineVM = (MachineViewModel)containerProvider.Resolve<IMachine>();

            // 命令绑定
            ChangeFormulaFileCommand = new DelegateCommand(() =>
            {
                string py_file = OpenSaveWindow.OpenFileDialog("公式文件(*.fml)|*.fml|Python文件(*.py)|*.py");
                if (py_file == null || !File.Exists(py_file))
                    return;
                FileInfo py_info = new(py_file);
                FileInfo file_name = new($"{ConfigStore.StoreDir}/{Path.GetFileName(py_file)}");
                if (file_name.FullName != py_info.FullName)
                {
                    if (file_name.Exists)
                    {
                        file_name.IsReadOnly = false;
                        file_name.Delete();
                    }
                    py_info.CopyTo(file_name.FullName);
                }
                MachineVM.FormulaFile.Value = $"{Path.GetFileName(py_file)}";
            });
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }
    }
}
