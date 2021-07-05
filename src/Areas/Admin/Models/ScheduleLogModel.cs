using System;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models
{
    public partial record ScheduleLogModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TaskName")]
        public string TaskName { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.StartDate")]
        public DateTime EventStartDateUtc { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.EndDate")]
        public DateTime? EventEndDateUtc { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TaskStatus")]
        public bool IsError { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.ExceptionMessage")]
        public string ExceptionMessage { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.ExceptionDetails")]
        public string ExceptionDetails { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TimeTaken")]
        public long? TotalMilliseconds { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TimeAgainstAverage")]
        public double? TimeAgainstAverage { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TriggerType")]
        public bool IsStartedManually { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.TriggeredByCustomerEmail")]
        public string TriggeredByCustomerEmail { get; set; }
    }
}
