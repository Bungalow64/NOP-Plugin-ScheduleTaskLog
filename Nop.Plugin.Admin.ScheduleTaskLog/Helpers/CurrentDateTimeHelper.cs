using System;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Helpers
{
    /// <summary>
    /// Provides the current date/time based on DateTime.UtcNow
    /// </summary>
    public class CurrentDateTimeHelper : ICurrentDateTimeHelper
    {
        /// <inheritdoc/>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
