using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Tasks;
using Nop.Plugin.Admin.ScheduleTaskLog.Settings;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using Nop.Web.Framework.Infrastructure;
using Task = System.Threading.Tasks.Task;

namespace Nop.Plugin.Admin.ScheduleTaskLog
{
    /// <summary>
    /// Plugin class handling install/uninstall
    /// </summary>
    public class ScheduleTaskLogPlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly WidgetSettings _widgetSettings;
        private readonly ISettingService _settingService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IWebHelper _webHelper;

        public bool HideInWidgetList => true;

        #endregion

        #region Ctor

        public ScheduleTaskLogPlugin(
            ILocalizationService localizationService,
            WidgetSettings widgetSettings,
            ISettingService settingService,
            IScheduleTaskService scheduleTaskService,
            IWebHelper webHelper)
        {
            _localizationService = localizationService;
            _widgetSettings = widgetSettings;
            _settingService = settingService;
            _scheduleTaskService = scheduleTaskService;
            _webHelper = webHelper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/ScheduleTaskLog/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override void Install()
        {
            _settingService.SaveSetting(new ScheduleTaskLogSettings
            {
                DisableLog = false,
                LogExpiryDays = 14
            });

            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Admin.ScheduleTaskLog.ListTitle"] = "Schedule task log",
                ["Plugins.Admin.ScheduleTaskLog.ListViewTitle"] = "Schedule task log entry",
                ["Plugins.Admin.ScheduleTaskLog.BackToTaskList"] = "back to schedule tasks",
                ["Plugins.Admin.ScheduleTaskLog.BackToList"] = "back to schedule task log",
                ["Plugins.Admin.ScheduleTaskLog.TaskName"] = "Task",
                ["Plugins.Admin.ScheduleTaskLog.StartDate"] = "Start",
                ["Plugins.Admin.ScheduleTaskLog.EndDate"] = "End",
                ["Plugins.Admin.ScheduleTaskLog.TaskStatus"] = "Status",
                ["Plugins.Admin.ScheduleTaskLog.TotalMilliseconds"] = "Time taken (milliseconds)",
                ["Plugins.Admin.ScheduleTaskLog.ExceptionMessage"] = "Error message",
                ["Plugins.Admin.ScheduleTaskLog.ExceptionDetails"] = "Error details",
                ["Plugins.Admin.ScheduleTaskLog.TimeAgainstAverage"] = "Time against average (%)",
                ["Plugins.Admin.ScheduleTaskLog.Success"] = "Success",
                ["Plugins.Admin.ScheduleTaskLog.Error"] = "Error",
                ["Plugins.Admin.ScheduleTaskLog.ClearLog"] = "Clear log",
                ["Plugins.Admin.ScheduleTaskLog.ActivityLog.DeleteScheduleTaskLog"] = "Clear log",
                ["Plugins.Admin.ScheduleTaskLog.List.ScheduleTaskId"] = "Schedule task",
                ["Plugins.Admin.ScheduleTaskLog.List.ScheduleTaskId.Hint"] = "Select a schedule task.",
                ["Plugins.Admin.ScheduleTaskLog.List.StateId"] = "State",
                ["Plugins.Admin.ScheduleTaskLog.List.StateId.Hint"] = "Select a state.",
                ["Plugins.Admin.ScheduleTaskLog.List.TriggerTypeId"] = "Trigger type",
                ["Plugins.Admin.ScheduleTaskLog.List.TriggerTypeId.Hint"] = "Select a trigger type.",
                ["Plugins.Admin.ScheduleTaskLog.List.StartedOnFrom"] = "Started date from",
                ["Plugins.Admin.ScheduleTaskLog.List.StartedOnFrom.Hint"] = "The earliest start date for this search.",
                ["Plugins.Admin.ScheduleTaskLog.List.StartedOnTo"] = "Started date to",
                ["Plugins.Admin.ScheduleTaskLog.List.StartedOnTo.Hint"] = "The latest start date for this search.",
                ["Plugins.Admin.ScheduleTaskLog.LogListLink"] = "View logs",
                ["Plugins.Admin.ScheduleTaskLog.TriggerType"] = "Trigger type",
                ["Plugins.Admin.ScheduleTaskLog.ByScheduler"] = "By scheduler",
                ["Plugins.Admin.ScheduleTaskLog.ByUser"] = "By user",
                ["Plugins.Admin.ScheduleTaskLog.TriggeredByCustomerEmail"] = "Triggered by customer email",
                ["Plugins.Admin.ScheduleTaskLog.UnknownUser"] = "Unknown user",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.DisableLogs"] = "Pause logs",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.DisableLogs.Hint"] = "Disable collection of logs",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays"] = "Log expiry (in days)",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays.Hint"] = "How long logs should be valid for (in days), until they are eligible for pruning",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.CouldNotBeSaved"] = "The changes could not be saved",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays.MustBePositive"] = "The log expiry must be greater than 0"
            });

            if (!_widgetSettings.ActiveWidgetSystemNames.Contains(ScheduleTaskLogPluginDefaults.WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME))
            {
                _widgetSettings.ActiveWidgetSystemNames.Add(ScheduleTaskLogPluginDefaults.WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME);
                _settingService.SaveSetting(_widgetSettings);
            }

            if (_scheduleTaskService.GetTaskByType(ScheduleTaskLogPluginDefaults.PRUNE_TASK_TYPE) == null)
            {
                _scheduleTaskService.InsertTask(new ScheduleTask
                {
                    Enabled = true,
                    Seconds = ScheduleTaskLogPluginDefaults.DEFAULT_PRUNE_PERIOD_HOURS * 60 * 60,
                    Name = ScheduleTaskLogPluginDefaults.PRUNE_TASK_NAME,
                    Type = ScheduleTaskLogPluginDefaults.PRUNE_TASK_TYPE,
                });
            }

            base.Install();
        }

        public override void Update(string currentVersion, string targetVersion)
        {
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Admin.ScheduleTaskLog.ListTitle"] = "Schedule task log",
                ["Plugins.Admin.ScheduleTaskLog.ListViewTitle"] = "Schedule task log entry",
                ["Plugins.Admin.ScheduleTaskLog.BackToTaskList"] = "back to schedule tasks",
                ["Plugins.Admin.ScheduleTaskLog.BackToList"] = "back to schedule task log",
                ["Plugins.Admin.ScheduleTaskLog.TaskName"] = "Task",
                ["Plugins.Admin.ScheduleTaskLog.StartDate"] = "Start",
                ["Plugins.Admin.ScheduleTaskLog.EndDate"] = "End",
                ["Plugins.Admin.ScheduleTaskLog.TaskStatus"] = "Status",
                ["Plugins.Admin.ScheduleTaskLog.TotalMilliseconds"] = "Time taken (milliseconds)",
                ["Plugins.Admin.ScheduleTaskLog.ExceptionMessage"] = "Error message",
                ["Plugins.Admin.ScheduleTaskLog.ExceptionDetails"] = "Error details",
                ["Plugins.Admin.ScheduleTaskLog.TimeAgainstAverage"] = "Time against average (%)",
                ["Plugins.Admin.ScheduleTaskLog.Success"] = "Success",
                ["Plugins.Admin.ScheduleTaskLog.Error"] = "Error",
                ["Plugins.Admin.ScheduleTaskLog.ClearLog"] = "Clear log",
                ["Plugins.Admin.ScheduleTaskLog.ActivityLog.DeleteScheduleTaskLog"] = "Clear log",
                ["Plugins.Admin.ScheduleTaskLog.List.ScheduleTaskId"] = "Schedule task",
                ["Plugins.Admin.ScheduleTaskLog.List.ScheduleTaskId.Hint"] = "Select a schedule task.",
                ["Plugins.Admin.ScheduleTaskLog.List.StateId"] = "State",
                ["Plugins.Admin.ScheduleTaskLog.List.StateId.Hint"] = "Select a state.",
                ["Plugins.Admin.ScheduleTaskLog.List.TriggerTypeId"] = "Trigger type",
                ["Plugins.Admin.ScheduleTaskLog.List.TriggerTypeId.Hint"] = "Select a trigger type.",
                ["Plugins.Admin.ScheduleTaskLog.List.StartedOnFrom"] = "Started date from",
                ["Plugins.Admin.ScheduleTaskLog.List.StartedOnFrom.Hint"] = "The earliest start date for this search.",
                ["Plugins.Admin.ScheduleTaskLog.List.StartedOnTo"] = "Started date to",
                ["Plugins.Admin.ScheduleTaskLog.List.StartedOnTo.Hint"] = "The latest start date for this search.",
                ["Plugins.Admin.ScheduleTaskLog.LogListLink"] = "View logs",
                ["Plugins.Admin.ScheduleTaskLog.TriggerType"] = "Trigger type",
                ["Plugins.Admin.ScheduleTaskLog.ByScheduler"] = "By scheduler",
                ["Plugins.Admin.ScheduleTaskLog.ByUser"] = "By user",
                ["Plugins.Admin.ScheduleTaskLog.TriggeredByCustomerEmail"] = "Triggered by customer email",
                ["Plugins.Admin.ScheduleTaskLog.UnknownUser"] = "Unknown user",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.DisableLogs"] = "Pause logs",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.DisableLogs.Hint"] = "Disable collection of logs",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays"] = "Log expiry (in days)",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays.Hint"] = "How long logs should be valid for (in days), until they are eligible for pruning",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.CouldNotBeSaved"] = "The changes could not be saved",
                ["Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays.MustBePositive"] = "The log expiry must be greater than 0"
            });

            base.Update(currentVersion, targetVersion);
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override void Uninstall()
        {
            if (_widgetSettings.ActiveWidgetSystemNames.Contains(ScheduleTaskLogPluginDefaults.WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME))
            {
                _widgetSettings.ActiveWidgetSystemNames.Remove(ScheduleTaskLogPluginDefaults.WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME);
                _settingService.SaveSetting(_widgetSettings);
            }

            var task = _scheduleTaskService.GetTaskByType(ScheduleTaskLogPluginDefaults.PRUNE_TASK_TYPE);
            if (!(task is null))
            {
                _scheduleTaskService.DeleteTask(task);
            }

            _settingService.DeleteSetting<ScheduleTaskLogSettings>();

            _localizationService.DeletePluginLocaleResources("Plugins.Admin.ScheduleTaskLog");

            base.Uninstall();
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string> { AdminWidgetZones.ScheduleTaskListButtons };
        }

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return ScheduleTaskLogPluginDefaults.WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME;
        }

        #endregion
    }
}
