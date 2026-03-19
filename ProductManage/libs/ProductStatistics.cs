using OperationLogManager.libs;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManage.libs
{
    /***
     * 日期,产量,加工时间(秒),空闲时间(秒)
     * 2025-06-01,15,3600,1200
     * 2025-06-02,23,5400,1800
     */
    public class ProductStatistics
    {
        public ProductStatistics()
        {
            if (!File.Exists(CSV_Filepath))
                File.Create(CSV_Filepath);

            ImportProductionFromCsv();
            //InitializeSampleData();
        }

        private readonly Dictionary<DateTime, DailyProductionData> _dailyData = new();
        private readonly string CSV_Filepath = Path.Combine(ConfigStore.StoreDir, "production_history.csv");

        // 每日生产数据
        public class DailyProductionData
        {
            public int ProductCount { get; set; }
            public double ProcessingTime { get; set; }
            public double IdleTime { get; set; }
        }

        // 从CSV文件读取生产记录并同步
        private void ImportProductionFromCsv()
        {
            try
            {
                if (!File.Exists(CSV_Filepath))
                {
                    LoggingService.Instance.LogError("读取失败", new Exception("文件 'production_history' 不存在"));
                    return;
                }

                using var reader = new StreamReader(CSV_Filepath);
                // 跳过标题行
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (values.Length >= 4 &&
                        DateTime.TryParse(values[0], out DateTime date) &&
                        int.TryParse(values[1], out int count) &&
                        double.TryParse(values[2], out double processingTime) &&
                        double.TryParse(values[3], out double idleTime))
                    {
                        RecordDailyProduction(date, count, processingTime, idleTime);
                    }
                    else
                    {
                        throw new Exception($"无效记录 '{line}'");
                    }
                }

                Console.WriteLine($"成功导入 {_dailyData.Count} 天的生产数据");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("读取生产记录失败", ex);
            }
        }

        // 记录指定日期的生产数据
        public void RecordDailyProduction(DateTime date, int productCount = 0, double processingTime = 0, double idleTime = 0)
        {
            if (!_dailyData.ContainsKey(date))
                _dailyData[date] = new DailyProductionData();

            var data = _dailyData[date];
            data.ProductCount += productCount;
            data.ProcessingTime += (int)Math.Floor(processingTime);
            data.IdleTime += (int)Math.Floor(idleTime);

            //ExportProductionToCsv(CSV_Filepath);
        }

        // 获取最近15天的生产件数（折线图数据）
        public Dictionary<string, int> GetLast15DaysProductionData()
        {
            var result = new Dictionary<string, int>();
            //int min_count = _dailyData.Count >= 15 ? 14 : _dailyData.Count - 1;
            for (int i = 15; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                string dateLabel = date.ToString("MM-dd");
                try
                {
                    result[dateLabel] = _dailyData.TryGetValue(date, out var data) ? data.ProductCount : 0;
                }
                catch (Exception ex)
                {
                }
                
            }
            return result;
        }

        // 导出生产数据到CSV文件
        public void ExportProductionToCsv()
        {
            try
            {
                using var writer = new StreamWriter(CSV_Filepath);
                writer.WriteLine("日期,产量,加工时间(秒),空闲时间(秒)");

                foreach (var item in _dailyData.OrderBy(x => x.Key))
                {
                    writer.WriteLine($"{item.Key:yyyy-MM-dd},{item.Value.ProductCount},{item.Value.ProcessingTime},{item.Value.IdleTime}");
                }

                //Console.WriteLine($"成功导出 {_dailyData.Count} 天的生产数据到 {filePath}");
                LoggingService.Instance.LogInfo("生产数据已保存");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"导出失败：{ex.Message}");
                LoggingService.Instance.LogError("生产数据保存失败", ex);
            }
        }

        /// <summary>
        /// 获取所有记录的总件数
        /// </summary>
        public int GetTotalProductCount()
        {
            return _dailyData.Values.Sum(d => d.ProductCount);
        }

        /// <summary>
        /// 获取所有记录的总生产时间（秒）
        /// </summary>
        public double GetTotalProcessingTime()
        {
            return _dailyData.Values.Sum(d => d.ProcessingTime);
        }

        /// <summary>
        /// 获取所有记录的总空闲时间（秒）
        /// </summary>
        public double GetTotalIdleTime()
        {
            return _dailyData.Values.Sum(d => d.IdleTime);
        }

        /// <summary>
        /// 获取平均每件产品的加工时间（秒）
        /// </summary>
        public double GetAverageProcessingTimePerProduct()
        {
            int totalProducts = GetTotalProductCount();
            return totalProducts > 0 ? (double)GetTotalProcessingTime() / totalProducts : 0;
        }

        // 初始化模拟数据
        public void InitializeSampleData()
        {
            var random = new Random();
            for (int i = 20; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var temp = new DailyProductionData();
                temp.ProductCount = random.Next(0, 150);
                temp.ProcessingTime = random.Next(5, 20); 
                temp.IdleTime = random.Next(1, 20);
                _dailyData[date] = temp;
            }
        }
    }
}
