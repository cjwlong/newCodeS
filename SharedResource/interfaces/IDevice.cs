using SharedResource.enums;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.interfaces
{
    /// <summary>
    /// 硬件通用接口
    /// General Hardware Interface
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// 设备名称
        /// </summary>
        string Name { get; }


        /// <summary>
        /// GUID
        /// </summary>
        string Id { get; }


        /// <summary>
        /// 设备类型
        /// </summary>
        DeviceType Type { get; }


        /// <summary>
        /// 设备状态
        /// </summary>
        DeviceStatus Status { get; }


        /// <summary>
        /// 设备所使用的通讯方式
        /// </summary>
        ConnectionType Channel { get; }


        /// <summary>
        /// 硬件对外提供的连接能力
        /// </summary>
        List<IConnection> Interfaces { get; }


        /// <summary>
        /// 连接设备
        /// </summary>
        /// <param name="con">端口</param>
        /// <param name="info">连接参数</param>
        /// <returns></returns>
        string Connect(IConnection con, IConnectionInfo info, List<IConnection> attach = null);


        /// <summary>
        /// 断开
        /// </summary>
        /// <returns></returns>
        string Disconnect();


        /// <summary>
        /// 设备状态的刷新率
        /// </summary>
        double RefreshRate { get; }
        /// <summary>
        /// 刷新设备状态
        /// </summary>
        /// <returns>刷新是否成功 (true: success)</returns>
        bool RefreshStatus();


        /// <summary>
        /// 刷新设备接口
        /// </summary>
        /// <returns>刷新是否成功 (true: success)</returns>
        bool RefreshInterfaces();
        /// <summary>
        /// 释放设备资源, 更换设备之前必须调用, 该操作将检查并释放所有接口资源
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item><strong>True:</strong>释放成功</item>
        /// <item><strong>False:</strong>释放失败</item>
        /// </list>
        /// </returns>
        bool Dispose();
    }
}
