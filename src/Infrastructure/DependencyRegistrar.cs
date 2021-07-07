using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Admin.ScheduleTaskLog.Helpers;
using Nop.Plugin.Admin.ScheduleTaskLog.Services;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Infrastructure
{
    /// <summary>
    /// Registers the plugin dependencies
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<CurrentDateTimeHelper>().As<ICurrentDateTimeHelper>().SingleInstance();
            builder.RegisterType<ScheduleTaskEventService>().As<IScheduleTaskEventService>().InstancePerLifetimeScope();
        }

        /// <summary>
        /// Order of this registration
        /// </summary>
        public int Order => 1;
    }
}
