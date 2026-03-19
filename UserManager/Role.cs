using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManager
{
    public class Role
    {
        public string RoleName { get; set; }

        public string Description { get; set; }

        public List<string> Permissions { get; set; } = new List<string>(); 
    }
}
