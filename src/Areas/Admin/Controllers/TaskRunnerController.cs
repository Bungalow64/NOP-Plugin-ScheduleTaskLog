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
        public virtual async Task<IActionResult> RunNow(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageScheduleTasks))
            {
                return AccessDeniedView();
            }

            ScheduleTaskEvent scheduleTaskEvent = null;
            try
            {
                //try to get a schedule task with the specified id
                var scheduleTask = await _scheduleTaskService.GetTaskByIdAsync(id)
                                   ?? throw new ArgumentException("Schedule task cannot be loaded", nameof(id));

                scheduleTaskEvent = await _scheduleTaskEventService.RecordEventStartAsync(scheduleTask, (await _workContext.GetCurrentCustomerAsync()).Id);

                //ensure that the task is enabled
                var task = new Task(scheduleTask) { Enabled = true };
                await task.ExecuteAsync(true, false);

                await _scheduleTaskEventService.RecordEventEndAsync(scheduleTaskEvent);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.System.ScheduleTasks.RunNow.Done"));
            }
            catch (Exception exc)
            {
                if (scheduleTaskEvent is not null)
                {
                    await _scheduleTaskEventService.RecordEventErrorAsync(scheduleTaskEvent, exc);
                }
                await _notificationService.ErrorNotificationAsync(exc);
            }

            return RedirectToAction("List", "ScheduleTask", new
            {
                area = "Admin"
            });
        }

        public virtual async Task<IActionResult> RunTask(string taskType)
        {
            var scheduleTask = await _scheduleTaskService.GetTaskByTypeAsync(taskType);
            if (scheduleTask is null)
            {
                //schedule task cannot be loaded
                return NoContent();
            }

            var scheduleTaskEvent = await _scheduleTaskEventService.RecordEventStartAsync(scheduleTask);

            var task = new Task(scheduleTask);
            try
            {
                await task.ExecuteAsync(true);

                await _scheduleTaskEventService.RecordEventEndAsync(scheduleTaskEvent);
            }
            catch (Exception exc)
            {
                await _scheduleTaskEventService.RecordEventErrorAsync(scheduleTaskEvent, exc);
            }

            return NoContent();
        }

        #endregion
    }
}
