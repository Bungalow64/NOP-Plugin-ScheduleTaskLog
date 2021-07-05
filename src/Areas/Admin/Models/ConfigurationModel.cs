using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.Configuration.DisableLogs")]
        public bool DisableLog { get; set; }

        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays")]
        public int LogExpiryDays { get; set; }
    }
}
