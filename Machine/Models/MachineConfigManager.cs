using Newtonsoft.Json;
using OperationLogManager.libs;
using SharedResource.libs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Machine.Models.MachineConfigManager;

namespace Machine.Models
{
  public  class MachineConfigManager
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
