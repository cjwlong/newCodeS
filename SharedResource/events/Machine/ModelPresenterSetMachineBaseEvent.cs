using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SharedResource.events.Machine
{
    public class ModelPresenterSetMachineBaseEvent : PubSubEvent<Tuple<Point3D, Vector3D, Vector3D>>
    {
    }
}
