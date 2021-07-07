using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Infrastructure
{
    /// <summary>
    /// Configures the locations of views
    /// </summary>
    public class ViewLocationExpander : IViewLocationExpander
    {
        /// <summary>
        /// Populates values
        /// </summary>
        /// <param name="context"></param>
        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }

        /// <summary>
        /// Adds view locations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="viewLocations"></param>
        /// <returns>The updated view locations</returns>
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.AreaName == "Admin")
            {
                viewLocations = new[] { $"/Plugins/Nop.Plugin.Admin.ScheduleTaskLog/Areas/Admin/Views/{context.ControllerName}/{context.ViewName}.cshtml" }.Concat(viewLocations);
            }
            else
            {
                viewLocations = new[] { $"/Plugins/Nop.Plugin.Admin.ScheduleTaskLog/Views/{context.ControllerName}/{context.ViewName}.cshtml" }.Concat(viewLocations);
            }

            return viewLocations;
        }
    }
}
