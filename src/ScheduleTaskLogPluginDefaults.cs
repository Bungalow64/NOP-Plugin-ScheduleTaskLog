namespace Nop.Plugin.Admin.ScheduleTaskLog
{
    /// <summary>
    /// Represents plugin constants
    /// </summary>
    public class ScheduleTaskLogPluginDefaults
    {
        /// <summary>
        /// The name of the widget showing the link to the schedule task log page
        /// </summary>
        public const string WIDGETS_SCHEDULE_TASK_LOG_BUTTON_NAME = "WidgetsScheduleTaskLogButton";
        /// <summary>
        /// The name of the prune task
        /// </summary>
        public const string PRUNE_TASK_NAME = "Prune schedule task log";
        /// <summary>
        /// The type of the prune task
        /// </summary>
        public const string PRUNE_TASK_TYPE = "Nop.Plugin.Admin.ScheduleTaskLog.Tasks.PruneScheduleTaskLogEventsTask";
        /// <summary>
        /// The default period that the prune task should run
        /// </summary>
        public const int DEFAULT_PRUNE_PERIOD_HOURS = 24;
    }
}