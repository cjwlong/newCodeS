using DeviceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceInterace
{
    public interface IMotionService:IDevice
    {

        Task ConnectAsync(CancellationToken token);
        Task DisconnectAsync();
        bool IsConnected { get; }

        // ② 使能  
        Task EnableAsync(string axis, CancellationToken token);
        Task DisableAsync(string axis);
        Task DisableAllAsync();

        // ③ 运动控制  
        Task SetSpeedAsync(string axis, double speed);

        Task HomeAsync(string axis, CancellationToken token);

        Task MoveAbsoluteAsync(string axis, double position, CancellationToken token);
        Task MoveRelativeAsync(string axis, double offset, CancellationToken token);

        Task JogAsync(string axis, double velocity, CancellationToken token);

        Task RunAsync(string axis, CancellationToken token);

        Task StopAsync(string axis);

      
    }
}
