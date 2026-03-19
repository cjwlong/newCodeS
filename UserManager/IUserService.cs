using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManager
{
    public interface IUserService
    {
        // 登录
        User Login(string userName, string password);

        // 退出
        void Logout(Guid userId);

        // 新增用户
        void CreateUser(User user, string password);

        // 修改用户
        void UpdateUser(User user);

        // 删除用户
        void DeleteUser(Guid userId);

        // 查询用户
        User GetUser(Guid userId);

        List<User> GetAllUsers();

        // 启用/禁用
        void EnableUser(Guid userId, bool enable);

        // 修改密码
        void ChangePassword(Guid userId, string oldPassword, string newPassword);
    }
}
