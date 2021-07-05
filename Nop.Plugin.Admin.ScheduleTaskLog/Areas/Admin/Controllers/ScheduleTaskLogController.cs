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

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSystemLog))
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

        [HttpPost]
        public virtual async Task<IActionResult> LogList(ScheduleLogSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSystemLog))
            {
                return AccessDeniedView();
            }

            //prepare model
            var model = await _scheduleTaskEventService.PrepareLogListModelAsync(searchModel);

            return Json(model);
        }

        public virtual async Task<IActionResult> View(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSystemLog))
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

        [HttpPost, ActionName("List")]
        [FormValueRequired("clearall")]
        public virtual async Task<IActionResult> ClearAll()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSystemLog))
            {
                return AccessDeniedView();
            }

            await _scheduleTaskEventService.ClearLogAsync();

            //activity log
            await _customerActivityService.InsertActivityAsync("DeleteScheduleTaskLog", await _localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.ActivityLog.DeleteScheduleTaskLog"));

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.System.Log.Cleared"));

            return RedirectToAction("List");
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
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

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
            {
                return AccessDeniedView();
            }

            if (!ModelState.IsValid)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Admin.ScheduleTaskLog.Configuration.CouldNotBeSaved"));
                return await Configure();
            }

            //save settings
            _settings.DisableLog = model.DisableLog;
            _settings.LogExpiryDays = model.LogExpiryDays;
            await _settingService.SaveSettingAsync(_settings);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}
