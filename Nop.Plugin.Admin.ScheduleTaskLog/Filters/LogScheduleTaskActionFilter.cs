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
    public class LogScheduleTaskActionFilter : IActionFilter
    {
        private readonly ScheduleTaskLogSettings _settings;
        private const string CONTROLLER_NAME = "TaskRunner";

        public LogScheduleTaskActionFilter(ScheduleTaskLogSettings settings)
        {
            _settings = settings;
        }

        public void OnActionExecuting(ActionExecutingContext context)
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

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
