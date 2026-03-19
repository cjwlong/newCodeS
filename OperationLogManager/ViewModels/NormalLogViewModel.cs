using OperationLogManager.libs;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperationLogManager.ViewModels
{
    public class NormalLogViewModel : BindableBase
    {
        //private readonly LoggingService _loggingService;

        public ObservableCollection<LogEntry> OperationLogs => LoggingService.Instance.OperationLogs;

        public NormalLogViewModel()
        {
        }
    }
}
