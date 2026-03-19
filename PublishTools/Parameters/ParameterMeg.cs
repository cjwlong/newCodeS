using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.Parameters
{
    /// <summary>
    /// 参数类
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    internal class ParameterMeg<T>
    {
        /// <summary>
        /// 参数Id
        /// </summary>
        public int Id;
        /// <summary>
        /// 参数名
        /// </summary>
        public string Name;
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DispName;
        /// <summary>
        /// 参数值
        /// </summary>
        public T Value;
        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description;
        /// <summary>
        /// 参数类型
        /// </summary>
        public string Type = typeof(T).Name;
    }
}
