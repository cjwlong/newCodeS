
using DeviceInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeviceAdapter
{
   
    public class ScanObject : IScanEngine
    {
        private object _instance;

        private MethodInfo _initMethod;
        private MethodInfo _jumpMethod;
        private MethodInfo _markMethod;


        public bool IsBusy => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public event Action<string> OnError;



        public ScanObject()
        {
           

        }
        private void LoadPlugin()
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

        public void Execute()
        {
            throw new NotImplementedException();
        }

        public DeviceStatus GetStatus()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
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

        

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
