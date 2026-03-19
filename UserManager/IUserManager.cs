using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManager
{
    public interface IUserManager
    {
        IUserService UserService { get; }
        IPermissionService PermissionService { get; }

        User CurrentUser { get; }

        bool CheckPermission(string permissionKey);
    }
}
