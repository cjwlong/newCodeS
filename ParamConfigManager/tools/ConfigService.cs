using Newtonsoft.Json;
using OperationLogManager.libs;
using ParamConfigManager.interfaces;
using ParamConfigManager.libs;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParamConfigManager.tools
{
    public class ConfigService : IConfigService
    {
        public async Task<List<string>> GetConfigFilesAsync(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                var files = await Task.Run(() =>
                    Directory.GetFiles(directoryPath, "*.json").ToList()
                );

                return files.Select(file => Path.GetFileNameWithoutExtension(file)).ToList();
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("获取配置文件列表失败", ex);
                return null;
            }
        }

        public ConfigModel CreateDefaultConfig()
        {
            return new ConfigModel
            {
                EtchingName = "默认刻蚀配置",
                BallDiameter = 10.0,
                Type = SharedResource.enums.Configfile_type.craft,
                LaserParameters = new LaserParameters
                {
                    PowerPercentage = 75.0,
                    Frequency = 100.0,
                    Divider = 8,
                },
                AxesParameters = new AxesParameters
                {
                    XProcessPlace = 0.00,
                    YProcessPlace = 0.00,
                    ZProcessPlace = 0.00,
                    AProcessPlace = 0.00,
                    BProcessPlace = 0.00,
                    XSpeed = 10.0,
                    YSpeed = 10.0,
                    ZSpeed = 10.0,
                    ASpeed = 10.0,
                    BSpeed = 10.0,
                    XAccelerate = 100,
                    YAccelerate = 100,
                    ZAccelerate = 100,
                    AAccelerate = 100,
                    BAccelerate = 100,
                    XDecelerate = 100,
                    YDecelerate = 100,
                    ZDecelerate = 100,
                    ADecelerate = 100,
                    BDecelerate = 100,
                    XMAXProcessSpeed = 50,
                    YMAXProcessSpeed = 50,
                    ZMAXProcessSpeed = 50,
                    AMAXProcessSpeed = 50,
                    BMAXProcessSpeed = 50,
                },
                ScriptParameters = new ScriptParameters
                {
                    A = 5,
                    B = 2,
                    m_Number = 1,
                    m_Loop = 0,
                    m_StartForMove = 0.0,
                    m_EndForMove = 0.0,
                    m_StartForMachi = 0.0,
                    m_EndForMachi = 0.0,
                    m_SpiralDip = 0.0,
                }
            };
        }

        public PositionModel CreatePositionConfig()
        {
            return new PositionModel
            {
                Name = "null",
                Type = SharedResource.enums.Configfile_type.preinstall,
                ALimitPlace = 0,
                BLimitPlace = 0,
                XLimitPlace = 0,
                YLimitPlace = 0,
                ZLimitPlace = 0,
                XPresetPlace = 0,
                YPresetPlace = 0,
                ZPresetPlace = 0,
                APresetPlace = 0,
                BPresetPlace = 0,
                IsAbsolute = true
            };
        }

        public async Task<T> LoadConfigAsync<T>(string filePath) where T : class, new()
        {
            try
            {
                // 读取文件内容
                using var streamReader = new StreamReader(filePath);
                var json = await streamReader.ReadToEndAsync();

                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置文件失败: {ex.Message}");
                throw;
            }
        }

        public async Task SaveConfigAsync<T>(T config, string filePath) where T : class
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (System.Exception ex)
            {
                // 处理异常
                LoggingService.Instance.LogError("保存配置文件失败", ex);
            }
        }
    }
}
