using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;
using Nop.Plugin.Admin.ScheduleTaskLog.Services;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.ScheduleTasks;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Controllers
{
    /// <summary>
    /// The controller that executes tasks, including logging the events in the schedule task log
    /// </summary>
    public class TaskRunnerController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IScheduleTaskEventService _scheduleTaskEventService;
        private readonly IScheduleTaskRunner _taskRunner;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates an instance of the <see cref="TaskRunnerController"/>
        /// </summary>
        /// <param name="localizationService"></param>
        /// <param name="notificationService"></param>
        /// <param name="permissionService"></param>
        /// <param name="scheduleTaskService"></param>
        /// <param name="scheduleTaskEventService"></param>
        /// <param name="taskRunner"></param>
        /// <param name="workContext"></param>
        public TaskRunnerController(
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            IScheduleTaskService scheduleTaskService,
            IScheduleTaskEventService scheduleTaskEventService,
            IScheduleTaskRunner taskRunner,
            IWorkContext workContext)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _scheduleTaskService = scheduleTaskService;
            _scheduleTaskEventService = scheduleTaskEventService;
            _taskRunner = taskRunner;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes a specific task immediately
        /// </summary>
        /// <param name="id">The id of the task</param>
        /// <returns>The list page of schedule tasks</returns>
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
                var scheduleTask = await _scheduleTaskService.GetTaskByIdAsync(id)
                                   ?? throw new ArgumentException("Schedule task cannot be loaded", nameof(id));

                scheduleTaskEvent = await _scheduleTaskEventService.RecordEventStartAsync(scheduleTask, (await _workContext.GetCurrentCustomerAsync()).Id);

                await _taskRunner.ExecuteAsync(scheduleTask, true, true, false);

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

        /// <summary>
        /// Executes a task as part of a schedule
        /// </summary>
        /// <param name="taskType">The type of the task to execute</param>
        /// <returns>Nothing</returns>
        public virtual async Task<IActionResult> RunTask(string taskType)
        {
            var scheduleTask = await _scheduleTaskService.GetTaskByTypeAsync(taskType);
            if (scheduleTask is null)
            {
                return NoContent();
            }

            var scheduleTaskEvent = await _scheduleTaskEventService.RecordEventStartAsync(scheduleTask);

            try
            {
                await _taskRunner.ExecuteAsync(scheduleTask, true, true);

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
