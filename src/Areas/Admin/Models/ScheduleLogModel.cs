using System;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models
{
    /// <summary>
    /// The details of a specific log entry
    /// </summary>
    public partial class ScheduleLogModel : BaseNopEntityModel
    {
        /// <summary>
        /// The name of the task
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TaskName")]
        public string TaskName { get; set; }

        /// <summary>
        /// The date/time that the event started, in UTC
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.StartDate")]
        public DateTime EventStartDateUtc { get; set; }

        /// <summary>
        /// The date/time that the event ended, in UTC
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.EndDate")]
        public DateTime? EventEndDateUtc { get; set; }

        /// <summary>
        /// Whether the task failed
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TaskStatus")]
        public bool IsError { get; set; }

        /// <summary>
        /// The message of the exception, if any
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.ExceptionMessage")]
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// The details of the exception, if any
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.ExceptionDetails")]
        public string ExceptionDetails { get; set; }

        /// <summary>
        /// The total number of milliseconds that the task took to run
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TotalMilliseconds")]
        public long? TotalMilliseconds { get; set; }

        /// <summary>
        /// The time taken compared against the average time (a positive percentage represents a slower than average time)
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TimeAgainstAverage")]
        public double? TimeAgainstAverage { get; set; }

        /// <summary>
        /// Whether the task was triggered manually (instead of being called via the scheduler)
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TriggerType")]
        public bool IsStartedManually { get; set; }

        /// <summary>
        /// The email address of the customer who triggered the task, if it was triggered manually
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TriggeredByCustomerEmail")]
        public string TriggeredByCustomerEmail { get; set; }
    }
}
