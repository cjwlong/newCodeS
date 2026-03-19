using Prism.Events;
using SharedResource.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.events
{
    public class CamerConnectStatus : PubSubEvent<CameraState>
    {
    }
}
