using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Components
{
    /// <summary>
    /// The widget that shows the link to the list of schedule task events
    /// </summary>
    [ViewComponent(Name = ScheduleTaskLogPluginDefaults.WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME)]
    public class WidgetsScheduleTaskLogButtonComponent : NopViewComponent
    {
        /// <summary>
        /// Invokes the widget
        /// </summary>
        /// <returns>Returns the view of the widget</returns>
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Admin.ScheduleTaskLog/Views/LogListButton.cshtml");
        }
    }
}
