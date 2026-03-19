using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManager
{
    public class User
    {
        public Guid Id { get; set; }

        public string UserName { get; set; }

        public string PasswordHash { get; set; }

        public string RealName { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? LastLoginTime { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }
}
