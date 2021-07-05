using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models
{
    public partial record ScheduleLogSearchModel : BaseSearchModel
    {
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.StartedOnFrom")]
        [UIHint("DateNullable")]
        public DateTime? StartedOnFrom { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.StartedOnTo")]
        [UIHint("DateNullable")]
        public DateTime? StartedOnTo { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.ScheduleTaskId")]
        public int ScheduleTaskId { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.StateId")]
        public int StateId { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.List.TriggerTypeId")]
        public int TriggerTypeId { get; set; }

        public IList<SelectListItem> AvailableScheduleTasks { get; set; } = new List<SelectListItem>();

        public IList<SelectListItem> AvailableStates { get; set; } = new List<SelectListItem>();

        public IList<SelectListItem> AvailableTriggerTypes { get; set; } = new List<SelectListItem>();
    }
}
