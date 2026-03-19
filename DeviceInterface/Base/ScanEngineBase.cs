using DeviceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceInterface.Base
{
    public abstract class ScanEngineBase : IScanEngine
    {
        public string Name => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public bool IsBusy => throw new NotImplementedException();

        public event Action<string> OnError;

        public abstract void Initialize();

       

        public abstract void Execute();

        public abstract void Stop();





        public abstract void SetSpeed(double jumpSpeed, double markSpeed);
        public abstract void SetPower(double power);
        public abstract void SetFrequency(double frequency);


        public virtual void Dispose()
        {
            Stop();
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

        public void JumpAbs(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void JumpRel(double dx, double dy)
        {
            throw new NotImplementedException();
        }

        public void MarkAbs(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void MarkRel(double dx, double dy)
        {
            throw new NotImplementedException();
        }

     
    }

}
