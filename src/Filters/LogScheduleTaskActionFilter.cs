using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Nop.Plugin.Admin.ScheduleTaskLog.Settings;
using AdminController = Nop.Web.Areas.Admin.Controllers;
using RootController = Nop.Web.Controllers;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Filters
{
    /// <summary>
    /// Filter that intercepts calls to the task execution actions, and redirects to the plugin-specific actions that includes the extra logging
    /// </summary>
    public class LogScheduleTaskActionFilter : IActionFilter
    {
        private readonly ScheduleTaskLogSettings _settings;
        private const string CONTROLLER_NAME = "TaskRunner";

        /// <summary>
        /// Creates an instance of <see cref="LogScheduleTaskActionFilter"/>
        /// </summary>
        /// <param name="settings"></param>
        public LogScheduleTaskActionFilter(ScheduleTaskLogSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Detects of the action is for a task execution (either scheduled or manual) and redirects to the plugin controller actions
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnActionExecuting(ActionExecutingContext context)
        {
            if (_settings.DisableLog)
            {
                return;
            }

            if (context.Controller is RootController.ScheduleTaskController)
            {
                var actionName = ((ControllerActionDescriptor)context.ActionDescriptor).ActionName;
                if (actionName.Equals("runtask", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                    {
                        controller = CONTROLLER_NAME,
                        action = "RunTask",
                        taskType = context.ActionArguments["taskType"],
                        area = string.Empty
                    }));
                }
            }
            else if (context.Controller is AdminController.ScheduleTaskController)
            {
                var actionName = ((ControllerActionDescriptor)context.ActionDescriptor).ActionName;
                if (actionName.Equals("runnow", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                    {
                        controller = CONTROLLER_NAME,
                        action = "RunNow",
                        id = context.ActionArguments["id"],
                        area = "Admin"
                    }));
                }
            }
        }

        /// <inheritdoc/>
        public virtual void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
