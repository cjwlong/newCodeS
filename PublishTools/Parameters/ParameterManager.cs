using Microsoft.Data.Sqlite;
using Prism.Ioc;
using SharedResource.tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SharedResource.Parameters
{
    public class ParameterManager
    {
        // 应用程序数据目录（推荐：适用于存储用户数据，具有读写权限）
        private static readonly string _appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Application.Current?.Properties["AppName"] as string ?? "MyApp");

        // 配置文件目录
        private static readonly string _configDir = Path.Combine(_appDataDir, "Config");

        // 数据库文件目录
        private static readonly string _dbDir = Path.Combine(_appDataDir, "Data");

        // 默认CSV文件路径（通常用于初始数据或模板）
        private static readonly string _defaultCsvPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,  // 程序运行目录
            "Data",
            "para.csv");

        // 实际使用的数据库路径（用户数据存储位置）
        private static readonly string _paraDbPath = Path.Combine(_dbDir, "para.db");

        // 数据库连接字符串
        private static readonly string _paraConnectionString = $"Data Source={_paraDbPath}";
        static ParameterManager()
        {
            Initialize();
        }

        public static bool Test()
        {
            AddPara(new ParameterViewModel<int>() { Name = "aaa", Description = "bbbb", Type = typeof(int).Name, Value = 12 });
            return false;
        }
        public static bool SavePara<T>(ParameterViewModel<T> para)
        {
            try
            {
                if (!HasName(para.Name))
                    return AddPara(para);
                else
                    return UpdatePara(para);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static bool LoadPara<T>(ParameterViewModel<T> para)
        {
            try
            {
                var readed = GetParaByName<T>(para.Name);
                if (readed == null)
                    return false;
                else
                {
                    para.Value = readed.Value;
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static ParameterViewModel<T> LoadPara<T>([CallerMemberName] string para_name = null)
        {
            try
            {
                var readed = GetParaByName<T>(para_name);
                return readed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}");
                return null;
            }
        }

        private static bool AddPara<T>(ParameterViewModel<T> para)
        {
            try
            {
                string commandText = "INSERT INTO Para (Name, Value, Description, Type) VALUES (@Name, @Value, @Description, @Type)";

                SqliteParameter[] parameters = {
                    new("@Name", para.Name),
                    new("@Value", para.Value?.ToString()),
                    new("@Description", para.Description),
                    new("@Type", para.Type),
                };

                ExecuteNonQuery(commandText, CommandType.Text, parameters);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static ParameterViewModel<T> GetParaByID<T>(uint id)
        {
            string commandText = "SELECT * FROM Para WHERE ID = @ID";

            SqliteParameter[] parameters = {
                new SqliteParameter("@ID", id)
            };

            using (SqliteDataReader reader = ExecuteReader(commandText, CommandType.Text, parameters))
            {
                if (reader.Read())
                {
                    return new ParameterViewModel<T>(
                        Id: (int)Convert.ChangeType(reader["ID"], typeof(int)),
                        Name: reader["Name"].ToString(),
                        Value: (T)Convert.ChangeType(reader["Value"], typeof(T)),
                        Description: (string)reader["Description"]);
                }
                else
                {
                    return null;
                }
            }
        }

        private static ParameterViewModel<T> GetParaByName<T>(string name)
        {
            string commandText = "SELECT * FROM Para WHERE Name = @Name";

            SqliteParameter[] parameters = {
                new SqliteParameter("@Name", name)
            };

            // 先尝试读取本机配置
            using SqliteDataReader reader = ExecuteReader(commandText, CommandType.Text, parameters);
            if (reader.Read())
            {
                return new ParameterViewModel<T>(
                    Id: (int)Convert.ChangeType(reader["ID"], typeof(int)),
                    Name: reader["Name"].ToString(),
                    Value: (T)Convert.ChangeType(reader["Value"], typeof(T)),
                    Description: (string)reader["Description"]);
            }
            // 尝试读取默认配置
            string[] lines = File.ReadAllLines(_defaultCsvPath);
            var query = lines
                .Skip(1)                            // 跳过第一行
                .Where(x => x.Split(',')[1] == name); // 找名字相同的
            if (query.First() != null)
            {
                var data = query.First().Split(",");
                return new ParameterViewModel<T>(
                    Id: (int)Convert.ChangeType(data[0], typeof(int)),
                    Name: data[1],
                    Value: (T)Convert.ChangeType(data[2], typeof(T)),
                    Description: data[3]);
            }
            //using SqliteDataReader readerDefault = ExecuteReader(commandText, CommandType.Text, true, parameters);
            //if (readerDefault.Read())
            //{
            //    var para = new Parameter<T>(
            //        Id: (int)Convert.ChangeType(readerDefault["ID"], typeof(int)),
            //        Name: readerDefault["Name"].ToString(),
            //        Value: (T)Convert.ChangeType(readerDefault["Value"], typeof(T)),
            //        Description: (string)readerDefault["Description"]);
            //    SavePara(para); // 把独到的默认值存在当前数据库中
            //    return para;
            //}
            else
            {
                throw new Exception("默认配置中仍未发现配置项或存在错误，请检查默认配置文件\n@Developer");
            }
        }
        private static bool HasName(string name)
        {
            string commandText = "SELECT * FROM Para WHERE Name = @Name";

            SqliteParameter[] parameters = {
                new SqliteParameter("@Name", name)
            };

            using SqliteDataReader reader = ExecuteReader(commandText, CommandType.Text, parameters);
            if (reader.Read())
                return true;
            else
                return false;
        }

        private static void DeleteParameter(int id)
        {
            string commandText = "DELETE FROM Para WHERE ID = @ID";

            SqliteParameter[] parameters = {
                new("@ID", id)
            };

            ExecuteNonQuery(commandText, CommandType.Text, parameters);
        }
        private static bool UpdatePara<T>(ParameterViewModel<T> para)
        {
            try
            {
                string commandText = "UPDATE Para SET Name = @Name, Value = @Value, Description = @Description, Type = @Type WHERE ID = @ID";

                SqliteParameter[] parameters = {
                    new("@ID", para.Id),
                    new("@Name", para.Name),
                    new("@Value", para.Value?.ToString()),
                    new("@Description", para.Description),
                    new("@Type", para.Type),
                };

                ExecuteNonQuery(commandText, CommandType.Text, parameters);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        #region 数据库操作


        // 初始化连接字符串
        public static void Initialize()
        {
            // 确保数据库文件路径存在
            var directory = Path.GetDirectoryName(_paraDbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (!File.Exists(_paraDbPath))
            {
                File.Copy("Data/para.db", _paraDbPath, true); // 复制默认配置
            }
            using (var connection = new SqliteConnection($"Data Source={_paraDbPath}"))
            {
                connection.Open();

                var scanningSettingCommand = connection.CreateCommand();
                scanningSettingCommand.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS ScanningSetting (
                        ScanningSettingId TEXT PRIMARY KEY,
                        UserId TEXT NOT NULL,
                        LaserMode TINYINT NOT NULL,
                        ScanerLimitX DOUBLE,
                        ScanerLimitY DOUBLE,
                        ScanerLimitZ DOUBLE,
                        ScanerOffsetX DOUBLE,
                        ScanerOffsetY DOUBLE,
                        ScanerOffsetZ DOUBLE,
                        ScanerOffsetA DOUBLE,
                        ScanerOffsetB DOUBLE,
                        ScanerOffsetC DOUBLE
                    );
                ";
                scanningSettingCommand.ExecuteNonQuery();
                var BurinLibraryCommand = connection.CreateCommand();
                BurinLibraryCommand.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS BurinLibrary (
                        LibraryId TEXT PRIMARY KEY,
                        Id TEXT NOT NULL,
                        Name TEXT,
                        UserId TEXT NOT NULL,
                        LaserParaId TEXT NOT NULL,
                        ScannerParaId TEXT NOT NULL,
                        Sort INT
                    );
                ";
                BurinLibraryCommand.ExecuteNonQuery();
                var CreateIndexCommand = connection.CreateCommand();
                CreateIndexCommand.CommandText =
                @"
                    CREATE INDEX IF NOT EXISTS idx_UserId ON BurinLibrary (UserId);
                ";
                CreateIndexCommand.ExecuteNonQuery();
                var LaserParaCommand = connection.CreateCommand();
                LaserParaCommand.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS LaserPara (
                        LaserParaId TEXT PRIMARY KEY,
                        Power DOUBLE,
                        Frequency INT,
                        PulseLength INT,
                        Simmer INT,
                        SpotSize DOUBLE
                    );
                ";
                LaserParaCommand.ExecuteNonQuery();
                var ScannerParaCommand = connection.CreateCommand();
                ScannerParaCommand.CommandText =
@"
                    CREATE TABLE IF NOT EXISTS ScannerPara (
                        ScannerParaId TEXT PRIMARY KEY,
                        MarkSpeed DOUBLE,
                        JumpSpeed DOUBLE,
                        InitialImpulseSuppression INT,
                        DelayLaserOn INT,
                        DelayLaserOff INT,
                        DelayAfterJump INT,
                        DelayAfterMark INT,
                        DelayPolygon INT,
                        MultiSpeedScan_GroupMode TINYINT,
                        MultiSpeedScan_Direction TINYINT,
                        MultiSpeedScan_RowTimes INT,
                        MultiSpeedScan_SpanRows INT,
                        SkyWritingMode TINYINT,
                        Timelag DOUBLE,
                        LaserOnShift INT,
                        Nprev INT,
                        Npost INT,
                        CosAngle DOUBLE,
                        MarkToMiddle TINTINT
                    );
                ";
                ScannerParaCommand.ExecuteNonQuery();

            }
        }

        // 执行非查询命令（插入、更新、删除）
        public static int ExecuteNonQuery(string commandText, CommandType commandType, params SqliteParameter[] parameters)
        {
            using SqliteConnection connection = new SqliteConnection(_paraConnectionString);
            using SqliteCommand command = new SqliteCommand(commandText, connection);
            command.CommandType = commandType;
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            connection.Open();
            return command.ExecuteNonQuery();
        }

        // 执行查询并返回单个值
        public static object ExecuteScalar(string commandText, CommandType commandType, params SqliteParameter[] parameters)
        {
            using SqliteConnection connection = new SqliteConnection(_paraConnectionString);
            using SqliteCommand command = new SqliteCommand(commandText, connection);

            command.CommandType = commandType;
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            connection.Open();
            return command.ExecuteScalar();
        }

        // 执行查询并返回数据读取器
        public static SqliteDataReader ExecuteReader(string commandText, CommandType commandType, params SqliteParameter[] parameters)
        {
            SqliteConnection connection;
            connection = new SqliteConnection(_paraConnectionString);
            SqliteCommand command = new SqliteCommand(commandText, connection)
            {
                CommandType = commandType
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            connection.Open();

            // CommandBehavior.CloseConnection ensures the connection is closed when the reader is closed
            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }
        #endregion
    }
}
