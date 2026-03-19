using ParamConfigManager.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParamConfigManager.interfaces
{
    public interface IConfigService
    {
        Task SaveConfigAsync<T>(T config, string filePath) where T : class;
        Task<T> LoadConfigAsync<T>(string filePath) where T : class, new();
        Task<List<string>> GetConfigFilesAsync(string directoryPath);
        ConfigModel CreateDefaultConfig();
        PositionModel CreatePositionConfig();
    }
}
