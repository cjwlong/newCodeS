using ExternalIOManager.libs;
using SharedResource.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalIOManager.Interfaces
{
    public interface IIODeviceService
    {
        string IpAddress { get; set; }
        int Port { get; set; }
        DeviceStatus DeviceStatus { get; set; }

        bool[] ReadOutputCoils(ushort start, ushort length, byte slaveAddress = 1);
        string ReadInput(int device, out bool value);

        void WriteSingleCoil(ushort coilAddress, bool value, byte slaveAddress = 1);

        Task Connect();
        void Dispose();
        void SaveConfig2File();
        void LoadConfigForFile();
    }
}
