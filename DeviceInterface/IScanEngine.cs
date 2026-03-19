using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceInterface
{
  public  interface IScanEngine:IDevice
    {
       
        void Initialize();

       

        void JumpAbs(double x, double y);
        void JumpRel(double dx, double dy);

        void MarkAbs(double x, double y);
      



        bool IsBusy { get; }
        event Action<string> OnError;

        /// <summary>
        /// 开始执行
        /// </summary>
        void Execute();

        /// <summary>
        /// 停止
        /// </summary>
        void Stop();
    }
}
