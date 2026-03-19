using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CCD.libs
{
    public class LocationModel
    {
        public int ID { get; set; }
        public Point CCDPoint { get; set; }
        public Point Location { get; set; }
        public string Mode { get; set; }
        public LocationModel(int id, Point ccdPoint, Point location, string mode)
        {
            ID = id;
            CCDPoint = ccdPoint;
            Location = location;
            Mode = mode;
        }
    }
}
