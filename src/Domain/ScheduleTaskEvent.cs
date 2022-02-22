using Nop.Core;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Plugin.Admin.ScheduleTaskLog.Helpers;
using System;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Domain
{
    /// <summary>
    /// Represents an event relating to a schedule task
    /// </summary>
    public partial class ScheduleTaskEvent : BaseEntity
    {
        /// <summary>
        /// The name of the task
        /// </summary>
        public int ScheduleTaskId { get; set; }

        /// <summary>
        /// The date/time when the event started, in UTC
        /// </summary>
        public DateTime EventStartDateUtc { get; set; }

        /// <summary>
        /// The date/time when the event ended, in UTC
        /// </summary>
        public DateTime EventEndDateUtc { get; set; }

        /// <summary>
        /// The total number of milliseconds that have elapsed
        /// </summary>
        public long TotalMilliseconds { get; set; }

        /// <summary>
        /// Whether the task failed
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// If the task failed, this is the first 200 characters of the message for the top exception
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// If the task failed, this is the full exception details
        /// </summary>
        public string ExceptionDetails { get; set; }

        /// <summary>
        /// Whether the task was started manually (instead of being called via the scheduler)
        /// </summary>
        public bool IsStartedManually { get; set; }

        /// <summary>
        /// The id of the customer who triggered the event, if the event was triggered by a customer
        /// </summary>
        public int? TriggeredByCustomerId { get; set; }

        /// <summary>
        /// Starts the event
        /// </summary>
        /// <param name="currentDateTimeHelper">Helper to get the current date/time</param>
        /// <param name="scheduleTask">The task being executed</param>
        /// <returns>Returns the started event</returns>
        public static ScheduleTaskEvent Start(ICurrentDateTimeHelper currentDateTimeHelper, ScheduleTask scheduleTask)
        {
            if (currentDateTimeHelper is null)
            {
                throw new ArgumentNullException(nameof(currentDateTimeHelper));
            }

            if (scheduleTask is null)
            {
                throw new ArgumentNullException(nameof(scheduleTask));
            }

            return new()
            {
                ScheduleTaskId = scheduleTask.Id,
                EventStartDateUtc = currentDateTimeHelper.UtcNow
            };
        }

        /// <summary>
        /// Marks the event as being triggered manually
        /// </summary>
        /// <param name="customerId">The id of the customer who triggered the task</param>
        /// <returns>Returns the updated event</returns>
        public ScheduleTaskEvent SetTriggeredManually(int customerId)
        {
            IsStartedManually = true;
            TriggeredByCustomerId = customerId;
            return this;
        }

        /// <summary>
        /// Ends the event
        /// </summary>
        /// <param name="currentDateTimeHelper">Helper to get the current date/time</param>
        /// <returns>Returns the updated event</returns>
        public ScheduleTaskEvent End(ICurrentDateTimeHelper currentDateTimeHelper)
        {
            if (currentDateTimeHelper is null)
            {
                throw new ArgumentNullException(nameof(currentDateTimeHelper));
            }

            EventEndDateUtc = currentDateTimeHelper.UtcNow;
            TotalMilliseconds = Convert.ToInt64((EventEndDateUtc - EventStartDateUtc).TotalMilliseconds);
            return this;
        }

        /// <summary>
        /// Marks the event as an error
        /// </summary>
        /// <param name="currentDateTimeHelper">Helper to get the current date/time</param>
        /// <param name="ex">The caught exception</param>
        /// <returns>Returns the updated event</returns>
        /// <remarks>If a null exception is passed in, then the event is still marked as errored, but no exception details are stored</remarks>
        public ScheduleTaskEvent Error(ICurrentDateTimeHelper currentDateTimeHelper, Exception ex)
        {
            if (currentDateTimeHelper is null)
            {
                throw new ArgumentNullException(nameof(currentDateTimeHelper));
            }

            if (ex is not null)
            {
                ExceptionMessage = Truncate(ex.Message, 200);
                ExceptionDetails = ex.ToString();
            }

            IsError = true;

            return End(currentDateTimeHelper);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (value is null)
            {
                return string.Empty;
            }

            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }
    }
}
