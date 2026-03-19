using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.tools
{
    /// <summary>
    /// 配置存储工具类，用于在用户文档目录下存储和加载应用程序配置
    /// </summary>
    public class ConfigStore
    {
        /// <summary>
        /// 配置文件存储目录路径，格式为：我的文档/应用程序名/
        /// </summary>
        public static string StoreDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            + "/" + Assembly.GetEntryAssembly().GetName().Name  // 程序名
            + "/";

        /// <summary>
        /// 检查并创建配置文件存储目录
        /// </summary>
        public static void CheckStoreFloder()
        {
            string docFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // 构建完整路径：我的文档/应用程序名/
            string temp = Path.Combine(docFolder, Assembly.GetEntryAssembly().GetName().Name);

            // 若目录不存在，则创建
            if (!Directory.Exists(temp))
                Directory.CreateDirectory(temp);

            // 更新存储目录路径
            StoreDir = temp;
        }

        /// <summary>
        /// 将配置对象序列化为JSON并存储到文件
        /// </summary>
        /// <typeparam name="T">配置对象类型</typeparam>
        /// <param name="conf">要存储的配置对象</param>
        /// <returns>存储成功返回true，失败返回false</returns>
        /// <remarks>
        /// 集合类型会生成特殊格式的文件名，例如ListConfigure.json
        /// 文件路径格式：我的文档/应用程序名/类型名Configure.json
        /// </remarks>
        public static bool StoreConfiguration<T>(T conf)
        {
            // 确保存储目录存在
            if (!Directory.Exists(StoreDir))
                CheckStoreFloder();

            // 序列化为JSON字符串
            string json = JsonConvert.SerializeObject(conf);

            // 构建文件名（默认使用类型名）
            string filename = StoreDir + "/" + conf.GetType().Name + "Configure.json";

            // 处理集合类型（获取第一个元素类型作为文件名）
            if (conf is IEnumerable confs && confs.Cast<object>().Any())
            {
                var firstItemType = confs.Cast<object>().First().GetType();
                filename = StoreDir + "/" + firstItemType.Name + "ListConfigure.json";
            }

            // 写入文件
            File.WriteAllText(filename, json);
            return true;
        }

        /// <summary>
        /// 从文件加载配置并反序列化为对象
        /// </summary>
        /// <typeparam name="T">要加载的配置对象类型</typeparam>
        /// <returns>反序列化后的对象，若加载失败则返回类型默认值</returns>
        /// <remarks>
        /// 自动根据类型名查找配置文件
        /// 集合类型会查找特殊格式的文件名，例如ListConfigure.json
        /// </remarks>
        public static T LoadConfiguration<T>()
        {
            try
            {
                // 构建默认文件名
                string filename = StoreDir + "/" + typeof(T).Name + "Configure.json";

                // 处理集合类型
                if (typeof(T).GetInterfaces().Any(i => i == typeof(IEnumerable)))
                {
                    // 获取泛型参数类型作为文件名
                    if (typeof(T).IsGenericType && typeof(T).GetGenericArguments().Length > 0)
                    {
                        filename = StoreDir + "/" + typeof(T).GetGenericArguments()[0].Name + "ListConfigure.json";
                    }
                }

                // 读取文件内容并反序列化
                string json = File.ReadAllText(filename);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception)
            {
                // 加载失败时返回类型默认值（如null、0等）
                return default(T);
            }
        }
    }
}
