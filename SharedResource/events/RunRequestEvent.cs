using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events
{
  
    public class RunRequestEvent : PubSubEvent<RunRequestEventArgs>
    {
    }

    public class RunRequestEventArgs
    {
        public string Data { get; set; }

        // 用于订阅者完成通知
        public TaskCompletionSource<bool> CompletionSource { get; set; } = new TaskCompletionSource<bool>();
    }
}
