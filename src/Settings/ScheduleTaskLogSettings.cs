using Nop.Core.Configuration;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Settings
{
    public class ScheduleTaskLogSettings : ISettings
    {
        /// <summary>
        /// Gets or sets whether logging should be disabled
        /// </summary>
        public virtual bool DisableLog { get; set; }

        /// <summary>
        /// Gets or sets how long logs should be valid for (in days), until they are eligible for pruning
        /// </summary>
        public virtual int LogExpiryDays { get; set; }
    }
}
