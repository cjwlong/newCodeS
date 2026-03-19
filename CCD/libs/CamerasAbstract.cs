using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCD.libs
{
    public abstract class CamerasAbstract
    {
        public delegate void CameraImage(IntPtr intPtr);
        public event CameraImage CameraImageEvent;

        public static int DefaultSize = 100;

        public abstract bool CameraInit();

        public abstract void GetHW(out int width, out int height);
        public abstract void OneShot();
        public abstract void KeepShot(out int width, out int height);
        public abstract void Stop();
        public abstract void CamerasClose();

        public abstract void GetCameraPatam(out string timeOfExposure, out string gain, out string framerate);
        public abstract void SetCameraParam(string timeOfExposure, string gain, string framerate);
        public abstract bool SetAutoExposure(object obj, string str);

        //public abstract void CamerasSetting();

        public void OnImageGet(IntPtr intPtr)
        {
            CameraImageEvent?.Invoke(intPtr);
        }

        public virtual void Adjust()
        {
            return;
        }
    }
}
