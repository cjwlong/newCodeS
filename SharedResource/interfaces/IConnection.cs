using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.interfaces
{
    public interface IConnection
    {
        /// <summary>
        /// 接口名称
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 是否为共享信道
        /// </summary>
        bool Shared { get; }
        /// <summary>
        /// 接口类型
        /// </summary>
        ConnectionType Type { get; }
        /// <summary>
        /// 接口使用状态
        /// </summary>
        bool IsInUse { get; set; }
    }
}
