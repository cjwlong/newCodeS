using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceInterface
{
    public interface IDevice
    {

        string Name { get; }
        string Version { get; }

        void Initialize();
        void Shutdown();

        void Connect(string ipOrPort);
        void Disconnect();
        bool IsConnected { get; }

        // ========== 状态 ==========
        DeviceStatus GetStatus();
    }

    public enum DeviceStatus
    {
        Unknown,
        Disconnected,
        Connected,
        Running
    }

}
