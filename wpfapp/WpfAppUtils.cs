using System;
using System.Threading;
using System.Threading.Tasks;

namespace wpfapp
{
    public static class WpfAppUtils
    {
        public static Task ShowDialog()
        {
            return StartStaTask(() =>
            {
                var window = new MainWindow();
                window.ShowDialog();
            });
        }

        private static Task StartStaTask(Action action)
        {
            var completionSource = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    completionSource.SetResult(null);
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return completionSource.Task;
        }
    }
}