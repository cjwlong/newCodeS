using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.ViewModels
{
    public class ProgressBoxViewModel : BindableBase, IDialogAware
    {
        private BackgroundWorker _backgroundWorker;
        private string _title = "信息";
        private string _taskMessage = "";
        private string _buttonText = "取消";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public string TaskMessage
        {
            get => _taskMessage;
            set => SetProperty(ref _taskMessage, value);
        }
        public string ButtonText
        {
            get => _buttonText;
            set => SetProperty(ref _buttonText, value);
        }

        public ProgressBoxViewModel()
        {

        }

        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand<string> CloseDialogCommand =>
            _closeDialogCommand ?? (_closeDialogCommand = new DelegateCommand<string>(CloseDialog));

        public event Action<IDialogResult> RequestClose;
        Func<bool> CancelOperation;
        protected virtual void CloseDialog(string parameter)
        {
            ButtonResult result = ButtonResult.None;

            if (parameter?.ToLower() == "确定")
                result = ButtonResult.OK;
            else if (parameter?.ToLower() == "取消")
                result = ButtonResult.Cancel;

            RaiseRequestClose(new DialogResult(result));
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            if (dialogResult.Result == ButtonResult.Cancel)
                CancelOperation?.Invoke();
            RequestClose.Invoke(dialogResult);
        }

        public bool CanCloseDialog()
        {
            return true;
        }
        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = parameters.GetValue<string>("title");
            TaskMessage = parameters.GetValue<string>("message");
            var Work = parameters.GetValue<DoWorkEventHandler>("task");
            CancelOperation = parameters.GetValue<Func<bool>>("cancel");

            if (_backgroundWorker == null)
                _backgroundWorker = new BackgroundWorker();
            //bool类型，指示BackgroundWorker是否可以报告进度更新。当该属性值为True时，将可以成功调用ReportProgress方法
            _backgroundWorker.WorkerReportsProgress = true;
            //bool类型，指示BackgroundWorker是否支持异步取消操作。当该属性值为True是，将可以成功调用CancelAsync方法
            _backgroundWorker.WorkerSupportsCancellation = true;
            //执行RunWorkerAsync方法后触发DoWork，将异步执行backgroundWorker_DoWork方法中的代码
            _backgroundWorker.DoWork += new DoWorkEventHandler(Work);
            ////执行ReportProgress方法后触发ProgressChanged，将执行ProgressChanged方法中的代码
            //_backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(_backgroundWorker_ProgressChanged);
            //异步操作完成或取消时执行的操作，当调用DoWork事件执行完成时触发。 
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);

            _backgroundWorker.RunWorkerAsync();
        }
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if ((string)e.Result == null)
                {
                    TaskMessage = "已完成";
                    //MessageBox.Show("操作已完成");
                }
                else
                {
                    TaskMessage = $"出现错误\n{(string)e.Result}";
                }
            }
            catch (Exception ex)
            {
                TaskMessage = $"内部错误，无法判断是否回零完成，请人工确认\n{ex.Message}";
            }
            ButtonText = "确定";
        }
    }
}
