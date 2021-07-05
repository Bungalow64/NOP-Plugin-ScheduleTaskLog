using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Components
{
    [ViewComponent(Name = ScheduleTaskLogPluginDefaults.WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME)]
    public class WidgetsScheduleTaskLogButtonComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Admin.ScheduleTaskLog/Views/LogListButton.cshtml");
        }
    }
}
