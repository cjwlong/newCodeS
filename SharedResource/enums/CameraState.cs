using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.enums
{
    public enum CameraState
    {
        CapturingImage, // 正在采集图像
        Paused, // 暂停
        Disconnected // 相机未连接
    }
}
