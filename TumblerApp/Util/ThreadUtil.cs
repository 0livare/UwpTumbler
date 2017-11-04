using System;
using Windows.ApplicationModel.Core;
using Windows.System.Threading;
using Windows.UI.Core;

namespace TumblerApp.Util
{
    public static class ThreadUtil
    {
        public static ThreadPoolTimer SetTimeoutUI(
            DispatchedHandler callback, double timeoutMillis)
        {
            return ThreadPoolTimer.CreateTimer((t) =>
            {
                RunAsyncUI(callback.Invoke);
            }, TimeSpan.FromMilliseconds(timeoutMillis));
        }

        public static async void RunAsyncUI(
            DispatchedHandler callback, 
            CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(priority, callback);
        }
    }
}