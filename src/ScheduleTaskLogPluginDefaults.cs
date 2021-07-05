namespace Nop.Plugin.Admin.ScheduleTaskLog
{
    /// <summary>
    /// Represents plugin constants
    /// </summary>
    public class ScheduleTaskLogPluginDefaults
    {
        public const string WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME = "WidgetsScheduleTaskLogButton";
        public const string PRUNE_TASK_NAME = "Prune schedule task log";
        public const string PRUNE_TASK_TYPE = "Nop.Plugin.Admin.ScheduleTaskLog.Tasks.PruneScheduleTaskLogEventsTask";
        public const int DEFAULT_PRUNE_PERIOD_HOURS = 24;
    }
}