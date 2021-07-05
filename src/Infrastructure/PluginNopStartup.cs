using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Admin.ScheduleTaskLog.Filters;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Infrastructure
{
    public class PluginNopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new ViewLocationExpander());
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(LogScheduleTaskActionFilter));
            });
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order => 11;
    }
}