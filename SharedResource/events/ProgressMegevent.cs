using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events
{
    public class ProgressMegevent : PubSubEvent<string>
    {
    }

    /// <summary>
    /// 加工起始点，文件路径
    /// </summary>
    public class StartProcessEvent : PubSubEvent
    {
    }
    public class Cmd_StartProcessPrepareEvent : PubSubEvent<ProcessPrepareRequest>
    {
    }
    public class ProcessPrepareRequest
    {
        public List<double> Values { get; set; }
        public string Message { get; set; }

        public TaskCompletionSource<bool> Completion { get; set; } = new TaskCompletionSource<bool>();
    }
    public class Cmd_StartProcessEvent : PubSubEvent<string>
    {
    }

    public class PauseProcessEvent : PubSubEvent 
    { }
    public class Cmd_PauseProcessEvent : PubSubEvent
    { }

    public class StopProcessEvent : PubSubEvent
    {
    }
    public class Cmd_StopProcessEvent : PubSubEvent<StopProcessRequest>
    {
    }
    public class StopProcessRequest
    {
        // 发布者等待的完成任务
        public TaskCompletionSource<bool> Completion { get; set; }
            = new TaskCompletionSource<bool>();
    }
    public class ContinueProcessEvent : PubSubEvent { }
    public class Cmd_ContinueProcessEvent : PubSubEvent { }
}
