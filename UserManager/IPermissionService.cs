using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManager
{
    public interface IPermissionService
    {
        // 创建角色
        void CreateRole(Role role);

        // 删除角色
        void DeleteRole(string roleName);

        // 更新角色权限
        void UpdateRolePermissions(string roleName, List<string> permissions);

        // 给用户分配角色
        void AssignRole(Guid userId, string roleName);

        // 移除用户角色
        void RemoveRole(Guid userId, string roleName);

        // 获取用户权限
        List<string> GetUserPermissions(Guid userId);

        // 判断是否有权限
        bool HasPermission(Guid userId, string permissionKey);
    }
}
