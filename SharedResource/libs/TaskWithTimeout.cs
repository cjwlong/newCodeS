using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedResource.libs
{
    /// <summary>
    /// 有返回值，可超时，可取消的Task
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TaskWithTimeout<T>
    {
        #region 字段
        private Func<T> _func;
        private CancellationToken _token;
        private event AsyncCompletedEventHandler _asyncCompletedEvent;
        private TaskCompletionSource<AsyncCompletedEventArgs> _tcs;
        #endregion

        #region 静态方法
        public static async Task<T> StartNewTask(Func<T> func, CancellationToken token,
            int timeout = Timeout.Infinite)
        {
            var task = new TaskWithTimeout<T>(func, token, timeout);

            return await task.Run();
        }

        public static async Task<T> StartNewTask(Func<T> func, int timeout)
        {
            return await TaskWithTimeout<T>.StartNewTask(func, CancellationToken.None, timeout);
        }

        public static async Task<T> StartNewTask(Func<T> func, CancellationToken token)
        {
            return await TaskWithTimeout<T>.StartNewTask(func, token, Timeout.Infinite);
        }
        #endregion

        #region 构造
        protected TaskWithTimeout(Func<T> func, CancellationToken token) : this(func, token, Timeout.Infinite)
        {

        }

        protected TaskWithTimeout(Func<T> func, int timeout = Timeout.Infinite) : this(func, CancellationToken.None, timeout)
        {

        }

        protected TaskWithTimeout(Func<T> func, CancellationToken token, int timeout = Timeout.Infinite)
        {
            _func = func;

            _tcs = new TaskCompletionSource<AsyncCompletedEventArgs>();

            if (timeout != Timeout.Infinite)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(timeout);
                _token = cts.Token;
            }
            else
            {
                _token = token;
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 运行Task
        /// </summary>
        /// <returns></returns>
        private async Task<T> Run()
        {
            _asyncCompletedEvent += AsyncCompletedEventHandler;

            try
            {
                using (_token.Register(() => _tcs.TrySetCanceled()))
                {
                    ExecuteFunc();
                    var args = await _tcs.Task.ConfigureAwait(false);
                    return (T)args.UserState;
                }

            }
            finally
            {
                _asyncCompletedEvent -= AsyncCompletedEventHandler;
            }

        }

        /// <summary>
        /// 执行
        /// </summary>
        private void ExecuteFunc()
        {
            ThreadPool.QueueUserWorkItem(s =>
            {
                var result = _func.Invoke();

                OnAsyncCompleteEvent(result);
            });
        }

        /// <summary>
        /// 异步完成事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AsyncCompletedEventHandler(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _tcs.TrySetCanceled();
            }
            else if (e.Error != null)
            {
                _tcs.TrySetException(e.Error);
            }
            else
            {
                _tcs.TrySetResult(e);
            }
        }

        /// <summary>
        /// 触发异步完成事件
        /// </summary>
        /// <param name="userState"></param>
        private void OnAsyncCompleteEvent(object userState)
        {
            if (_asyncCompletedEvent != null)
            {
                _asyncCompletedEvent(this, new AsyncCompletedEventArgs(error: null, cancelled: false, userState: userState));
            }
        }
        #endregion
    }
}
