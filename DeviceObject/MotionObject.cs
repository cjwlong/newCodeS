using DeviceInterace;
using DeviceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DeviceAdapter
{
    public class MotionObject : IMotionService
    {
        private readonly object _acs;
        private readonly Dictionary<string, int> _axisMap;
        string dllPath = "";
        private object _instance;
        private Type _type;
        public MotionObject(object acs)
        {

        }
     

        #region Connection

        public bool IsConnected { get; private set; }

        public string Name => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public async Task ConnectAsync(CancellationToken token, object config)
        {
           
        }

        public async Task DisconnectAsync()
        {
        
        }

        #endregion

        #region Enable

        public async Task EnableAsync(string axis, CancellationToken token)
        {
           
        }

        public async Task DisableAsync(string axis)
        {
         
        }

        public async Task DisableAllAsync()
        {
           
        }

        #endregion



        public async Task SetSpeedAsync(string axis, double speed)
        {
           
        }

        public async Task HomeAsync(string axis, CancellationToken token)
        {
         
        }

        public async Task MoveAbsoluteAsync(string axis, double position, CancellationToken token)
        {
           
        }

        public async Task MoveRelativeAsync(string axis, double offset, CancellationToken token)
        {
           
        }

        public async Task JogAsync(string axis, double velocity, CancellationToken token)
        {
            
        }

        public async Task RunAsync(string axis, CancellationToken token)
        {
           
           
        }

        public async Task StopAsync(string axis)
        {
          
        }



        #region Status

        public async Task<string > GetPositionAsync(string axis)
        {
            return await Task.FromResult(""
            );

        }

        public async Task<string> GetAxisStateAsync(string axis)
        {
            return await Task.FromResult(""
              );
        }

        public async Task<string> GetLimitStatusAsync(string axis)
        {
          
            return await Task.FromResult(""
            );
        }

        #endregion

        
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public void Connect(string ipOrPort)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public DeviceStatus GetStatus()
        {
            throw new NotImplementedException();
        }

        public Task ConnectAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }

}
public enum AxisState
{
    Unknown,
    Disabled,
    Enabled,
    Moving,
    Homing,
    Error
}




