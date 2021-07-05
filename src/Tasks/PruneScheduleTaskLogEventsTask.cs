﻿using Nop.Plugin.Admin.ScheduleTaskLog.Services;
using Nop.Services.Tasks;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Tasks
{
    public class PruneScheduleTaskLogEventsTask : IScheduleTask
    {
        private readonly IScheduleTaskEventService _scheduleTaskEventService;

        public PruneScheduleTaskLogEventsTask(IScheduleTaskEventService scheduleTaskEventService)
        {
            _scheduleTaskEventService = scheduleTaskEventService;
        }

        public System.Threading.Tasks.Task ExecuteAsync()
        {
            return _scheduleTaskEventService.PruneEventsAsync();
        }
    }
}