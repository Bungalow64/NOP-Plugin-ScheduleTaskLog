using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;
using Nop.Plugin.Admin.ScheduleTaskLog.Services;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Tasks;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Task = Nop.Services.Tasks.Task;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Controllers
{
    public class TaskRunnerController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IScheduleTaskEventService _scheduleTaskEventService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public TaskRunnerController(
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            IScheduleTaskService scheduleTaskService,
            IScheduleTaskEventService scheduleTaskEventService,
            IWorkContext workContext)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _scheduleTaskService = scheduleTaskService;
            _scheduleTaskEventService = scheduleTaskEventService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        [ValidateIpAddress]
        [AuthorizeAdmin]
        [ValidateVendor]
        public virtual IActionResult RunNow(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageScheduleTasks))
            {
                return AccessDeniedView();
            }

            ScheduleTaskEvent scheduleTaskEvent = null;
            try
            {
                //try to get a schedule task with the specified id
                var scheduleTask = _scheduleTaskService.GetTaskById(id)
                                   ?? throw new ArgumentException("Schedule task cannot be loaded", nameof(id));

                scheduleTaskEvent = _scheduleTaskEventService.RecordEventStart(scheduleTask, _workContext.CurrentCustomer.Id);

                //ensure that the task is enabled
                var task = new Task(scheduleTask) { Enabled = true };
                task.Execute(true, false);

                _scheduleTaskEventService.RecordEventEnd(scheduleTaskEvent);

                _notificationService.SuccessNotification(_localizationService.GetResource("Admin.System.ScheduleTasks.RunNow.Done"));
            }
            catch (Exception exc)
            {
                if (!(scheduleTaskEvent is null))
                {
                    _scheduleTaskEventService.RecordEventError(scheduleTaskEvent, exc);
                }
                _notificationService.ErrorNotification(exc);
            }

            return RedirectToAction("List", "ScheduleTask", new
            {
                area = "Admin"
            });
        }

        public virtual IActionResult RunTask(string taskType)
        {
            var scheduleTask = _scheduleTaskService.GetTaskByType(taskType);
            if (scheduleTask is null)
            {
                //schedule task cannot be loaded
                return NoContent();
            }

            var scheduleTaskEvent = _scheduleTaskEventService.RecordEventStart(scheduleTask);

            var task = new Task(scheduleTask);
            try
            {
                task.Execute(true);

                _scheduleTaskEventService.RecordEventEnd(scheduleTaskEvent);
            }
            catch (Exception exc)
            {
                _scheduleTaskEventService.RecordEventError(scheduleTaskEvent, exc);
            }

            return NoContent();
        }

        #endregion
    }
}
