using Nop.Plugin.Admin.ScheduleTaskLog.Services;
using Nop.Services.Tasks;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Tasks
{
    /// <summary>
    /// The definition of the task that prunes the schedule task log
    /// </summary>
    public class PruneScheduleTaskLogEventsTask : IScheduleTask
    {
        private readonly IScheduleTaskEventService _scheduleTaskEventService;

        /// <summary>
        /// Creates an instance of <see cref="PruneScheduleTaskLogEventsTask"/>
        /// </summary>
        /// <param name="scheduleTaskEventService"></param>
        public PruneScheduleTaskLogEventsTask(IScheduleTaskEventService scheduleTaskEventService)
        {
            _scheduleTaskEventService = scheduleTaskEventService;
        }

        /// <summary>
        /// Executes the task
        /// </summary>
        /// <returns>The task to be awaited</returns>
        public System.Threading.Tasks.Task ExecuteAsync()
        {
            return _scheduleTaskEventService.PruneEventsAsync();
        }
    }
}
