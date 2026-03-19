using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PublishTools.tools
{
    public static class OpenSaveWindow
    {
        public static string? OpenFileDialog(string filter, int? defaultFilterIndex = null)
        {
            string? file_name = null;
            Application.Current.Dispatcher?.Invoke(new Action(() =>
            {
                var d = new OpenFileDialog();
                d.Filter = filter;
                d.CustomPlaces.Clear();
                if (defaultFilterIndex.HasValue && defaultFilterIndex.Value >= 1)
                {
                    d.FilterIndex = defaultFilterIndex.Value;
                }
                if (d.ShowDialog() != true)
                    file_name = null;
                file_name = d.FileName;
            }));
            return file_name;
        }

        public static string[] OpenMultiFile(string filter, int? defaultFilterIndex = null)
        {
            string[] files = null;

            Application.Current.Dispatcher?.Invoke(new Action(() =>
            {
                var multiFileSelect = new OpenFileDialog
                {
                    Multiselect = true,
                    Filter = filter,
                    CustomPlaces = { }
                };
                if (defaultFilterIndex.HasValue && defaultFilterIndex.Value >= 1)
                {
                    multiFileSelect.FilterIndex = defaultFilterIndex.Value;
                }
                bool? result = multiFileSelect.ShowDialog();
                if (result == true)
                {
                    files = multiFileSelect.FileNames;
                }
            }));
            return files;
        }
        public static int SaveFileDialog(string filter, out string path, string file_name = "test")
        {
            var d = new SaveFileDialog();
            d.Filter = filter;
            d.FileName = file_name;
            if (d.ShowDialog() == true)
            {
                path = d.FileName;
                return d.FilterIndex - 1;//This is tarting from 1. So must minus 1
            }
            else
            {
                path = "";
                return -1;
            }
        }
        //public static string OpenFolderDialog()
        //{
        //    var dialog = new System.Windows.Forms.FolderBrowserDialog
        //    {
        //        Description = "选择目录",
        //        ShowNewFolderButton = true
        //    };
        //    var result = dialog.ShowDialog();
        //    string folderName;
        //    if (result == System.Windows.Forms.DialogResult.OK)
        //    {
        //        folderName = dialog.SelectedPath;
        //        return folderName;
        //    }
        //    MessageBox.Show("请选择正确路径");
        //    return null;
        //}
    }
}
