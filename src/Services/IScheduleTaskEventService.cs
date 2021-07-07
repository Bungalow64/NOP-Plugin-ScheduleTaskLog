using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Tasks;
using Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models;
using Nop.Plugin.Admin.ScheduleTaskLog.Domain;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Services
{
    /// <summary>
    /// Handles all interactions with the schedule task event log
    /// </summary>
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
        /// <summary>
        /// Creates the model to be used for the list of events
        /// </summary>
        /// <param name="searchModel">The search options for the list</param>
        /// <returns>The created model</returns>
        ScheduleLogListModel PrepareLogListModel(ScheduleLogSearchModel searchModel);
        /// <summary>
        /// Gets the details for a specific event
        /// </summary>
        /// <param name="id">The id of the <see cref="ScheduleTaskEvent"/></param>
        /// <returns>The details of the event</returns>
        ScheduleLogModel GetScheduleTaskEventById(int id);
        /// <summary>
        /// Clears all logs
        /// </summary>
        void ClearLog();
        /// <summary>
        /// Gets the list of available tasks
        /// </summary>
        /// <returns>The list of available tasks</returns>
        IList<SelectListItem> GetAvailableTasks();
        /// <summary>
        /// Gets the list of available states
        /// </summary>
        /// <returns>The list of available states</returns>
        IList<SelectListItem> GetAvailableStates();
        /// <summary>
        /// Gets the list of trigger types
        /// </summary>
        /// <returns>The list of trigger types</returns>
        IList<SelectListItem> GetAvailableTriggerTypes();
        /// <summary>
        /// Prunes the expired logs, according to the plugin configuration
        /// </summary>
        void PruneEvents();
    }
}
