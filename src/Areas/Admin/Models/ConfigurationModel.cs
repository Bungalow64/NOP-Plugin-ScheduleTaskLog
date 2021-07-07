using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models
{
    /// <summary>
    /// The model containing the configuration settings for the plugin
    /// </summary>
    public partial class ConfigurationModel : BaseNopModel
    {
        /// <summary>
        /// Whether the log should be disabled
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.Configuration.DisableLogs")]
        public bool DisableLog { get; set; }

        /// <summary>
        /// How long (in days) the entries in the log should be kept, until they can be purged
        /// </summary>
        [NopResourceDisplayName("Plugins.Admin.ScheduleTaskLog.Configuration.LogExpiryDays")]
        public int LogExpiryDays { get; set; }
    }
}
