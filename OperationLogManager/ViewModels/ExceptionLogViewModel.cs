using OperationLogManager.libs;
using Prism.Events;
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
    public class ExceptionLogViewModel : BindableBase
    {
        public ExceptionLogViewModel()
        {
        }

        public ObservableCollection<LogEntry> ExceptionLogs => LoggingService.Instance.ExceptionLogs;
    }
}
