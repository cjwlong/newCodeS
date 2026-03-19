using NLog;
using OperationLogManager.libs;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SharedResource.tools
{
    public static class MessageWindow
    {
        public static IDialogService dialogService;
        public static void ShowDialog(DialogParameters paras, Action<IDialogResult>? callback = null)
        {
            if (callback == null)
                callback = r => { };
            Application.Current.Dispatcher?.Invoke(() =>
            {
                dialogService.ShowDialog("MessageBox",
                    paras,
                    callback);
            });
        }
        public static void ShowDialog(string message)
        {
            Application.Current.Dispatcher?.Invoke(() =>
            {
                dialogService.ShowDialog("MessageBox",
                    new DialogParameters($"message={message}"),
                    r => { });
            });
        }

        /// <summary>
        /// 确认窗口
        /// </summary>
        /// <param name="message">信息</param>
        /// <param name="callback">回调函数</param>
        /// <returns>是否确认，null为点叉号</returns>
        public static bool? ConfirmWindow(string message, Action<IDialogResult>? callback = null)
        {
            bool? flag = null;
            Application.Current.Dispatcher?.Invoke(new Action(() =>
            {
                dialogService.ShowDialog("ConfirmBox",
                new DialogParameters($"message={message}"), r =>
                {
                    if (r.Result == ButtonResult.Cancel)
                        flag = false;
                    else if (r.Result == ButtonResult.OK)
                        flag = true;
                    callback?.Invoke(r);
                });
            }));
            return flag;
        }
        public static void Show(DialogParameters paras, Action<IDialogResult>? callback = null)
        {
            if (callback == null)
                callback = r => { };
            Application.Current.Dispatcher?.BeginInvoke(() =>
            {
                dialogService.ShowDialog("MessageBox",
                    paras,
                    callback);
            });
        }

        /// <summary>
        /// 不阻塞当前线程阻塞UI线程
        /// </summary>
        /// <param name="message">信息</param>
        /// <param name="logLevel">警告等级</param>
        public static void Show(string message)
        {
            Application.Current.Dispatcher?.BeginInvoke(() =>
            {
                dialogService.ShowDialog("MessageBox",
                    new DialogParameters($"message={message}"),
                    r => { });
            });
        }

        public static IDialogResult ShowDialog(string dialog, DialogParameters para)
        {
            IDialogResult result = null;
            Application.Current.Dispatcher?.Invoke(() =>
            {
                dialogService.ShowDialog(dialog, para, r => { result = r; });
            });
            return result;
        }
    }
}
