using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace PublishTools.Helpers
{
    public static class FileOperations
    {

        /// <summary>
        /// 递归删除文件夹，避免只读文件导致删除不了的情况
        /// </summary>
        /// <param name="dir">文件夹全路径</param>
        public static void DeleteDir(string dir)
        {
            if (Directory.Exists(dir)) //判断是否存在   
            {
                foreach (string childName in Directory.GetFileSystemEntries(dir))//获取子文件和子文件夹
                {
                    if (File.Exists(childName)) //如果是文件
                    {
                        FileInfo fi = new FileInfo(childName);
                        if (fi.IsReadOnly)
                            fi.IsReadOnly = false; //更改文件的只读属性
                        File.Delete(childName); //直接删除其中的文件    
                    }
                    else//不是文件就是文件夹
                        DeleteDir(childName); //递归删除子文件夹   
                }
                Directory.Delete(dir, true); //删除空文件夹            
            }
        }
        /// <summary>
        /// 递归移动文件和文件夹
        /// </summary>
        public static void MoveFilesAndFolders(string sourceFolder, string destFolder)
        {
            // 若源文件夹为空,直接返回
            if (Directory.GetFiles(sourceFolder).Length == 0)
            {
                return;
            }

            // 获取源目录当前下文件和子文件夹
            string[] files = Directory.GetFiles(sourceFolder);
            string[] folders = Directory.GetDirectories(sourceFolder);
            // 递归移动子文件夹
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                if (!Directory.Exists(dest))
                    // 确保目标目录存在
                    Directory.CreateDirectory(dest);
                MoveFilesAndFolders(folder, dest);
            }
            // 移动文件到目标目录
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Move(file, dest);
            }
            // 移除原文件夹
            Directory.Delete(sourceFolder, true);
        }

        public static string? ExtractFile(string zip_file, string destFolder)
        {
            try
            {
                if (Directory.Exists(destFolder))  // 存在即删除
                    //Directory.Delete(extractPath, true);
                    FileOperations.DeleteDir(destFolder);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destFolder));
            }
            catch (Exception ex)
            {
                return $"文件未正确关闭，请重启软件\n{ex.Message}";
            }
            try
            {
                using (ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(zip_file))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // 构建正确的目标路径
                        string entryPath = destFolder + @"\" + entry.FullName;
                        string entryFolder = System.IO.Path.GetDirectoryName(entryPath);
                        if (!Directory.Exists(entryFolder))
                            // 确保目标目录存在
                            Directory.CreateDirectory(entryFolder);
                        if (entryPath.EndsWith("\\") || entryPath.EndsWith("/"))
                            continue;
                        // 提取zip条目到目标路径
                        entry.ExtractToFile(entryFolder, true);
                    }
                    //if (Directory.Exists(@".\extract\extract\"))
                    //{
                    //    MoveFilesAndFolders(@".\extract\extract\", @".\extract\");
                    //}
                }
                return null;
            }
            catch (Exception ex)
            {
                return $"项目读取失败或文件已损坏\n{ex.Message}";
            }
        }
        public static string? ExtractFile(string zip_file, string destFolder, string password)
        {
            try
            {
                FastZip zip = new()
                {
                    Password = password,
                };
                zip.ExtractZip(zip_file, destFolder,
                    FastZip.Overwrite.Always, (s) => true, "", "", true);
                return null;
            }
            catch (Exception e) { return e.ToString(); }
        }  //解压文件，同时解密
        public static string? CreateFile(string destFolder, string zip_file, string password)  //压缩文件，同时加密
        {
            try
            {
                FastZip zip = new()
                {
                    Password = password,
                };
                zip.CreateZip(destFolder, zip_file, true, "", "");
                return null;
            }
            catch (Exception e) { return e.ToString(); }
        }

    }
}
