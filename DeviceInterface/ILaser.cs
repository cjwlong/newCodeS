using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceInterface
{
    interface ILaser: IDevice
    {

          
            double GetTemperature();
            double GetOutputPower();

            void SetMode(string mode);         
            void SetPulseFrequency(double freq);
            void SetPulseCount(int count);
            void SetDivisionNumber(int division);

            void SetFrequency(double frequency);
            void SetTargetPower(double power);

        

            void Start();
            void Stop();
        
    }
}

