using Nop.Web.Framework.Models;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Areas.Admin.Models
{
    /// <summary>
    /// The model representing the list of log entries
    /// </summary>
    public partial record ScheduleLogListModel : BasePagedListModel<ScheduleLogModel>
    {
    }
}
