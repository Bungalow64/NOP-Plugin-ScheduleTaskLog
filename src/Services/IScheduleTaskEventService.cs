using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Tasks;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Services
{
    public interface IScheduleTaskEventService
    {
        /// <summary>
        /// Starts the event representing the schedule task execution.
        /// </summary>
        /// <param name="scheduleTask">The task being executed</param>
        /// <param name="customerId">The id of the customer triggering the task, or null if the task is being executed via the scheduler</param>
        /// <returns>The <see cref="ScheduleTaskEvent"/> object</returns>
        /// <remarks>If there is a problem starting this event, the returned object will be null</remarks>
        ScheduleTaskEvent RecordEventStart(ScheduleTask scheduleTask, int? customerId = null);
        /// <summary>
        /// Records the end of the event
        /// </summary>
        /// <param name="scheduleTaskEvent">The started <see cref="ScheduleTaskEvent"/>.  Can be null.</param>
        /// <returns>Returns the ended <see cref="ScheduleTaskEvent"/></returns>
        /// <remarks>If <paramref name="scheduleTaskEvent"/> is null, then nothing is saved and null is returned</remarks>
        ScheduleTaskEvent RecordEventEnd(ScheduleTaskEvent scheduleTaskEvent);
        /// <summary>
        /// Records the end of the event
        /// </summary>
        /// <param name="scheduleTaskEvent">The started <see cref="ScheduleTaskEvent"/>.  Can be null.</param>
        /// <param name="exc">The exception raised</param>
        /// <returns>Returns the ended <see cref="ScheduleTaskEvent"/></returns>
        /// <remarks>If <paramref name="scheduleTaskEvent"/> is null, then nothing is saved and null is returned.  If <paramref name="exc"/> is null then the event is still recorded as an error, but with no details</remarks>
        ScheduleTaskEvent RecordEventError(ScheduleTaskEvent scheduleTaskEvent, Exception exc);
        ScheduleLogListModel PrepareLogListModel(ScheduleLogSearchModel searchModel);
        ScheduleLogModel GetScheduleTaskEventById(int id);
        void ClearLog();
        IList<SelectListItem> GetAvailableTasks();
        IList<SelectListItem> GetAvailableStates();
        IList<SelectListItem> GetAvailableTriggerTypes();
        void PruneEvents();
    }
}
