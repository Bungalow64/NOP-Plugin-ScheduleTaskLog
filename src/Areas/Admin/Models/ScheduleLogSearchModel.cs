using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models
{
    /// <summary>
    /// The details of a search for schedule log entries
    /// </summary>
    public partial record ScheduleLogSearchModel : BaseSearchModel
    {
        /// <summary>
        /// The earliest start date
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.StartedOnFrom")]
        [UIHint("DateNullable")]
        public DateTime? StartedOnFrom { get; set; }

        /// <summary>
        /// The latest start date
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.StartedOnTo")]
        [UIHint("DateNullable")]
        public DateTime? StartedOnTo { get; set; }

        /// <summary>
        /// The id of the schedule task that was executed
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.ScheduleTaskId")]
        public int ScheduleTaskId { get; set; }

        /// <summary>
        /// The completion state of the task execution
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.StateId")]
        public int StateId { get; set; }

        /// <summary>
        /// The trigger type of the task execution
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.TriggerTypeId")]
        public int TriggerTypeId { get; set; }

        /// <summary>
        /// The list of schedule tasks
        /// </summary>
        public IList<SelectListItem> AvailableScheduleTasks { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// The list of states
        /// </summary>
        public IList<SelectListItem> AvailableStates { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// The list of trigger types
        /// </summary>
        public IList<SelectListItem> AvailableTriggerTypes { get; set; } = new List<SelectListItem>();
    }
}
