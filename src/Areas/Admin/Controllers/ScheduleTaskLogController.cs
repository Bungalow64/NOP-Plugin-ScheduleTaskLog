using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
using Nop.Plugin.Admin.ScheduleTaskLog.Services;
using Nop.Plugin.Admin.ScheduleTaskLog.Settings;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller for handling actions to configure and view the schedule task log
    /// </summary>
    public class ScheduleTaskLogController : BaseAdminController
    {
        #region Fields

        private readonly IPermissionService _permissionService;
        private readonly IScheduleTaskEventService _scheduleTaskEventService;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ISettingService _settingService;
        private readonly ScheduleTaskLogSettings _settings;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates an instance of the <see cref="ScheduleTaskController"/>
        /// </summary>
        /// <param name="permissionService"></param>
        /// <param name="scheduleTaskEventService"></param>
        /// <param name="notificationService"></param>
        /// <param name="localizationService"></param>
        /// <param name="customerActivityService"></param>
        /// <param name="settingService"></param>
        /// <param name="settings"></param>
        public ScheduleTaskLogController(
            IPermissionService permissionService,
            IScheduleTaskEventService scheduleTaskEventService,
            INotificationService notificationService,
            ILocalizationService localizationService,
            ICustomerActivityService customerActivityService,
            ISettingService settingService,
            ScheduleTaskLogSettings settings)
        {
            _permissionService = permissionService;
            _scheduleTaskEventService = scheduleTaskEventService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _customerActivityService = customerActivityService;
            _settingService = settingService;
            _settings = settings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Redirects to the list page
        /// </summary>
        /// <returns>Returns the redirection</returns>
        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// The base page for the list of schedule task logs
        /// </summary>
        /// <returns>The page</returns>
        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            {
                return AccessDeniedView();
            }

            var model = new ScheduleLogSearchModel();
            var tasks = await _scheduleTaskEventService.GetAvailableTasksAsync();
            var states = await _scheduleTaskEventService.GetAvailableStatesAsync();
            var triggerTypes = await _scheduleTaskEventService.GetAvailableTriggerTypesAsync();

            tasks.Insert(0, new SelectListItem { Text = await _localizationService.GetResourceAsync("Admin.Common.All"), Value = "0" });
            states.Insert(0, new SelectListItem { Text = await _localizationService.GetResourceAsync("Admin.Common.All"), Value = "0" });
            triggerTypes.Insert(0, new SelectListItem { Text = await _localizationService.GetResourceAsync("Admin.Common.All"), Value = "0" });

            model.AvailableScheduleTasks = tasks;
            model.AvailableStates = states;
            model.AvailableTriggerTypes = triggerTypes;
            model.SetGridPageSize();

            return View("~/Plugins/Admin.ScheduleTaskLog/Areas/Admin/Views/ScheduleTaskLog/List.cshtml", model);
        }

        /// <summary>
        /// Requests a page of logs
        /// </summary>
        /// <param name="searchModel">The search details</param>
        /// <returns>The list of log entries</returns>
        [HttpPost]
        public virtual async Task<IActionResult> LogList(ScheduleLogSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            {
                return AccessDeniedView();
            }

            var model = await _scheduleTaskEventService.PrepareLogListModelAsync(searchModel);

            return Json(model);
        }

        /// <summary>
        /// The page to view the specific details of an event
        /// </summary>
        /// <param name="id">The id of the log entry</param>
        /// <remarks>If the <paramref name="id"/> is not found then this action will redirect to the list page</remarks>
        /// <returns>The page containing the details of the requested event, or a redirection to the list page if the <paramref name="id"/> is not found</returns>
        public virtual async Task<IActionResult> View(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            {
                return AccessDeniedView();
            }

            var model = await _scheduleTaskEventService.GetScheduleTaskEventByIdAsync(id);
            if (model is null)
            {
                return RedirectToAction("List");
            }

            return View("~/Plugins/Admin.ScheduleTaskLog/Areas/Admin/Views/ScheduleTaskLog/View.cshtml", model);
        }

        /// <summary>
        /// Clears all entries in the schedule task log
        /// </summary>
        /// <returns>Returns the base list page</returns>
        [HttpPost, ActionName("List")]
        [FormValueRequired("clearall")]
        public virtual async Task<IActionResult> ClearAll()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            {
                return AccessDeniedView();
            }

            await _scheduleTaskEventService.ClearLogAsync();

            //activity log
            await _customerActivityService.InsertActivityAsync("DeleteScheduleTaskLog", await _localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.ActivityLog.DeleteScheduleTaskLog"));

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.System.Log.Cleared"));

            return RedirectToAction("List");
        }

        /// <summary>
        /// Gets the page showing the configuration options for the plugin
        /// </summary>
        /// <returns>Returns the configuration page</returns>
        public virtual async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            {
                return AccessDeniedView();
            }

            var model = new ConfigurationModel
            {
                DisableLog = _settings.DisableLog,
                LogExpiryDays = _settings.LogExpiryDays
            };

            return View("~/Plugins/Admin.ScheduleTaskLog/Areas/Admin/Views/Configure.cshtml", model);
        }

        /// <summary>
        /// Updates the configuration settings for the plugin
        /// </summary>
        /// <param name="model">The updated settings</param>
        /// <returns>Returns the configuration page</returns>
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            {
                return AccessDeniedView();
            }

            if (!ModelState.IsValid)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.Configuration.CouldNotBeSaved"));
                return await Configure();
            }

            _settings.DisableLog = model.DisableLog;
            _settings.LogExpiryDays = model.LogExpiryDays;
            await _settingService.SaveSettingAsync(_settings);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}
