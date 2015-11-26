using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Fzuhelper.Views;

namespace Fzuhelper
{
    public sealed class BackgroundRefresh : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral _deferral = taskInstance.GetDeferral();
            await HttpRequest.GetTimetable();
            _deferral.Complete();
        }
    }
}
