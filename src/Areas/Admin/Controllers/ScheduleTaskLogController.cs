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

        public virtual IActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageSystemLog))
            {
                return AccessDeniedView();
            }

            var model = new ScheduleLogSearchModel();
            var tasks = _scheduleTaskEventService.GetAvailableTasks();
            var states = _scheduleTaskEventService.GetAvailableStates();
            var triggerTypes = _scheduleTaskEventService.GetAvailableTriggerTypes();

            tasks.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            states.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            triggerTypes.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });

            model.AvailableScheduleTasks = tasks;
            model.AvailableStates = states;
            model.AvailableTriggerTypes = triggerTypes;
            model.SetGridPageSize();

            return View("~/Plugins/Admin.ScheduleTaskLog/Areas/Admin/Views/ScheduleTaskLog/List.cshtml", model);
        }

        [HttpPost]
        public virtual IActionResult LogList(ScheduleLogSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageSystemLog))
            {
                return AccessDeniedView();
            }

            //prepare model
            var model = _scheduleTaskEventService.PrepareLogListModel(searchModel);

            return Json(model);
        }

        public virtual IActionResult View(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageSystemLog))
            {
                return AccessDeniedView();
            }

            var model = _scheduleTaskEventService.GetScheduleTaskEventById(id);
            if (model is null)
            {
                return RedirectToAction("List");
            }

            return View("~/Plugins/Admin.ScheduleTaskLog/Areas/Admin/Views/ScheduleTaskLog/View.cshtml", model);
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("clearall")]
        public virtual IActionResult ClearAll()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageSystemLog))
            {
                return AccessDeniedView();
            }

            _scheduleTaskEventService.ClearLog();

            //activity log
            _customerActivityService.InsertActivity("DeleteScheduleTaskLog", _localizationService.GetResource("Plugins.Admin.ScheduleTaskLog.ActivityLog.DeleteScheduleTaskLog"));

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.System.Log.Cleared"));

            return RedirectToAction("List");
        }

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
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
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
            {
                return AccessDeniedView();
            }

            if (!ModelState.IsValid)
            {
                _notificationService.ErrorNotification( _localizationService.GetResource("Plugins.Admin.ScheduleTaskLog.Configuration.CouldNotBeSaved"));
                return Configure();
            }

            //save settings
            _settings.DisableLog = model.DisableLog;
            _settings.LogExpiryDays = model.LogExpiryDays;
            _settingService.SaveSetting(_settings);

            _notificationService.SuccessNotification( _localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}
